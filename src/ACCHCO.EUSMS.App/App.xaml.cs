using System.Data.Common;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Messaging;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ACCHCO.EUSMS.App.Helpers;
using ACCHCO.EUSMS.App.ViewModels;
using ACCHCO.EUSMS.Data.Context;
using ACCHCO.EUSMS.Data.Entities;
using ACCHCO.EUSMS.Data.Repositories;
using ACCHCO.EUSMS.Reports.Csv;
using ACCHCO.EUSMS.Reports.Excel;
using ACCHCO.EUSMS.Reports.Pdf;
using ACCHCO.EUSMS.Services.Implementations;
using ACCHCO.EUSMS.Services.Interfaces;

namespace ACCHCO.EUSMS.App;

public partial class App : Application
{
    public static IServiceProvider? ServiceProvider { get; private set; }

    public App()
    {
        LiveCharts.Configure(config =>
            config.HasGlobalSKTypeface(SKTypeface.FromFamilyName("Segoe UI")));
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(Path.Combine(logDirectory, "log-.txt"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("Application Starting...");

        DispatcherUnhandledException += (s, args) =>
        {
            Log.Error(args.Exception, "Unhandled exception occurred");
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                Log.Error(ex, "Unhandled AppDomain exception occurred");
        };
        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            Log.Error(args.Exception, "Unobserved task exception occurred");
            args.SetObserved();
        };

        try
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .Build();

            var connectionString = config.GetConnectionString("DefaultConnection")
                ?? "Data Source=ACCHCO_EUSMS.db";
            
            connectionString = RootSqliteDataSource(connectionString);
            if (!connectionString.Contains("Pooling=", StringComparison.OrdinalIgnoreCase))
                connectionString += ";Pooling=False";
            var dbPath = GetSqliteDataSourcePath(connectionString);
            Log.Information("Database path: {DbPath}", dbPath);

            var services = new ServiceCollection();

            services.AddDbContext<EusmsDbContext>(options =>
                options.UseSqlite(connectionString)
                       .AddInterceptors(new SqliteBusyTimeoutInterceptor()));

            // Repositories
            services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
            services.AddTransient<ITicketRepository, TicketRepository>();
            services.AddTransient<IEquipmentRepository, EquipmentRepository>();
            services.AddTransient<IUserRepository, UserRepository>();
            services.AddTransient<ISettingRepository, SettingRepository>();
            services.AddTransient<IShiftHandoverRepository, ShiftHandoverRepository>();
            services.AddTransient<IAuditLogRepository, AuditLogRepository>();

            // Services
            services.AddSingleton(sp => new DbChangeTracker(dbPath));
            services.AddTransient<IAppUserService, AppUserService>();
            services.AddTransient<IAuditService, AuditService>();
            services.AddTransient<ITicketService, TicketService>();
            services.AddTransient<IEquipmentService, EquipmentService>();
            services.AddTransient<IShiftHandoverService, ShiftHandoverService>();
            services.AddTransient<ISettingService, SettingService>();
            services.AddTransient<IDashboardService, DashboardService>();
            services.AddTransient<IActiveDirectoryService, ActiveDirectoryService>();
            services.AddTransient<INetworkDiscoveryService, NetworkDiscoveryService>();
            services.AddTransient<IIncidentService, IncidentService>();
            services.AddTransient<IChangeRequestService, ChangeRequestService>();
            services.AddTransient<IProblemService, ProblemService>();
            services.AddTransient<IKnownErrorService, KnownErrorService>();
            services.AddTransient<IConfigurationItemService, ConfigurationItemService>();
            services.AddTransient<IServiceRequestService, ServiceRequestService>();
            services.AddTransient<ISlaService, SlaService>();
            services.AddTransient<IEscalationService, EscalationService>();
            services.AddTransient<INotificationService, NotificationService>();
            services.AddTransient<IItsmAnalyticsService, ItsmAnalyticsService>();

            // Report generators
            services.AddTransient<PdfReportGenerator>();
            services.AddTransient<ExcelReportGenerator>();
            services.AddTransient<CsvReportGenerator>();

            // ViewModels (Singletons for Messaging)
            services.AddSingleton<DashboardViewModel>();
            services.AddSingleton<TicketListViewModel>();
            services.AddSingleton<EquipmentViewModel>();
            
            // ViewModels (Transients)
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<TicketDetailViewModel>();
            services.AddTransient<DeviceReplacementsViewModel>();
            services.AddTransient<NetworkDevicesViewModel>();
            services.AddTransient<ShiftHandoverViewModel>();
            services.AddTransient<AuditLogViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<HelpViewModel>();
            services.AddTransient<UserManagementViewModel>();
            services.AddTransient<ReportsViewModel>();
            services.AddTransient<PerformanceAnalyticsViewModel>();
            services.AddTransient<IncidentManagementViewModel>();
            services.AddTransient<ChangeManagementViewModel>();
            services.AddTransient<ProblemManagementViewModel>();
            services.AddTransient<KnownErrorViewModel>();
            services.AddTransient<CmdbViewModel>();
            services.AddTransient<ServiceRequestViewModel>();
            services.AddTransient<ItsmDashboardViewModel>();
            services.AddTransient<ItsmAnalyticsViewModel>();

            ServiceProvider = services.BuildServiceProvider();

            InitializeDatabaseAsync().GetAwaiter().GetResult();

            var changeTracker = ServiceProvider.GetRequiredService<DbChangeTracker>();
            var syncing = false;
            var lastSendTime = DateTime.MinValue;
            var syncTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            syncTimer.Tick += async (s, e) =>
            {
                if (syncing) return;
                syncing = true;
                try
                {
                    var changed = await Task.Run(changeTracker.HasChanged);
                    if (changed && (DateTime.UtcNow - lastSendTime).TotalMilliseconds >= 1000)
                    {
                        lastSendTime = DateTime.UtcNow;
                        WeakReferenceMessenger.Default.Send(new DataChangedMessage());
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "Sync poll failed");
                }
                finally
                {
                    syncing = false;
                }
            };
            syncTimer.Start();

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = ServiceProvider.CreateScope();
                    var ad = scope.ServiceProvider.GetRequiredService<IActiveDirectoryService>();
                    if (ad.IsAvailable())
                    {
                        var users = await ad.SyncUsersAsync();
                        var computers = await ad.SyncComputersAsync();
                        Log.Information($"Auto AD sync completed: {users.Count()} users, {computers.Count()} computers");
                    }
                }
                catch (Exception ex) { Log.Error(ex, "Auto AD sync failed"); }
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Startup initialization failed");
            MessageBox.Show($"Failed to initialize the application.\n\n{ex.Message}", "Application startup error", MessageBoxButton.OK, MessageBoxImage.Error);
            Current?.Shutdown();
        }
    }

    private static string RootSqliteDataSource(string connectionString)
    {
        var result = new List<string>();
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = part.IndexOf('=');
            if (eq > 0 && part[..eq].Trim().Equals("Data Source", StringComparison.OrdinalIgnoreCase))
            {
                var path = part[(eq + 1)..].Trim();
                if (!string.IsNullOrWhiteSpace(path) && !Path.IsPathRooted(path))
                    path = Path.Combine(AppContext.BaseDirectory, path);
                result.Add("Data Source=" + path);
            }
            else
            {
                result.Add(part);
            }
        }
        return string.Join(";", result);
    }

    private static string GetSqliteDataSourcePath(string connectionString)
    {
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = part.IndexOf('=');
            if (eq > 0 && part[..eq].Trim().Equals("Data Source", StringComparison.OrdinalIgnoreCase))
            {
                var path = part[(eq + 1)..].Trim();
                if (!string.IsNullOrWhiteSpace(path) && !Path.IsPathRooted(path))
                    path = Path.Combine(AppContext.BaseDirectory, path);
                return path;
            }
        }
        return Path.Combine(AppContext.BaseDirectory, "ACCHCO_EUSMS.db");
    }

    private static async Task InitializeDatabaseAsync()
    {
        using var scope = ServiceProvider!.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EusmsDbContext>();

        await context.Database.EnsureCreatedAsync();
        await EnsureUserSchemaAsync(context);

        try
        {
            await context.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout = 5000;");
            await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=DELETE;");
        }
        catch
        {
            // Journal mode is optional and should not block startup.
        }

        SyncStampStore.EnsureTable(context.Database.GetDbConnection().ConnectionString);
        SequenceNumberStore.EnsureTable(context.Database.GetDbConnection().ConnectionString);

        await SeedEquipmentAsync(scope.ServiceProvider.GetRequiredService<IEquipmentRepository>());
        await scope.ServiceProvider.GetRequiredService<ISettingService>().InitializeDefaultSettingsAsync();
    }

    private static async Task EnsureUserSchemaAsync(EusmsDbContext context)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(\"Users\");";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(1));
        }

        if (!columns.Contains("PasswordHash"))
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE \"Users\" ADD COLUMN \"PasswordHash\" TEXT NOT NULL DEFAULT '';");
        }

        if (!columns.Contains("LastPasswordChangeDate"))
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE \"Users\" ADD COLUMN \"LastPasswordChangeDate\" TEXT NULL;");
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        Log.CloseAndFlush();
    }

    private static async Task SeedEquipmentAsync(IEquipmentRepository repo)
    {
        var equipmentData = new Dictionary<string, (EquipmentType Type, string IpAddress)>
        {
            // Access Points (AP) - Subnet 172.17.10
            ["AP-50-Roof"] = (EquipmentType.AccessPoint, "172.17.10.50"),
            ["AP-51-DG-CFS"] = (EquipmentType.AccessPoint, "172.17.10.51"),
            ["AP-52-C10"] = (EquipmentType.AccessPoint, "172.17.10.52"),
            ["AP-53-T"] = (EquipmentType.AccessPoint, "172.17.10.53"),
            ["AP-54-RoRo"] = (EquipmentType.AccessPoint, "172.17.10.54"),
            ["AP-55-C8"] = (EquipmentType.AccessPoint, "172.17.10.55"),
            ["AP-56-CFS"] = (EquipmentType.AccessPoint, "172.17.10.56"),
            ["AP-57-E"] = (EquipmentType.AccessPoint, "172.17.10.57"),
            ["AP-58-XRAY"] = (EquipmentType.AccessPoint, "172.17.10.58"),
            ["AP-59-C12"] = (EquipmentType.AccessPoint, "172.17.10.59"),
            ["AP-60-FR1"] = (EquipmentType.AccessPoint, "172.17.10.60"),
            ["AP-61-FR2"] = (EquipmentType.AccessPoint, "172.17.10.61"),
            ["AP-62-Roof"] = (EquipmentType.AccessPoint, "172.17.10.62"),
            ["AP-63-E"] = (EquipmentType.AccessPoint, "172.17.10.63"),
            ["AP-64-T"] = (EquipmentType.AccessPoint, "172.17.10.64"),

            // RTG - Subnet 172.17.70
            ["RTG-1"] = (EquipmentType.RTG, "172.17.70.101"),
            ["RTG-2"] = (EquipmentType.RTG, "172.17.70.102"),
            ["RTG-3"] = (EquipmentType.RTG, "172.17.70.103"),
            ["RTG-4"] = (EquipmentType.RTG, "172.17.70.104"),
            ["RTG-5"] = (EquipmentType.RTG, "172.17.70.105"),
            ["RTG-6"] = (EquipmentType.RTG, "172.17.70.106"),
            ["RTG-7"] = (EquipmentType.RTG, "172.17.70.107"),
            ["RTG-8"] = (EquipmentType.RTG, "172.17.70.108"),
            ["RTG-9"] = (EquipmentType.RTG, "172.17.70.109"),
            ["RTG-10"] = (EquipmentType.RTG, "172.17.70.110"),
            ["RTG-11"] = (EquipmentType.RTG, "172.17.70.111"),
            ["RTG-12"] = (EquipmentType.RTG, "172.17.70.112"),
            ["RTG-13"] = (EquipmentType.RTG, "172.17.70.113"),
            ["RTG-14"] = (EquipmentType.RTG, "172.17.70.114"),
            ["RTG-15"] = (EquipmentType.RTG, "172.17.70.115"),
            ["RTG-16"] = (EquipmentType.RTG, "172.17.70.116"),
            ["RTG-17"] = (EquipmentType.RTG, "172.17.70.117"),
            ["RTG-18"] = (EquipmentType.RTG, "172.17.70.118"),
            ["RTG-19"] = (EquipmentType.RTG, "172.17.70.119"),
            ["RTG-20"] = (EquipmentType.RTG, "172.17.70.120"),

            // Calmar (CLMR) - Subnet 172.17.70
            ["CLMR-1"] = (EquipmentType.Kalmar, "172.17.70.121"),
            ["CLMR-2"] = (EquipmentType.Kalmar, "172.17.70.122"),
            ["CLMR-3"] = (EquipmentType.Kalmar, "172.17.70.123"),
            ["CLMR-4"] = (EquipmentType.Kalmar, "172.17.70.124"),
            ["CLMR-5"] = (EquipmentType.Kalmar, "172.17.70.125"),
            ["CLMR-6"] = (EquipmentType.Kalmar, "172.17.70.126"),
            ["CLMR-7"] = (EquipmentType.Kalmar, "172.17.70.127"),
            ["CLMR-8"] = (EquipmentType.Kalmar, "172.17.70.128"),
            ["CLMR-9"] = (EquipmentType.Kalmar, "172.17.70.129"),
            ["CLMR-15"] = (EquipmentType.Kalmar, "172.17.70.130"),
            ["CLMR-16"] = (EquipmentType.Kalmar, "172.17.70.131"),
            ["CLMR-17"] = (EquipmentType.Kalmar, "172.17.70.132"),
            ["CLMR-18"] = (EquipmentType.Kalmar, "172.17.70.133"),
            ["CLMR-19"] = (EquipmentType.Kalmar, "172.17.70.134"),
            ["CLMR-20"] = (EquipmentType.Kalmar, "172.17.70.135"),

            // Fantosy (RS) - Subnet 172.17.70
            ["RS-1"] = (EquipmentType.ReachStacker, "172.17.70.136"),
            ["RS-2"] = (EquipmentType.ReachStacker, "172.17.70.137"),
            ["RS-3"] = (EquipmentType.ReachStacker, "172.17.70.138"),
            ["RS-4"] = (EquipmentType.ReachStacker, "172.17.70.139"),

            // Haster (HSTR) - Subnet 172.17.70
            ["HSTR-1"] = (EquipmentType.Hyster, "172.17.70.141"),
            ["HSTR-2"] = (EquipmentType.Hyster, "172.17.70.142"),
            ["HSTR-3"] = (EquipmentType.Hyster, "172.17.70.143"),
            ["HSTR-4"] = (EquipmentType.Hyster, "172.17.70.144"),
            ["HSTR-5"] = (EquipmentType.Hyster, "172.17.70.147"),
            ["HSTR-10"] = (EquipmentType.Hyster, "172.17.70.145"),
            ["HSTR-11"] = (EquipmentType.Hyster, "172.17.70.146"),

            // RDT (GETAC) - Subnet 172.17.70
            ["GETAC-11"] = (EquipmentType.RDT, "172.17.70.11"),
            ["GETAC-12"] = (EquipmentType.RDT, "172.17.70.12"),
            ["GETAC-13"] = (EquipmentType.RDT, "172.17.70.13"),
            ["GETAC-14"] = (EquipmentType.RDT, "172.17.70.14"),
            ["GETAC-15"] = (EquipmentType.RDT, "172.17.70.15"),

            // RDT (NEWLAND) - Subnet 172.17.70
            ["NEWLAND-81"] = (EquipmentType.RDT, "172.17.70.81"),
            ["NEWLAND-82"] = (EquipmentType.RDT, "172.17.70.82"),
            ["NEWLAND-83"] = (EquipmentType.RDT, "172.17.70.83"),

            // RDT (EMDOOR) - Subnet 172.17.70
            ["EMDOOR-1"] = (EquipmentType.RDT, "172.17.70.16"),
            ["EMDOOR-2"] = (EquipmentType.RDT, "172.17.70.17"),
            ["EMDOOR-3"] = (EquipmentType.RDT, "172.17.70.18"),
            ["EMDOOR-4"] = (EquipmentType.RDT, "172.17.70.19"),
            ["EMDOOR-5"] = (EquipmentType.RDT, "172.17.70.20"),
            ["EMDOOR-6"] = (EquipmentType.RDT, "172.17.70.21"),
            ["EMDOOR-7"] = (EquipmentType.RDT, "172.17.70.22"),
            ["EMDOOR-8"] = (EquipmentType.RDT, "172.17.70.23"),
            ["EMDOOR-9"] = (EquipmentType.RDT, "172.17.70.24"),
            ["EMDOOR-10"] = (EquipmentType.RDT, "172.17.70.25"),
            ["EMDOOR-11"] = (EquipmentType.RDT, "172.17.70.26"),
            ["EMDOOR-12"] = (EquipmentType.RDT, "172.17.70.27"),
            ["EMDOOR-13"] = (EquipmentType.RDT, "172.17.70.28"),
            ["EMDOOR-14"] = (EquipmentType.RDT, "172.17.70.29"),
            ["EMDOOR-15"] = (EquipmentType.RDT, "172.17.70.30")
        };

        var existing = await repo.GetAllAsync();
        var existingDict = existing.ToDictionary(e => e.Name, e => e);

        foreach (var kvp in equipmentData)
        {
            var name = kvp.Key;
            var data = kvp.Value;

            if (existingDict.TryGetValue(name, out var existingEq))
            {
                if (existingEq.IpAddress != data.IpAddress)
                {
                    existingEq.IpAddress = data.IpAddress;
                    await repo.UpdateAsync(existingEq);
                }
            }
            else
            {
                await repo.AddAsync(new Equipment
                {
                    Name = name,
                    Type = data.Type,
                    IpAddress = data.IpAddress,
                    Status = EquipmentStatus.Active
                });
            }
        }
    }
}
