using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;

namespace ragaccountmgr
{
    public partial class AccountControl : UserControl, INotifyPropertyChanged
    {
        public AccountControl()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => SetupCommands();
        }

        private void SetupCommands()
        {
            if (DataContext is Account account)
            {
                CopyUsernameCommand = new RelayCommand(() => 
                {
                    Clipboard.SetText(account.Username);
                    SetGlobalLastCopiedField("Username");
                });
                CopyPasswordCommand = new RelayCommand(() => 
                {
                    Clipboard.SetText(account.Password);
                    SetGlobalLastCopiedField("Password");
                });
                CopyOtpCodeCommand = new RelayCommand(() => 
                {
                    var totpCode = GetTotpCode(account);
                    Clipboard.SetText(totpCode);
                    SetGlobalLastCopiedField("OtpCode");
                });
                EditAccountCommand = new RelayCommand(() => 
                {
                    EditAccount(account);
                });
                ShareAccountCommand = new RelayCommand(() => 
                {
                    ShareAccount(account);
                });
                OnPropertyChanged(nameof(CopyUsernameCommand));
                OnPropertyChanged(nameof(CopyPasswordCommand));
                OnPropertyChanged(nameof(CopyOtpCodeCommand));
                OnPropertyChanged(nameof(EditAccountCommand));
                OnPropertyChanged(nameof(ShareAccountCommand));
            }
        }

        private void EditAccount(Account account)
        {
            // Find the MainWindow and trigger edit mode
            var mainWindow = Window.GetWindow(this);
            if (mainWindow?.DataContext is MainWindowViewModel viewModel)
            {
                viewModel.EditAccount(account);
            }
        }

        private void SetGlobalLastCopiedField(string fieldName)
        {
            // Find the MainWindow and update its LastCopiedField
            var mainWindow = Window.GetWindow(this);
            if (mainWindow?.DataContext is MainWindowViewModel viewModel && DataContext is Account account)
            {
                // Include username to make the field identifier unique per account
                viewModel.LastCopiedField = $"{account.Username}_{fieldName}";
            }
        }

        private string GetTotpCode(Account account)
        {
            // Find the MainWindow to get the TOTP service
            var mainWindow = Window.GetWindow(this);
            if (mainWindow?.DataContext is MainWindowViewModel viewModel)
            {
                // If there's a TOTP seed, use the service to get the current code
                if (!string.IsNullOrWhiteSpace(account.OtpSeed))
                {
                    return viewModel.TotpService.GetTotpCode(account.Username);
                }
                // No TOTP seed, return empty
                return string.Empty;
            }
            return string.Empty;
        }

        private async void ShareAccount(Account account)
        {
            var shareText = FormatAccountDetails(account);
            Clipboard.SetText(shareText);
            await ShowCopiedTooltip();
        }

        private string FormatAccountDetails(Account account)
        {
            var sb = new StringBuilder();
            sb.AppendLine("-------- RO CONTAS --------");
            sb.AppendLine($"Usuario: {account.Username}");
            sb.AppendLine($"Senha: {account.Password}");

            if (account.HasPinCode)
            {
                sb.AppendLine($"PIN: {account.PinCode}");
            }

            if (account.HasStorageCode)
            {
                sb.AppendLine($"KAFRA: {account.StorageCode}");
            }

            if (account.HasOtpCode)
            {
                sb.AppendLine($"OTP Segredo: {account.OtpSeed}");
            }

            if (account.HasComments)
            {
                sb.AppendLine($"Comentarios: {account.Comments}");
            }

            sb.AppendLine("---------------------------");

            return sb.ToString().TrimEnd();
        }

        private async Task ShowCopiedTooltip()
        {
            var copiedPopup = this.FindName("CopiedPopup") as Border;
            if (copiedPopup != null)
            {
                // Show the popup immediately
                copiedPopup.Visibility = Visibility.Visible;
                
                // Wait for display time
                await Task.Delay(1500);
                
                // Hide the popup
                copiedPopup.Visibility = Visibility.Collapsed;
            }
        }

        public ICommand CopyUsernameCommand { get; private set; } = new RelayCommand(() => { });
        public ICommand CopyPasswordCommand { get; private set; } = new RelayCommand(() => { });
        public ICommand CopyOtpCodeCommand { get; private set; } = new RelayCommand(() => { });
        public ICommand EditAccountCommand { get; private set; } = new RelayCommand(() => { });
        public ICommand ShareAccountCommand { get; private set; } = new RelayCommand(() => { });

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
} 