using System.Windows;
using System.Windows.Input;
using VaultApp.ViewModels;

namespace VaultApp.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => PasswordBox.Focus();
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) TryUnlock();
    }

    private void UnlockButton_Click(object sender, RoutedEventArgs e)
        => TryUnlock();

    private void TryUnlock()
    {
        if (DataContext is LoginViewModel vm)
            vm.UnlockCommand.Execute(PasswordBox.Password);
    }
}
