namespace Common.Tools;

public static class StringExtensions
{
    public static string NormalizeForLookup(this string value) => value.Trim().ToUpperInvariant();
}
