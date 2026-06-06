namespace Aegis.ViewModels;

public class PaymentsHistoryViewModel : ViewModelBase
{
    private string _title = "История платежей";

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
}