using System.Text.Json;
using System.Xml.Linq;

namespace DealerDatabase.Import.DataReaders;

/// <summary>
/// Reads SAF (Smaller Automotive Federation) membership register from XML format.
/// </summary>
public class SafMembersReader : IDataReader
{
    public string SourceName => "saf_members";

    public async Task<IEnumerable<RawDealerRecord>> ReadAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourcePath))
            return Enumerable.Empty<RawDealerRecord>();

        var records = new List<RawDealerRecord>();

        var xmlContent = await File.ReadAllTextAsync(sourcePath, cancellationToken);
        var xdoc = XDocument.Parse(xmlContent);

        var members = xdoc.Descendants("Member");

        foreach (var member in members)
        {
            var id = member.Attribute("id")?.Value ?? string.Empty;
            var name = member.Element("Name")?.Value ?? string.Empty;
            var legalName = member.Element("LegalName")?.Value;
            var tradingAs = member.Element("TradingAs")?.Value;
            var town = member.Element("Town")?.Value;
            var postcode = member.Element("Postcode")?.Value;
            var telephone = member.Element("Telephone")?.Value;
            var website = member.Element("Website")?.Value;
            var status = member.Element("Status")?.Value;
            var expiry = member.Element("Expiry")?.Value;

            // Use TradingAs if available, otherwise use Name
            var primaryName = !string.IsNullOrWhiteSpace(tradingAs) ? tradingAs : name;

            // Build address
            var address = town;

            var record = new RawDealerRecord
            {
                Source = SourceName,
                SourceId = id,
                Name = primaryName ?? string.Empty,
                LegalName = legalName ?? name,
                Address = address,
                Postcode = postcode,
                PhoneNumber = telephone,
                Website = website,
                SafMemberStatus = status,
                RawData = JsonSerializer.Serialize(new
                {
                    id,
                    name,
                    legalName,
                    tradingAs,
                    town,
                    postcode,
                    telephone,
                    website,
                    status,
                    expiry
                })
            };

            records.Add(record);
        }

        return records;
    }
}
