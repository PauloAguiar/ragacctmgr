using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Diagnostics;
using System.Windows.Navigation;

namespace ragaccountmgr
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private const string AccountsFolder = ".ragcontas";
        private const string AccountsFile = "accounts.json";
        private static string AccountsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), AccountsFolder, AccountsFile);

        private bool _isBladeVisible;
        private Account _newAccount = new Account();
        private string _lastCopiedField;
        private bool _isEditMode;
        private Account? _editingAccount;
        private readonly TotpService _totpService;

        public MainWindowViewModel()
        {
            Accounts = new ObservableCollection<Account>();
            NewAccount = new Account();
            NewAccount.PropertyChanged += NewAccount_PropertyChanged;
            _lastCopiedField = string.Empty;
            _totpService = new TotpService();
            
            ShowAddBladeCommand = new RelayCommand(ShowAddBlade);
            HideAddBladeCommand = new RelayCommand(HideAddBlade);
            AddAccountCommand = new RelayCommand(AddAccount);
            DeleteAccountCommand = new RelayCommand(DeleteAccount);
            
            // Optional field commands
            AddPinCodeCommand = new RelayCommand(AddPinCode);
            RemovePinCodeCommand = new RelayCommand(RemovePinCode);
            AddOtpCodeCommand = new RelayCommand(AddOtpCode);
            RemoveOtpCodeCommand = new RelayCommand(RemoveOtpCode);
            AddStorageCodeCommand = new RelayCommand(AddStorageCode);
            RemoveStorageCodeCommand = new RelayCommand(RemoveStorageCode);
            AddCommentsCommand = new RelayCommand(AddComments);
            RemoveCommentsCommand = new RelayCommand(RemoveComments);
            OpenConfigFolderCommand = new RelayCommand(OpenConfigFolder);
            
            LoadAccounts();
        }

        private void NewAccount_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(NewAccount));
        }

        public ObservableCollection<Account> Accounts { get; }

        public bool IsBladeVisible
        {
            get => _isBladeVisible;
            set
            {
                _isBladeVisible = value;
                OnPropertyChanged();
            }
        }

        public Account NewAccount
        {
            get => _newAccount;
            set
            {
                if (_newAccount != null)
                    _newAccount.PropertyChanged -= NewAccount_PropertyChanged;
                _newAccount = value;
                if (_newAccount != null)
                    _newAccount.PropertyChanged += NewAccount_PropertyChanged;
                OnPropertyChanged();
            }
        }

        public ICommand ShowAddBladeCommand { get; }
        public ICommand HideAddBladeCommand { get; }
        public ICommand AddAccountCommand { get; }
        public ICommand DeleteAccountCommand { get; }
        
        // Optional field commands
        public ICommand AddPinCodeCommand { get; }
        public ICommand RemovePinCodeCommand { get; }
        public ICommand AddOtpCodeCommand { get; }
        public ICommand RemoveOtpCodeCommand { get; }
        public ICommand AddStorageCodeCommand { get; }
        public ICommand RemoveStorageCodeCommand { get; }
        public ICommand AddCommentsCommand { get; }
        public ICommand RemoveCommentsCommand { get; }
        public ICommand OpenConfigFolderCommand { get; }

        public string LastCopiedField
        {
            get => _lastCopiedField;
            set
            {
                _lastCopiedField = value;
                OnPropertyChanged();
            }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                _isEditMode = value;
                OnPropertyChanged();
            }
        }

        public void EditAccount(Account account)
        {
            _editingAccount = account;
            NewAccount = account.Clone();
            
            // Set the Show properties for optional fields that have values
            NewAccount.ShowPinCodeField = !string.IsNullOrWhiteSpace(account.PinCode);
            NewAccount.ShowOtpCodeField = !string.IsNullOrWhiteSpace(account.OtpSeed);
            NewAccount.ShowStorageCodeField = !string.IsNullOrWhiteSpace(account.StorageCode);
            
            IsEditMode = true;
            IsBladeVisible = true;
        }

        private void DeleteAccount()
        {
            if (_editingAccount != null)
            {
                var result = MessageBox.Show(
                    $"Tem certeza que deseja excluir a conta '{_editingAccount.Username}'? Esta ação não pode ser desfeita.",
                    "Confirmar Exclusão",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    // Remove TOTP seed if it exists
                    if (!string.IsNullOrWhiteSpace(_editingAccount.OtpSeed))
                    {
                        _totpService.RemoveTotpSeed(_editingAccount.Username);
                    }
                    
                    Accounts.Remove(_editingAccount);
                    SaveAccounts();
                    HideAddBlade();
                }
            }
        }

        private void ShowAddBlade()
        {
            NewAccount = new Account();
            IsEditMode = false;
            _editingAccount = null;
            IsBladeVisible = true;
        }

        public void HideAddBlade()
        {
            IsBladeVisible = false;
            IsEditMode = false;
            _editingAccount = null;
            NewAccount = new Account();
        }

        private void AddAccount()
        {
            if (string.IsNullOrWhiteSpace(NewAccount.Username))
            {
                MessageBox.Show("Usuário é obrigatório.", "Erro de Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(NewAccount.Password))
            {
                MessageBox.Show("Senha é obrigatória.", "Erro de Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (IsEditMode && _editingAccount != null)
            {
                // Remove old TOTP seed if it exists
                if (!string.IsNullOrWhiteSpace(_editingAccount.OtpSeed))
                {
                    _totpService.RemoveTotpSeed(_editingAccount.Username);
                }
                
                // Update existing account
                var index = Accounts.IndexOf(_editingAccount);
                if (index >= 0)
                {
                    var updatedAccount = NewAccount.Clone();
                    Accounts[index] = updatedAccount;
                    
                    // Add new TOTP seed if it exists
                    if (!string.IsNullOrWhiteSpace(updatedAccount.OtpSeed))
                    {
                        _totpService.AddTotpSeed(updatedAccount.Username, updatedAccount.OtpSeed);
                    }
                }
            }
            else
            {
                // Add new account
                var newAccount = NewAccount.Clone();
                Accounts.Add(newAccount);
                
                // Add TOTP seed if it exists
                if (!string.IsNullOrWhiteSpace(newAccount.OtpSeed))
                {
                    _totpService.AddTotpSeed(newAccount.Username, newAccount.OtpSeed);
                }
            }
            SaveAccounts();
            HideAddBlade();
        }

        private void AddPinCode()
        {
            NewAccount.ShowPinCodeField = true;
        }

        private void RemovePinCode()
        {
            NewAccount.ShowPinCodeField = false;
            NewAccount.PinCode = string.Empty;
        }

        private void AddOtpCode()
        {
            NewAccount.ShowOtpCodeField = true;
        }

        private void RemoveOtpCode()
        {
            NewAccount.ShowOtpCodeField = false;
            NewAccount.OtpSeed = string.Empty;
        }

        private void AddStorageCode()
        {
            NewAccount.ShowStorageCodeField = true;
        }

        private void RemoveStorageCode()
        {
            NewAccount.ShowStorageCodeField = false;
            NewAccount.StorageCode = string.Empty;
        }

        private void AddComments()
        {
            NewAccount.Comments = string.Empty;
            OnPropertyChanged(nameof(NewAccount));
        }

        private void RemoveComments()
        {
            NewAccount.Comments = string.Empty;
            OnPropertyChanged(nameof(NewAccount));
        }

        private void LoadAccounts()
        {
            try
            {
                var dir = Path.GetDirectoryName(AccountsPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir!);
                if (File.Exists(AccountsPath))
                {
                    var json = File.ReadAllText(AccountsPath);
                    var loaded = JsonSerializer.Deserialize<List<Account>>(json);
                    if (loaded != null)
                    {
                        Accounts.Clear();
                        foreach (var acc in loaded)
                        {
                            Accounts.Add(acc);
                            if (!string.IsNullOrWhiteSpace(acc.OtpSeed))
                                _totpService.AddTotpSeed(acc.Username, acc.OtpSeed);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha ao carregar contas: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveAccounts()
        {
            try
            {
                var dir = Path.GetDirectoryName(AccountsPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir!);
                // Only serialize relevant fields
                var toSave = Accounts.Select(acc => new {
                    acc.Username,
                    acc.Password,
                    acc.PinCode,
                    acc.OtpSeed,
                    acc.StorageCode,
                    acc.Comments
                }).ToList();
                var json = JsonSerializer.Serialize(toSave, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(AccountsPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha ao salvar contas: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public TotpService TotpService => _totpService;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OpenConfigFolder()
        {
            var dir = Path.GetDirectoryName(AccountsPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir!);
            Process.Start(new ProcessStartInfo
            {
                FileName = dir!,
                UseShellExecute = true,
                Verb = "open"
            });
        }
    }
}