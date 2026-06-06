namespace Aegis.ViewModels;

public class ParkingEditorViewModel : ViewModelBase
{
    private string _title = "Редактор парковок";

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
}