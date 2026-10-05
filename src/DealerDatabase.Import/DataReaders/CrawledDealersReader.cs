using CsvHelper;
using System.Globalization;
using System.Text.Json;

namespace DealerDatabase.Import.DataReaders;

/// <summary>
/// Reads crawled dealer data from website scraping in CSV format.
/// This data is typically wide, inconsistent, and may contain noise.
/// </summary>
public class CrawledDealersReader : IDataReader
{
    public string SourceName => "crawled_dealers";

    public async Task<IEnumerable<RawDealerRecord>> ReadAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourcePath))
            return Enumerable.Empty<RawDealerRecord>();

        var records = new List<RawDealerRecord>();

        using (var reader = new StreamReader(sourcePath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            await csv.ReadAsync();
            csv.ReadHeader();

            int recordCount = 0;
            while (await csv.ReadAsync())
            {
                // Try to extract common fields, using GetField with fallback to null
                var dealerId = csv.GetField("id") ?? csv.GetField("dealer_id") ?? recordCount.ToString();
                var name = csv.GetField("name") ?? csv.GetField("dealer_name") ?? string.Empty;
                var phone = GetFieldSafe(csv, "phone", "telephone", "contact_phone");
                var address = GetFieldSafe(csv, "address", "street", "street_address");
                var city = GetFieldSafe(csv, "city", "town", "locality");
                var postcode = GetFieldSafe(csv, "postcode", "zip", "postal_code");
                var website = GetFieldSafe(csv, "website", "url", "domain");

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var record = new RawDealerRecord
                {
                    Source = SourceName,
                    SourceId = dealerId ?? recordCount.ToString(),
                    Name = name,
                    Address = address,
                    AddressLine2 = city,
                    Postcode = postcode,
                    PhoneNumber = phone,
                    Website = website,
                    RawData = SerializeRowAsJson(csv)
                };

                records.Add(record);
                recordCount++;
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
