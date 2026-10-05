using System.Text.Json;
using System.Text.Json.Serialization;

namespace DealerDatabase.Import.DataReaders;

/// <summary>
/// Reads VAT number lookup results from JSON files in the vat_lookups directory.
/// </summary>
public class VatLookupsReader : IDataReader
{
    public string SourceName => "vat_lookups";

    public async Task<IEnumerable<RawDealerRecord>> ReadAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        var records = new List<RawDealerRecord>();

        // If sourcePath is a directory, read all JSON files in it
        if (Directory.Exists(sourcePath))
        {
            var jsonFiles = Directory.EnumerateFiles(sourcePath, "*.json");

            foreach (var jsonFile in jsonFiles)
            {
                var fileRecords = await ReadVatLookupFile(jsonFile, cancellationToken);
                records.AddRange(fileRecords);
            }
        }
        else if (File.Exists(sourcePath))
        {
            var fileRecords = await ReadVatLookupFile(sourcePath, cancellationToken);
            records.AddRange(fileRecords);
        }

        return records;
    }

    private async Task<IEnumerable<RawDealerRecord>> ReadVatLookupFile(string filePath, CancellationToken cancellationToken)
    {
        var records = new List<RawDealerRecord>();

        try
        {
            var jsonContent = await File.ReadAllTextAsync(filePath, cancellationToken);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var lookup = JsonSerializer.Deserialize<VatLookup>(jsonContent, options);
            if (lookup?.target == null)
                return records;

            var target = lookup.target;
            var fileName = Path.GetFileNameWithoutExtension(filePath);

            var record = new RawDealerRecord
            {
                Source = SourceName,
                SourceId = target.vatNumber ?? fileName,
                Name = target.name ?? string.Empty,
                VatNumber = target.vatNumber,
                RawData = jsonContent
            };

            if (target.address != null)
            {
                record.Address = target.address.line1;
                record.AddressLine2 = target.address.line3 ?? target.address.line2;
                record.Postcode = target.address.postcode;
            }

            records.Add(record);
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Warning: Failed to parse VAT lookup file {Path.GetFileName(filePath)}: {ex.Message}");
        }

        return records;
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    private class VatLookup
    {
        public VatTarget? target { get; set; }
        public string? processingDate { get; set; }
    }

    private class VatTarget
    {
        public string? name { get; set; }
        public string? vatNumber { get; set; }
        public VatAddress? address { get; set; }
    }

    private class VatAddress
    {
        public string? line1 { get; set; }
        public string? line2 { get; set; }
        public string? line3 { get; set; }
        public string? postcode { get; set; }
        public string? countryCode { get; set; }
    }
}
