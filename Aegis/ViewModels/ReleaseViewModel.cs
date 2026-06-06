namespace Aegis.ViewModels;

public class ReleaseViewModel : ViewModelBase
{
    private string _title = "Выдача автомобиля";

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
}