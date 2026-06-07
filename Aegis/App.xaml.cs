using System.Windows;
using Aegis.Data;
using Aegis.Services;
using Aegis.ViewModels;
using Aegis.Views;
using Microsoft.Extensions.DependencyInjection;
using Aegis.Services.Repositories;
namespace Aegis;

public partial class App : Application
{
    public static IServiceProvider? ServiceProvider { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        // ⚠️ ВАЖНО: регистрируем БД и AuthService!
        services.AddSingleton<AppDbContext>();
        services.AddSingleton<IAuthService, AuthService>();

        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<MainViewModel>();

        // Оператор
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ReceptionViewModel>();
        services.AddTransient<ReleaseViewModel>();
        services.AddTransient<HistoryViewModel>();

        // Админ
        services.AddTransient<AdminDashboardViewModel>();
        services.AddTransient<EmployeesViewModel>();
        services.AddTransient<ParkingEditorViewModel>();
        services.AddTransient<AnalyticsViewModel>();
        services.AddTransient<AdminHistoryViewModel>();
        services.AddTransient<PaymentsHistoryViewModel>();

        services.AddSingleton<ITariffRepository, TariffRepository>();
        services.AddSingleton<ISpotRepository, SpotRepository>();
        services.AddSingleton<IParkingRepository, ParkingRepository>();
        services.AddSingleton<IReceptionRepository, ReceptionRepository>();

        services.AddSingleton<IParkingRepository, ParkingRepository>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<AppDbContext>();

        ServiceProvider = services.BuildServiceProvider();

        var mainViewModel = ServiceProvider.GetRequiredService<MainViewModel>();
        var navigationService = ServiceProvider.GetRequiredService<INavigationService>() as NavigationService;

        navigationService?.SetMainViewModel(mainViewModel);
        mainViewModel.Initialize();

        var mainWindow = new MainWindow
        {
            DataContext = mainViewModel
        };
        mainWindow.Show();
    }
}