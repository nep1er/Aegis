using Aegis.Commands;
using Aegis.Data.Entities;
using Aegis.Models;
using Aegis.Services;
using Aegis.Services.Repositories;

namespace Aegis.ViewModels;

public class MainViewModel : ViewModelBase
{
    private ViewModelBase? _currentViewModel;
    private string _currentUserName = string.Empty;
    private string _currentUserRole = string.Empty;
    private bool _isLoggedIn;
    private bool _isAdmin;
    private ParkingDisplayModel? _currentParking;  // ← ОБЪЯВЛЕНО ОДИН РАЗ

    private readonly INavigationService _navigationService;
    private readonly IAuthService _authService;
    private readonly IReleaseRepository _releaseRepository;   // ← ДОБАВЛЕНО
    private readonly ITariffRepository _tariffRepository;     // ← ДОБАВЛЕНО

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

    public bool IsAdmin
    {
        get => _isAdmin;
        set => SetProperty(ref _isAdmin, value);
    }

    public ParkingDisplayModel? CurrentParking
    {
        get => _currentParking;
        set => SetProperty(ref _currentParking, value);
    }

    // Команды навигации для оператора
    public RelayCommand NavigateToDashboardCommand { get; }
    public RelayCommand NavigateToReceptionCommand { get; }
    public RelayCommand NavigateToReleaseCommand { get; }
    public RelayCommand NavigateToHistoryCommand { get; }

    // Команды навигации для админа
    public RelayCommand NavigateToAdminDashboardCommand { get; }
    public RelayCommand NavigateToEmployeesCommand { get; }
    public RelayCommand NavigateToParkingEditorCommand { get; }
    public RelayCommand NavigateToAnalyticsCommand { get; }
    public RelayCommand NavigateToAdminHistoryCommand { get; }
    public RelayCommand NavigateToPaymentsHistoryCommand { get; }

    public RelayCommand LogoutCommand { get; }

    public MainViewModel(
        INavigationService navigationService,
        IAuthService authService,
        IReleaseRepository releaseRepository,   // ← ДОБАВЛЕНО
        ITariffRepository tariffRepository)     // ← ДОБАВЛЕНО
    {
        _navigationService = navigationService;
        _authService = authService;
        _releaseRepository = releaseRepository;   // ← ДОБАВЛЕНО
        _tariffRepository = tariffRepository;     // ← ДОБАВЛЕНО

        // Команды оператора
        NavigateToDashboardCommand = new RelayCommand(_ => _navigationService.NavigateTo<DashboardViewModel>());
        NavigateToReceptionCommand = new RelayCommand(_ => _navigationService.NavigateTo<ReceptionViewModel>());
        NavigateToReleaseCommand = new RelayCommand(_ => NavigateToRelease(), _ => CanNavigateToRelease());
        NavigateToHistoryCommand = new RelayCommand(_ => _navigationService.NavigateTo<HistoryViewModel>());

        // Команды админа
        NavigateToAdminDashboardCommand = new RelayCommand(_ => _navigationService.NavigateTo<AdminDashboardViewModel>());
        NavigateToEmployeesCommand = new RelayCommand(_ => _navigationService.NavigateTo<EmployeesViewModel>());
        NavigateToParkingEditorCommand = new RelayCommand(_ => _navigationService.NavigateTo<ParkingEditorViewModel>());
        NavigateToAnalyticsCommand = new RelayCommand(_ => _navigationService.NavigateTo<AnalyticsViewModel>());
        NavigateToAdminHistoryCommand = new RelayCommand(_ => _navigationService.NavigateTo<AdminHistoryViewModel>());
        NavigateToPaymentsHistoryCommand = new RelayCommand(_ => _navigationService.NavigateTo<PaymentsHistoryViewModel>());

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
            IsAdmin = _authService.CurrentUser.Role?.Name == "Администратор";

            if (IsAdmin)
            {
                _navigationService.NavigateTo<AdminDashboardViewModel>();
            }
            else
            {
                _navigationService.NavigateTo<DashboardViewModel>();
            }
        }
    }

    private void Logout()
    {
        _authService.Logout();
        CurrentUserName = string.Empty;
        CurrentUserRole = string.Empty;
        CurrentParking = null;
        ShowLogin();
    }

    private bool CanNavigateToRelease()
    {
        return _currentParking != null;
    }

    private void NavigateToRelease()
    {
        if (_currentParking == null) return;

        var releaseVm = new ReleaseViewModel(
            _currentParking,
            _authService,
            _navigationService,
            _releaseRepository,
            _tariffRepository);

        _navigationService.NavigateTo(releaseVm);
    }

    public void SetCurrentParking(ParkingDisplayModel? parking)
    {
        CurrentParking = parking;
    }
}