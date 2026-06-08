using System.Windows;
using Aegis.Models;
using Aegis.Services.Repositories;

namespace Aegis.Views;

public partial class EditUserWindow : Window
{
    private readonly int _userId;
    private readonly IUserRepository _userRepository;

    public EditUserWindow(int userId, IUserRepository userRepository)
    {
        InitializeComponent();
        _userId = userId;
        _userRepository = userRepository;
        LoadData();
    }

    private async void LoadData()
    {
        var user = await _userRepository.GetUserDetailsAsync(_userId);
        if (user != null)
        {
            TxtFullName.Text = user.FullName;
            TxtLogin.Text = user.Login;

            var roles = await _userRepository.GetAllRolesAsync();
            CmbRole.ItemsSource = roles;
            CmbRole.DisplayMemberPath = "Name";
            CmbRole.SelectedItem = roles.FirstOrDefault(r => r.Id == user.RoleId);

            var parkings = await _userRepository.GetAllParkingsAsync();
            LstParkings.ItemsSource = parkings;
            LstParkings.DisplayMemberPath = "Address";

            foreach (var parkingId in user.ParkingIds)
            {
                var parking = parkings.FirstOrDefault(p => p.ParkingId == parkingId);
                if (parking != null)
                    LstParkings.SelectedItems.Add(parking);
            }
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var selectedRole = CmbRole.SelectedItem as RoleModel;
            if (selectedRole == null)
            {
                MessageBox.Show("Выберите роль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var parkingIds = LstParkings.SelectedItems.Cast<ParkingDisplayModel>().Select(p => p.ParkingId).ToList();

            var userData = new UpdateUserData
            {
                UserId = _userId,
                RoleId = selectedRole.Id,
                ParkingIds = parkingIds
            };

            await _userRepository.UpdateUserAsync(userData);
            MessageBox.Show("Изменения сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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