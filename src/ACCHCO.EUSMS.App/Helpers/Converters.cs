using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ACCHCO.EUSMS.Data.Entities;

namespace ACCHCO.EUSMS.App.Helpers;

public class CurrentUser
{
    public static string Username { get; set; } = Environment.UserName;
    public static string FullName { get; set; } = Environment.UserDomainName + "\\" + Environment.UserName;
    public static string Role { get; set; } = UserRole.SupportSpecialist.ToString();
    public static int UserId { get; set; }

    public static void Apply(AppUser? user)
    {
        if (user == null)
        {
            Username = Environment.UserName;
            FullName = Environment.UserDomainName + "\\" + Environment.UserName;
            Role = UserRole.SupportSpecialist.ToString();
            UserId = 0;
            return;
        }

        Username = user.Username;
        FullName = string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName;
        Role = user.Role.ToString();
        UserId = user.Id;
    }

    public static bool HasPermission(string requiredRole)
    {
        var hierarchy = new[] { "SystemAdministrator", "DepartmentManager", "SupportSpecialist", "ReadOnly" };
        var currentLevel = Array.IndexOf(hierarchy, Role);
        var requiredLevel = Array.IndexOf(hierarchy, requiredRole);
        return currentLevel >= 0 && requiredLevel >= 0 && currentLevel <= requiredLevel;
    }

    public static bool IsAdmin => string.Equals(Role, UserRole.SystemAdministrator.ToString(), StringComparison.OrdinalIgnoreCase);
    public static bool IsManager => IsAdmin || string.Equals(Role, UserRole.DepartmentManager.ToString(), StringComparison.OrdinalIgnoreCase);
}

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Visibility.Visible;
}

public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return false;
    }
}

public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? Visibility.Collapsed : Visibility.Visible;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility.Collapsed;
    }
}

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value != null)
        {
            var str = value.ToString();
            return str switch
            {
                "Open" or "مفتوحة" => "#FF4CAF50",
                "InProgress" or "قيد التنفيذ" => "#FFFF9800",
                "OnHold" or "موقوفة مؤقتاً" => "#FFFF5722",
                "Resolved" or "تم الحل" => "#FF2196F3",
                "Closed" or "مغلقة" => "#FF9E9E9E",
                "Critical" or "حرجة جداً" => "#FFF44336",
                "High" or "عالية" => "#FFFF5722",
                "Medium" or "متوسطة" => "#FFFF9800",
                "Low" or "منخفضة" => "#FF4CAF50",
                _ => "#FF757575"
            };
        }
        return "#FF757575";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is string s ? s : string.Empty;
    }
}

public class EquipmentStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is EquipmentStatus status)
        {
            return status switch
            {
                EquipmentStatus.Active => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                EquipmentStatus.Inactive => new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                EquipmentStatus.UnderMaintenance => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                EquipmentStatus.Lost => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                _ => new SolidColorBrush(Color.FromRgb(117, 117, 117))
            };
        }
        return new SolidColorBrush(Color.FromRgb(117, 117, 117));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush brush)
        {
            return brush.Color == Color.FromRgb(76, 175, 80)
                ? EquipmentStatus.Active
                : brush.Color == Color.FromRgb(158, 158, 158)
                    ? EquipmentStatus.Inactive
                    : brush.Color == Color.FromRgb(255, 152, 0)
                        ? EquipmentStatus.UnderMaintenance
                        : brush.Color == Color.FromRgb(244, 67, 54)
                            ? EquipmentStatus.Lost
                            : EquipmentStatus.Active;
        }

        return EquipmentStatus.Active;
    }
}

public class BoolToStatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return new SolidColorBrush(b
                ? Color.FromRgb(76, 175, 80)
                : Color.FromRgb(244, 67, 54));
        return new SolidColorBrush(Color.FromRgb(117, 117, 117));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush brush)
        {
            return brush.Color == Color.FromRgb(76, 175, 80);
        }

        return false;
    }
}

public class BoolToStatusTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && parameter is string labels)
        {
            var parts = labels.Split('|');
            return b
                ? (parts.Length > 0 ? parts[0] : "نعم")
                : (parts.Length > 1 ? parts[1] : "لا");
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string text)
        {
            return text.Equals("نعم", StringComparison.OrdinalIgnoreCase) || text.Equals("Yes", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}

public class EnumToArabicConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is EquipmentType eq) return eq.ToArabicString();
        if (value is TicketStatus st) return st.ToArabicString();
        if (value is Priority pr) return pr.ToArabicString();
        if (value is Shift sh) return sh.ToArabicString();
        if (value is FaultType ft) return ft.ToArabicString();
        if (value is UserRole ur) return ur.ToArabicString();
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() ?? string.Empty;
    }
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility visibility && visibility == Visibility.Visible;
    }
}

public class NullToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value != null;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && b;
    }
}

public class InverseNullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value == null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility visibility && visibility == Visibility.Visible;
    }
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? Visibility.Collapsed : Visibility.Visible;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Visibility.Collapsed;
}

public class EnumValuesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is Type enumType && enumType.IsEnum)
            return Enum.GetValues(enumType);
        return Array.Empty<object>();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value ?? Array.Empty<object>();
    }
}

public class DateTimeFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dt)
            return dt.ToString("dd/MM/yyyy HH:mm");
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && DateTime.TryParse(s, out var dt))
            return dt;
        return DateTime.MinValue;
    }
}

public class AdStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? "● متصل" : "○ غير متصل";
        return "? غير معروف";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is string text && (text.Contains("متصل") || text.Contains("Connected"));
    }
}

public class DonutFractionConverter : IValueConverter
{
    private const double Radius = 75;
    private const double Thickness = 16;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var fraction = System.Convert.ToDouble(value);
        fraction = Math.Clamp(fraction, 0, 1);
        var unit = (2 * Math.PI * Radius) / Thickness;
        var dash = Math.Max(fraction * unit, 0.5);
        var gap = Math.Max((1 - fraction) * unit, 0.5);
        return new DoubleCollection { dash, gap };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class RatioToStarConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var v = System.Convert.ToDouble(value);
        if (v <= 0) v = 0.0001;
        return new GridLength(v, GridUnitType.Star);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count) return count > 0 ? Visibility.Visible : Visibility.Collapsed;
        if (value is double d) return d > 0 ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class EmptyCountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count) return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
