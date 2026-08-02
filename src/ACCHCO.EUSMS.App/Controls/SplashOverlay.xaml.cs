using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace ACCHCO.EUSMS.App.Controls;

public partial class SplashOverlay : UserControl
{
    public SplashOverlay()
    {
        InitializeComponent();
    }

    public void FadeOut()
    {
        var fadeOut = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(500))
        };
        fadeOut.Completed += (s, e) => Visibility = Visibility.Collapsed;
        BeginAnimation(OpacityProperty, fadeOut);
    }
}
