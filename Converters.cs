using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ragaccountmgr
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }

    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Collapsed;
            }
            return true;
        }
    }

    public class LastCopiedFieldConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string lastCopiedField = value as string ?? string.Empty;
            string currentField = parameter as string ?? string.Empty;
            
            // If the lastCopiedField contains an underscore, it's in the new format (username_fieldname)
            if (lastCopiedField.Contains('_'))
            {
                // Extract the field name part after the underscore
                string[] parts = lastCopiedField.Split('_', 2);
                if (parts.Length == 2)
                {
                    string lastFieldName = parts[1];
                    return lastFieldName == currentField ? new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#BBD8A3")) : System.Windows.Media.Brushes.LightGray;
                }
            }
            
            // Fallback to old format for backward compatibility
            return lastCopiedField == currentField ? new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#BBD8A3")) : System.Windows.Media.Brushes.LightGray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LastCopiedFieldMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return System.Windows.Media.Brushes.White;
            
            string lastCopiedField = values[0] as string ?? string.Empty;
            string currentUsername = values[1] as string ?? string.Empty;
            string currentField = parameter as string ?? string.Empty;
            
            // Create the expected field identifier for this specific button
            string expectedFieldId = $"{currentUsername}_{currentField}";
            
            return lastCopiedField == expectedFieldId ? new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#BBD8A3")) : System.Windows.Media.Brushes.White;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EditModeHeaderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEditMode)
            {
                return isEditMode ? "Editar Conta" : "Adicionar Conta";
            }
            return "Adicionar Conta";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EditModeSaveButtonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEditMode)
            {
                return isEditMode ? "Salvar Alterações" : "Adicionar Conta";
            }
            return "Adicionar Conta";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TotpCodeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is Account account && values[1] is TotpService totpService)
            {
                // If there's a TOTP seed, use the service to get the current code
                if (!string.IsNullOrWhiteSpace(account.OtpSeed))
                {
                    var code = totpService.GetTotpCode(account.Username);
                    if (!string.IsNullOrEmpty(code) && code.Length == 6)
                    {
                        return code.Substring(0, 3) + " " + code.Substring(3, 3);
                    }
                    return code;
                }
                // No TOTP seed, return empty
                return string.Empty;
            }
            return string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TotpRemainingSecondsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is Account account && values[1] is TotpService totpService)
            {
                if (!string.IsNullOrWhiteSpace(account.OtpSeed))
                {
                    return totpService.GetRemainingSeconds(account.Username).ToString();
                }
                return "";
            }
            return "";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TotpProgressConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int seconds)
            {
                // 30 seconds full, 0 seconds empty
                return (double)seconds / 30.0;
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TotpProgressArcConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Get progress from Account and TotpService
            double progress = 1.0;
            if (values.Length >= 2 && values[0] is Account account && values[1] is TotpService totpService)
            {
                if (!string.IsNullOrWhiteSpace(account.OtpSeed))
                {
                    int seconds = totpService.GetRemainingSeconds(account.Username);
                    progress = (double)seconds / 30.0;
                }
            }
            
            progress = Math.Max(0, Math.Min(1, progress));

            double angle = 360 * progress;
            double radius = 14; // slightly less than half of 32px for padding
            double center = 16;
            double startAngle = -90;
            double endAngle = startAngle + angle;

            double startRadians = startAngle * Math.PI / 180.0;
            double endRadians = endAngle * Math.PI / 180.0;

            double x1 = center + radius * Math.Cos(startRadians);
            double y1 = center + radius * Math.Sin(startRadians);
            double x2 = center + radius * Math.Cos(endRadians);
            double y2 = center + radius * Math.Sin(endRadians);

            bool isLargeArc = angle > 180;

            return Geometry.Parse($"M{x1},{y1} A{radius},{radius} 0 {(isLargeArc ? 1 : 0)},1 {x2},{y2}");
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TotpArcProgressConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is Account account && values[1] is TotpService totpService)
            {
                if (!string.IsNullOrWhiteSpace(account.OtpSeed))
                {
                    int seconds = totpService.GetRemainingSeconds(account.Username);
                    return (double)seconds / 30.0;
                }
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TotpCircleColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is Account account && values[1] is TotpService totpService)
            {
                if (!string.IsNullOrWhiteSpace(account.OtpSeed))
                {
                    int seconds = totpService.GetRemainingSeconds(account.Username);
                    // Green for normal countdown, red when below 4 seconds
                    return seconds <= 4 ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Green);
                }
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TotpCounterColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is Account account && values[1] is TotpService totpService)
            {
                if (!string.IsNullOrWhiteSpace(account.OtpSeed))
                {
                    int seconds = totpService.GetRemainingSeconds(account.Username);
                    return seconds <= 3 ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Green;
                }
            }
            return System.Windows.Media.Brushes.Gray;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ButtonHoverColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return System.Windows.Media.Brushes.White;
            
            var background = values[0] as System.Windows.Media.SolidColorBrush;
            var isMouseOver = values[1] is bool && (bool)values[1];
            var isPressed = parameter?.ToString() == "pressed";
            
            if (background == null) return System.Windows.Media.Brushes.White;
            
            // Check if it's the copied state (green background)
            var copiedColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#BBD8A3");
            if (background.Color == copiedColor)
            {
                if (isPressed)
                    return new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#81C784"));
                else if (isMouseOver)
                    return new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#A5D6A7"));
                else
                    return background; // Return original green
            }
            else
            {
                // Default state
                if (isPressed)
                    return new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#D0D0D0"));
                else if (isMouseOver)
                    return new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E8E8E8"));
                else
                    return background; // Return original background
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 