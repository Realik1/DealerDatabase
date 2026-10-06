using CsvHelper;
using System.Globalization;
using System.Text.Json;
using CsvHelper.Configuration;

namespace DealerDatabase.Import.DataReaders;

/// <summary>
/// Reads ICO (Information Commissioner's Office) data protection register from CSV format.
/// </summary>
public class IcoRegisterReader : IDataReader
{
    public string SourceName => "ico_register";

    public async Task<IEnumerable<RawDealerRecord>> ReadAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourcePath))
            return Enumerable.Empty<RawDealerRecord>();

        var records = new List<RawDealerRecord>();

        using (var reader = new StreamReader(sourcePath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            // Skip any initial blank lines or header comments
            await csv.ReadAsync();
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                // Use GetFieldSafe to match the actual CSV header names (e.g., "Registration_number")
                var registrationNumber = GetFieldSafe(csv, "Registration_number", "Registration number") ?? string.Empty;
                var organisationName = GetFieldSafe(csv, "Organisation_name", "Organisation name") ?? string.Empty;
                var addressLine1 = GetFieldSafe(csv, "Organisation_address_line_1", "Address");
                var addressLine2 = GetFieldSafe(csv, "Organisation_address_line_2");
                var postcode = GetFieldSafe(csv, "Organisation_postcode", "Postcode");
                var expiryDateStr = GetFieldSafe(csv, "End_date_of_registration", "Expiry date");

                if (string.IsNullOrEmpty(organisationName))
                    continue;

                var record = new RawDealerRecord
                {
                    Source = SourceName,
                    SourceId = registrationNumber,
                    Name = organisationName,
                    Address = addressLine1,
                    AddressLine2 = addressLine2,
                    Postcode = postcode,
                    IcoRegistrationNumber = registrationNumber,
                    RawData = SerializeRowAsJson(csv)
                };

                records.Add(record);
            }
        }

        return records;
    }

    private static string? GetFieldSafe(CsvReader csv, params string[] fieldNames)
    {
        foreach (var fieldName in fieldNames)
        {
            try
            {
                var value = csv.GetField(fieldName);
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
            catch
            {
                // Field doesn't exist, try next
            }
        }
        return null;
    }

    private static string SerializeRowAsJson(CsvReader csv)
    {
        var dict = new Dictionary<string, object?>();
        if (csv.HeaderRecord != null)
        {
            foreach (var header in csv.HeaderRecord)
            {
                try
                {
                    var value = csv.GetField(header);
                    dict[header] = value;
                }
                catch
                {
                    // Field doesn't exist
                }
            }
        }
        return JsonSerializer.Serialize(dict);
    }
}