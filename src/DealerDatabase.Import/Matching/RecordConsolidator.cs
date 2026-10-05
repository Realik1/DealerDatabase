using DealerDatabase.Import.Normalization;

namespace DealerDatabase.Import.Matching;

/// <summary>
/// Consolidates matched record clusters into single dealer records,
/// resolving conflicts and preferring higher-quality source data.
/// </summary>
public class RecordConsolidator
{
    /// <summary>
    /// Consolidates a cluster of matched records into a single dealer record.
    /// </summary>
    public ConsolidatedRecord Consolidate(RecordCluster cluster)
    {
        var consolidated = new ConsolidatedRecord
        {
            ClusterId = cluster.ClusterId,
            SourceRecords = cluster.Records
        };

        // Sort records by quality score to prefer better quality data
        var sortedRecords = cluster.Records.OrderByDescending(r => r.QualityScore).ToList();

        // Consolidate each field using a strategy (prefer best source, or merge)
        consolidated.Name = ConsolidateField(sortedRecords, r => r.NormalizedName ?? r.Name, "Name");
        consolidated.LegalName = ConsolidateField(sortedRecords, r => r.LegalName);
        consolidated.Address = ConsolidateField(sortedRecords, r => r.Address);
        consolidated.AddressLine2 = ConsolidateField(sortedRecords, r => r.AddressLine2);
        consolidated.Postcode = ConsolidateField(sortedRecords, r => r.NormalizedPostcode ?? r.Postcode);
        consolidated.PhoneNumber = ConsolidateField(sortedRecords, r => r.NormalizedPhoneNumber ?? r.PhoneNumber);
        consolidated.WebsiteDomain = ConsolidateField(sortedRecords, r => r.NormalizedWebsite ?? r.Website);

        // Registration numbers - prefer any that exists
        consolidated.CompaniesHouseNumber = sortedRecords
            .FirstOrDefault(r => !string.IsNullOrEmpty(r.CompaniesHouseNumber))?.CompaniesHouseNumber;

        consolidated.VatNumber = sortedRecords
            .FirstOrDefault(r => !string.IsNullOrEmpty(r.VatNumber))?.VatNumber;

        consolidated.FcaFirmRefNumber = sortedRecords
            .FirstOrDefault(r => !string.IsNullOrEmpty(r.FcaFirmRefNumber))?.FcaFirmRefNumber;

        consolidated.IcoRegistrationNumber = sortedRecords
            .FirstOrDefault(r => !string.IsNullOrEmpty(r.IcoRegistrationNumber))?.IcoRegistrationNumber;

        consolidated.SafMemberStatus = sortedRecords
            .FirstOrDefault(r => !string.IsNullOrEmpty(r.SafMemberStatus))?.SafMemberStatus;

        // Dates - use the earliest (or first occurrence)
        consolidated.IncorporationDate = sortedRecords
            .Where(r => r.IncorporationDate.HasValue)
            .Select(r => r.IncorporationDate)
            .OrderBy(d => d)
            .FirstOrDefault();

        consolidated.DissolutionDate = sortedRecords
            .Where(r => r.DissolutionDate.HasValue)
            .Select(r => r.DissolutionDate)
            .FirstOrDefault();

        // Check for conflicts
        consolidated.HasConflicts = DetectConflicts(cluster, consolidated);

        if (consolidated.HasConflicts)
        {
            consolidated.ConflictNotes = GenerateConflictNotes(cluster, consolidated);
        }

        return consolidated;
    }

    /// <summary>
    /// Consolidates a list of field values, picking the best one or merging.
    /// </summary>
    private string? ConsolidateField(
        List<NormalizedDealerRecord> records,
        Func<NormalizedDealerRecord, string?> extractor,
        string fieldName = "")
    {
        var values = records
            .Select(extractor)
            .Where(v => !string.IsNullOrEmpty(v))
            .Distinct()
            .ToList();

        if (values.Count == 0)
            return null;

        if (values.Count == 1)
            return values[0];

        // Multiple different values - return the one from highest quality record
        var bestRecord = records.FirstOrDefault(r => !string.IsNullOrEmpty(extractor(r)));
        return bestRecord != null ? extractor(bestRecord) : values[0];
    }

    /// <summary>
    /// Detects whether this cluster has conflicting information.
    /// </summary>
    private bool DetectConflicts(RecordCluster cluster, ConsolidatedRecord consolidated)
    {
        if (cluster.Records.Count <= 1)
            return false;

        var nameConflict = cluster.Records
            .Select(r => r.NormalizedName ?? r.Name)
            .Distinct()
            .Count(n => !string.IsNullOrEmpty(n)) > 1;

        var addressConflict = cluster.Records
            .Select(r => r.NormalizedPostcode ?? r.Postcode)
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct()
            .Count() > 1;

        var phoneConflict = cluster.Records
            .Select(r => r.NormalizedPhoneNumber ?? r.PhoneNumber)
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct()
            .Count() > 1;

        return nameConflict || addressConflict || phoneConflict;
    }

    /// <summary>
    /// Generates a human-readable report of conflicts in the cluster.
    /// </summary>
    private string GenerateConflictNotes(RecordCluster cluster, ConsolidatedRecord consolidated)
    {
        var notes = new List<string>();

        // Name conflicts
        var names = cluster.Records
            .Select(r => r.NormalizedName ?? r.Name)
            .Distinct()
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        if (names.Count > 1)
        {
            notes.Add($"Name variations: {string.Join("; ", names)}");
        }

        // Postcode conflicts
        var postcodes = cluster.Records
            .Select(r => r.NormalizedPostcode ?? r.Postcode)
            .Distinct()
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        if (postcodes.Count > 1)
        {
            notes.Add($"Postcode variations: {string.Join("; ", postcodes)}");
        }

        // Regulatory status conflicts
        var safStatuses = cluster.Records
            .Where(r => r.Source == "saf_members")
            .Select(r => r.SafMemberStatus)
            .Distinct()
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        if (safStatuses.Count > 1)
        {
            notes.Add($"SAF status varies: {string.Join("; ", safStatuses)}");
        }

        return string.Join(" | ", notes);
    }

    /// <summary>
    /// Consolidates all clusters in a batch.
    /// </summary>
    public List<ConsolidatedRecord> ConsolidateBatch(IEnumerable<RecordCluster> clusters)
    {
        return clusters.Select(Consolidate).ToList();
    }
}
