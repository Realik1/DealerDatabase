using DealerDatabase.Import.Normalization;

namespace DealerDatabase.Import.Staging;

/// <summary>
/// Staging DTO for ICO data protection fee register records.
/// Captured from ico_register.csv rows.
/// </summary>
public class IcoStagingDto : StagingDtoBase
{
    public string? RegistrationNumber { get; set; }
    public string? OrganisationName { get; set; }
    public string? Address { get; set; }
    public string? Postcode { get; set; }
    public string? ExpiryDate { get; set; }

    public override NormalizedDealerRecord ToNormalizedRecord()
    {
        var normalized = new NormalizedDealerRecord
        {
            Source = SourceName,
            SourceId = SourceId,
            Name = OrganisationName ?? string.Empty,
            Address = Address,
            Postcode = Postcode,
            IcoRegistrationNumber = RegistrationNumber,
            RawData = RawJson
        };

        normalized.NormalizedName = DataNormalizer.NormalizeName(normalized.Name);
        normalized.NormalizedPostcode = DataNormalizer.NormalizePostcode(normalized.Postcode);
        normalized.MatchKey = DataNormalizer.GenerateMatchKey(normalized.NormalizedName ?? normalized.Name, normalized.NormalizedPostcode ?? normalized.Postcode);
        normalized.QualityScore = CalculateQualityScore(normalized);

        return normalized;
    }

    private int CalculateQualityScore(NormalizedDealerRecord record)
    {
        var score = 70; // Regulatory but not as authoritative, operational data source

        if (!string.IsNullOrEmpty(record.IcoRegistrationNumber))
            score += 5;

        return Math.Min(score, 100);
    }
}

/// <summary>
/// Staging DTO for SAF membership register records.
/// Captured from saf_members.xml Member elements.
/// </summary>
public class SafMembersStagingDto : StagingDtoBase
{
    public string? Name { get; set; }
    public string? LegalName { get; set; }
    public string? TradingAs { get; set; }
    public string? Town { get; set; }
    public string? Postcode { get; set; }
    public string? Telephone { get; set; }
    public string? Website { get; set; }
    public string? Status { get; set; }
    public string? Expiry { get; set; }

    public override NormalizedDealerRecord ToNormalizedRecord()
    {
        var primaryName = !string.IsNullOrWhiteSpace(TradingAs) ? TradingAs : Name;

        var normalized = new NormalizedDealerRecord
        {
            Source = SourceName,
            SourceId = SourceId,
            Name = primaryName ?? string.Empty,
            LegalName = LegalName ?? Name,
            Address = Town,
            Postcode = Postcode,
            PhoneNumber = Telephone,
            Website = Website,
            SafMemberStatus = Status,
            RawData = RawJson
        };

        normalized.NormalizedName = DataNormalizer.NormalizeName(normalized.Name);
        normalized.NormalizedPostcode = DataNormalizer.NormalizePostcode(normalized.Postcode);
        normalized.NormalizedPhoneNumber = DataNormalizer.NormalizePhoneNumber(normalized.PhoneNumber);
        normalized.NormalizedWebsite = DataNormalizer.NormalizeDomain(normalized.Website);
        normalized.MatchKey = DataNormalizer.GenerateMatchKey(normalized.NormalizedName ?? normalized.Name, normalized.NormalizedPostcode ?? normalized.Postcode);
        normalized.QualityScore = CalculateQualityScore(normalized);

        return normalized;
    }

    private int CalculateQualityScore(NormalizedDealerRecord record)
    {
        var score = 75; // Trade association - moderate authority

        if (!string.IsNullOrEmpty(record.SafMemberStatus) && record.SafMemberStatus?.Equals("Active", StringComparison.OrdinalIgnoreCase) == true)
            score += 5;

        return Math.Min(score, 100);
    }
}

/// <summary>
/// Staging DTO for VAT number lookup results.
/// One DTO per individual JSON file in vat_lookups/.
/// </summary>
public class VatLookupStagingDto : StagingDtoBase
{
    public string? CompanyName { get; set; }
    public string? VatNumber { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? AddressLine3 { get; set; }
    public string? Postcode { get; set; }
    public string? CountryCode { get; set; }
    public string? ProcessingDate { get; set; }

    public override NormalizedDealerRecord ToNormalizedRecord()
    {
        var normalized = new NormalizedDealerRecord
        {
            Source = SourceName,
            SourceId = SourceId,
            Name = CompanyName ?? string.Empty,
            VatNumber = VatNumber,
            Address = AddressLine1,
            AddressLine2 = AddressLine3 ?? AddressLine2,
            Postcode = Postcode,
            RawData = RawJson
        };

        normalized.NormalizedName = DataNormalizer.NormalizeName(normalized.Name);
        normalized.NormalizedPostcode = DataNormalizer.NormalizePostcode(normalized.Postcode);
        normalized.MatchKey = DataNormalizer.GenerateMatchKey(normalized.NormalizedName ?? normalized.Name, normalized.NormalizedPostcode ?? normalized.Postcode);
        normalized.QualityScore = CalculateQualityScore(normalized);

        return normalized;
    }

    private int CalculateQualityScore(NormalizedDealerRecord record)
    {
        var score = 80; // Regulatory VAT data - reliable

        if (!string.IsNullOrEmpty(record.VatNumber))
            score += 10;

        return Math.Min(score, 100);
    }
}

/// <summary>
/// Staging DTO for crawled dealer website data.
/// Captured from crawled_dealers.csv rows.
/// Wide, inconsistent, and potentially noisy data.
/// </summary>
public class CrawledDealersStagingDto : StagingDtoBase
{
    public string? DealerName { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Postcode { get; set; }
    public string? Website { get; set; }
    public string? Email { get; set; }

    public override NormalizedDealerRecord ToNormalizedRecord()
    {
        var normalized = new NormalizedDealerRecord
        {
            Source = SourceName,
            SourceId = SourceId,
            Name = DealerName ?? string.Empty,
            Address = Address,
            AddressLine2 = City,
            Postcode = Postcode,
            PhoneNumber = Phone,
            Website = Website,
            RawData = RawJson
        };

        normalized.NormalizedName = DataNormalizer.NormalizeName(normalized.Name);
        normalized.NormalizedPostcode = DataNormalizer.NormalizePostcode(normalized.Postcode);
        normalized.NormalizedPhoneNumber = DataNormalizer.NormalizePhoneNumber(normalized.PhoneNumber);
        normalized.NormalizedWebsite = DataNormalizer.NormalizeDomain(normalized.Website);
        normalized.MatchKey = DataNormalizer.GenerateMatchKey(normalized.NormalizedName ?? normalized.Name, normalized.NormalizedPostcode ?? normalized.Postcode);
        normalized.QualityScore = CalculateQualityScore(normalized);

        return normalized;
    }

    private int CalculateQualityScore(NormalizedDealerRecord record)
    {
        var score = 40; // Crawled data - lowest quality, noisy

        if (!string.IsNullOrEmpty(record.Postcode))
            score += 10;
        if (!string.IsNullOrEmpty(record.Website))
            score += 5;

        return Math.Min(score, 100);
    }
}
