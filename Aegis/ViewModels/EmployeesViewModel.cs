using System.Collections.ObjectModel;
using System.Windows;
using Aegis.Commands;
using Aegis.Models;
using Aegis.Services.Repositories;

namespace Aegis.ViewModels;

public class EmployeesViewModel : ViewModelBase
{
    private readonly IUserRepository _userRepository;

    private ObservableCollection<UserDisplayModel> _users = new();
    private UserDisplayModel? _selectedUser;
    private UserDetailsModel? _selectedUserDetails;
    private bool _isDetailsExpanded;

    public ObservableCollection<UserDisplayModel> Users
    {
        get => _users;
        set => SetProperty(ref _users, value);
    }

    public UserDisplayModel? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (SetProperty(ref _selectedUser, value))
            {
                if (value != null)
                    ShowDetailsCommand.Execute(value);
            }
        }
    }

    public UserDetailsModel? SelectedUserDetails
    {
        get => _selectedUserDetails;
        set => SetProperty(ref _selectedUserDetails, value);
    }

    public bool IsDetailsExpanded
    {
        get => _isDetailsExpanded;
        set => SetProperty(ref _isDetailsExpanded, value);
    }

    public RelayCommand LoadUsersCommand { get; }
    public RelayCommand ShowDetailsCommand { get; }
    public RelayCommand HideDetailsCommand { get; }
    public RelayCommand CreateUserCommand { get; }
    public RelayCommand EditUserCommand { get; }
    public RelayCommand DeleteUserCommand { get; }

    public EmployeesViewModel(IUserRepository userRepository)
    {
        _userRepository = userRepository;

        LoadUsersCommand = new RelayCommand(async _ => await LoadUsersAsync());
        ShowDetailsCommand = new RelayCommand(async param => await ShowDetailsAsync(param));
        HideDetailsCommand = new RelayCommand(_ => HideDetails());
        CreateUserCommand = new RelayCommand(_ => CreateUser());
        EditUserCommand = new RelayCommand(_ => EditUser(), _ => SelectedUser != null);
        DeleteUserCommand = new RelayCommand(async _ => await DeleteUserAsync(), _ => SelectedUser != null);

        LoadUsersCommand.Execute(null);
    }

    private async Task LoadUsersAsync()
    {
        var users = await _userRepository.GetAllUsersAsync();
        Users.Clear();
        foreach (var user in users)
        {
            Users.Add(user);
        }
    }

    private async Task ShowDetailsAsync(object? param)
    {
        if (param is UserDisplayModel user)
        {
            SelectedUser = user;
            var details = await _userRepository.GetUserDetailsAsync(user.Id);
            SelectedUserDetails = details;
            IsDetailsExpanded = true;
        }
    }

    private void HideDetails()
    {
        SelectedUserDetails = null;
        IsDetailsExpanded = false;
    }

    private void CreateUser()
    {
        var createWindow = new Views.CreateUserWindow(_userRepository);
        createWindow.ShowDialog();
        LoadUsersCommand.Execute(null);
    }

    private void EditUser()
    {
        if (SelectedUser == null) return;

        var editWindow = new Views.EditUserWindow(SelectedUser.Id, _userRepository);
        editWindow.ShowDialog();
        LoadUsersCommand.Execute(null);
    }

    private async Task DeleteUserAsync()
    {
        if (SelectedUser == null) return;

        var result = MessageBox.Show(
            $"Вы уверены, что хотите удалить пользователя {SelectedUser.FullName}?",
            "Подтверждение удаления",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _userRepository.DeleteUserAsync(SelectedUser.Id);
                MessageBox.Show("Пользователь удалён", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                HideDetails();
                LoadUsersCommand.Execute(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}