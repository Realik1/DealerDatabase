namespace DealerDatabase.Import.Staging;

/// <summary>
/// Base class for all source-specific staging DTOs.
/// Captures raw deserialized data and source metadata for tracing through the pipeline.
/// </summary>
public abstract class StagingDtoBase
{
    /// <summary>The source system this record came from.</summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>Unique identifier within the source system.</summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>The raw JSON/XML as it was deserialized for audit purposes.</summary>
    public string? RawJson { get; set; }

    /// <summary>When this record was imported or extracted from the source.</summary>
    public DateTime ImportedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Convert this staging DTO to the normalized intermediate model.
    /// Each subclass implements its own mapping logic.
    /// </summary>
    public abstract Normalization.NormalizedDealerRecord ToNormalizedRecord();
}

/// <summary>Authority/trustworthiness rank of a source (1=highest, 7=lowest).</summary>
public enum SourceAuthorityRank
{
    CompaniesHouse = 1, 
    FcaRegister = 2,
    IcoRegister = 3,
    VatLookups = 4,
    SafMembers = 5,
    MarketCheck = 6,
    CrawledDealers = 7
}

/// <summary>Maps source name to authority rank.</summary>
public static class SourceAuthorityHelper
{
    public static SourceAuthorityRank GetRank(string sourceName) => sourceName.ToLowerInvariant() switch
    {
        "companies_house" => SourceAuthorityRank.CompaniesHouse,
        "fca_register" => SourceAuthorityRank.FcaRegister,
        "ico_register" => SourceAuthorityRank.IcoRegister,
        "vat_lookups" => SourceAuthorityRank.VatLookups,
        "saf_members" => SourceAuthorityRank.SafMembers,
        "marketcheck" => SourceAuthorityRank.MarketCheck,
        "crawled_dealers" => SourceAuthorityRank.CrawledDealers,
        _ => SourceAuthorityRank.CrawledDealers
    };
}
