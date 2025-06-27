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

        public TotpService()
        {
            // Get the current dispatcher (UI thread)
            _dispatcher = Dispatcher.CurrentDispatcher;
            
            // Update TOTP codes every 30 seconds
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(30);
            _timer.Tick += UpdateTotpCodes;
            _timer.Start();

            // Update countdown every second
            _countdownTimer = new DispatcherTimer();
            _countdownTimer.Interval = TimeSpan.FromSeconds(1);
            _countdownTimer.Tick += UpdateCountdown;
            _countdownTimer.Start();
        }

        public void AddTotpSeed(string accountId, string seed)
        {
            if (string.IsNullOrWhiteSpace(seed))
                return;

            try
            {
                // Convert the seed to bytes (assuming it's base32 encoded)
                var key = Base32Encoding.ToBytes(seed);
                var totp = new Totp(key);
                
                // Calculate initial remaining seconds
                var now = DateTime.UtcNow;
                var timeStep = 30; // TOTP standard is 30 seconds
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
                    LastUpdate = DateTime.UtcNow,
                    RemainingSeconds = remainingSeconds
                };

                OnPropertyChanged(nameof(GetTotpCode));
            }
            catch (Exception ex)
            {
                // Handle invalid seed format
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
            foreach (var kvp in _totpGenerators)
            {
                var generator = kvp.Value;
                var newCode = generator.Totp.ComputeTotp();
                
                if (newCode != generator.CurrentCode)
                {
                    generator.CurrentCode = newCode;
                    generator.LastUpdate = DateTime.UtcNow;
                    updated = true;
                }
            }

            if (updated)
            {
                // Notify UI that TOTP codes have changed
                OnPropertyChanged(nameof(GetTotpCode));
            }
        }

        private void UpdateCountdown(object? sender, EventArgs e)
        {
            if (_disposed) return;

            var updated = false;
            var codeUpdated = false;
            foreach (var kvp in _totpGenerators)
            {
                var generator = kvp.Value;
                
                // Calculate remaining seconds
                var now = DateTime.UtcNow;
                var timeStep = 30; // TOTP standard is 30 seconds
                var secondsSinceEpoch = (long)(now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                var currentStep = secondsSinceEpoch / timeStep;
                var nextStep = currentStep + 1;
                var nextStepTime = nextStep * timeStep;
                var remainingSeconds = (int)(nextStepTime - secondsSinceEpoch);

                // Always check if the code needs to be updated
                var newCode = generator.Totp.ComputeTotp();
                if (newCode != generator.CurrentCode || remainingSeconds == 0)
                {
                    generator.CurrentCode = newCode;
                    generator.LastUpdate = DateTime.UtcNow;
                    codeUpdated = true;
                }
                
                if (remainingSeconds != generator.RemainingSeconds)
                {
                    generator.RemainingSeconds = remainingSeconds;
                    updated = true;
                }
            }

            // Always increment TimerTick to force UI update
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