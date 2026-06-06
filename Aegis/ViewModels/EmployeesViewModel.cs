namespace Aegis.ViewModels;

public class EmployeesViewModel : ViewModelBase
{
    private string _title = "Сотрудники";

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
}