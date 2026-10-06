using DealerDatabase.Import.Normalization;

namespace DealerDatabase.Import.Staging;

/// <summary>
/// Staging DTO for FCA Financial Services Register firms.
/// Captured from fca_register.json array.
/// </summary>
public class FcaStagingDto : StagingDtoBase
{
    public string? FirmReferenceNumber { get; set; }
    public string? Name { get; set; }
    public List<string>? TradingNames { get; set; }
    public string? AuthorisationStatus { get; set; }
    public string? FirmType { get; set; }
    public List<string>? PermissionTypes { get; set; } 

    public override NormalizedDealerRecord ToNormalizedRecord()
    {
        var normalized = new NormalizedDealerRecord
        {
            Source = SourceName,
            SourceId = SourceId,
            Name = TradingNames?.FirstOrDefault() ?? Name ?? string.Empty,
            LegalName = Name,
            FcaFirmRefNumber = FirmReferenceNumber,
            RawData = RawJson
        };

        // Normalize fields
        normalized.NormalizedName = DataNormalizer.NormalizeName(normalized.Name);
        normalized.MatchKey = DataNormalizer.GenerateMatchKey(normalized.NormalizedName ?? normalized.Name, normalized.NormalizedPostcode ?? "");

        normalized.QualityScore = CalculateQualityScore(normalized);

        return normalized;
    }

    private int CalculateQualityScore(NormalizedDealerRecord record)
    {
        var score = 85; // Regulatory data - high quality

        if (!string.IsNullOrEmpty(record.FcaFirmRefNumber))
            score += 15;

        if (AuthorisationStatus?.Equals("Active", StringComparison.OrdinalIgnoreCase) != true)
        {
            record.QualityFlags.Add($"FCA status: {AuthorisationStatus}");
            score -= 10;
        }

        return Math.Min(score, 100);
    }
}
