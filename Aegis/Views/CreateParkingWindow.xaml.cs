using Aegis.Models;
using Aegis.Services.Repositories;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Aegis.Views;

public partial class CreateParkingWindow : Window
{
    private readonly IParkingEditorRepository _repository;
    private List<VehicleTypeModel> _vehicleTypes = new();
    private List<SpotInput> _spots = new();

    public CreateParkingWindow(IParkingEditorRepository repository)
    {
        InitializeComponent();
        _repository = repository;
        LoadData();
    }

    private async void LoadData()
    {
        _vehicleTypes = (await _repository.GetAllVehicleTypesAsync()).ToList();
        LstTariffs.ItemsSource = _vehicleTypes;
        CmbSpotType.ItemsSource = _vehicleTypes;
        CmbSpotType.DisplayMemberPath = "Name";
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

        _spots.Add(new SpotInput { Number = number, VehicleTypeId = selectedType.Id });
        LstSpots.Items.Add($"{number} - {selectedType.Name}");
        TxtSpotNumber.Clear();
    }

    private async void Create_Click(object sender, RoutedEventArgs e)
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
            var data = new CreateParkingData
            {
                City = TxtCity.Text,
                Street = TxtStreet.Text,
                Building = TxtBuilding.Text
            };

            // Собираем тарифы
            foreach (var vehicleType in _vehicleTypes)
            {
                var tariffBox = LstTariffs.ItemContainerGenerator.ContainerFromItem(vehicleType) as FrameworkElement;
                if (tariffBox != null)
                {
                    var textBox = FindVisualChild<TextBox>(tariffBox);
                    if (textBox != null && decimal.TryParse(textBox.Text, out decimal price))
                    {
                        data.Tariffs.Add(new TariffInput
                        {
                            VehicleTypeId = vehicleType.Id,
                            Price = price
                        });
                    }
                }
            }

            // Добавляем места
            data.Spots = _spots;

            await _repository.CreateParkingAsync(data);
            MessageBox.Show("Парковка создана!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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