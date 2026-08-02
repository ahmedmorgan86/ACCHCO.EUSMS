using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ACCHCO.EUSMS.App.Controls;

public partial class NotificationSnackbar : UserControl
{
    private readonly DispatcherTimer _timer;

    public NotificationSnackbar()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _timer.Tick += (s, e) => { Visibility = Visibility.Collapsed; _timer.Stop(); };
    }

    public void Show(string message)
    {
        MessageText.Text = message;
        Visibility = Visibility.Visible;
        _timer.Start();
    }
}
