using System.Text.RegularExpressions;

namespace API.Extensions;

public static partial class CredentialRules
{
    public const int UsernameMin = 3;
    public const int UsernameMax = 30;
    public const int PasswordMin = 8;
    public const int PasswordMax = 100;

    [GeneratedRegex("^[a-zA-Z0-9._-]+$")]
    private static partial Regex UsernamePattern();

    public static string? ValidateUsername(string? username)
    {
        var u = username?.Trim() ?? "";
        if (u.Length < UsernameMin || u.Length > UsernameMax)
            return $"Username must be {UsernameMin}-{UsernameMax} characters";
        if (!UsernamePattern().IsMatch(u))
            return "Username can contain only letters, digits, '.', '_' and '-'";
        return null;
    }

    public static string? ValidatePassword(string? password)
    {
        var p = password ?? "";
        if (p.Length < PasswordMin || p.Length > PasswordMax)
            return $"Password must be {PasswordMin}-{PasswordMax} characters";
        if (!p.Any(char.IsLetter) || !p.Any(char.IsDigit))
            return "Password must contain at least one letter and one digit";
        return null;
    }

    public static string? Validate(string? username, string? password) =>
        ValidateUsername(username) ?? ValidatePassword(password);
}
