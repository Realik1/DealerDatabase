using DealerDatabase.Import.Normalization;
using System.Text.Json;

namespace DealerDatabase.Import.Staging;

/// <summary>
/// Staging DTO for MarketCheck CSV dealer records.
/// Represents one row from marketcheck_dealers.csv
/// </summary>
public class MarketCheckStagingDto : StagingDtoBase 
{
    public string? SellerName { get; set; }
    public string? SellerType { get; set; }
    public string? FranchiseMake { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? County { get; set; }
    public string? Postcode { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? Email { get; set; }
    public int? InventoryCount { get; set; }
    public decimal? AvgListedPrice { get; set; }
    public decimal? AvgSoldPrice { get; set; }
    public int? AvgDaysInStock { get; set; }
    public int? SoldLast30Days { get; set; }
    public string? VehicleTypes { get; set; }
    public string? LastSeen { get; set; }

    public override NormalizedDealerRecord ToNormalizedRecord()
    {
        var normalized = new NormalizedDealerRecord
        {
            Source = SourceName,
            SourceId = SourceId,
            Name = SellerName ?? string.Empty,
            Address = Street,
            AddressLine2 = City,
            Postcode = Postcode,
            PhoneNumber = Phone,
            Website = Website,
            RawData = RawJson
        };

        // Normalize fields
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
        var score = 55; // MarketCheck data is operational, moderate quality

        if (InventoryCount.HasValue && InventoryCount > 0)
            score += 10;
        if (!string.IsNullOrEmpty(record.Website))
            score += 5;
        if (!string.IsNullOrEmpty(record.PhoneNumber))
            score += 5;
        if (!string.IsNullOrEmpty(record.Postcode))
            score += 10;

        return Math.Min(score, 100);
    }
}
