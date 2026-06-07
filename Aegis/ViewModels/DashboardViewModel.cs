using System.Collections.ObjectModel;
using Aegis.Commands;
using Aegis.Models;
using Aegis.Services;
using Aegis.Services.Repositories;

namespace Aegis.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private ObservableCollection<ParkingDisplayModel> _parkings = new();
    private ParkingDisplayModel? _selectedParking;
    private ParkingDisplayModel? _currentParking;
    private ObservableCollection<SpotDisplayModel> _spots = new();
    private ObservableCollection<TariffInfo> _tariffs = new();
    private string _title = "Парковки";

    private readonly IParkingRepository _parkingRepository;
    private readonly ITariffRepository _tariffRepository;
    private readonly ISpotRepository _spotRepository;
    private readonly IReceptionRepository _receptionRepository;  // ← ДОБАВЛЕНО
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;

    public ObservableCollection<ParkingDisplayModel> Parkings
    {
        get => _parkings;
        set => SetProperty(ref _parkings, value);
    }

    public ParkingDisplayModel? SelectedParking
    {
        get => _selectedParking;
        set
        {
            if (SetProperty(ref _selectedParking, value))
            {
                _currentParking = value;
                // Обновляем в MainViewModel
                if (_navigationService is NavigationService navService)
                {
                    navService.SetCurrentParking(value);
                }
                LoadSpotsCommand.Execute(null);
                LoadTariffsCommand.Execute(null);
            }
        }
    }

    public ParkingDisplayModel? CurrentParking
    {
        get => _currentParking;
        private set => SetProperty(ref _currentParking, value);
    }

    public ObservableCollection<SpotDisplayModel> Spots
    {
        get => _spots;
        set => SetProperty(ref _spots, value);
    }

    public ObservableCollection<TariffInfo> Tariffs
    {
        get => _tariffs;
        set => SetProperty(ref _tariffs, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public RelayCommand LoadParkingsCommand { get; }
    public RelayCommand LoadSpotsCommand { get; }
    public RelayCommand LoadTariffsCommand { get; }
    public RelayCommand NavigateToReceptionCommand { get; }

    public DashboardViewModel(
        IParkingRepository parkingRepository,
        ITariffRepository tariffRepository,
        ISpotRepository spotRepository,
        IReceptionRepository receptionRepository,  // ← ДОБАВЛЕНО
        IAuthService authService,
        INavigationService navigationService)
    {
        _parkingRepository = parkingRepository;
        _tariffRepository = tariffRepository;
        _spotRepository = spotRepository;
        _receptionRepository = receptionRepository;  // ← ДОБАВЛЕНО
        _authService = authService;
        _navigationService = navigationService;

        LoadParkingsCommand = new RelayCommand(async _ => await LoadParkingsAsync());
        LoadSpotsCommand = new RelayCommand(async _ => await LoadSpotsAsync());
        LoadTariffsCommand = new RelayCommand(async _ => await LoadTariffsAsync());
        NavigateToReceptionCommand = new RelayCommand(_ => NavigateToReception(), _ => CanNavigateToReception());

        LoadParkingsCommand.Execute(null);
    }

    private async Task LoadParkingsAsync()
    {
        if (_authService.CurrentUser == null) return;

        var parkings = await _parkingRepository.GetParkingsForOperatorAsync(_authService.CurrentUser.Id);

        Parkings.Clear();
        foreach (var parking in parkings)
        {
            Parkings.Add(parking);
        }

        if (Parkings.Any())
        {
            SelectedParking = Parkings.First();
        }
    }

    private async Task LoadSpotsAsync()
    {
        if (SelectedParking == null) return;

        var spots = await _parkingRepository.GetSpotsForParkingAsync(SelectedParking.ParkingId);

        Spots.Clear();
        foreach (var spot in spots)
        {
            Spots.Add(spot);
        }
    }

    private async Task LoadTariffsAsync()
    {
        if (SelectedParking == null) return;

        var tariffs = await _tariffRepository.GetTariffsForParkingAsync(SelectedParking.ParkingId);

        Tariffs.Clear();
        foreach (var tariff in tariffs)
        {
            Tariffs.Add(tariff);
        }
    }

    private bool CanNavigateToReception()
    {
        return _currentParking != null;
    }



    private void NavigateToReception()
    {
        if (_currentParking == null) return;

        // Передаём все зависимости через конструктор
        var receptionVm = new ReceptionViewModel(
            _currentParking,
            _authService,
            _navigationService,
            _tariffRepository,
            _spotRepository,           // ← используем инжектированный, а не новый!
            _parkingRepository,
            _receptionRepository);     // ← ДОБАВЛЕНО

        _navigationService.NavigateTo(receptionVm);
    }
}