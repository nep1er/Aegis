using Aegis.Commands;
using Aegis.Services;

namespace Aegis.ViewModels;

public class MainViewModel : ViewModelBase
{
    private ViewModelBase? _currentViewModel;
    private string _currentUserName = string.Empty;
    private string _currentUserRole = string.Empty;
    private bool _isLoggedIn;

    private readonly INavigationService _navigationService;
    private readonly IAuthService _authService;

    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    public string CurrentUserName
    {
        get => _currentUserName;
        set => SetProperty(ref _currentUserName, value);
    }

    public string CurrentUserRole
    {
        get => _currentUserRole;
        set => SetProperty(ref _currentUserRole, value);
    }

    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        set => SetProperty(ref _isLoggedIn, value);
    }

    // Команды навигации
    public RelayCommand NavigateToDashboardCommand { get; }
    public RelayCommand NavigateToReceptionCommand { get; }
    public RelayCommand NavigateToReleaseCommand { get; }
    public RelayCommand NavigateToHistoryCommand { get; }
    public RelayCommand LogoutCommand { get; }

    public MainViewModel(INavigationService navigationService, IAuthService authService)
    {
        _navigationService = navigationService;
        _authService = authService;

        NavigateToDashboardCommand = new RelayCommand(_ => _navigationService.NavigateTo<DashboardViewModel>());
        NavigateToReceptionCommand = new RelayCommand(_ => _navigationService.NavigateTo<ReceptionViewModel>());
        NavigateToReleaseCommand = new RelayCommand(_ => _navigationService.NavigateTo<ReleaseViewModel>());
        NavigateToHistoryCommand = new RelayCommand(_ => _navigationService.NavigateTo<HistoryViewModel>());
        LogoutCommand = new RelayCommand(_ => Logout());
    }

    public void Initialize()
    {
        ShowLogin();
    }

    private void ShowLogin()
    {
        IsLoggedIn = false;
        var loginVm = new LoginViewModel(_authService, OnLoginSuccess);
        CurrentViewModel = loginVm;
    }

    private void OnLoginSuccess()
    {
        if (_authService.CurrentUser != null)
        {
            CurrentUserName = _authService.CurrentUser.FullName ?? _authService.CurrentUser.Login;
            CurrentUserRole = _authService.CurrentUser.Role?.Name ?? "Неизвестно";
            IsLoggedIn = true;
            _navigationService.NavigateTo<DashboardViewModel>();
        }
    }

    private void Logout()
    {
        _authService.Logout();
        CurrentUserName = string.Empty;
        CurrentUserRole = string.Empty;
        ShowLogin();
    }
}