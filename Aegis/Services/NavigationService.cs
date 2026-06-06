using Aegis.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Services;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private MainViewModel? _mainViewModel;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void SetMainViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        if (_mainViewModel == null)
            throw new InvalidOperationException("MainViewModel не установлен!");

        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        _mainViewModel.CurrentViewModel = viewModel;
    }
}