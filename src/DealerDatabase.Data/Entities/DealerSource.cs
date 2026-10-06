namespace DealerDatabase.Data.Entities;

/// <summary>
/// Tracks which source records have been consolidated into a dealer.
/// Maintains lineage and enables audit trails, plus stores source-specific data.
/// </summary>
public class DealerSource
{
    public int Id { get; set; }

    /// <summary>The consolidated dealer this source belongs to.</summary>
    public int DealerId { get; set; }
    public Dealer? Dealer { get; set; }

    /// <summary>Name of the source system (e.g., "marketcheck", "companies_house", "fca_register", "ico_register").</summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>Unique identifier within the source system.</summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>The original name as recorded in the source.</summary>
    public string SourceName_Value { get; set; } = string.Empty;

    /// <summary>Address as recorded in the source (may differ from consolidated).</summary>
    public string? SourceAddress { get; set; }

    /// <summary>Postcode as recorded in the source.</summary>
    public string? SourcePostcode { get; set; }

    /// <summary>Phone number as recorded in the source.</summary>
    public string? SourcePhoneNumber { get; set; }

    /// <summary>Email address as recorded in the source.</summary>
    public string? SourceEmailAddress { get; set; }

    /// <summary>Confidence score of the match that linked this source to the dealer (0-100).</summary>
    public int MatchConfidence { get; set; }

    /// <summary>Reason or method used for matching (e.g., "fuzzy_name", "vat_number", "exact_match").</summary>
    public string? MatchReason { get; set; }

    /// <summary>When this source record was imported or last updated.</summary>
    public DateTime ImportedDate { get; set; } = DateTime.UtcNow;

    /// <summary>Raw JSON or serialized data from the source for reference/audit.</summary>
    public string? SourceData { get; set; }

    /// <summary>Source-specific stock figure (e.g., number of vehicles in inventory).</summary>
    public int? StockFigure { get; set; }

    /// <summary>Source-specific director/owner information (serialized as JSON if multiple).</summary>
    public string? DirectorInfo { get; set; }

    /// <summary>Source-specific finance calculator details or terms.</summary>
    public string? FinanceCalculatorDetails { get; set; }

    /// <summary>Source-specific vehicle type or category information.</summary>
    public string? VehicleTypeInfo { get; set; }

    /// <summary>Source-specific rating or review score, if available.</summary>
    public double? SourceRating { get; set; }

    /// <summary>Last validation date for this source record (e.g., when VAT or registration was verified).</summary>
    public DateTime? LastValidationDate { get; set; }
}
