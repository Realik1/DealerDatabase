using System.Text.Json;
using DealerDatabase.Import.Matching;
using DealerDatabase.Import.Normalization;
using DealerDatabase.Import.Staging;

namespace DealerDatabase.Import.Consolidation;

/// <summary>
/// Consolidates matched record clusters using source hierarchy rules.
/// When sources disagree, selects the most authoritative source for each field.
/// </summary>
public class ConflictResolver
{
    /// <summary>
    /// Consolidates a cluster of matched records into a single dealer record.
    /// Uses source hierarchy to select winning values for each field.
    /// </summary>
    public ConsolidatedRecord Resolve(RecordCluster cluster)
    {
        var consolidated = new ConsolidatedRecord
        {
            ClusterId = cluster.ClusterId,
            SourceRecords = cluster.Records,
            HasConflicts = false
        };

        // Group records by source authority rank for easier lookup
        var recordsByAuthority = cluster.Records
            .GroupBy(r => SourceAuthorityHelper.GetRank(r.Source))
            .OrderBy(g => g.Key) // Order by rank (1 = highest authority)
            .ToList();

        // Resolve Name (prefer legal name from high-authority sources, trading name from others)
        var nameOptions = cluster.Records
            .Select(r => (value: r.NormalizedName ?? r.Name, source: SourceAuthorityHelper.GetRank(r.Source)))
            .ToList();

        var selectedName = SourceHierarchy.SelectBest(nameOptions, SourceHierarchy.FieldAuthority.TradingName);
        if (selectedName?.value != null)
        {
            consolidated.Name = selectedName.Value.value;
        }

        // Resolve Legal Name
        var legalNameOptions = cluster.Records
            .Where(r => !string.IsNullOrEmpty(r.LegalName))
            .Select(r => (value: r.LegalName!, source: SourceAuthorityHelper.GetRank(r.Source)))
            .ToList();

        var selectedLegalName = SourceHierarchy.SelectBest(legalNameOptions, SourceHierarchy.FieldAuthority.LegalName);
        if (selectedLegalName?.value != null)
        {
            consolidated.LegalName = selectedLegalName.Value.value;
        }

        // Resolve Address (prefer registered from high-authority, operational from marketplace)
        var addressOptions = cluster.Records
            .Where(r => !string.IsNullOrEmpty(r.Address))
            .Select(r => (value: r.Address!, source: SourceAuthorityHelper.GetRank(r.Source)))
            .ToList();

        var selectedAddress = SourceHierarchy.SelectBest(addressOptions, SourceHierarchy.FieldAuthority.RegisteredAddress) 
            ?? SourceHierarchy.SelectBest(addressOptions, SourceHierarchy.FieldAuthority.OperationalAddress);

        if (selectedAddress?.value != null)
        {
            consolidated.Address = selectedAddress.Value.value;
        }

        // Resolve Postcode
        var postcodeOptions = cluster.Records
            .Where(r => !string.IsNullOrEmpty(r.NormalizedPostcode))
            .Select(r => (value: r.NormalizedPostcode, source: SourceAuthorityHelper.GetRank(r.Source)))
            .ToList();

        var selectedPostcode = SourceHierarchy.SelectBest(postcodeOptions, SourceHierarchy.FieldAuthority.Postcode);
        if (selectedPostcode?.value != null)
        {
            consolidated.Postcode = selectedPostcode.Value.value;
        }

        // Resolve Contact Details (phone, website)
        var phoneOptions = cluster.Records
            .Where(r => !string.IsNullOrEmpty(r.NormalizedPhoneNumber))
            .Select(r => (value: r.NormalizedPhoneNumber, source: SourceAuthorityHelper.GetRank(r.Source)))
            .ToList();

        var selectedPhone = SourceHierarchy.SelectBest(phoneOptions, SourceHierarchy.FieldAuthority.PhoneNumber);
        if (selectedPhone?.value != null)
        {
            consolidated.PhoneNumber = selectedPhone.Value.value;
        }

        var websiteOptions = cluster.Records
            .Where(r => !string.IsNullOrEmpty(r.NormalizedWebsite))
            .Select(r => (value: r.NormalizedWebsite, source: SourceAuthorityHelper.GetRank(r.Source)))
            .ToList();

        var selectedWebsite = SourceHierarchy.SelectBest(websiteOptions, SourceHierarchy.FieldAuthority.Website);
        if (selectedWebsite?.value != null)
        {
            consolidated.WebsiteDomain = selectedWebsite.Value.value;
        }

        // Resolve Regulatory Numbers (one per source, definitive if present)
        consolidated.CompaniesHouseNumber = cluster.Records
            .Where(r => !string.IsNullOrEmpty(r.CompaniesHouseNumber))
            .Select(r => r.CompaniesHouseNumber)
            .FirstOrDefault();

        consolidated.FcaFirmRefNumber = cluster.Records
            .Where(r => !string.IsNullOrEmpty(r.FcaFirmRefNumber))
            .Select(r => r.FcaFirmRefNumber)
            .FirstOrDefault();

        var vatOptions = cluster.Records
            .Where(r => !string.IsNullOrEmpty(r.VatNumber))
            .Select(r => (value: r.VatNumber, source: SourceAuthorityHelper.GetRank(r.Source)))
            .ToList();

        var selectedVat = SourceHierarchy.SelectBest(vatOptions, SourceHierarchy.FieldAuthority.VatNumber);
        if (selectedVat?.value != null)
        {
            consolidated.VatNumber = selectedVat.Value.value;
        }

        consolidated.IcoRegistrationNumber = cluster.Records
            .Where(r => !string.IsNullOrEmpty(r.IcoRegistrationNumber))
            .Select(r => r.IcoRegistrationNumber)
            .FirstOrDefault();

        consolidated.SafMemberStatus = cluster.Records
            .Where(r => !string.IsNullOrEmpty(r.SafMemberStatus))
            .Select(r => r.SafMemberStatus)
            .FirstOrDefault();

        // Resolve Dates
        var incorporationOptions = cluster.Records
            .Where(r => r.IncorporationDate.HasValue)
            .Select(r => (value: r.IncorporationDate, source: SourceAuthorityHelper.GetRank(r.Source)))
            .ToList();

        var selectedInc = SourceHierarchy.SelectBestDate(incorporationOptions, SourceHierarchy.FieldAuthority.IncorporationDate);
        if (selectedInc?.value.HasValue == true)
        {
            consolidated.IncorporationDate = selectedInc.Value.value;
        }

        var dissolutionOptions = cluster.Records
            .Where(r => r.DissolutionDate.HasValue)
            .Select(r => (value: r.DissolutionDate, source: SourceAuthorityHelper.GetRank(r.Source)))
            .ToList();

        var selectedDiss = SourceHierarchy.SelectBestDate(dissolutionOptions, SourceHierarchy.FieldAuthority.DissolutionDate);
        if (selectedDiss?.value.HasValue == true)
        {
            consolidated.DissolutionDate = selectedDiss.Value.value;
        }

        // Detect conflicts
        consolidated.HasConflicts = DetectConflicts(cluster);
        if (consolidated.HasConflicts)
        {
            consolidated.ConflictNotes = GenerateConflictReport(cluster, consolidated);
        }

        return consolidated;
    }

