namespace VaultApp.Core.Services;

/// <summary>
/// Copia texto para a área de transferência e agenda limpeza automática.
/// A limpeza é disparada via callback para desacoplar do Windows.Forms/WPF.
/// </summary>
public class ClipboardService
{
    public int ClearAfterSeconds { get; set; } = 30;

    private CancellationTokenSource? _cts;

    /// <summary>
    /// Copia o texto e agenda a limpeza.
    /// <paramref name="setClipboard"/> deve chamar Clipboard.SetText (WPF).
    /// <paramref name="clearClipboard"/> deve chamar Clipboard.Clear (WPF).
    /// </summary>
    public void CopyWithTimer(string text,
                              Action<string> setClipboard,
                              Action clearClipboard)
    {
        // Cancela timer anterior se ainda estiver ativo
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        setClipboard(text);

        var token   = _cts.Token;
        var seconds = ClearAfterSeconds;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(seconds), token);
                clearClipboard();
            }
            catch (OperationCanceledException) { /* substituído por nova cópia */ }
        }, token);
    }

    public void CancelPendingClear()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
