using System.Windows;
using Aegis.Services;
using Aegis.Services.Repositories;

namespace Aegis.Views;

public partial class PaymentWindow : Window
{
    private readonly ReleaseData _releaseData;
    private readonly IReleaseRepository _releaseRepository;
    private readonly INavigationService _navigationService;

    public PaymentWindow(ReleaseData releaseData, IReleaseRepository releaseRepository, INavigationService navigationService)
    {
        InitializeComponent();
        _releaseData = releaseData;
        _releaseRepository = releaseRepository;
        _navigationService = navigationService;

        LoadDisplayData();
    }

    private async void LoadDisplayData()
    {
        var vehicle = await _releaseRepository.GetActiveVehicleByIdAsync(_releaseData.ParkingRecordId);
        if (vehicle != null)
        {
            TxtLicensePlate.Text = vehicle.LicensePlate;
        }

        TxtReceiptNumber.Text = _releaseData.ReceiptNumber;  // ← Показываем номер чека
        TxtStorageFee.Text = $"{_releaseData.StorageFee} ₽";
        TxtTowFine.Text = $"{_releaseData.TowFine} ₽";
        TxtTotal.Text = $"{_releaseData.TotalAmount} ₽";
    }

    private async void Pay_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _releaseRepository.CompleteReleaseAsync(_releaseData);
            MessageBox.Show(
                $"Оплата прошла успешно!\n\n" +
                $"Номер чека: {_releaseData.ReceiptNumber}\n" +
                $"Сумма: {_releaseData.TotalAmount} ₽\n\n" +
                $"Автомобиль выдан.",
                "Успех",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}