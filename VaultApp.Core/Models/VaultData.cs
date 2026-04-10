namespace VaultApp.Core.Models;

public class VaultData
{
    public int Version { get; set; } = 1;
    public List<VaultEntry> Entries { get; set; } = [];
    public List<string> Categories { get; set; } = ["Geral", "Trabalho", "Pessoal", "Financeiro"];
}
