using Aegis.Commands;
using Aegis.Models;
using Aegis.Services;
using Aegis.Services.Repositories;
using System.Collections.ObjectModel;

namespace Aegis.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private ObservableCollection<ParkingDisplayModel> _parkings = new();
    private ParkingDisplayModel? _selectedParking;
    private ObservableCollection<SpotDisplayModel> _spots = new();
    private string _title = "Парковки";

    private readonly IParkingRepository _parkingRepository;
    private readonly IAuthService _authService;

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
                LoadSpotsCommand.Execute(null);
            }
        }
    }

    public ObservableCollection<SpotDisplayModel> Spots
    {
        get => _spots;
        set => SetProperty(ref _spots, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public RelayCommand LoadParkingsCommand { get; }
    public RelayCommand LoadSpotsCommand { get; }

    public DashboardViewModel(IParkingRepository parkingRepository, IAuthService authService)
    {
        _parkingRepository = parkingRepository;
        _authService = authService;

        LoadParkingsCommand = new RelayCommand(async _ => await LoadParkingsAsync());
        LoadSpotsCommand = new RelayCommand(async _ => await LoadSpotsAsync());

        // Загружаем данные при создании
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
}