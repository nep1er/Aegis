namespace Aegis.ViewModels;

public class AdminHistoryViewModel : ViewModelBase
{
    private string _title = "История автомобилей";

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
}