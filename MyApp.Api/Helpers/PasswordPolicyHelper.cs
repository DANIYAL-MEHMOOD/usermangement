using System.Text.RegularExpressions;

namespace MyApp.Api.Helpers;

public static class PasswordPolicyHelper
{
    private static readonly Regex UppercaseRegex = new(@"[A-Z]", RegexOptions.Compiled);
    private static readonly Regex LowercaseRegex = new(@"[a-z]", RegexOptions.Compiled);
    private static readonly Regex DigitRegex = new(@"[0-9]", RegexOptions.Compiled);
    private static readonly Regex SpecialCharRegex = new(@"[\W_]", RegexOptions.Compiled);

    public static bool ValidatePassword(string password, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            errorMessage = "Password must be at least 8 characters long.";
            return false;
        }
        if (!UppercaseRegex.IsMatch(password))
        {
            errorMessage = "Password must contain at least one uppercase letter.";
            return false;
        }
        if (!LowercaseRegex.IsMatch(password))
        {
            errorMessage = "Password must contain at least one lowercase letter.";
            return false;
        }
        if (!DigitRegex.IsMatch(password))
        {
            errorMessage = "Password must contain at least one number.";
            return false;
        }
        if (!SpecialCharRegex.IsMatch(password))
        {
            errorMessage = "Password must contain at least one special character.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}

public static class SecurityHelper
{
    public static string SanitizeInput(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input.Trim().Replace("--", "").Replace("'", "''");
    }
}
