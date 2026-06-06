using Aegis.Commands;
using Aegis.Services;

namespace Aegis.ViewModels;

public class MainViewModel : ViewModelBase
{
    private ViewModelBase? _currentViewModel;
    private readonly INavigationService _navigationService;

    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    // Команды навигации
    public RelayCommand NavigateToDashboardCommand { get; }
    public RelayCommand NavigateToReceptionCommand { get; }
    public RelayCommand NavigateToReleaseCommand { get; }
    public RelayCommand NavigateToHistoryCommand { get; }

    public MainViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;

        // Инициализация команд
        NavigateToDashboardCommand = new RelayCommand(_ => _navigationService.NavigateTo<DashboardViewModel>());
        NavigateToReceptionCommand = new RelayCommand(_ => _navigationService.NavigateTo<ReceptionViewModel>());
        NavigateToReleaseCommand = new RelayCommand(_ => _navigationService.NavigateTo<ReleaseViewModel>());
        NavigateToHistoryCommand = new RelayCommand(_ => _navigationService.NavigateTo<HistoryViewModel>());
    }

    public void Initialize()
    {
        _navigationService.NavigateTo<DashboardViewModel>();
    }
}