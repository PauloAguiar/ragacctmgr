using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using OtpNet;

namespace ragaccountmgr
{
    public class TotpService : INotifyPropertyChanged, IDisposable
    {
        private readonly Dictionary<string, TotpGenerator> _totpGenerators = new();
        private readonly DispatcherTimer _timer;
        private readonly DispatcherTimer _countdownTimer;
        private readonly Dispatcher _dispatcher;
        private bool _disposed = false;
        private int _timerTick;
        private DateTime? _ntpUtcNow = null;
        private DateTime _ntpLastSync = DateTime.MinValue;
        private TimeSpan _ntpSyncInterval = TimeSpan.FromMinutes(5);
        private DispatcherTimer _ntpSyncTimer;
        private TimeSpan _ntpMaxSkew = TimeSpan.FromSeconds(10); // If NTP is too old, fallback

        public TotpService()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;

            // NTP sync timer
            _ntpSyncTimer = new DispatcherTimer();
            _ntpSyncTimer.Interval = _ntpSyncInterval;
            _ntpSyncTimer.Tick += async (s, e) => await SyncNtpTimeAsync();
            _ntpSyncTimer.Start();
            // Initial NTP fetch
            _ = SyncNtpTimeAsync();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(30);
            _timer.Tick += UpdateTotpCodes;
            _timer.Start();

            _countdownTimer = new DispatcherTimer();
            _countdownTimer.Interval = TimeSpan.FromSeconds(1);
            _countdownTimer.Tick += UpdateCountdown;
            _countdownTimer.Start();
        }

        private async Task SyncNtpTimeAsync()
        {
            var ntp = await NtpClient.GetNetworkUtcTimeAsync();
            if (ntp.HasValue)
            {
                _ntpUtcNow = ntp.Value;
                _ntpLastSync = DateTime.UtcNow;
            }
        }

        private DateTime GetCurrentUtcNow()
        {
            if (_ntpUtcNow.HasValue)
            {
                var elapsed = DateTime.UtcNow - _ntpLastSync;
                if (elapsed < _ntpSyncInterval + _ntpMaxSkew)
                {
                    return _ntpUtcNow.Value + elapsed;
                }
            }
            return DateTime.UtcNow;
        }

        public void AddTotpSeed(string accountId, string seed)
        {
            if (string.IsNullOrWhiteSpace(seed))
                return;

            try
            {
                var key = Base32Encoding.ToBytes(seed);
                var totp = new Totp(key);

                var now = GetCurrentUtcNow();
                var timeStep = 30;
                var secondsSinceEpoch = (long)(now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                var currentStep = secondsSinceEpoch / timeStep;
                var nextStep = currentStep + 1;
                var nextStepTime = nextStep * timeStep;
                var remainingSeconds = (int)(nextStepTime - secondsSinceEpoch);

                _totpGenerators[accountId] = new TotpGenerator
                {
                    Totp = totp,
                    Seed = seed,
                    CurrentCode = totp.ComputeTotp(),
                    LastUpdate = now,
                    RemainingSeconds = remainingSeconds
                };

                OnPropertyChanged(nameof(GetTotpCode));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Invalid TOTP seed for account {accountId}: {ex.Message}");
            }
        }

        public void RemoveTotpSeed(string accountId)
        {
            if (_totpGenerators.ContainsKey(accountId))
            {
                _totpGenerators.Remove(accountId);
                OnPropertyChanged(nameof(GetTotpCode));
            }
        }

        public string GetTotpCode(string accountId)
        {
            if (_totpGenerators.TryGetValue(accountId, out var generator))
            {
                return generator.CurrentCode;
            }
            return string.Empty;
        }

        public int GetRemainingSeconds(string accountId)
        {
            if (_totpGenerators.TryGetValue(accountId, out var generator))
            {
                // Return the cached value that's updated every second
                return generator.RemainingSeconds;
            }
            return 0;
        }

        public int TimerTick
        {
            get => _timerTick;
            private set
            {
                if (_timerTick != value)
                {
                    _timerTick = value;
                    OnPropertyChanged(nameof(TimerTick));
                }
            }
        }

        // Method to manually trigger an update for testing
        public void ForceUpdate()
        {
            UpdateCountdown(null, EventArgs.Empty);
        }

        private void UpdateTotpCodes(object? sender, EventArgs e)
        {
            if (_disposed) return;

            var updated = false;
            var now = GetCurrentUtcNow();
            foreach (var kvp in _totpGenerators)
            {
                var generator = kvp.Value;
                var newCode = generator.Totp.ComputeTotp();

                if (newCode != generator.CurrentCode)
                {
                    generator.CurrentCode = newCode;
                    generator.LastUpdate = now;
                    updated = true;
                }
            }

            if (updated)
            {
                OnPropertyChanged(nameof(GetTotpCode));
            }
        }

        private void UpdateCountdown(object? sender, EventArgs e)
        {
            if (_disposed) return;

            var updated = false;
            var codeUpdated = false;
            var now = GetCurrentUtcNow();
            foreach (var kvp in _totpGenerators)
            {
                var generator = kvp.Value;

                var timeStep = 30;
                var secondsSinceEpoch = (long)(now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                var currentStep = secondsSinceEpoch / timeStep;
                var nextStep = currentStep + 1;
                var nextStepTime = nextStep * timeStep;
                var remainingSeconds = (int)(nextStepTime - secondsSinceEpoch);

                var newCode = generator.Totp.ComputeTotp();
                if (newCode != generator.CurrentCode || remainingSeconds == 0)
                {
                    generator.CurrentCode = newCode;
                    generator.LastUpdate = now;
                    codeUpdated = true;
                }

                if (remainingSeconds != generator.RemainingSeconds)
                {
                    generator.RemainingSeconds = remainingSeconds;
                    updated = true;
                }
            }

            TimerTick++;

            if (updated)
            {
                OnPropertyChanged(nameof(GetRemainingSeconds));
            }

            if (codeUpdated)
            {
                OnPropertyChanged(nameof(GetTotpCode));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _timer?.Stop();
                _countdownTimer?.Stop();
                _disposed = true;
            }
        }

        private class TotpGenerator
        {
            public Totp Totp { get; set; } = null!;
            public string Seed { get; set; } = string.Empty;
            public string CurrentCode { get; set; } = string.Empty;
            public DateTime LastUpdate { get; set; }
            public int RemainingSeconds { get; set; }
        }
    }
} 