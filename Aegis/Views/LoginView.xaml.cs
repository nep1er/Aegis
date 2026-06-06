using System.Windows;
using System.Windows.Controls;

namespace Aegis.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.LoginViewModel vm)
        {
            vm.Password = ((PasswordBox)sender).Password;
        }
    }
}