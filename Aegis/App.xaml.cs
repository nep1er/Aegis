using System.Windows;
using Aegis.Data;
using Aegis.Services;
using Aegis.ViewModels;
using Aegis.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis;

public partial class App : Application
{
    public static IServiceProvider? ServiceProvider { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        services.AddSingleton<AppDbContext>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ReceptionViewModel>();
        services.AddTransient<ReleaseViewModel>();
        services.AddTransient<HistoryViewModel>();

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