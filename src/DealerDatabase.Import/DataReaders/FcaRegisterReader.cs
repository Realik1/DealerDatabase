using System.Text.Json;
using System.Text.Json.Serialization;

namespace DealerDatabase.Import.DataReaders;

/// <summary>
/// Reads FCA Financial Services Register firms from JSON format.
/// </summary>
public class FcaRegisterReader : IDataReader
{
    public string SourceName => "fca_register";

    public async Task<IEnumerable<RawDealerRecord>> ReadAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourcePath))
            return Enumerable.Empty<RawDealerRecord>();

        var records = new List<RawDealerRecord>();

        var jsonContent = await File.ReadAllTextAsync(sourcePath, cancellationToken);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        try
        {
            var firmsList = JsonSerializer.Deserialize<List<FcaFirmRecord>>(jsonContent, options);
            if (firmsList == null)
                return records;

            foreach (var firm in firmsList)
            {
                var record = new RawDealerRecord
                {
                    Source = SourceName,
                    SourceId = firm.firm_reference_number ?? string.Empty,
                    Name = firm.name ?? firm.trading_names?.FirstOrDefault() ?? string.Empty,
                    LegalName = firm.name,
                    FcaFirmRefNumber = firm.firm_reference_number,
                    RawData = JsonSerializer.Serialize(firm)
                };

                records.Add(record);
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse FCA Register JSON: {ex.Message}", ex);
        }

        return records;
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
    private class FcaFirmRecord
    {
        public string? firm_reference_number { get; set; }
        public string? name { get; set; }
        public List<string>? trading_names { get; set; }
        public string? authorisation_status { get; set; }
        public string? firm_type { get; set; }
        public List<string>? permission_types { get; set; }
    }
}
