using CsvHelper;
using System.Globalization;
using System.Text.Json;

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
                var registrationNumber = csv.GetField("Registration number")?.Trim() ?? string.Empty;
                var organisationName = csv.GetField("Organisation name")?.Trim() ?? string.Empty;
                var address = csv.GetField("Address")?.Trim();
                var postcode = csv.GetField("Postcode")?.Trim();
                var expiryDateStr = csv.GetField("Expiry date")?.Trim();

                if (string.IsNullOrEmpty(organisationName))
                    continue;

                var record = new RawDealerRecord
                {
                    Source = SourceName,
                    SourceId = registrationNumber,
                    Name = organisationName,
                    Address = address,
                    Postcode = postcode,
                    IcoRegistrationNumber = registrationNumber,
                    RawData = JsonSerializer.Serialize(new { registrationNumber, organisationName, address, postcode, expiryDateStr })
                };

                records.Add(record);
            }
        }

        return records;
    }
}