    /// <summary>
    /// Detects whether this cluster has conflicting information across sources.
    /// </summary>
    private bool DetectConflicts(RecordCluster cluster)
    {
        if (cluster.Records.Count <= 1)
            return false;

        // Check for name conflicts
        var names = cluster.Records
            .Select(r => r.NormalizedName ?? r.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count(n => !string.IsNullOrEmpty(n));

        if (names > 1)
            return true;

        // Check for postcode conflicts
        var postcodes = cluster.Records
            .Select(r => r.NormalizedPostcode ?? r.Postcode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count(p => !string.IsNullOrEmpty(p));

        if (postcodes > 1)
            return true;

        // Check for regulatory number conflicts (same type from different sources with different values)
        var companyNumbers = cluster.Records
            .Where(r => !string.IsNullOrEmpty(r.CompaniesHouseNumber))
            .Select(r => r.CompaniesHouseNumber)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        if (companyNumbers > 1)
            return true;

        var vatNumbers = cluster.Records
            .Where(r => !string.IsNullOrEmpty(r.VatNumber))
            .Select(r => r.VatNumber)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        if (vatNumbers > 1)
            return true;

        return false;
    }

    /// <summary>
    /// Generates a detailed conflict report explaining all discrepancies found.
    /// </summary>
    private string GenerateConflictReport(RecordCluster cluster, ConsolidatedRecord consolidated)
    {
        var report = new List<string>();

        // Name conflicts
        var names = cluster.Records
            .Select(r => new { Source = r.Source, Name = r.NormalizedName ?? r.Name })
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1 || cluster.Records.Count > 1)
            .ToList();

        if (names.Count > 1)
        {
            var nameList = string.Join("; ", names.Select(n => $"{n.First().Name} ({string.Join(", ", n.Select(x => x.Source))})"));
            report.Add($"NAME CONFLICT: Multiple names recorded - {nameList}");
        }

        // Postcode conflicts
        var postcodes = cluster.Records
            .Select(r => new { Source = r.Source, Postcode = r.NormalizedPostcode ?? r.Postcode })
            .Where(x => !string.IsNullOrEmpty(x.Postcode))
            .GroupBy(x => x.Postcode, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();

        if (postcodes.Count > 1)
        {
            var postcodeList = string.Join("; ", postcodes.Select(p => $"{p.Key} ({string.Join(", ", p.Select(x => x.Source))})"));
            report.Add($"POSTCODE CONFLICT: Different locations - {postcodeList}");
        }

        // Regulatory number conflicts
        var chNumbers = cluster.Records
            .Where(r => !string.IsNullOrEmpty(r.CompaniesHouseNumber))
            .GroupBy(r => r.CompaniesHouseNumber)
            .Where(g => g.Count() > 1)
            .ToList();

        if (chNumbers.Count > 1)
        {
            report.Add($"COMPANY NUMBER CONFLICT: Multiple company numbers - {string.Join("; ", chNumbers.Select(g => g.Key))}");
        }

        // Source count summary
        var sourcesByRank = cluster.Records
            .GroupBy(r => SourceAuthorityHelper.GetRank(r.Source))
            .OrderBy(g => g.Key)
            .ToList();

        var sourceSummary = string.Join(", ", sourcesByRank.Select(g => $"{g.Count()} from {g.Key}"));
        report.Insert(0, $"SOURCES USED: {sourceSummary}; Selected: {string.Join(", ", sourcesByRank.Select(g => g.First().Source).Distinct())}");

        return string.Join(" | ", report);
    }

    /// <summary>
    /// Consolidates all clusters in a batch.
    /// </summary>
    public List<ConsolidatedRecord> ResolveBatch(IEnumerable<RecordCluster> clusters)
    {
        return clusters.Select(Resolve).ToList();
    }
}
