using System.Diagnostics.CodeAnalysis;
using DealerDatabase.Import.Normalization;

namespace DealerDatabase.Import.Matching;

/// <summary>
/// Result of matching a record against a cluster of potential duplicates.
/// </summary>
public class MatchResult
{
    /// <summary>The best matching cluster this record belongs to (if any).</summary>
    public RecordCluster? BestMatchCluster { get; set; }

    /// <summary>Confidence score of the match (0-100).</summary>
    public int MatchConfidence { get; set; }

    /// <summary>Reason why this record matched or didn't match.</summary>
    public string? MatchReason { get; set; }

    /// <summary>Was a match found above the confidence threshold?</summary>
    [MemberNotNullWhen(true, nameof(BestMatchCluster))]
    public bool IsMatched => BestMatchCluster != null && MatchConfidence >= 50;
}

/// <summary>
/// Represents a cluster of records that are likely to be the same dealership.
/// </summary>
public class RecordCluster
{
    /// <summary>Unique identifier for this cluster.</summary>
    public string ClusterId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>All records in this cluster.</summary>
    public List<NormalizedDealerRecord> Records { get; set; } = new();

    /// <summary>The consolidated/primary record for this cluster.</summary>
    public ConsolidatedRecord? Consolidated { get; set; }

    /// <summary>Average confidence score across all records in cluster.</summary>
    public double AverageConfidence { get; set; }

    /// <summary>Whether there are conflicting values in this cluster.</summary>
    public bool HasConflicts { get; set; }
}

/// <summary>
/// A consolidated dealer record created by merging multiple source records.
/// </summary>
public class ConsolidatedRecord
{
    public string ClusterId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? Address { get; set; }
    public string? AddressLine2 { get; set; }
    public string? Postcode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? EmailAddress { get; set; }
    public string? WebsiteDomain { get; set; }
    public string? CompaniesHouseNumber { get; set; }
    public string? FcaFirmRefNumber { get; set; }
    public string? FcaStatus { get; set; }
    public string? VatNumber { get; set; }
    public string? VatValidationStatus { get; set; }
    public string? IcoRegistrationNumber { get; set; }
    public DateTime? IcoExpiryDate { get; set; }
    public string? SafMemberStatus { get; set; }
    public DateTime? SafExpiryDate { get; set; }
    public DateTime? IncorporationDate { get; set; }
    public DateTime? DissolutionDate { get; set; }
    public bool HasConflicts { get; set; }
    public string? ConflictNotes { get; set; }
    public List<NormalizedDealerRecord> SourceRecords { get; set; } = new();
}
