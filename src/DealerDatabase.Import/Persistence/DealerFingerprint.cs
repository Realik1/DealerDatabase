using System.Text;
using DealerDatabase.Import.Normalization;

namespace DealerDatabase.Import.Persistence;

/// <summary>
/// Generates stable, deterministic fingerprints for consolidated dealers.
/// Used as a unique key to identify existing dealers for idempotent upserts.
/// 
/// Fingerprint strategy (in order of preference):
/// 1. Companies House Number (most reliable, globally unique)
/// 2. VAT Number + Postcode (highly reliable)
/// 3. FCA Firm Reference Number (for regulated firms)
/// 4. Postcode + Normalized Name (for general matching)
/// </summary>
public class DealerFingerprint
{
    /// <summary>
    /// The structured fingerprint that can be used as a database key.
    /// </summary>
    public string FingerprintKey { get; private set; } = string.Empty;

    /// <summary>
    /// A human-readable explanation of how this fingerprint was generated.
    /// </summary>
    public string FingerprintReason { get; private set; } = string.Empty;

    /// <summary>
    /// Confidence level of this fingerprint (0-100).
    /// 100 = definitive (Company Number), 70-90 = high (VAT+Postcode), 50-70 = medium (Name+Postcode).
    /// </summary>
    public int Confidence { get; private set; }

    private DealerFingerprint() { }

    /// <summary>
    /// Generates a fingerprint for a consolidated record.
    /// The fingerprint is stable and can be used to detect duplicates across import runs.
    /// </summary>
    public static DealerFingerprint Generate(Matching.ConsolidatedRecord record)
    {
        var fingerprint = new DealerFingerprint();

        // Priority 1: Companies House Number (globally unique, definitive)
        if (!string.IsNullOrEmpty(record.CompaniesHouseNumber))
        {
            fingerprint.FingerprintKey = $"CH|{record.CompaniesHouseNumber}";
            fingerprint.FingerprintReason = $"Companies House Number: {record.CompaniesHouseNumber}";
            fingerprint.Confidence = 100;
            return fingerprint;
        }

        // Priority 2: VAT Number (highly reliable, combined with postcode for extra confidence)
        if (!string.IsNullOrEmpty(record.VatNumber) && !string.IsNullOrEmpty(record.Postcode))
        {
            var normalizedPostcode = DataNormalizer.NormalizePostcode(record.Postcode) ?? record.Postcode;
            fingerprint.FingerprintKey = $"VAT|{record.VatNumber}|{normalizedPostcode}";
            fingerprint.FingerprintReason = $"VAT Number {record.VatNumber} + Postcode {normalizedPostcode}";
            fingerprint.Confidence = 90;
            return fingerprint;
        }

        // Priority 2b: FCA Firm Reference Number (alternative for regulated firms)
        if (!string.IsNullOrEmpty(record.FcaFirmRefNumber))
        {
            fingerprint.FingerprintKey = $"FCA|{record.FcaFirmRefNumber}";
            fingerprint.FingerprintReason = $"FCA Firm Reference Number: {record.FcaFirmRefNumber}";
            fingerprint.Confidence = 90;
            return fingerprint;
        }

        // Priority 3: Normalized Name + Postcode (general purpose, moderate reliability)
        if (!string.IsNullOrEmpty(record.Postcode))
        {
            var normalizedName = DataNormalizer.NormalizeName(record.Name) ?? record.Name;
            var normalizedPostcode = DataNormalizer.NormalizePostcode(record.Postcode) ?? record.Postcode;

            // Remove special characters for fingerprint stability
            var nameKey = RemoveSpecialChars(normalizedName);
            var postcodeKey = RemoveSpecialChars(normalizedPostcode);

            fingerprint.FingerprintKey = $"NAME_PC|{nameKey}|{postcodeKey}";
            fingerprint.FingerprintReason = $"Normalized Name '{nameKey}' + Postcode '{postcodeKey}'";
            fingerprint.Confidence = 65;
            return fingerprint;
        }

        // Fallback: Name + Phone (last resort)
        if (!string.IsNullOrEmpty(record.PhoneNumber))
        {
            var normalizedName = DataNormalizer.NormalizeName(record.Name) ?? record.Name;
            var phoneKey = RemoveSpecialChars(record.PhoneNumber);
            var nameKey = RemoveSpecialChars(normalizedName);

            fingerprint.FingerprintKey = $"NAME_PHONE|{nameKey}|{phoneKey}";
            fingerprint.FingerprintReason = $"Normalized Name '{nameKey}' + Phone '{phoneKey}'";
            fingerprint.Confidence = 50;
            return fingerprint;
        }

        // Last resort: Just normalized name (very low confidence, high collision risk)
        var fallbackName = DataNormalizer.NormalizeName(record.Name) ?? record.Name;
        fingerprint.FingerprintKey = $"NAME_ONLY|{RemoveSpecialChars(fallbackName)}";
        fingerprint.FingerprintReason = $"Normalized Name only: '{fallbackName}' (LOW CONFIDENCE)";
        fingerprint.Confidence = 30;

        return fingerprint;
    }

    /// <summary>
    /// Removes special characters from a string for fingerprint stability.
    /// Keeps only alphanumeric and spaces, converted to lowercase.
    /// </summary>
    private static string RemoveSpecialChars(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var c in input)
        {
            if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
            {
                sb.Append(c);
            }
        }

        // Collapse multiple spaces and trim
        var result = System.Text.RegularExpressions.Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        return result.ToLowerInvariant();
    }

    /// <summary>
    /// Checks if two fingerprints could represent the same dealer.
    /// Priority-based: Company Number > VAT > FCA > Name+Postcode
    /// </summary>
    public bool CouldMatch(DealerFingerprint other)
    {
        // Exact match
        if (this.FingerprintKey == other.FingerprintKey)
            return true;

        // If both have Company Numbers, they must match exactly (already checked above)
        if (this.FingerprintKey.StartsWith("CH|") && other.FingerprintKey.StartsWith("CH|"))
            return false;

        // If both have VAT Numbers, they must match exactly
        if (this.FingerprintKey.StartsWith("VAT|") && other.FingerprintKey.StartsWith("VAT|"))
            return false;

        // If both have FCA Numbers, they must match exactly
        if (this.FingerprintKey.StartsWith("FCA|") && other.FingerprintKey.StartsWith("FCA|"))
            return false;

        // Name+Postcode can match partially if they share the postcode
        if (this.FingerprintKey.StartsWith("NAME_PC|") && other.FingerprintKey.StartsWith("NAME_PC|"))
        {
            var thisParts = this.FingerprintKey.Split('|');
            var otherParts = other.FingerprintKey.Split('|');

            // Both should have format: NAME_PC|name|postcode
            if (thisParts.Length >= 3 && otherParts.Length >= 3)
            {
                // Match if postcode part is the same (high confidence match)
                return thisParts[2] == otherParts[2];
            }
        }

        return false;
    }

    public override string ToString() => FingerprintKey;
}
