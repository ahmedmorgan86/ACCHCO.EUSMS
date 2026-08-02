using System.Windows;
using System.Windows.Controls;

namespace ACCHCO.EUSMS.App.Helpers;

public static class DialogHelper
{
    public static bool Confirm(string message, string title = "تأكيد")
    {
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }

    public static void ShowInfo(string message, string title = "معلومات")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public static void ShowWarning(string message, string title = "تنبيه")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public static void ShowError(string message, string title = "خطأ")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public static string? ShowInput(string title, string prompt, string defaultValue = "")
    {
        var window = new Window
        {
            Title = title,
            Width = 400,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            FlowDirection = FlowDirection.RightToLeft
        };

        var stack = new StackPanel { Margin = new Thickness(15) };
        stack.Children.Add(new TextBlock
        {
            Text = prompt,
            Margin = new Thickness(0, 0, 0, 10),
            TextWrapping = TextWrapping.Wrap
        });

        var textBox = new TextBox
        {
            Text = defaultValue,
            Margin = new Thickness(0, 0, 0, 15),
            Padding = new Thickness(5)
        };
        stack.Children.Add(textBox);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        var okButton = new Button
        {
            Content = "موافق",
            Width = 80,
            Margin = new Thickness(0, 0, 10, 0),
            IsDefault = true
        };
        okButton.Click += (s, e) => { window.DialogResult = true; };
        buttonPanel.Children.Add(okButton);

        var cancelButton = new Button
        {
            Content = "إلغاء",
            Width = 80,
            IsCancel = true
        };
        buttonPanel.Children.Add(cancelButton);

        stack.Children.Add(buttonPanel);
        window.Content = stack;

        return window.ShowDialog() == true ? textBox.Text : null;
    }
}

public static class DependencyObjectExtensions
{
    public static T? FindParent<T>(this DependencyObject child) where T : DependencyObject
    {
        var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
        while (parent != null && parent is not T)
        {
            parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
        }
        return parent as T;
    }

    public static T? FindChild<T>(this DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T found)
                return found;
            var result = FindChild<T>(child);
            if (result != null)
                return result;
        }
        return null;
    }
}
