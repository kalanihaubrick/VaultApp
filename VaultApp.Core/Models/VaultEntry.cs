namespace VaultApp.Core.Models;

public class VaultEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Category { get; set; } = "Geral";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
