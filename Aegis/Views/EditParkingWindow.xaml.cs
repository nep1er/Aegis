using Aegis.Models;
using Aegis.Services.Repositories;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Aegis.Views;

public partial class EditParkingWindow : Window
{
    private readonly int _parkingId;
    private readonly IParkingEditorRepository _repository;
    private List<VehicleTypeModel> _vehicleTypes = new();
    private List<SpotModel> _existingSpots = new();
    private List<int> _spotsToDelete = new();
    private List<SpotInput> _newSpots = new();

    public EditParkingWindow(int parkingId, IParkingEditorRepository repository)
    {
        InitializeComponent();
        _parkingId = parkingId;
        _repository = repository;
        LoadData();
    }

    private async void LoadData()
    {
        var parking = await _repository.GetParkingDetailsAsync(_parkingId);
        if (parking != null)
        {
            TxtCity.Text = parking.City;
            TxtStreet.Text = parking.Street;
            TxtBuilding.Text = parking.Building;

            LstTariffs.ItemsSource = parking.Tariffs;

            _vehicleTypes = (await _repository.GetAllVehicleTypesAsync()).ToList();
            CmbSpotType.ItemsSource = _vehicleTypes;
            CmbSpotType.DisplayMemberPath = "Name";

            await LoadSpotsAsync();
        }
    }

    private async Task LoadSpotsAsync()
    {
        var spots = await _repository.GetParkingSpotsAsync(_parkingId);
        _existingSpots = spots.ToList();

        // Фильтруем места, которые не помечены на удаление
        var displaySpots = _existingSpots.Where(s => !_spotsToDelete.Contains(s.Id)).ToList();
        LstSpots.ItemsSource = displaySpots;
    }

    private void NumberOnly(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !Regex.IsMatch(e.Text, "^[0-9.,]+$");
    }

    private void AddSpot_Click(object sender, RoutedEventArgs e)
    {
        var number = TxtSpotNumber.Text.Trim();
        var selectedType = CmbSpotType.SelectedItem as VehicleTypeModel;

        if (string.IsNullOrEmpty(number) || selectedType == null)
        {
            MessageBox.Show("Заполните номер места и выберите тип!", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _newSpots.Add(new SpotInput { Number = number, VehicleTypeId = selectedType.Id });

        var tempSpot = new SpotModel
        {
            Number = number,
            VehicleType = selectedType.Name,
            Status = "Новое",
            Id = 0,
            IsOccupied = false  // Новое место не занято
        };

        var tempList = LstSpots.ItemsSource.Cast<SpotModel>().ToList();
        tempList.Add(tempSpot);
        LstSpots.ItemsSource = tempList;

        TxtSpotNumber.Clear();
    }

    private void RemoveSpot_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int spotId)
        {
            var spot = _existingSpots.FirstOrDefault(s => s.Id == spotId);
            if (spot != null)
            {
                if (spot.IsOccupied)
                {
                    MessageBox.Show("Невозможно удалить занятое место!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _spotsToDelete.Add(spotId);
                LoadSpotsAsync();  // Перезагружаем список
            }
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtCity.Text) ||
            string.IsNullOrWhiteSpace(TxtStreet.Text) ||
            string.IsNullOrWhiteSpace(TxtBuilding.Text))
        {
            MessageBox.Show("Заполните все обязательные поля!", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await _repository.UpdateParkingAsync(new UpdateParkingData
            {
                ParkingId = _parkingId,
                City = TxtCity.Text,
                Street = TxtStreet.Text,
                Building = TxtBuilding.Text
            });

            foreach (var tariffItem in LstTariffs.Items)
            {
                if (tariffItem is TariffModel tariff)
                {
                    var tariffBox = LstTariffs.ItemContainerGenerator.ContainerFromItem(tariffItem) as FrameworkElement;
                    if (tariffBox != null)
                    {
                        var textBox = FindVisualChild<TextBox>(tariffBox);
                        if (textBox != null && decimal.TryParse(textBox.Text, out decimal price))
                        {
                            await _repository.SetTariffAsync(_parkingId, tariff.VehicleTypeId, price);
                        }
                    }
                }
            }

            foreach (var spotId in _spotsToDelete)
            {
                await _repository.DeleteSpotAsync(spotId);
            }

            foreach (var spot in _newSpots)
            {
                await _repository.AddSpotAsync(_parkingId, spot.Number, spot.VehicleTypeId);
            }

            MessageBox.Show("Изменения сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
                return result;
            var found = FindVisualChild<T>(child);
            if (found != null)
                return found;
        }
        return null;
    }
}