using System.Collections.ObjectModel;
using System.Windows;
using Aegis.Commands;
using Aegis.Models;
using Aegis.Services;
using Aegis.Services.Repositories;

namespace Aegis.ViewModels;

public class ReceptionViewModel : ViewModelBase
{
    private readonly ParkingDisplayModel _parking;
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly ITariffRepository _tariffRepository;
    private readonly ISpotRepository _spotRepository;
    private readonly IParkingRepository _parkingRepository;
    private readonly IReceptionRepository _receptionRepository;

    private string _title = "Оформление приемки";
    private string _operatorName = string.Empty;
    private string _parkingAddress = string.Empty;
    private string _licensePlate = string.Empty;
    private int _selectedVehicleTypeId;
    private ObservableCollection<VehicleTypeItem> _vehicleTypes = new();
    private VehicleTypeItem? _selectedVehicleType;
    private ObservableCollection<SpotDisplayModel> _freeSpots = new();
    private SpotDisplayModel? _selectedSpot;
    private decimal _currentTariff;
    private DateTime _admissionDate = DateTime.Now;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string OperatorName
    {
        get => _operatorName;
        set => SetProperty(ref _operatorName, value);
    }

    public string ParkingAddress
    {
        get => _parkingAddress;
        set => SetProperty(ref _parkingAddress, value);
    }

    public string LicensePlate
    {
        get => _licensePlate;
        set => SetProperty(ref _licensePlate, value);
    }

    public ObservableCollection<VehicleTypeItem> VehicleTypes
    {
        get => _vehicleTypes;
        set => SetProperty(ref _vehicleTypes, value);
    }

    public VehicleTypeItem? SelectedVehicleType
    {
        get => _selectedVehicleType;
        set
        {
            if (SetProperty(ref _selectedVehicleType, value))
            {
                LoadFreeSpotsCommand.Execute(null);
            }
        }
    }

    public ObservableCollection<SpotDisplayModel> FreeSpots
    {
        get => _freeSpots;
        set => SetProperty(ref _freeSpots, value);
    }

    public SpotDisplayModel? SelectedSpot
    {
        get => _selectedSpot;
        set => SetProperty(ref _selectedSpot, value);
    }

    public decimal CurrentTariff
    {
        get => _currentTariff;
        set => SetProperty(ref _currentTariff, value);
    }

    public DateTime AdmissionDate
    {
        get => _admissionDate;
        set => SetProperty(ref _admissionDate, value);
    }

    public string AdmissionDateText => _admissionDate.ToString("dd.MM.yyyy HH:mm");

    public RelayCommand LoadVehicleTypesCommand { get; }
    public RelayCommand LoadFreeSpotsCommand { get; }
    public RelayCommand SaveReceptionCommand { get; }
    public RelayCommand CancelCommand { get; }

    public ReceptionViewModel(
        ParkingDisplayModel parking,
        IAuthService authService,
        INavigationService navigationService,
        ITariffRepository tariffRepository,
        ISpotRepository spotRepository,
        IParkingRepository parkingRepository,
        IReceptionRepository receptionRepository)
    {
        _parking = parking;
        _authService = authService;
        _navigationService = navigationService;
        _tariffRepository = tariffRepository;
        _spotRepository = spotRepository;
        _parkingRepository = parkingRepository;
        _receptionRepository = receptionRepository;

        LoadVehicleTypesCommand = new RelayCommand(async _ => await LoadVehicleTypesAsync());
        LoadFreeSpotsCommand = new RelayCommand(async _ => await LoadFreeSpotsAsync());
        SaveReceptionCommand = new RelayCommand(async _ => await SaveReceptionAsync());
        CancelCommand = new RelayCommand(_ => Cancel());

        Initialize();
    }

    private void Initialize()
    {
        if (_authService.CurrentUser != null)
        {
            OperatorName = _authService.CurrentUser.FullName ?? _authService.CurrentUser.Login;
        }

        ParkingAddress = _parking.Address;
        LoadVehicleTypesCommand.Execute(null);
    }

    private async Task LoadVehicleTypesAsync()
    {
        using var connection = new Npgsql.NpgsqlConnection("Host=localhost;Database=Aegis;Username=postgres;Password=12345");
        await connection.OpenAsync();

        using var command = new Npgsql.NpgsqlCommand(
            "SELECT id, type FROM \"vehicletypes\" ORDER BY id",
            connection);

        using var reader = await command.ExecuteReaderAsync();

        VehicleTypes.Clear();
        while (await reader.ReadAsync())
        {
            VehicleTypes.Add(new VehicleTypeItem
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            });
        }

        if (VehicleTypes.Any())
        {
            SelectedVehicleType = VehicleTypes.First();
        }
    }

    private async Task LoadFreeSpotsAsync()
    {
        if (SelectedVehicleType == null) return;

        var spots = await _spotRepository.GetFreeSpotsAsync(_parking.ParkingId, SelectedVehicleType.Id);

        FreeSpots.Clear();
        foreach (var spot in spots)
        {
            FreeSpots.Add(spot);
        }

        if (FreeSpots.Any())
        {
            SelectedSpot = FreeSpots.First();
        }

        CurrentTariff = await _tariffRepository.GetTariffAsync(_parking.ParkingId, SelectedVehicleType.Id);
        OnPropertyChanged(nameof(AdmissionDateText));
    }

    private async Task SaveReceptionAsync()
    {
        if (string.IsNullOrWhiteSpace(LicensePlate))
        {
            MessageBox.Show("Введите государственный номер!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (SelectedSpot == null || SelectedVehicleType == null)
        {
            MessageBox.Show("Выберите место и тип автомобиля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_authService.CurrentUser == null)
        {
            MessageBox.Show("Пользователь не авторизован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            // Получаем ID статуса "На стоянке"
            int vehicleStatusId = 1; // По умолчанию

            // Создаём данные для сохранения
            var receptionData = new ReceptionData
            {
                LicensePlate = LicensePlate,
                SpotId = SelectedSpot.Id,
                VehicleTypeId = SelectedVehicleType.Id,
                OperatorId = _authService.CurrentUser.Id,
                VehicleId = null, // Будет найден или создан в репозитории
                VehicleStatusId = vehicleStatusId,
                AdmissionDate = DateTime.Now
            };

            // Сохраняем в БД
            var parkingRecordId = await _receptionRepository.CreateReceptionAsync(receptionData);

            MessageBox.Show(
                $"Автомобиль {LicensePlate} принят на парковку!\n" +
                $"Место: {SelectedSpot.Number}\n" +
                $"Тариф: {CurrentTariff} ₽/час\n" +
                $"ID записи: {parkingRecordId}",
                "Успех",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Cancel();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Ошибка при сохранении: {ex.Message}",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Cancel()
    {
        _navigationService.NavigateTo<DashboardViewModel>();
    }
}

public class VehicleTypeItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}