using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Aegis.Commands;
using Aegis.Models;
using Aegis.Services.Repositories;

namespace Aegis.ViewModels;

public class ParkingEditorViewModel : ViewModelBase
{
    private readonly IParkingEditorRepository _repository;

    private ObservableCollection<ParkingDisplayModel> _parkings = new();
    private ParkingDisplayModel? _selectedParking;
    private ParkingDetailsModel? _selectedDetails;
    private bool _isDetailsExpanded;
    private bool _canDelete;

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
                if (value != null)
                    ShowDetailsCommand.Execute(value);
            }
        }
    }

    public ParkingDetailsModel? SelectedDetails
    {
        get => _selectedDetails;
        set
        {
            if (SetProperty(ref _selectedDetails, value))
            {
                UpdateCanDelete();
            }
        }
    }

    public bool IsDetailsExpanded
    {
        get => _isDetailsExpanded;
        set => SetProperty(ref _isDetailsExpanded, value);
    }

    public bool CanDelete
    {
        get => _canDelete;
        set => SetProperty(ref _canDelete, value);
    }

    public RelayCommand LoadParkingsCommand { get; }
    public RelayCommand ShowDetailsCommand { get; }
    public RelayCommand HideDetailsCommand { get; }
    public RelayCommand CreateParkingCommand { get; }
    public RelayCommand EditParkingCommand { get; }
    public RelayCommand DeleteParkingCommand { get; }  // ← БЕЗ canExecute

    public ParkingEditorViewModel(IParkingEditorRepository repository)
    {
        _repository = repository;

        LoadParkingsCommand = new RelayCommand(async _ => await LoadParkingsAsync());
        ShowDetailsCommand = new RelayCommand(async param => await ShowDetailsAsync(param));
        HideDetailsCommand = new RelayCommand(_ => HideDetails());
        CreateParkingCommand = new RelayCommand(_ => CreateParking());
        EditParkingCommand = new RelayCommand(_ => EditParking());

        // ← УБРАЛИ canExecute, полагаемся на IsEnabled в XAML
        DeleteParkingCommand = new RelayCommand(async _ => await DeleteParkingAsync());

        LoadParkingsCommand.Execute(null);
    }

    private async Task LoadParkingsAsync()
    {
        var parkings = await _repository.GetAllParkingsAsync();
        Parkings.Clear();
        foreach (var p in parkings)
            Parkings.Add(p);
    }

    private async Task ShowDetailsAsync(object? param)
    {
        if (param is ParkingDisplayModel parking)
        {
            SelectedParking = parking;

            var details = await _repository.GetParkingDetailsAsync(parking.ParkingId);
            SelectedDetails = details;
            IsDetailsExpanded = true;
            await UpdateCanDelete();
        }
    }

    private void HideDetails()
    {
        SelectedDetails = null;
        IsDetailsExpanded = false;
        CanDelete = false;
    }

    private async Task UpdateCanDelete()
    {
        if (SelectedDetails == null)
        {
            CanDelete = false;
            return;
        }

        bool hasOccupied = await _repository.HasOccupiedSpotsAsync(SelectedDetails.Id);
        CanDelete = !hasOccupied;
    }

    private void CreateParking()
    {
        var createWindow = new Views.CreateParkingWindow(_repository);
        createWindow.ShowDialog();
        LoadParkingsCommand.Execute(null);
    }

    private void EditParking()
    {
        if (SelectedDetails == null) return;

        var editWindow = new Views.EditParkingWindow(SelectedDetails.Id, _repository);
        editWindow.ShowDialog();
        LoadParkingsCommand.Execute(null);
    }

    private async Task DeleteParkingAsync()
    {
        if (SelectedParking == null) return;

        // Двойная проверка перед удалением
        bool hasOccupied = await _repository.HasOccupiedSpotsAsync(SelectedParking.ParkingId);

        if (hasOccupied)
        {
            MessageBox.Show(
                "Невозможно удалить парковку!\n\n" +
                "На парковке есть занятые места.\n" +
                "Сначала освободите все места.",
                "Ошибка удаления",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"Вы уверены, что хотите удалить парковку {SelectedParking.Address}?\n\n" +
            "Все тарифы и места будут удалены.",
            "Подтверждение удаления",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _repository.DeleteParkingAsync(SelectedParking.ParkingId);
                MessageBox.Show("Парковка удалена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                HideDetails();
                LoadParkingsCommand.Execute(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}