using System.Text.Json;
using System.Text.Json.Serialization;

namespace DealerDatabase.Import.DataReaders;

/// <summary>
/// Reads Companies House company records from JSON format.
/// </summary>
public class CompaniesHouseReader : IDataReader
{
    public string SourceName => "companies_house";

    public async Task<IEnumerable<RawDealerRecord>> ReadAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourcePath))
            return Enumerable.Empty<RawDealerRecord>();

        var resultRecords = new List<RawDealerRecord>();

        var jsonContent = await File.ReadAllTextAsync(sourcePath, cancellationToken);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        try
        {
            var response = JsonSerializer.Deserialize<CompaniesHouseResponse>(jsonContent, options);
            var companies = response?.Items;

            if (companies == null)
                return resultRecords;

            foreach (var company in companies)
            {
                var record = new RawDealerRecord
                {
                    Source = SourceName,
                    SourceId = company.company_number ?? string.Empty,
                    Name = company.company_name ?? string.Empty,
                    LegalName = company.company_name,
                    CompaniesHouseNumber = company.company_number,
                    IncorporationDate = ParseDate(company.date_of_creation),
                    DissolutionDate = ParseDate(company.date_of_dissolution),
                    RawData = JsonSerializer.Serialize(company)
                };

                if (company.registered_office_address != null)
                {
                    var addr = company.registered_office_address;
                    record.Address = addr.address_line_1;
                    record.AddressLine2 = !string.IsNullOrEmpty(addr.address_line_2)
                        ? $"{addr.locality}, {addr.region}"
                        : addr.locality;
                    record.Postcode = addr.postal_code;
                }

                resultRecords.Add(record);
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse Companies House JSON: {ex.Message}", ex);
        }

        return resultRecords;
    }

    private static DateTime? ParseDate(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString))
            return null;

        if (DateTime.TryParse(dateString, out var date))
            return date;

        return null;
    }
}

public class CompaniesHouseResponse
{
    [JsonPropertyName("items")]
    public List<CompaniesHouseRecord> Items { get; set; } = new();
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
public class CompaniesHouseRecord
{
    public string? company_number { get; set; }
    public string? company_name { get; set; }
    public string? company_status { get; set; }
    public string? type { get; set; }
    public string? date_of_creation { get; set; }
    public string? date_of_dissolution { get; set; }
    public Address? registered_office_address { get; set; }
    public List<string>? sic_codes { get; set; }
    public List<Officer>? officers { get; set; }
}

public class Address
{
    public string? address_line_1 { get; set; }
    public string? address_line_2 { get; set; }
    public string? locality { get; set; }
    public string? region { get; set; }
    public string? postal_code { get; set; }
    public string? country { get; set; }
}

public class Officer
{
    public string? name { get; set; }
    public string? officer_role { get; set; }
    public string? appointed_on { get; set; }
    public string? resigned_on { get; set; }
    public string? occupation { get; set; }
    public string? nationality { get; set; }
}