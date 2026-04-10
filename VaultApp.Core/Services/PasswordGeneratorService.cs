using System.Security.Cryptography;
using System.Text;

namespace VaultApp.Core.Services;

public class PasswordGeneratorOptions
{
    public int Length            { get; set; } = 20;
    public bool UseUppercase     { get; set; } = true;
    public bool UseLowercase     { get; set; } = true;
    public bool UseDigits        { get; set; } = true;
    public bool UseSymbols       { get; set; } = true;
    public bool ExcludeAmbiguous { get; set; } = true; // remove 0,O,l,1,I
}

public class PasswordGeneratorService
{
    private const string Uppercase  = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Lowercase  = "abcdefghijklmnopqrstuvwxyz";
    private const string Digits     = "0123456789";
    private const string Symbols    = "!@#$%^&*()-_=+[]{}|;:,.<>?";
    private const string Ambiguous  = "0O1lI";

    public string Generate(PasswordGeneratorOptions options)
    {
        var charset = BuildCharset(options);
        if (charset.Length == 0)
            throw new ArgumentException("Selecione ao menos um tipo de caractere.");

        // Usa RandomNumberGenerator para garantir entropia criptográfica
        var result = new StringBuilder(options.Length);
        var bytes  = new byte[options.Length * 2]; // sobra para rejeição

        do
        {
            result.Clear();
            RandomNumberGenerator.Fill(bytes);

            foreach (var b in bytes)
            {
                // Rejeição uniforme: evita viés de módulo
                var limit = (byte)(256 - 256 % charset.Length);
                if (b >= limit) continue;
                result.Append(charset[b % charset.Length]);
                if (result.Length == options.Length) break;
            }
        } while (result.Length < options.Length || !MeetsRequirements(result.ToString(), options));

        return result.ToString();
    }

    private static string BuildCharset(PasswordGeneratorOptions opts)
    {
        var sb = new StringBuilder();
        if (opts.UseUppercase) sb.Append(Uppercase);
        if (opts.UseLowercase) sb.Append(Lowercase);
        if (opts.UseDigits)    sb.Append(Digits);
        if (opts.UseSymbols)   sb.Append(Symbols);

        if (!opts.ExcludeAmbiguous) return sb.ToString();

        return new string(sb.ToString().Where(c => !Ambiguous.Contains(c)).ToArray());
    }

    private static bool MeetsRequirements(string password, PasswordGeneratorOptions opts)
    {
        if (opts.UseUppercase && !password.Any(char.IsUpper)) return false;
        if (opts.UseLowercase && !password.Any(char.IsLower)) return false;
        if (opts.UseDigits    && !password.Any(char.IsDigit)) return false;
        if (opts.UseSymbols   && !password.Any(c => Symbols.Contains(c))) return false;
        return true;
    }
}
