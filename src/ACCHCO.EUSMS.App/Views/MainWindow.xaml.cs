using System.Windows;
using System.Windows.Controls;
using ACCHCO.EUSMS.App.Helpers;
using CommunityToolkit.Mvvm.Messaging;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Services.Helpers;
using ACCHCO.EUSMS.Services.Interfaces;

namespace ACCHCO.EUSMS.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        TxtCurrentUser.Text = $"  {CurrentUser.FullName}";
        TxtCopyright.Text = $"© {DateTime.Now.Year} Ahmed Morgan";
        SetupNavigation();
        ShowDashboard();
        _ = EnsureCurrentUserRegisteredAsync();

        Loaded += (s, e) =>
        {
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1500)
            };
            timer.Tick += (tm, tme) =>
            {
                timer.Stop();
                MainSplashOverlay.FadeOut();
            };
            timer.Start();
        };

        WeakReferenceMessenger.Default.Register<NavigateMessage>(this, (r, m) =>
        {
            switch (m.Target)
            {
                case "Tickets":
                    NavTickets.IsChecked = true;
                    ShowTickets();
                    break;
                case "CreateTicket":
                    {
                        var window = m.Data switch
                        {
                            NetworkDevice device => new TicketDetailWindow(null, device),
                            Equipment eq => new TicketDetailWindow(null, null, eq),
                            _ => new TicketDetailWindow()
                        };
                        window.ShowDialog();
                    }
                    break;
                case "Devices":
                    NavDevices.IsChecked = true;
                    ShowDevices();
                    break;
                case "Dashboard":
                    NavDashboard.IsChecked = true;
                    ShowDashboard();
                    break;
                case "Help":
                    NavHelp.IsChecked = true;
                    ShowHelp();
                    break;
            }
        });

        WeakReferenceMessenger.Default.Register<ShowLoadingMessage>(this, (r, m) =>
        {
            MainLoadingOverlay.Visibility = m.IsLoading ? Visibility.Visible : Visibility.Collapsed;
        });

        WeakReferenceMessenger.Default.Register<ShowSnackbarMessage>(this, (r, m) =>
        {
            MainSnackbar.Show(m.Message);
        });
    }

    private void SetupNavigation()
    {
        NavDashboard.Checked += (s, e) => ShowDashboard();
        NavTickets.Checked += (s, e) => ShowTickets();
        NavEquipment.Checked += (s, e) => ShowEquipment();
        NavDevices.Checked += (s, e) => ShowDevices();
        NavReplacements.Checked += (s, e) => ShowReplacements();
        NavHandover.Checked += (s, e) => ShowHandover();
        NavReports.Checked += (s, e) => ShowReports();
        NavPerformance.Checked += (s, e) => ShowPerformanceAnalytics();
        NavUsers.Checked += (s, e) => ShowUsers();
        NavAuditLog.Checked += (s, e) => ShowAuditLog();
        NavSettings.Checked += (s, e) => ShowSettings();
        NavHelp.Checked += (s, e) => ShowHelp();
    }

    private void ShowDashboard()
    {
        MainContent.Content = new DashboardView();
    }

    private void ShowTickets()
    {
        MainContent.Content = new TicketListView();
    }

    private void ShowEquipment()
    {
        MainContent.Content = new EquipmentView();
    }

    private void ShowDevices()
    {
        MainContent.Content = new NetworkDevicesView();
    }

    private void ShowReplacements()
    {
        MainContent.Content = new DeviceReplacementsView();
    }

    private void ShowHandover()
    {
        MainContent.Content = new ShiftHandoverView();
    }

    private void ShowReports()
    {
        MainContent.Content = new ReportsView();
    }

    private void ShowUsers()
    {
        MainContent.Content = new UserManagementView();
    }

    private void ShowAuditLog()
    {
        MainContent.Content = new AuditLogView();
    }

    private void ShowSettings()
    {
        MainContent.Content = new SettingsView();
    }

    private void ShowPerformanceAnalytics()
    {
        MainContent.Content = new PerformanceAnalyticsView();
    }

    private void ShowHelp()
    {
        MainContent.Content = new HelpView();
    }

    private async Task EnsureCurrentUserRegisteredAsync()
    {
        try
        {
            using var scope = App.ServiceProvider!.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IAppUserService>();
            var existingUser = await userService.GetByUsernameAsync(CurrentUser.Username);

            string fullName = CurrentUser.Username;
            string email = string.Empty;
            string phone = string.Empty;
            string section = string.Empty;

            try
            {
                var adService = scope.ServiceProvider.GetRequiredService<IActiveDirectoryService>();
                var adUser = await adService.FindUserByUsernameAsync(CurrentUser.Username);
                if (adUser != null)
                {
                    fullName = adUser.DisplayName ?? CurrentUser.Username;
                    email = adUser.Email ?? string.Empty;
                    phone = adUser.Phone ?? string.Empty;
                    section = adUser.Department ?? adUser.Office ?? string.Empty;
                }
            }
            catch
            {
                // Ignore AD lookup failures and fall back to the local OS identity.
            }

            if (existingUser != null)
            {
                if (string.IsNullOrWhiteSpace(existingUser.FullName) || existingUser.FullName == existingUser.Username)
                {
                    existingUser.FullName = fullName;
                    existingUser.Email = string.IsNullOrWhiteSpace(existingUser.Email) ? email : existingUser.Email;
                    existingUser.Phone = string.IsNullOrWhiteSpace(existingUser.Phone) ? phone : existingUser.Phone;
                    existingUser.Section = string.IsNullOrWhiteSpace(existingUser.Section) ? section : existingUser.Section;
                    await userService.UpdateAsync(existingUser, CurrentUser.Username);
                }

                CurrentUser.Apply(existingUser);
                return;
            }

            var newUser = new AppUser
            {
                Username = CurrentUser.Username,
                FullName = fullName,
                Email = email,
                Phone = phone,
                Section = section,
                Role = UserRole.SupportSpecialist,
                IsActive = true,
                PasswordHash = PasswordHasher.Hash("P@ssw0rd!"),
                LastPasswordChangeDate = DateTime.Now
            };

            await userService.CreateAsync(newUser, "System");
            CurrentUser.Apply(newUser);
        }
        catch
        {
            CurrentUser.Apply(null);
        }
    }
}
