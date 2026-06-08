using System.Collections.ObjectModel;
using LiveCharts;
using LiveCharts.Wpf;
using Aegis.Commands;
using Aegis.Models;
using Aegis.Services.Repositories;

namespace Aegis.ViewModels;

public class AnalyticsViewModel : ViewModelBase
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly IParkingRepository _parkingRepository;

    private DateTime _dateFrom = DateTime.Now.AddMonths(-6);
    private DateTime _dateTo = DateTime.Now;
    private ObservableCollection<ParkingDisplayModel> _parkings = new();
    private ParkingDisplayModel? _selectedParking;
    private ParkingDisplayModel? _selectedParkingForMonthlyChart;

    private ObservableCollection<MonthlyParkingRevenue> _monthlyRevenues = new();
    private ObservableCollection<ParkingRevenue> _parkingRevenues = new();
    private ObservableCollection<VehicleTypeStatistics> _vehicleTypeStats = new();
    private ObservableCollection<CityStatistics> _cityStats = new();
    private ObservableCollection<CityParkingCount> _cityParkingCounts = new();

    private ChartValues<double> _monthlyRevenueValues = new();
    private ChartValues<double> _parkingRevenueValues = new();
    private SeriesCollection _vehicleTypeSeries = new();
    private ChartValues<double> _cityRevenueValues = new();
    private ChartValues<double> _cityParkingCountValues = new();

    private string[] _monthLabels = Array.Empty<string>();
    private string[] _parkingLabels = Array.Empty<string>();
    private string[] _cityLabels = Array.Empty<string>();

    private ParkingStatistics _parkingStats = new();

    public DateTime DateFrom
    {
        get => _dateFrom;
        set { if (SetProperty(ref _dateFrom, value)) LoadAllDataCommand.Execute(null); }
    }

    public DateTime DateTo
    {
        get => _dateTo;
        set { if (SetProperty(ref _dateTo, value)) LoadAllDataCommand.Execute(null); }
    }

    public ObservableCollection<ParkingDisplayModel> Parkings
    {
        get => _parkings;
        set => SetProperty(ref _parkings, value);
    }

    public ParkingDisplayModel? SelectedParking
    {
        get => _selectedParking;
        set { if (SetProperty(ref _selectedParking, value)) LoadParkingStatisticsCommand.Execute(null); }
    }

    public ParkingDisplayModel? SelectedParkingForMonthlyChart
    {
        get => _selectedParkingForMonthlyChart;
        set { if (SetProperty(ref _selectedParkingForMonthlyChart, value)) LoadMonthlyRevenueAsync(); }
    }

    public ObservableCollection<MonthlyParkingRevenue> MonthlyRevenues
    {
        get => _monthlyRevenues;
        set => SetProperty(ref _monthlyRevenues, value);
    }

    public ObservableCollection<ParkingRevenue> ParkingRevenues
    {
        get => _parkingRevenues;
        set => SetProperty(ref _parkingRevenues, value);
    }

    public ObservableCollection<VehicleTypeStatistics> VehicleTypeStats
    {
        get => _vehicleTypeStats;
        set => SetProperty(ref _vehicleTypeStats, value);
    }

    public ObservableCollection<CityStatistics> CityStats
    {
        get => _cityStats;
        set => SetProperty(ref _cityStats, value);
    }

    public ObservableCollection<CityParkingCount> CityParkingCounts
    {
        get => _cityParkingCounts;
        set => SetProperty(ref _cityParkingCounts, value);
    }

    public ChartValues<double> MonthlyRevenueValues
    {
        get => _monthlyRevenueValues;
        set => SetProperty(ref _monthlyRevenueValues, value);
    }

    public ChartValues<double> ParkingRevenueValues
    {
        get => _parkingRevenueValues;
        set => SetProperty(ref _parkingRevenueValues, value);
    }

    public SeriesCollection VehicleTypeSeries
    {
        get => _vehicleTypeSeries;
        set => SetProperty(ref _vehicleTypeSeries, value);
    }

    public ChartValues<double> CityRevenueValues
    {
        get => _cityRevenueValues;
        set => SetProperty(ref _cityRevenueValues, value);
    }

    public ChartValues<double> CityParkingCountValues
    {
        get => _cityParkingCountValues;
        set => SetProperty(ref _cityParkingCountValues, value);
    }

    public string[] MonthLabels
    {
        get => _monthLabels;
        set => SetProperty(ref _monthLabels, value);
    }

    public string[] ParkingLabels
    {
        get => _parkingLabels;
        set => SetProperty(ref _parkingLabels, value);
    }

    public string[] CityLabels
    {
        get => _cityLabels;
        set => SetProperty(ref _cityLabels, value);
    }

    public ParkingStatistics ParkingStats
    {
        get => _parkingStats;
        set => SetProperty(ref _parkingStats, value);
    }

    public RelayCommand LoadAllDataCommand { get; }
    public RelayCommand LoadParkingStatisticsCommand { get; }

    public AnalyticsViewModel(IAnalyticsRepository analyticsRepository, IParkingRepository parkingRepository)
    {
        _analyticsRepository = analyticsRepository;
        _parkingRepository = parkingRepository;

        LoadAllDataCommand = new RelayCommand(async _ => await LoadAllDataAsync());
        LoadParkingStatisticsCommand = new RelayCommand(async _ => await LoadParkingStatisticsAsync());

        InitializeAsync();
    }
    public Func<double, string> FormatLabel { get; } = value => value.ToString("F2");
    private async void InitializeAsync()
    {
        var parkings = await _parkingRepository.GetAllParkingsAsync();
        Parkings.Clear();
        Parkings.Add(new ParkingDisplayModel { ParkingId = 0, Address = "Все парковки" });
        foreach (var p in parkings) Parkings.Add(p);

        SelectedParking = Parkings.First();
        SelectedParkingForMonthlyChart = Parkings.First();
        await LoadAllDataAsync();
    }

    private async Task LoadAllDataAsync()
    {
        await Task.WhenAll(
            LoadMonthlyRevenueAsync(),
            LoadParkingRevenueAsync(),
            LoadVehicleTypeStatisticsAsync(),
            LoadCityStatisticsAsync(),
            LoadCityParkingCountsAsync(),
            LoadParkingStatisticsAsync()
        );
    }

    private async Task LoadMonthlyRevenueAsync()
    {
        int? parkingId = SelectedParkingForMonthlyChart?.ParkingId == 0 ? null : SelectedParkingForMonthlyChart?.ParkingId;
        var data = await _analyticsRepository.GetMonthlyRevenueByParkingIdAsync(parkingId, DateFrom, DateTo);
        var list = data.ToList();

        MonthlyRevenues.Clear();
        foreach (var item in list) MonthlyRevenues.Add(item);

        MonthlyRevenueValues = new ChartValues<double>(list.Select(d => Math.Round((double)d.Revenue, 2)));
        MonthLabels = list.Select(d => $"{d.MonthName} {d.Year}").ToArray();
    }

    private async Task LoadParkingRevenueAsync()
    {
        var data = await _analyticsRepository.GetRevenueByParkingAsync(DateFrom, DateTo);
        var list = data.ToList();

        ParkingRevenues.Clear();
        foreach (var item in list) ParkingRevenues.Add(item);

        ParkingRevenueValues = new ChartValues<double>(list.Select(d => Math.Round((double)d.TotalRevenue, 2)));
        ParkingLabels = list.Select(d => d.ParkingAddress).ToArray();
    }

    private async Task LoadVehicleTypeStatisticsAsync()
    {
        var data = await _analyticsRepository.GetVehicleTypeStatisticsAsync(DateFrom, DateTo);
        var list = data.ToList();

        VehicleTypeStats.Clear();
        foreach (var item in list) VehicleTypeStats.Add(item);

        VehicleTypeSeries.Clear();
        if (!list.Any()) return;

        // ОТДЕЛЬНЫЙ PieSeries для каждого типа авто
        foreach (var item in list)
        {
            VehicleTypeSeries.Add(new PieSeries
            {
                Title = item.VehicleTypeName,
                Values = new ChartValues<double> { (double)item.Count },
                DataLabels = true,
                LabelPoint = point => $"{point.Y} ({point.Participation:P})"
            });
        }
    }

    private async Task LoadParkingStatisticsAsync()
    {
        var parkingId = SelectedParking?.ParkingId == 0 ? (int?)null : SelectedParking?.ParkingId;
        ParkingStats = await _analyticsRepository.GetParkingStatisticsAsync(parkingId);
    }

    private async Task LoadCityStatisticsAsync()
    {
        var data = await _analyticsRepository.GetCityStatisticsAsync(DateFrom, DateTo);
        var list = data.ToList();

        CityStats.Clear();
        foreach (var item in list) CityStats.Add(item);

        CityRevenueValues = new ChartValues<double>(list.Select(d => Math.Round((double)d.TotalRevenue, 2)));
        CityLabels = list.Select(d => d.City).ToArray();
    }

    private async Task LoadCityParkingCountsAsync()
    {
        var data = await _analyticsRepository.GetCityParkingCountsAsync();
        var list = data.ToList();

        CityParkingCounts.Clear();
        foreach (var item in list) CityParkingCounts.Add(item);

        CityParkingCountValues = new ChartValues<double>(list.Select(d => (double)d.ParkingCount));
        if (!CityLabels.Any()) CityLabels = list.Select(d => d.City).ToArray();
    }
}