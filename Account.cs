using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ragaccountmgr
{
    public class Account : INotifyPropertyChanged
    {
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _pinCode = string.Empty;
        private string _otpSeed = string.Empty;
        private string _storageCode = string.Empty;
        private string _comments = string.Empty;
        private bool _showPinCodeField;
        private bool _showOtpCodeField;
        private bool _showStorageCodeField;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string PinCode
        {
            get => _pinCode;
            set => SetProperty(ref _pinCode, value);
        }

        public string OtpSeed
        {
            get => _otpSeed;
            set => SetProperty(ref _otpSeed, value);
        }

        public string StorageCode
        {
            get => _storageCode;
            set => SetProperty(ref _storageCode, value);
        }

        public string Comments
        {
            get => _comments;
            set => SetProperty(ref _comments, value);
        }

        public bool HasPinCode => !string.IsNullOrWhiteSpace(PinCode);
        public bool HasOtpCode => !string.IsNullOrWhiteSpace(OtpSeed);
        public bool HasStorageCode => !string.IsNullOrWhiteSpace(StorageCode);
        public bool HasComments => !string.IsNullOrWhiteSpace(Comments);

        public bool ShowPinCodeField
        {
            get => _showPinCodeField;
            set => SetProperty(ref _showPinCodeField, value);
        }
        public bool ShowOtpCodeField
        {
            get => _showOtpCodeField;
            set => SetProperty(ref _showOtpCodeField, value);
        }
        public bool ShowStorageCodeField
        {
            get => _showStorageCodeField;
            set => SetProperty(ref _showStorageCodeField, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            
            // Notify Has properties when string properties change
            if (propertyName == nameof(PinCode))
            {
                OnPropertyChanged(nameof(HasPinCode));
            }
            else if (propertyName == nameof(OtpSeed))
            {
                OnPropertyChanged(nameof(HasOtpCode));
            }
            else if (propertyName == nameof(StorageCode))
            {
                OnPropertyChanged(nameof(HasStorageCode));
            }
            else if (propertyName == nameof(Comments))
            {
                OnPropertyChanged(nameof(HasComments));
            }
            
            return true;
        }

        public Account Clone()
        {
            return new Account
            {
                Username = this.Username,
                Password = this.Password,
                PinCode = this.PinCode,
                OtpSeed = this.OtpSeed,
                StorageCode = this.StorageCode,
                Comments = this.Comments
            };
        }
    }
} 