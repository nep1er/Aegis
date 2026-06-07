using System.Collections.ObjectModel;
using System.Windows;
using Aegis.Commands;
using Aegis.Models;
using Aegis.Services;
using Aegis.Services.Repositories;

namespace Aegis.ViewModels;

public class ReleaseViewModel : ViewModelBase
{
    private readonly ParkingDisplayModel _parking;
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly IReleaseRepository _releaseRepository;
    private readonly ITariffRepository _tariffRepository;

    private ObservableCollection<ActiveVehicleModel> _activeVehicles = new();
    private ActiveVehicleModel? _selectedVehicle;
    private ObservableCollection<DocumentTypeItem> _documentTypes = new();
    private DocumentTypeItem? _selectedDocumentType;

    private string _ownerFullName = string.Empty;
    private string _vin = string.Empty;
    private string _brand = string.Empty;
    private string _model = string.Empty;
    private string _documentNumber = string.Empty;
    private string _receiptNumber = string.Empty;

    private decimal _storageFee;
    private decimal _towFine;
    private decimal _totalAmount;
    private int _hoursParked;

    private DateTime _releaseDate = DateTime.Now;

    public ObservableCollection<ActiveVehicleModel> ActiveVehicles
    {
        get => _activeVehicles;
        set => SetProperty(ref _activeVehicles, value);
    }

    public ActiveVehicleModel? SelectedVehicle
    {
        get => _selectedVehicle;
        set
        {
            if (SetProperty(ref _selectedVehicle, value))
            {
                RecalculateFees();
            }
        }
    }

    public ObservableCollection<DocumentTypeItem> DocumentTypes
    {
        get => _documentTypes;
        set => SetProperty(ref _documentTypes, value);
    }

    public string DocumentFormatHint
    {
        get
        {
            if (SelectedDocumentType == null || string.IsNullOrWhiteSpace(SelectedDocumentType.NumberFormat))
                return "Введите номер документа";

            return $"Формат: {SelectedDocumentType.NumberFormat}";
        }
    }

    // Обновляем SelectedDocumentType
    public DocumentTypeItem? SelectedDocumentType
    {
        get => _selectedDocumentType;
        set
        {
            if (SetProperty(ref _selectedDocumentType, value))
            {
                OnPropertyChanged(nameof(DocumentFormatHint));
            }
        }
    }

    public string OwnerFullName
    {
        get => _ownerFullName;
        set => SetProperty(ref _ownerFullName, value);
    }

    public string Vin
    {
        get => _vin;
        set => SetProperty(ref _vin, value);
    }

    public string Brand
    {
        get => _brand;
        set => SetProperty(ref _brand, value);
    }

    public string Model
    {
        get => _model;
        set => SetProperty(ref _model, value);
    }

    public string DocumentNumber
    {
        get => _documentNumber;
        set => SetProperty(ref _documentNumber, value);
    }

    public string ReceiptNumber
    {
        get => _receiptNumber;
        set => SetProperty(ref _receiptNumber, value);
    }

    public decimal StorageFee
    {
        get => _storageFee;
        set => SetProperty(ref _storageFee, value);
    }

    public decimal TowFine
    {
        get => _towFine;
        set => SetProperty(ref _towFine, value);
    }

    public decimal TotalAmount
    {
        get => _totalAmount;
        set => SetProperty(ref _totalAmount, value);
    }

    public int HoursParked
    {
        get => _hoursParked;
        set => SetProperty(ref _hoursParked, value);
    }

    public string ParkingAddress => _parking.Address;
    public string OperatorName => _authService.CurrentUser?.FullName ?? _authService.CurrentUser?.Login ?? "";
    public string ReleaseDateText => _releaseDate.ToString("dd.MM.yyyy HH:mm");

    public RelayCommand LoadDataCommand { get; }
    public RelayCommand ProceedToPaymentCommand { get; }
    public RelayCommand CancelCommand { get; }

