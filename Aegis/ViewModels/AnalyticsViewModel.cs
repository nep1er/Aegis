namespace Aegis.ViewModels;

public class AnalyticsViewModel : ViewModelBase
{
    private string _title = "Аналитика";

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
}