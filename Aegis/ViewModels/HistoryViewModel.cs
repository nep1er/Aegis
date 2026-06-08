using System.Collections.ObjectModel;
using Aegis.Commands;
using Aegis.Models;
using Aegis.Services;
using Aegis.Services.Repositories;

namespace Aegis.ViewModels;

public class HistoryViewModel : ViewModelBase
{
    private readonly IHistoryRepository _historyRepository;
    private readonly IAuthService _authService;
    private readonly IParkingRepository _parkingRepository;

    private ObservableCollection<HistoryItemModel> _historyItems = new();
    private HistoryDetailsModel? _selectedDetails;
    private bool _isDetailsExpanded;

    // Фильтры
    private string _licensePlateFilter = string.Empty;
    private string _vinFilter = string.Empty;
    private DateTime _dateFrom = DateTime.Now.AddMonths(-1);
    private DateTime _dateTo = DateTime.Now;

    // Фильтр по парковке (для операторов)
    private ObservableCollection<ParkingDisplayModel> _parkings = new();
    private ParkingDisplayModel? _selectedParking;
    private bool _isOperator;

    public ObservableCollection<HistoryItemModel> HistoryItems
    {
        get => _historyItems;
        set => SetProperty(ref _historyItems, value);
    }

    public HistoryDetailsModel? SelectedDetails
    {
        get => _selectedDetails;
        set => SetProperty(ref _selectedDetails, value);
    }

    public bool IsDetailsExpanded
    {
        get => _isDetailsExpanded;
        set => SetProperty(ref _isDetailsExpanded, value);
    }

    public string LicensePlateFilter
    {
        get => _licensePlateFilter;
        set => SetProperty(ref _licensePlateFilter, value);
    }

    public string VinFilter
    {
        get => _vinFilter;
        set => SetProperty(ref _vinFilter, value);
    }

    public DateTime DateFrom
    {
        get => _dateFrom;
        set => SetProperty(ref _dateFrom, value);
    }

    public DateTime DateTo
    {
        get => _dateTo;
        set => SetProperty(ref _dateTo, value);
    }

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
                LoadHistoryCommand.Execute(null);
            }
        }
    }

    public bool IsOperator
    {
        get => _isOperator;
        set => SetProperty(ref _isOperator, value);
    }

    public RelayCommand LoadHistoryCommand { get; }
    public RelayCommand ClearFiltersCommand { get; }
    public RelayCommand ShowDetailsCommand { get; }
    public RelayCommand HideDetailsCommand { get; }

    public HistoryViewModel(
        IHistoryRepository historyRepository,
        IAuthService authService,
        IParkingRepository parkingRepository)
    {
        _historyRepository = historyRepository;
        _authService = authService;
        _parkingRepository = parkingRepository;

        LoadHistoryCommand = new RelayCommand(async _ => await LoadHistoryAsync());
        ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
        ShowDetailsCommand = new RelayCommand(async param =>
        {
            if (param is HistoryItemModel item)
                await ShowDetailsAsync(item);
        });
        HideDetailsCommand = new RelayCommand(_ => HideDetails());

        Initialize();
    }

    private void Initialize()
    {
        // Проверяем роль пользователя
        IsOperator = _authService.CurrentUser?.Role?.Name != "Администратор";

        if (IsOperator && _authService.CurrentUser != null)
        {
            // Загружаем парковки оператора
            LoadOperatorParkingsAsync();
        }
        else
        {
            // Для админа загружаем все парковки
            LoadAllParkingsAsync();
        }
    }

    private async Task LoadOperatorParkingsAsync()
    {
        var parkings = await _parkingRepository.GetParkingsForOperatorAsync(_authService.CurrentUser!.Id);
        Parkings.Clear();
        foreach (var p in parkings)
            Parkings.Add(p);

        if (Parkings.Any())
            SelectedParking = Parkings.First();
    }

    private async Task LoadAllParkingsAsync()
    {
        var parkings = await _parkingRepository.GetAllParkingsAsync();
        Parkings.Clear();
        foreach (var p in parkings)
            Parkings.Add(p);

        // Добавляем пункт "Все парковки"
        Parkings.Insert(0, new ParkingDisplayModel
        {
            ParkingId = 0,
            Address = "Все парковки"
        });

        SelectedParking = Parkings.First();
    }

    private async Task LoadHistoryAsync()
    {
        var filter = new HistoryFilter
        {
            LicensePlate = string.IsNullOrWhiteSpace(LicensePlateFilter) ? null : LicensePlateFilter,
            Vin = string.IsNullOrWhiteSpace(VinFilter) ? null : VinFilter,
            DateFrom = DateFrom,
            DateTo = DateTo,
            ParkingId = SelectedParking?.ParkingId == 0 ? null : SelectedParking?.ParkingId
        };

        var items = await _historyRepository.GetHistoryAsync(filter);

        HistoryItems.Clear();
        foreach (var item in items)
        {
            HistoryItems.Add(item);
        }
    }

    private async Task ShowDetailsAsync(HistoryItemModel item)
    {
        if (item == null) return;

        var details = await _historyRepository.GetHistoryDetailsAsync(item.ParkingRecordId);
        SelectedDetails = details;
        IsDetailsExpanded = true;
    }

    private void HideDetails()
    {
        SelectedDetails = null;
        IsDetailsExpanded = false;
    }

    private void ClearFilters()
    {
        LicensePlateFilter = string.Empty;
        VinFilter = string.Empty;
        DateFrom = DateTime.Now.AddMonths(-1);
        DateTo = DateTime.Now;
        LoadHistoryCommand.Execute(null);
    }
}