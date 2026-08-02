using System.Windows;
using ACCHCO.EUSMS.App.Helpers;
using ACCHCO.EUSMS.App.ViewModels;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Services.Interfaces;

namespace ACCHCO.EUSMS.App.Views;

public partial class ShiftHandoverCreateWindow : Window
{
    private readonly ShiftHandoverViewModel _viewModel;

    public ShiftHandoverCreateWindow(ShiftHandoverViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        viewModel.NewHandover = new ShiftHandover
        {
            HandoverDate = DateTime.Today,
            FromShift = Shift.Red,
            ToShift = Shift.Yellow
        };
    }

    private async void OnSave(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_viewModel.NewHandover.Summary))
        {
            DialogHelper.ShowWarning("الملخص مطلوب.");
            return;
        }

        var handoverService = App.ServiceProvider!.GetRequiredService<IShiftHandoverService>();
        try
        {
            await handoverService.CreateAsync(_viewModel.NewHandover, new List<int>(), CurrentUser.Username);
            DialogHelper.ShowInfo("تم إنشاء التسليمة بنجاح.");
            _viewModel.IsCreating = false;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            DialogHelper.ShowError($"فشل إنشاء التسليمة: {ex.Message}");
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        _viewModel.IsCreating = false;
        DialogResult = false;
        Close();
    }
}
