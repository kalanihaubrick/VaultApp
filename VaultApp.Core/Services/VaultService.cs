using VaultApp.Core.Models;

namespace VaultApp.Core.Services;

public class VaultService
{
    private readonly StorageService _storage;
    private VaultData? _data;
    private string? _masterPassword;
    private System.Timers.Timer? _lockTimer;

    public bool IsUnlocked => _data is not null;
    public int AutoLockMinutes { get; set; } = 5;

    public event EventHandler? VaultLocked;

    public VaultService(StorageService storage)
    {
        _storage = storage;
    }

    // -------------------------------------------------------------------------
    // Ciclo de vida
    // -------------------------------------------------------------------------

    public bool VaultExists() => _storage.VaultExists();

    /// <summary>Cria um novo vault com a senha master informada.</summary>
    public void Create(string masterPassword)
    {
        _data           = new VaultData();
        _masterPassword = masterPassword;
        _storage.Save(_data, masterPassword);
        ResetLockTimer();
    }

    /// <summary>Abre o vault existente. Lança UnauthorizedAccessException se a senha estiver errada.</summary>
    public void Unlock(string masterPassword)
    {
        _data           = _storage.Load(masterPassword);
        _masterPassword = masterPassword;
        ResetLockTimer();
    }

    /// <summary>Trava o vault e limpa dados sensíveis da memória.</summary>
    public void Lock()
    {
        _data           = null;
        _masterPassword = null;
        _lockTimer?.Stop();
        VaultLocked?.Invoke(this, EventArgs.Empty);
    }

    // -------------------------------------------------------------------------
    // CRUD de entradas
    // -------------------------------------------------------------------------

    public IReadOnlyList<VaultEntry> GetEntries()
    {
        EnsureUnlocked();
        return _data!.Entries.AsReadOnly();
    }

    public IReadOnlyList<VaultEntry> GetEntriesByCategory(string category)
    {
        EnsureUnlocked();
        return _data!.Entries
            .Where(e => e.Category == category)
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<VaultEntry> Search(string query)
    {
        EnsureUnlocked();
        var q = query.Trim().ToLowerInvariant();
        return _data!.Entries
            .Where(e => e.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                     || e.Username.Contains(q, StringComparison.OrdinalIgnoreCase)
                     || e.Url.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    public void AddEntry(VaultEntry entry)
    {
        EnsureUnlocked();
        ResetLockTimer();
        _data!.Entries.Add(entry);
        Persist();
    }

    public void UpdateEntry(VaultEntry entry)
    {
        EnsureUnlocked();
        ResetLockTimer();
        var idx = _data!.Entries.FindIndex(e => e.Id == entry.Id);
        if (idx < 0) throw new KeyNotFoundException($"Entrada {entry.Id} não encontrada.");
        entry.UpdatedAt     = DateTime.UtcNow;
        _data.Entries[idx]  = entry;
        Persist();
    }

    public void DeleteEntry(Guid id)
    {
        EnsureUnlocked();
        ResetLockTimer();
        _data!.Entries.RemoveAll(e => e.Id == id);
        Persist();
    }

    // -------------------------------------------------------------------------
    // Categorias
    // -------------------------------------------------------------------------

    public IReadOnlyList<string> GetCategories()
    {
        EnsureUnlocked();
        return _data!.Categories.AsReadOnly();
    }

    public void AddCategory(string name)
    {
        EnsureUnlocked();
        if (!_data!.Categories.Contains(name))
        {
            _data.Categories.Add(name);
            Persist();
        }
    }

    // -------------------------------------------------------------------------
    // Helpers privados
    // -------------------------------------------------------------------------

    private void Persist()
        => _storage.Save(_data!, _masterPassword!);

    private void EnsureUnlocked()
    {
        if (!IsUnlocked)
            throw new InvalidOperationException("O vault está travado.");
    }

    private void ResetLockTimer()
    {
        _lockTimer?.Stop();
        _lockTimer?.Dispose();

        if (AutoLockMinutes <= 0) return;

        _lockTimer = new System.Timers.Timer(TimeSpan.FromMinutes(AutoLockMinutes))
        {
            AutoReset = false
        };
        _lockTimer.Elapsed += (_, _) => Lock();
        _lockTimer.Start();
    }

    public void Dispose()
    {
        Lock();
        _lockTimer?.Dispose();
    }
}
