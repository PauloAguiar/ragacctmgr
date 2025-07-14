using System;
using System.Windows;

namespace ragaccountmgr
{
    public partial class ImportDialog : Window
    {
        public string ImportData { get; private set; } = string.Empty;
        public new bool DialogResult { get; private set; } = false;

        public ImportDialog()
        {
            InitializeComponent();
            ImportDataTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear any previous error message
            ErrorMessage.Visibility = Visibility.Collapsed;
            
            var inputData = ImportDataTextBox.Text.Trim();
            
            // Check if input is empty
            if (string.IsNullOrWhiteSpace(inputData))
            {
                ShowErrorMessage("O campo não pode estar vazio.");
                return;
            }

            // Validate the input data format
            if (!IsValidAccountData(inputData))
            {
                ShowErrorMessage("Dados inválidos. Verifique o formato dos dados da conta.");
                return;
            }

            // If validation passes, set the result and close
            ImportData = inputData;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShowErrorMessage(string message)
        {
            ErrorMessage.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;
        }

        private bool IsValidAccountData(string data)
        {
            try
            {
                // Basic validation - check if it contains key-value pairs
                var lines = data.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                bool hasUsername = false;
                bool hasPassword = false;
                
                foreach (var line in lines)
                {
                    // Skip separator lines
                    if (line.Trim().StartsWith("---"))
                        continue;
                        
                    var colonIndex = line.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        var key = line.Substring(0, colonIndex).Trim().ToLower();
                        var value = line.Substring(colonIndex + 1).Trim();
                        
                        // Check for required fields
                        if (key == "usuario" || key == "username")
                        {
                            hasUsername = !string.IsNullOrWhiteSpace(value);
                        }
                        else if (key == "senha" || key == "password")
                        {
                            hasPassword = !string.IsNullOrWhiteSpace(value);
                        }
                    }
                }
                
                // Must have at least username and password
                return hasUsername && hasPassword;
            }
            catch
            {
                return false;
            }
        }
    }
} 