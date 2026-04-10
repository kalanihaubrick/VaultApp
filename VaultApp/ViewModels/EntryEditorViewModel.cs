using System.Windows.Input;
using VaultApp.Core.Models;
using VaultApp.Core.Services;

namespace VaultApp.ViewModels;

public class EntryEditorViewModel : BaseViewModel
{
    private readonly VaultEntry _original;
    private string _title, _username, _password, _url, _notes, _category;
    private bool   _showPassword;

    public bool IsNew { get; }

    public string Title    { get => _title;    set => SetField(ref _title,    value); }
    public string Username { get => _username; set => SetField(ref _username, value); }
    public string Password { get => _password; set => SetField(ref _password, value); }
    public string Url      { get => _url;      set => SetField(ref _url,      value); }
    public string Notes    { get => _notes;    set => SetField(ref _notes,    value); }
    public string Category { get => _category; set => SetField(ref _category, value); }

    public bool ShowPassword
    {
        get => _showPassword;
        set => SetField(ref _showPassword, value);
    }

    public List<string> Categories => App.VaultService.GetCategories().ToList();

    // Opções do gerador
    public int  GeneratorLength     { get; set; } = 20;
    public bool GenUppercase        { get; set; } = true;
    public bool GenLowercase        { get; set; } = true;
    public bool GenDigits           { get; set; } = true;
    public bool GenSymbols          { get; set; } = true;
    public bool GenExcludeAmbiguous { get; set; } = true;

    public ICommand SaveCommand     { get; }
    public ICommand GenerateCommand { get; }
    public ICommand ToggleShowCommand { get; }

    // Sinaliza para a View fechar com DialogResult = true
    public event EventHandler? SaveRequested;

    public EntryEditorViewModel(VaultEntry? entry)
    {
        IsNew     = entry is null;
        _original = entry ?? new VaultEntry();

        _title    = _original.Title;
        _username = _original.Username;
        _password = _original.Password;
        _url      = _original.Url;
        _notes    = _original.Notes;
        _category = _original.Category;

        SaveCommand       = new RelayCommand(Save, () => !string.IsNullOrWhiteSpace(Title));
        GenerateCommand   = new RelayCommand(GeneratePassword);
        ToggleShowCommand = new RelayCommand(() => ShowPassword = !ShowPassword);
    }

    private void Save()
    {
        _original.Title    = Title;
        _original.Username = Username;
        _original.Password = Password;
        _original.Url      = Url;
        _original.Notes    = Notes;
        _original.Category = Category;

        if (IsNew)
            App.VaultService.AddEntry(_original);
        else
            App.VaultService.UpdateEntry(_original);

        SaveRequested?.Invoke(this, EventArgs.Empty);
    }

    private void GeneratePassword()
    {
        var opts = new PasswordGeneratorOptions
        {
            Length            = GeneratorLength,
            UseUppercase      = GenUppercase,
            UseLowercase      = GenLowercase,
            UseDigits         = GenDigits,
            UseSymbols        = GenSymbols,
            ExcludeAmbiguous  = GenExcludeAmbiguous
        };
        Password    = App.GeneratorService.Generate(opts);
        ShowPassword = true;
    }
}
