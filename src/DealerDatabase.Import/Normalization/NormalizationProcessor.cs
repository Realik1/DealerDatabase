using DealerDatabase.Import.DataReaders;

namespace DealerDatabase.Import.Normalization;

/// <summary>
/// Processes raw dealer records, applying normalization and calculating quality scores.
/// </summary>
public class NormalizationProcessor
{
    /// <summary>
    /// Normalizes a raw dealer record.
    /// </summary>
    public NormalizedDealerRecord Normalize(RawDealerRecord rawRecord)
    {
        var normalized = new NormalizedDealerRecord
        {
            // Copy base fields
            Source = rawRecord.Source,
            SourceId = rawRecord.SourceId,
            Name = rawRecord.Name,
            LegalName = rawRecord.LegalName,
            Address = rawRecord.Address,
            AddressLine2 = rawRecord.AddressLine2,
            Postcode = rawRecord.Postcode,
            PhoneNumber = rawRecord.PhoneNumber,
            Website = rawRecord.Website,
            CompaniesHouseNumber = rawRecord.CompaniesHouseNumber,
            FcaFirmRefNumber = rawRecord.FcaFirmRefNumber,
            VatNumber = rawRecord.VatNumber,
            IcoRegistrationNumber = rawRecord.IcoRegistrationNumber,
            SafMemberStatus = rawRecord.SafMemberStatus,
            IncorporationDate = rawRecord.IncorporationDate,
            DissolutionDate = rawRecord.DissolutionDate,
            RawData = rawRecord.RawData,

            // Apply normalization
            NormalizedName = DataNormalizer.NormalizeName(rawRecord.Name),
            NormalizedPostcode = DataNormalizer.NormalizePostcode(rawRecord.Postcode),
            NormalizedPhoneNumber = DataNormalizer.NormalizePhoneNumber(rawRecord.PhoneNumber),
            NormalizedWebsite = DataNormalizer.NormalizeDomain(rawRecord.Website)
        };

        // Create match key for duplicate detection
        normalized.MatchKey = DataNormalizer.GenerateMatchKey(
            normalized.NormalizedName ?? normalized.Name,
            normalized.NormalizedPostcode ?? normalized.Postcode
        );

        // Calculate quality score
        normalized.QualityScore = CalculateQualityScore(normalized);

        return normalized;
    }

    /// <summary>
    /// Calculates a quality score for a record (0-100).
    /// Higher score = more complete and reliable data.
    /// </summary>
    private int CalculateQualityScore(NormalizedDealerRecord record)
    {
        var score = 50; // Base score

        // Add points for having key identifiers
        if (!string.IsNullOrEmpty(record.CompaniesHouseNumber))
            score += 15;
        if (!string.IsNullOrEmpty(record.VatNumber))
            score += 15;
        if (!string.IsNullOrEmpty(record.FcaFirmRefNumber))
            score += 10;

        // Add points for name quality
        if (!string.IsNullOrEmpty(record.NormalizedName) && record.NormalizedName.Length >= 5)
            score += 5;

        // Add points for address completeness
        if (!string.IsNullOrEmpty(record.Address) && !string.IsNullOrEmpty(record.NormalizedPostcode))
            score += 5;

        // Add points for contact details
        if (!string.IsNullOrEmpty(record.PhoneNumber))
            score += 5;
        if (!string.IsNullOrEmpty(record.Website))
            score += 5;

        // Flag issues that reduce confidence
        if (record.DissolutionDate.HasValue && record.DissolutionDate < DateTime.Now)
        {
            record.QualityFlags.Add("Company may be dissolved or inactive");
            score -= 20;
        }

        if (string.IsNullOrEmpty(record.NormalizedPostcode))
        {
            record.QualityFlags.Add("Missing or invalid postcode");
            score -= 10;
        }

        if (record.Name.Length > 200)
        {
            record.QualityFlags.Add("Name is unusually long");
            score -= 5;
        }

        // Ensure score is within bounds
        return Math.Max(0, Math.Min(100, score));
    }

    /// <summary>
    /// Normalizes a batch of records.
    /// </summary>
    public IEnumerable<NormalizedDealerRecord> NormalizeBatch(IEnumerable<RawDealerRecord> records)
    {
        return records.Select(Normalize).ToList();
    }
}
