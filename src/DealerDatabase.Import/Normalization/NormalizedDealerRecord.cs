using DealerDatabase.Import.DataReaders;

namespace DealerDatabase.Import.Normalization;

/// <summary>
/// A raw dealer record after normalization has been applied.
/// Ready for matching and consolidation.
/// </summary>
public class NormalizedDealerRecord : RawDealerRecord
{
    /// <summary>Normalized name generated from the original name.</summary>
    public string? NormalizedName { get; set; }

    /// <summary>Normalized postcode.</summary>
    public string? NormalizedPostcode { get; set; }

    /// <summary>Normalized phone number.</summary>
    public string? NormalizedPhoneNumber { get; set; }

    /// <summary>Normalized website domain (without protocol or www).</summary>
    public string? NormalizedWebsite { get; set; }

    /// <summary>
    /// Match key used for identifying potential duplicates.
    /// Combination of name and postcode with special characters removed.
    /// </summary>
    public string? MatchKey { get; set; }

    /// <summary>Score indicating data quality (0-100, higher is better).</summary>
    public int QualityScore { get; set; }

    /// <summary>Reasons or flags that might affect matching confidence.</summary>
    public List<string> QualityFlags { get; set; } = new();
}
