using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text.Json;

namespace DealerDatabase.Import.DataReaders;

/// <summary>
/// Reads MarketCheck dealers from CSV format.
/// </summary>
public class MarketCheckReader : IDataReader
{
    public string SourceName => "marketcheck";

    public async Task<IEnumerable<RawDealerRecord>> ReadAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourcePath))
            return Enumerable.Empty<RawDealerRecord>();

        var records = new List<RawDealerRecord>();

        using (var reader = new StreamReader(sourcePath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var csvRecords = new List<MarketCheckCsvRecord>();
            await foreach (var record in csv.GetRecordsAsync<MarketCheckCsvRecord>(cancellationToken))
            {
                csvRecords.Add(record);
            }

            foreach (var record in csvRecords)
            {
                records.Add(new RawDealerRecord
                {
                    Source = SourceName,
                    SourceId = record.mc_dealer_id ?? string.Empty,
                    Name = record.seller_name ?? string.Empty,
                    Address = record.street,
                    AddressLine2 = record.city,
                    Postcode = record.postcode,
                    PhoneNumber = record.phone,
                    Website = record.website,
                    RawData = JsonSerializer.Serialize(record)
                });
            }
        }

        return records;
    }

    // CSV record class matching the header
    private class MarketCheckCsvRecord
    {
        public string? mc_dealer_id { get; set; }
        public string? seller_name { get; set; }
        public string? seller_type { get; set; }
        public string? franchise_make { get; set; }
        public string? street { get; set; }
        public string? city { get; set; }
        public string? county { get; set; }
        public string? postcode { get; set; }
        public string? phone { get; set; }
        public string? website { get; set; }
        public string? email { get; set; }
        public int? inventory_count { get; set; }
        public decimal? avg_listed_price { get; set; }
        public decimal? avg_sold_price { get; set; }
        public int? avg_days_in_stock { get; set; }
        public int? sold_last_30_days { get; set; }
        public string? vehicle_types { get; set; }
        public int? car_pct { get; set; }
        public int? van_pct { get; set; }
        public int? cnt_under_80k_miles_under_7yrs { get; set; }
        public int? cnt_under_100k_miles_under_10yrs { get; set; }
        public int? cnt_under_120k_miles_under_12yrs { get; set; }
        public int? cnt_over_20yrs_over_50k_miles { get; set; }
        public int? cnt_price_over_10k { get; set; }
        public int? cnt_price_over_20k { get; set; }
        public int? cnt_price_over_50k { get; set; }
        public string? stock_feed_provider { get; set; }
        public string? last_seen { get; set; }
    }
}
