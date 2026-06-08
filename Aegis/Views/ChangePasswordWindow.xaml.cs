using System.Windows;
using Aegis.Services;

namespace Aegis.Views;

public partial class ChangePasswordWindow : Window
{
    private readonly IAuthService _authService;

    public ChangePasswordWindow(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        // Валидация
        if (string.IsNullOrWhiteSpace(TxtOldPassword.Password))
        {
            MessageBox.Show("Введите старый пароль!", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtNewPassword.Password))
        {
            MessageBox.Show("Введите новый пароль!", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (TxtNewPassword.Password.Length < 4)
        {
            MessageBox.Show("Новый пароль должен быть не менее 4 символов!", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (TxtNewPassword.Password != TxtConfirmPassword.Password)
        {
            MessageBox.Show("Новый пароль и подтверждение не совпадают!", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_authService.CurrentUser == null)
        {
            MessageBox.Show("Пользователь не авторизован!", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            var success = await _authService.ChangePasswordAsync(
                _authService.CurrentUser.Id,
                TxtOldPassword.Password,
                TxtNewPassword.Password);

            if (success)
            {
                MessageBox.Show(
                    "Пароль успешно изменён!\n\n" +
                    "Используйте новый пароль при следующем входе.",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(
                    "Неверный старый пароль!",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Ошибка при смене пароля: {ex.Message}",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}