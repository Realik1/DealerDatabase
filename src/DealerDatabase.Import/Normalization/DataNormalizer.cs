using System.Text.RegularExpressions;

namespace DealerDatabase.Import.Normalization;

/// <summary>
/// Provides data cleaning and normalization utilities for dealership records.
/// Handles names, addresses, postcodes, phone numbers, dates, and domains.
/// </summary>
public static class DataNormalizer
{
    /// <summary>
    /// Normalizes a company or dealership name:
    /// - Trim whitespace
    /// - Convert to title case
    /// - Remove common suffixes (Ltd, Limited, Inc, etc.)
    /// - Remove extra spaces
    /// </summary>
    public static string? NormalizeName(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var normalized = input.Trim();

        // Remove common company suffixes at the end
        normalized = Regex.Replace(normalized, @"\s+(Ltd\.?|Limited|Inc\.?|Incorporated|LLC|Ltd|Ltd\.|Company|Co\.|Corp\.?|Corporation|PLC|plc|Plc)\.?\s*$", "", RegexOptions.IgnoreCase);

        // Collapse multiple spaces
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

        // Title case normalization (capitalize first letter of words)
        normalized = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(normalized.ToLowerInvariant());

        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    /// <summary>
    /// Normalizes a postcode/ZIP code:
    /// - Remove spaces and dashes
    /// - Convert to uppercase
    /// - Handle both UK postcodes (e.g., "SW1A1AA") and US ZIPs
    /// </summary>
    public static string? NormalizePostcode(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var normalized = input.Trim()
            .Replace(" ", "")
            .Replace("-", "")
            .ToUpperInvariant();

        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    /// <summary>
    /// Normalizes a phone number:
    /// - Remove spaces, dashes, parentheses
    /// - Keep only digits and +
    /// - Ensure it starts with + or at least 10 digits
    /// </summary>
    public static string? NormalizePhoneNumber(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var normalized = Regex.Replace(input.Trim(), @"[\s\-\(\)]+", "");

        // Keep only +, 0-9, extensions
        normalized = Regex.Replace(normalized, @"[^0-9+x#]", "");

        if (string.IsNullOrEmpty(normalized) || normalized.Length < 10)
            return null;

        // Standardize format: remove leading 0 if it has +44, etc.
        if (normalized.StartsWith("0044"))
            normalized = "+" + normalized.Substring(2);

        return normalized;
    }

    /// <summary>
    /// Normalizes an address by:
    /// - Trimming whitespace
    /// - Standardizing abbreviations
    /// - Removing extra spaces
    /// </summary>
    public static string? NormalizeAddress(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var normalized = input.Trim();

        // Standardize common abbreviations
        var abbreviations = new Dictionary<string, string>
        {
            { @"\bSt\.", "Street" },
            { @"\bRd\.", "Road" },
            { @"\bAve\.", "Avenue" },
            { @"\bBlvd\.", "Boulevard" },
            { @"\bLn\.", "Lane" },
            { @"\bCt\.", "Court" },
            { @"\bDr\.", "Drive" },
            { @"\bPl\.", "Place" },
            { @"\bSq\.", "Square" },
            { @"\bNo\.", "Number" }
        };

        foreach (var (pattern, replacement) in abbreviations)
        {
            normalized = Regex.Replace(normalized, pattern, replacement, RegexOptions.IgnoreCase);
        }

        // Remove extra whitespace
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    /// <summary>
    /// Normalizes a website domain:
    /// - Convert to lowercase
    /// - Remove http:// or https://
    /// - Remove www.
    /// - Remove trailing slash
    /// - Extract domain without path
    /// </summary>
    public static string? NormalizeDomain(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var normalized = input.Trim().ToLowerInvariant();

        // Remove protocol
        normalized = Regex.Replace(normalized, @"^https?://", "");

        // Remove www.
        normalized = Regex.Replace(normalized, @"^www\.", "");

        // Remove trailing path
        var slashIndex = normalized.IndexOf('/');
        if (slashIndex > 0)
            normalized = normalized.Substring(0, slashIndex);

        // Remove port numbers
        var colonIndex = normalized.IndexOf(':');
        if (colonIndex > 0)
            normalized = normalized.Substring(0, colonIndex);

        normalized = normalized.Trim().TrimEnd('/');

        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    /// <summary>
    /// Normalizes registration numbers (Companies House, FCA, VAT, ICO):
    /// - Remove spaces and dashes
    /// - Convert to uppercase (except for format-specific rules)
    /// - Validate basic format
    /// </summary>
    public static string? NormalizeRegistrationNumber(string? input, string numberType = "")
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var normalized = input.Trim();

        // Remove spaces and dashes
        normalized = Regex.Replace(normalized, @"[\s\-]", "");

        switch (numberType.ToLowerInvariant())
        {
            case "vat":
                // GB followed by 9 digits, or valid VAT format
                normalized = normalized.ToUpperInvariant();
                // Remove "GB" prefix if present for comparison elsewhere
                break;

            case "companies_house":
                // 8 digits, left-padded with zeros
                if (Regex.IsMatch(normalized, @"^\d{1,8}$"))
                    normalized = normalized.PadLeft(8, '0');
                break;

            case "fca":
                // 6 digits
                normalized = Regex.Replace(normalized, @"\D", "");
                break;
        }

        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    /// <summary>
    /// Parses a date string with flexible formats:
    /// Handles ISO format, UK format (DD/MM/YYYY), US format (MM/DD/YYYY), etc.
    /// </summary>
    public static DateTime? ParseDate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var trimmed = input.Trim();

        // Try standard DateTime parsing first
        if (DateTime.TryParse(trimmed, out var result))
            return result.Date;

        // Try common UK date formats
        var ukFormats = new[] { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy" };
        foreach (var format in ukFormats)
        {
            if (DateTime.TryParseExact(trimmed, format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var ukResult))
                return ukResult.Date;
        }

        return null;
    }

    /// <summary>
    /// Generates a normalized key for fuzzy matching purposes.
    /// Used to identify potential duplicates across sources.
    /// </summary>
    public static string GenerateMatchKey(string name, string? postcode)
    {
        // Normalize and create a simplified key for matching
        var normalizedName = NormalizeName(name) ?? "";
        var normalizedPostcode = NormalizePostcode(postcode) ?? "";

        // Remove all special characters and extra spaces for key
        var nameKey = Regex.Replace(normalizedName, @"[^a-za-z0-9]", "").ToLowerInvariant();
        var postcodeKey = Regex.Replace(normalizedPostcode, @"[^a-za-z0-9]", "").ToLowerInvariant();

        return $"{nameKey}|{postcodeKey}";
    }
}
