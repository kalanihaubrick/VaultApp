using System.Windows;
using System.Windows.Input;
using VaultApp.Views;

namespace VaultApp.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private string _errorMessage = string.Empty;
    private bool   _isLoading;

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetField(ref _isLoading, value);
    }

    public bool IsNewVault => !App.VaultService.VaultExists();

    public ICommand UnlockCommand { get; }

    public LoginViewModel()
    {
        UnlockCommand = new RelayCommand<string>(Unlock, p => !string.IsNullOrWhiteSpace(p));
    }

    private async void Unlock(string? password)
    {
        if (string.IsNullOrWhiteSpace(password)) return;

        IsLoading    = true;
        ErrorMessage = string.Empty;

        try
        {
            // Operação pesada (PBKDF2) em background para não travar a UI
            await Task.Run(() =>
            {
                if (IsNewVault)
                    App.VaultService.Create(password);
                else
                    App.VaultService.Unlock(password);
            });

            // Navega para a janela principal
            var main = new MainWindow();
            main.Show();

            // Fecha a janela de login
            Application.Current.Windows
                .OfType<LoginWindow>()
                .FirstOrDefault()
                ?.Close();
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "Senha incorreta. Tente novamente.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao abrir o vault: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
