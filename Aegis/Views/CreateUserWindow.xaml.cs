using System.Windows;
using Aegis.Models;
using Aegis.Services.Repositories;

namespace Aegis.Views;

public partial class CreateUserWindow : Window
{
    private readonly IUserRepository _userRepository;

    public CreateUserWindow(IUserRepository userRepository)
    {
        InitializeComponent();
        _userRepository = userRepository;
        LoadData();
    }

    private async void LoadData()
    {
        var roles = await _userRepository.GetAllRolesAsync();
        CmbRole.ItemsSource = roles;
        CmbRole.DisplayMemberPath = "Name";

        var parkings = await _userRepository.GetAllParkingsAsync();
        LstParkings.ItemsSource = parkings;
        LstParkings.DisplayMemberPath = "Address";
    }

    private async void Create_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtLogin.Text) || string.IsNullOrWhiteSpace(TxtPassword.Password))
        {
            MessageBox.Show("Заполните обязательные поля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var selectedRole = CmbRole.SelectedItem as RoleModel;
            if (selectedRole == null)
            {
                MessageBox.Show("Выберите роль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var parkingIds = LstParkings.SelectedItems.Cast<ParkingDisplayModel>().Select(p => p.ParkingId).ToList();

            var userData = new CreateUserData
            {
                Login = TxtLogin.Text,
                Password = TxtPassword.Password,
                FullName = TxtFullName.Text,
                PhoneNumber = TxtPhone.Text,
                RoleId = selectedRole.Id,
                ParkingIds = parkingIds
            };

            await _userRepository.CreateUserAsync(userData);
            MessageBox.Show("Пользователь создан!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}