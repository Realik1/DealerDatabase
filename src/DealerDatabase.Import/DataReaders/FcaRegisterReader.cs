using System.Text.Json;
using System.Text.Json.Serialization;

namespace DealerDatabase.Import.DataReaders;

/// <summary>
/// Reads FCA Register firm records from JSON format.
/// </summary>
public class FcaRegisterReader : IDataReader
{
    public string SourceName => "fca_register";

    public async Task<IEnumerable<RawDealerRecord>> ReadAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourcePath))
            return Enumerable.Empty<RawDealerRecord>();

        var resultRecords = new List<RawDealerRecord>();

        var jsonContent = await File.ReadAllTextAsync(sourcePath, cancellationToken);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        try
        {
            var response = JsonSerializer.Deserialize<FcaRegisterResponse>(jsonContent, options);
            var firms = response?.Data ?? response?.Items;

            if (firms == null)
                return resultRecords;

            foreach (var firm in firms)
            {
                var frnString = firm.GetFrnString();
                var firmName = firm.GetNameString();

                // Skip records that have no name and no reference number
                if (string.IsNullOrWhiteSpace(frnString))
                    continue;

                // Fallback name to the FRN if the register entry lacks a formal name
                var resolvedName = !string.IsNullOrWhiteSpace(firmName) ? firmName : $"FCA Firm {frnString}";

                var record = new RawDealerRecord
                {
                    Source = SourceName,
                    SourceId = frnString ?? string.Empty,
                    Name = firmName ?? string.Empty,
                    LegalName = firmName,
                    FcaFirmRefNumber = frnString,
                    RawData = JsonSerializer.Serialize(firm)
                };

                resultRecords.Add(record);
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse FCA Register JSON: {ex.Message}", ex);
        }

        return resultRecords;
    }
}

public class FcaRegisterResponse
{
    [JsonPropertyName("data")]
    public List<FcaFirmRecord>? Data { get; set; }

    [JsonPropertyName("items")]
    public List<FcaFirmRecord>? Items { get; set; }
}

public class FcaFirmRecord
{
    [JsonPropertyName("FRN")]
    public JsonElement? Frn { get; set; }

    [JsonPropertyName("reference_number")]
    public JsonElement? ReferenceNumber { get; set; }

    [JsonPropertyName("name")]
    public JsonElement? Name { get; set; }

    [JsonPropertyName("firm_name")]
    public JsonElement? FirmName { get; set; }

    public string? GetFrnString()
    {
        if (Frn.HasValue && Frn.Value.ValueKind != JsonValueKind.Null)
            return Frn.Value.ToString();
        if (ReferenceNumber.HasValue && ReferenceNumber.Value.ValueKind != JsonValueKind.Null)
            return ReferenceNumber.Value.ToString();
        return null;
    }

    public string? GetNameString()
    {
        if (Name.HasValue && Name.Value.ValueKind != JsonValueKind.Null)
            return Name.Value.ToString();
        if (FirmName.HasValue && FirmName.Value.ValueKind != JsonValueKind.Null)
            return FirmName.Value.ToString();
        return null;
    }
}