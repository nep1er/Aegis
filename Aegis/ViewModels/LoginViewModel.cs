using Aegis.Commands;
using Aegis.Services;

namespace Aegis.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private string _login = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isError;

    public string Login
    {
        get => _login;
        set => SetProperty(ref _login, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsError
    {
        get => _isError;
        set => SetProperty(ref _isError, value);
    }

    public RelayCommand LoginCommand { get; }

    private readonly IAuthService _authService;
    private readonly Action _onLoginSuccess;

    public LoginViewModel(IAuthService authService, Action onLoginSuccess)
    {
        _authService = authService;
        _onLoginSuccess = onLoginSuccess;

        LoginCommand = new RelayCommand(async _ => await LoginAsync());
    }

    private async Task LoginAsync()
    {
        IsError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
        {
            IsError = true;
            ErrorMessage = "Заполните все поля";
            return;
        }

        var user = await _authService.LoginAsync(Login, Password);

        if (user != null)
        {
            _onLoginSuccess();
        }
        else
        {
            IsError = true;
            ErrorMessage = "Неверный логин или пароль";
        }
    }
}