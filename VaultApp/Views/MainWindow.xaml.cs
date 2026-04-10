using System.Windows;
using System.Windows.Input;
using VaultApp.ViewModels;

namespace VaultApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.EditEntryCommand.Execute(null);
    }
}