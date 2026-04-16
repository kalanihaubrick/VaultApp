using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using VaultApp.Core.Models;
using VaultApp.Views;

namespace VaultApp.ViewModels;

public class MainViewModel : BaseViewModel
{
    private string  _searchQuery     = string.Empty;
    private string? _selectedCategory;
    private VaultEntry? _selectedEntry;
    private string  _statusMessage   = string.Empty;

    public ObservableCollection<VaultEntry> Entries    { get; } = [];
    public ObservableCollection<string>     Categories { get; } = [];

    public AppViewModel AppViewModel { get; }

    public string SearchQuery
    {
        get => _searchQuery;
        set { SetField(ref _searchQuery, value); LoadEntries(); }
    }

    public string? SelectedCategory
    {
        get => _selectedCategory;
        set { SetField(ref _selectedCategory, value); LoadEntries(); }
    }

    public VaultEntry? SelectedEntry
    {
        get => _selectedEntry;
        set => SetField(ref _selectedEntry, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public ICommand NewEntryCommand   { get; }
    public ICommand EditEntryCommand  { get; }
    public ICommand DeleteEntryCommand{ get; }
    public ICommand CopyPasswordCommand { get; }
    public ICommand LockCommand       { get; }
    public ICommand ClearSearchCommand{ get; }

    public MainViewModel()
    {
        AppViewModel = App.AppViewModel;

        NewEntryCommand     = new RelayCommand(OpenNewEntry);
        EditEntryCommand    = new RelayCommand(OpenEditEntry,  () => SelectedEntry is not null);
        DeleteEntryCommand  = new RelayCommand(DeleteEntry,    () => SelectedEntry is not null);
        CopyPasswordCommand = new RelayCommand(CopyPassword,   () => SelectedEntry is not null);
        LockCommand         = new RelayCommand(() => App.VaultService.Lock());
        ClearSearchCommand  = new RelayCommand(() => SearchQuery = string.Empty);

        LoadCategories();
        LoadEntries();
    }

    public void LoadCategories()
    {
        Categories.Clear();
        Categories.Add("Todas");
        foreach (var c in App.VaultService.GetCategories())
            Categories.Add(c);
    }

    public void LoadEntries()
    {
        Entries.Clear();
        var entries = string.IsNullOrWhiteSpace(SearchQuery)
            ? (SelectedCategory is null or "Todas"
                ? App.VaultService.GetEntries()
                : App.VaultService.GetEntriesByCategory(SelectedCategory))
            : App.VaultService.Search(SearchQuery);

        foreach (var e in entries)
            Entries.Add(e);
    }

    private void OpenNewEntry()
    {
        var vm  = new EntryEditorViewModel(null);
        var dlg = new EntryEditorWindow(vm) { Owner = GetMainWindow() };
        if (dlg.ShowDialog() == true)
        {
            LoadCategories();
            LoadEntries();
            SetStatus("Entrada adicionada.");
        }
    }

    private void OpenEditEntry()
    {
        if (SelectedEntry is null) return;
        var vm  = new EntryEditorViewModel(SelectedEntry);
        var dlg = new EntryEditorWindow(vm) { Owner = GetMainWindow() };
        if (dlg.ShowDialog() == true)
        {
            LoadEntries();
            SetStatus("Entrada atualizada.");
        }
    }

    private void DeleteEntry()
    {
        if (SelectedEntry is null) return;
        var result = MessageBox.Show(
            $"Excluir \"{SelectedEntry.Title}\"?",
            "Confirmar exclusão",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            App.VaultService.DeleteEntry(SelectedEntry.Id);
            LoadEntries();
            SetStatus("Entrada excluída.");
        }
    }

    private void CopyPassword()
    {
        if (SelectedEntry is null) return;
        App.ClipboardService.CopyWithTimer(
            SelectedEntry.Password,
            text => Application.Current.Dispatcher.Invoke(() => Clipboard.SetText(text)),
            ()   => Application.Current.Dispatcher.Invoke(Clipboard.Clear));

        SetStatus($"Senha copiada! Área de transferência será limpa em {App.ClipboardService.ClearAfterSeconds}s.");
    }

    private async void SetStatus(string message)
    {
        StatusMessage = message;
        await Task.Delay(3000);
        if (StatusMessage == message) StatusMessage = string.Empty;
    }

    private static Window? GetMainWindow()
        => Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
}
