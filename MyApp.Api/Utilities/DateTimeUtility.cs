namespace MyApp.Api.Utilities;

public static class DateTimeUtility
{
    public static DateTime UtcNow => DateTime.UtcNow;

    public static string ToIsoString(DateTime? dateTime)
    {
        return dateTime?.ToString("o") ?? string.Empty;
    }
}

public static class StringUtility
{
    public static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
