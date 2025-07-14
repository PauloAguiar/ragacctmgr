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
        private readonly Timer _mainTimer;
        private readonly Dispatcher _dispatcher;
        private bool _disposed = false;
        private int _timerTick;
        private DateTime? _ntpUtcNow = null;
        private DateTime _ntpLastSync = DateTime.MinValue;
        private TimeSpan _ntpSyncInterval = TimeSpan.FromMinutes(5);
        private TimeSpan _ntpMaxSkew = TimeSpan.FromSeconds(10); // If NTP is too old, fallback

        public TotpService()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;

            // Initial NTP fetch
            _ = SyncNtpTimeAsync();

            // Use System.Threading.Timer for more reliable timing
            _mainTimer = new Timer(OnMainTimerTick, null, TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(200));
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
                var secondsSinceEpoch = (now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                var currentStepProgress = secondsSinceEpoch % timeStep;
                var remainingSeconds = Math.Max(1, (int)Math.Ceiling(timeStep - currentStepProgress));

                _totpGenerators[accountId] = new TotpGenerator
                {
                    Totp = totp,
                    Seed = seed,
                    CurrentCode = totp.ComputeTotp(now),
                    LastUpdate = now,
                    RemainingSeconds = remainingSeconds
                };

                _dispatcher.BeginInvoke(() => OnPropertyChanged(nameof(GetTotpCode)));
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
                _dispatcher.BeginInvoke(() => OnPropertyChanged(nameof(GetTotpCode)));
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
                    _dispatcher.BeginInvoke(() => OnPropertyChanged(nameof(TimerTick)));
                }
            }
        }

        // Method to manually trigger an update for testing
        public void ForceUpdate()
        {
            OnMainTimerTick(null);
        }

        private void OnMainTimerTick(object? state)
        {
            if (_disposed) return;

            // Check if NTP sync is needed
            var now = DateTime.UtcNow;
            if (now - _ntpLastSync >= _ntpSyncInterval)
            {
                _ = SyncNtpTimeAsync();
            }

            // Update TOTP codes and countdown
            UpdateTotpAndCountdown();

            // Increment timer tick
            TimerTick++;
        }

        private void UpdateTotpAndCountdown()
        {
            var countdownUpdated = false;
            var codeUpdated = false;
            var now = GetCurrentUtcNow();
            
            foreach (var kvp in _totpGenerators)
            {
                var generator = kvp.Value;

                // Calculate remaining seconds more precisely
                var timeStep = 30;
                var secondsSinceEpoch = (now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                var currentStepProgress = secondsSinceEpoch % timeStep;
                var remainingSeconds = Math.Max(1, (int)Math.Ceiling(timeStep - currentStepProgress));
                
                // Ensure we don't skip from 1 to 30 - handle the transition smoothly
                if (remainingSeconds == 30 && generator.RemainingSeconds == 1)
                {
                    remainingSeconds = 30;
                }
                else if (remainingSeconds > 30)
                {
                    remainingSeconds = 30;
                }

                var newCode = generator.Totp.ComputeTotp(now);
                if (newCode != generator.CurrentCode)
                {
                    generator.CurrentCode = newCode;
                    generator.LastUpdate = now;
                    codeUpdated = true;
                }

                if (remainingSeconds != generator.RemainingSeconds)
                {
                    generator.RemainingSeconds = remainingSeconds;
                    countdownUpdated = true;
                }
            }

            // Marshal UI updates back to the UI thread
            if (countdownUpdated)
            {
                _dispatcher.BeginInvoke(() => OnPropertyChanged(nameof(GetRemainingSeconds)));
            }

            if (codeUpdated)
            {
                _dispatcher.BeginInvoke(() => OnPropertyChanged(nameof(GetTotpCode)));
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
                _mainTimer?.Dispose();
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