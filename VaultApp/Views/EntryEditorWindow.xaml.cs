using System.Windows;
using VaultApp.ViewModels;

namespace VaultApp.Views;

public partial class EntryEditorWindow : Window
{
    private readonly EntryEditorViewModel _vm;

    public EntryEditorWindow(EntryEditorViewModel vm)
    {
        InitializeComponent();
        DataContext = _vm = vm;

        // Sincroniza PasswordBox → ViewModel (PasswordBox não suporta binding nativo)
        PasswordHidden.PasswordChanged += (_, _) =>
        {
            if (!_vm.ShowPassword)
                _vm.Password = PasswordHidden.Password;
        };

        // Sincroniza ViewModel → PasswordBox quando senha é gerada
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(_vm.Password) && !_vm.ShowPassword)
                PasswordHidden.Password = _vm.Password;
        };

        // Fecha a janela com DialogResult = true quando o save for concluído
        _vm.SaveRequested += (_, _) =>
        {
            DialogResult = true;
            Close();
        };

        // Popula o PasswordBox com o valor existente (ao editar)
        PasswordHidden.Password = _vm.Password;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
