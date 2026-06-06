namespace Aegis.ViewModels;

public class AdminDashboardViewModel : ViewModelBase
{
    private string _title = "Список парковок";

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
}