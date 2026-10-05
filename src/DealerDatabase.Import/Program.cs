using DealerDatabase.Data;
using DealerDatabase.Data.Entities;
using DealerDatabase.Import.DataReaders;
using DealerDatabase.Import.Matching;
using DealerDatabase.Import.Normalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDealerDatabase();

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

await using (var scope = host.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DealerDbContext>();
    await db.Database.MigrateAsync();
}

logger.LogInformation("Database: {DatabaseFile}", SolutionPaths.DatabaseFile);
logger.LogInformation("Data folder: {DataDirectory}", SolutionPaths.DataDirectory);

// Step 1: Discover all source files
logger.LogInformation("=== Discovering Data Sources ===");
var sourceFiles = DiscoverSourceFiles(SolutionPaths.DataDirectory);
foreach (var sourceFile in sourceFiles)
{
    logger.LogInformation("  Found source: {Source} -> {Path}", sourceFile.source, Path.GetFileName(sourceFile.path));
}

// Step 2: Read and collect all raw records from all sources
logger.LogInformation("\n=== Reading Source Data ===");
var allRawRecords = new List<RawDealerRecord>();

foreach (var (source, path) in sourceFiles)
{
    var reader = CreateReader(source);
    if (reader == null)
    {
        logger.LogWarning("  No reader for source: {Source}", source);
        continue;
    }

    try
    {
        logger.LogInformation("  Reading {Source}...", source);
        var records = await reader.ReadAsync(path);
        var recordList = records.ToList();
        allRawRecords.AddRange(recordList);
        logger.LogInformation("    Loaded {Count} records", recordList.Count);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error reading source {Source}: {Message}", source, ex.Message);
    }
}

logger.LogInformation("Total raw records loaded: {Count}", allRawRecords.Count);

// Step 3: Normalize and clean data
logger.LogInformation("\n=== Normalizing Data ===");
var normalizer = new NormalizationProcessor();
var normalizedRecords = normalizer.NormalizeBatch(allRawRecords).ToList();
logger.LogInformation("Normalized {Count} records", normalizedRecords.Count);

// Step 4: Match records across sources
logger.LogInformation("\n=== Matching Records ===");
var matcher = new RecordMatcher();
var clusters = matcher.MatchRecords(normalizedRecords);
logger.LogInformation("Created {Count} clusters", clusters.Count);

// Stats
var singletonClusters = clusters.Count(c => c.Records.Count == 1);
var multiRecordClusters = clusters.Count(c => c.Records.Count > 1);
var conflictClusters = clusters.Count(c => c.HasConflicts);
logger.LogInformation("  - {SingletonCount} clusters with 1 record", singletonClusters);
logger.LogInformation("  - {MultiRecordCount} clusters with multiple records", multiRecordClusters);
logger.LogInformation("  - {ConflictCount} clusters with conflicts", conflictClusters);

// Step 5: Consolidate matched records
logger.LogInformation("\n=== Consolidating Records ===");
var consolidator = new RecordConsolidator();
var consolidatedRecords = consolidator.ConsolidateBatch(clusters);
logger.LogInformation("Created {Count} consolidated records", consolidatedRecords.Count);

// Step 6: Persist to database
logger.LogInformation("\n=== Persisting to Database ===");

