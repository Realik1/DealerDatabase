using DealerDatabase.Import.Normalization;

namespace DealerDatabase.Import.Staging;

/// <summary>
/// Staging DTO for Companies House company records from JSON.
/// Captured from companies_house.json array.
/// </summary>
public class CompaniesHouseStagingDto : StagingDtoBase
{
    public string? CompanyNumber { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyStatus { get; set; }
    public string? Type { get; set; }
    public string? DateOfCreation { get; set; } 
    public string? DateOfDissolution { get; set; }
    public AddressDto? RegisteredOfficeAddress { get; set; }
    public List<string>? SicCodes { get; set; }

    public class AddressDto
    {
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? Locality { get; set; }
        public string? Region { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
    }

    public override NormalizedDealerRecord ToNormalizedRecord()
    {
        var normalized = new NormalizedDealerRecord
        {
            Source = SourceName,
            SourceId = SourceId,
            Name = CompanyName ?? string.Empty,
            LegalName = CompanyName,
            CompaniesHouseNumber = CompanyNumber,
            IncorporationDate = DataNormalizer.ParseDate(DateOfCreation),
            DissolutionDate = DataNormalizer.ParseDate(DateOfDissolution),
            RawData = RawJson
        };

        if (RegisteredOfficeAddress != null)
        {
            normalized.Address = RegisteredOfficeAddress.AddressLine1;
            normalized.AddressLine2 = !string.IsNullOrEmpty(RegisteredOfficeAddress.AddressLine2)
                ? $"{RegisteredOfficeAddress.Locality}, {RegisteredOfficeAddress.Region}"
                : RegisteredOfficeAddress.Locality;
            normalized.Postcode = RegisteredOfficeAddress.PostalCode;
        }

        // Normalize fields
        normalized.NormalizedName = DataNormalizer.NormalizeName(normalized.Name);
        normalized.NormalizedPostcode = DataNormalizer.NormalizePostcode(normalized.Postcode);
        normalized.MatchKey = DataNormalizer.GenerateMatchKey(normalized.NormalizedName ?? normalized.Name, normalized.NormalizedPostcode ?? normalized.Postcode);

        normalized.QualityScore = CalculateQualityScore(normalized);

        return normalized;
    }

    private int CalculateQualityScore(NormalizedDealerRecord record)
    {
        var score = 90; // Regulatory data - high quality

        if (!string.IsNullOrEmpty(record.CompaniesHouseNumber))
            score += 10;

        if (record.DissolutionDate.HasValue && record.DissolutionDate < DateTime.Now)
        {
            record.QualityFlags.Add("Company is dissolved/inactive");
            score -= 20;
        }

        return Math.Min(score, 100);
    }
}
