namespace Aegis.ViewModels;

public class ReceptionViewModel : ViewModelBase
{
    private string _title = "Оформление приемки";

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
}