await using (var scope = host.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DealerDbContext>();

    // Check for existing dealers to ensure idempotency
    var existingDealerCount = await db.Dealers.CountAsync();
    logger.LogInformation("Existing dealers in database: {Count}", existingDealerCount);

    var insertedCount = 0;
    var updatedCount = 0;

    foreach (var consolidatedRecord in consolidatedRecords)
    {
        // Try to find existing dealer by registration numbers (for idempotency)
        Dealer? existingDealer = null;

        if (!string.IsNullOrEmpty(consolidatedRecord.CompaniesHouseNumber))
        {
            existingDealer = await db.Dealers
                .FirstOrDefaultAsync(d => d.CompaniesHouseNumber == consolidatedRecord.CompaniesHouseNumber);
        }

        if (existingDealer == null && !string.IsNullOrEmpty(consolidatedRecord.VatNumber))
        {
            existingDealer = await db.Dealers
                .FirstOrDefaultAsync(d => d.VatNumber == consolidatedRecord.VatNumber);
        }

        if (existingDealer == null && !string.IsNullOrEmpty(consolidatedRecord.FcaFirmRefNumber))
        {
            existingDealer = await db.Dealers
                .FirstOrDefaultAsync(d => d.FcaFirmRefNumber == consolidatedRecord.FcaFirmRefNumber);
        }

        if (existingDealer != null)
        {
            // Update existing dealer (but preserve ID)
            existingDealer.Name = consolidatedRecord.Name;
            existingDealer.LegalName = consolidatedRecord.LegalName;
            existingDealer.Address = consolidatedRecord.Address;
            existingDealer.AddressLine2 = consolidatedRecord.AddressLine2;
            existingDealer.Postcode = consolidatedRecord.Postcode;
            existingDealer.PhoneNumber = consolidatedRecord.PhoneNumber;
            existingDealer.WebsiteDomain = consolidatedRecord.WebsiteDomain;
            existingDealer.CompaniesHouseNumber = consolidatedRecord.CompaniesHouseNumber;
            existingDealer.FcaFirmRefNumber = consolidatedRecord.FcaFirmRefNumber;
            existingDealer.VatNumber = consolidatedRecord.VatNumber;
            existingDealer.IcoRegistrationNumber = consolidatedRecord.IcoRegistrationNumber;
            existingDealer.SafMemberStatus = consolidatedRecord.SafMemberStatus;
            existingDealer.IncorporationDate = consolidatedRecord.IncorporationDate;
            existingDealer.DissolutionDate = consolidatedRecord.DissolutionDate;
            existingDealer.HasConflicts = consolidatedRecord.HasConflicts;
            existingDealer.Notes = consolidatedRecord.ConflictNotes;
            existingDealer.LastModifiedDate = DateTime.UtcNow;

            db.Dealers.Update(existingDealer);
            updatedCount++;
        }
        else
        {
            // Create new dealer
            var dealer = new Dealer
            {
                Name = consolidatedRecord.Name,
                LegalName = consolidatedRecord.LegalName,
                Address = consolidatedRecord.Address,
                AddressLine2 = consolidatedRecord.AddressLine2,
                Postcode = consolidatedRecord.Postcode,
                PhoneNumber = consolidatedRecord.PhoneNumber,
                WebsiteDomain = consolidatedRecord.WebsiteDomain,
                CompaniesHouseNumber = consolidatedRecord.CompaniesHouseNumber,
                FcaFirmRefNumber = consolidatedRecord.FcaFirmRefNumber,
                VatNumber = consolidatedRecord.VatNumber,
                IcoRegistrationNumber = consolidatedRecord.IcoRegistrationNumber,
                SafMemberStatus = consolidatedRecord.SafMemberStatus,
                IncorporationDate = consolidatedRecord.IncorporationDate,
                DissolutionDate = consolidatedRecord.DissolutionDate,
                HasConflicts = consolidatedRecord.HasConflicts,
                Notes = consolidatedRecord.ConflictNotes,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            db.Dealers.Add(dealer);
            insertedCount++;
        }

        // Add source records for audit trail
        if (existingDealer?.Id > 0)
        {
            // Remove old source records for this dealer
            var oldSources = await db.DealerSources
                .Where(s => s.DealerId == existingDealer.Id)
                .ToListAsync();
            db.DealerSources.RemoveRange(oldSources);
        }
    }

    // Save all changes
    await db.SaveChangesAsync();

    logger.LogInformation("  - {InsertedCount} new dealers inserted", insertedCount);
    logger.LogInformation("  - {UpdatedCount} existing dealers updated", updatedCount);
    logger.LogInformation("Total operations: {Total}", insertedCount + updatedCount);
}

logger.LogInformation("\n=== Import Complete ===");
logger.LogInformation("Successfully imported and consolidated dealer records.");

// Helper method to discover source files
static List<(string source, string path)> DiscoverSourceFiles(string dataDirectory)
{
    var sources = new List<(string, string)>();

    if (!Directory.Exists(dataDirectory))
        return sources;

    // MarketCheck dealers CSV
    var mcFile = Path.Combine(dataDirectory, "marketcheck_dealers.csv");
    if (File.Exists(mcFile))
        sources.Add(("marketcheck", mcFile));

    // Companies House JSON
    var chFile = Path.Combine(dataDirectory, "companies_house.json");
    if (File.Exists(chFile))
        sources.Add(("companies_house", chFile));

    // FCA Register JSON
    var fcaFile = Path.Combine(dataDirectory, "fca_register.json");
    if (File.Exists(fcaFile))
        sources.Add(("fca_register", fcaFile));

    // ICO Register CSV
    var icoFile = Path.Combine(dataDirectory, "ico_register.csv");
    if (File.Exists(icoFile))
        sources.Add(("ico_register", icoFile));

    // SAF Members XML
    var safFile = Path.Combine(dataDirectory, "saf_members.xml");
    if (File.Exists(safFile))
        sources.Add(("saf_members", safFile));

    // VAT Lookups directory
    var vatDir = Path.Combine(dataDirectory, "vat_lookups");
    if (Directory.Exists(vatDir))
        sources.Add(("vat_lookups", vatDir));

    // Crawled dealers CSV
    var crawledFile = Path.Combine(dataDirectory, "crawled_dealers.csv");
    if (File.Exists(crawledFile))
        sources.Add(("crawled_dealers", crawledFile));

    return sources;
}

// Helper method to create appropriate reader for each source
static IDataReader? CreateReader(string source) => source switch
{
    "marketcheck" => new MarketCheckReader(),
    "companies_house" => new CompaniesHouseReader(),
    "fca_register" => new FcaRegisterReader(),
    "ico_register" => new IcoRegisterReader(),
    "saf_members" => new SafMembersReader(),
    "vat_lookups" => new VatLookupsReader(),
    "crawled_dealers" => new CrawledDealersReader(),
    _ => null
};