    public ReleaseViewModel(
        ParkingDisplayModel parking,
        IAuthService authService,
        INavigationService navigationService,
        IReleaseRepository releaseRepository,
        ITariffRepository tariffRepository)
    {
        _parking = parking;
        _authService = authService;
        _navigationService = navigationService;
        _releaseRepository = releaseRepository;
        _tariffRepository = tariffRepository;

        LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
        ProceedToPaymentCommand = new RelayCommand(_ => ProceedToPayment(), _ => CanProceedToPayment());
        CancelCommand = new RelayCommand(_ => Cancel());

        LoadDataCommand.Execute(null);
    }

    private async Task LoadDataAsync()
    {
        var vehicles = await _releaseRepository.GetActiveVehiclesAsync(_parking.ParkingId);
        ActiveVehicles.Clear();
        foreach (var v in vehicles)
            ActiveVehicles.Add(v);

        using var connection = new Npgsql.NpgsqlConnection("Host=localhost;Database=Aegis;Username=postgres;Password=12345");
        await connection.OpenAsync();

        using var cmd = new Npgsql.NpgsqlCommand(
            "SELECT id, type, number_format FROM \"documenttypes\" ORDER BY id",
            connection);

        using var reader = await cmd.ExecuteReaderAsync();
        DocumentTypes.Clear();
        while (await reader.ReadAsync())
        {
            DocumentTypes.Add(new DocumentTypeItem
            {
                Id = reader.GetInt32(0),
                Type = reader.GetString(1),
                NumberFormat = reader.IsDBNull(2) ? "" : reader.GetString(2)
            });
        }

        if (DocumentTypes.Any())
            SelectedDocumentType = DocumentTypes.First();
    }

    private void RecalculateFees()
    {
        if (SelectedVehicle == null)
        {
            StorageFee = 0;
            TowFine = 0;
            TotalAmount = 0;
            HoursParked = 0;
            return;
        }

        // Считаем часы (округление вверх)
        var duration = DateTime.Now - SelectedVehicle.AdmissionDate;
        HoursParked = (int)Math.Ceiling(duration.TotalHours);
        if (HoursParked < 1) HoursParked = 1;

        StorageFee = HoursParked * SelectedVehicle.Tariff;
        TowFine = SelectedVehicle.TowFine;
        TotalAmount = StorageFee + TowFine;
    }

    private bool CanProceedToPayment()
    {
        return SelectedVehicle != null
            && SelectedDocumentType != null
            && !string.IsNullOrWhiteSpace(OwnerFullName)
            && !string.IsNullOrWhiteSpace(DocumentNumber)
            && TotalAmount > 0;
    }

    private void ProceedToPayment()
    {
        if (SelectedVehicle == null || SelectedDocumentType == null)
            return;

        // Генерируем номер чека автоматически
        var receiptNumber = GenerateReceiptNumber();

        var paymentData = new ReleaseData
        {
            ParkingRecordId = SelectedVehicle.ParkingRecordId,
            OperatorId = _authService.CurrentUser!.Id,
            OwnerFullName = OwnerFullName,
            Vin = Vin,
            Brand = Brand,
            Model = Model,
            DocumentTypeId = SelectedDocumentType.Id,
            DocumentNumber = DocumentNumber,
            StorageFee = StorageFee,
            TowFine = TowFine,
            TotalAmount = TotalAmount,
            TariffId = 0,
            ReceiptNumber = receiptNumber,  // ← Автоматически сгенерирован
            ReleaseDate = DateTime.Now
        };

        var paymentWindow = new Views.PaymentWindow(paymentData, _releaseRepository, _navigationService);
        paymentWindow.ShowDialog();

        Cancel();
    }

    private string GenerateReceiptNumber()
    {
        // Формат: REC-YYYYMMDD-XXXXXX (например, REC-20260607-A3F9B2)
        var datePart = DateTime.Now.ToString("yyyyMMdd");
        var randomPart = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
        return $"REC-{datePart}-{randomPart}";
    }




    private void Cancel()
    {
        _navigationService.NavigateTo<DashboardViewModel>();
    }
}

public class DocumentTypeItem
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string NumberFormat { get; set; } = string.Empty;  // ← ДОБАВЛЕНО
}