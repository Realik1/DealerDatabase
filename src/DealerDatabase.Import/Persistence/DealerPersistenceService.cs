using DealerDatabase.Data;
using DealerDatabase.Data.Entities;
using DealerDatabase.Import.Matching;
using Microsoft.EntityFrameworkCore;

namespace DealerDatabase.Import.Persistence;

/// <summary>
/// Persists consolidated dealer records to the database with idempotent upserts.
/// Uses fingerprinting to detect and update existing dealers, preventing duplicates on re-import.
/// Also maintains source lineage via DealerSource records for audit trail.
/// </summary>
public class DealerPersistenceService
{
    private readonly DealerDbContext _db;

    public DealerPersistenceService(DealerDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Persists all consolidated records to the database with idempotent upserts.
    /// Returns counts of inserted and updated dealers.
    /// </summary>
    public async Task<(int Inserted, int Updated)> PersistAsync(IEnumerable<ConsolidatedRecord> consolidatedRecords)
    {
        int inserted = 0;
        int updated = 0;

        foreach (var record in consolidatedRecords)
        {
            var fingerprint = DealerFingerprint.Generate(record);
            var existingDealer = await FindExistingDealerAsync(fingerprint);

            if (existingDealer != null)
            {
                UpdateDealerRecord(existingDealer, record);
                _db.Dealers.Update(existingDealer);
                updated++;
            }
            else
            {
                var newDealer = CreateDealerRecord(record);
                _db.Dealers.Add(newDealer);
                await _db.SaveChangesAsync(); // Save to get the ID for DealerSource records
                inserted++;
            }

            // Always update/add source records for audit trail
            var dealer = existingDealer ?? (await _db.Dealers
                .FirstOrDefaultAsync(d => d.CompaniesHouseNumber == record.CompaniesHouseNumber && !string.IsNullOrEmpty(record.CompaniesHouseNumber))
                ?? await _db.Dealers
                    .FirstOrDefaultAsync(d => d.VatNumber == record.VatNumber && !string.IsNullOrEmpty(record.VatNumber))
                ?? await _db.Dealers
                    .FirstOrDefaultAsync(d => d.Name == record.Name && d.Postcode == record.Postcode));

            if (dealer?.Id > 0)
            {
                await PersistSourceRecordsAsync(dealer.Id, record);
            }
        }

        await _db.SaveChangesAsync();
        return (inserted, updated);
    }

    /// <summary>
    /// Finds an existing dealer in the database using fingerprint-based lookup.
    /// </summary>
    private async Task<Dealer?> FindExistingDealerAsync(DealerFingerprint fingerprint)
    {
        // Parse fingerprint to determine lookup strategy
        var parts = fingerprint.FingerprintKey.Split('|');

        if (fingerprint.FingerprintKey.StartsWith("CH|") && parts.Length >= 2)
        {
            // Look up by Companies House number
            return await _db.Dealers
                .FirstOrDefaultAsync(d => d.CompaniesHouseNumber == parts[1]);
        }

        if (fingerprint.FingerprintKey.StartsWith("VAT|") && parts.Length >= 3)
        {
            // Look up by VAT number
            return await _db.Dealers
                .FirstOrDefaultAsync(d => d.VatNumber == parts[1] && d.Postcode == parts[2]);
        }

        if (fingerprint.FingerprintKey.StartsWith("FCA|") && parts.Length >= 2)
        {
            // Look up by FCA firm reference
            return await _db.Dealers
                .FirstOrDefaultAsync(d => d.FcaFirmRefNumber == parts[1]);
        }

        if (fingerprint.FingerprintKey.StartsWith("NAME_PC|") && parts.Length >= 3)
        {
            // For name+postcode matches, look for similar names and matching postcode
            // This is less precise but works for fuzzy matches
            var postcode = parts[2];
            var existingDealer = await _db.Dealers
                .Where(d => d.Postcode == postcode)
                .ToListAsync();

            // Try to find a match by name similarity
            foreach (var dealer in existingDealer)
            {
                if (NamesSimilar(dealer.Name, parts[1]))
                {
                    return dealer;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if two names are similar enough to be considered the same dealer.
    /// Simple check: remove special chars and compare.
    /// </summary>
    private bool NamesSimilar(string name1, string name2)
    {
        var n1 = RemoveSpecialChars(name1).ToLowerInvariant();
        var n2 = RemoveSpecialChars(name2).ToLowerInvariant();
        return n1 == n2;
    }

    private string RemoveSpecialChars(string? s)
    {
        if (string.IsNullOrEmpty(s))
            return string.Empty;

        return System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9]", "");
    }

    /// <summary>
    /// Creates a new Dealer entity from a consolidated record.
    /// </summary>
    private Dealer CreateDealerRecord(ConsolidatedRecord source)
    {
        return new Dealer
        {
            Name = source.Name,
            LegalName = source.LegalName,
            Address = source.Address,
            AddressLine2 = source.AddressLine2,
            Postcode = source.Postcode,
            PhoneNumber = source.PhoneNumber,
            WebsiteDomain = source.WebsiteDomain,
            CompaniesHouseNumber = source.CompaniesHouseNumber,
            FcaFirmRefNumber = source.FcaFirmRefNumber,
            VatNumber = source.VatNumber,
            IcoRegistrationNumber = source.IcoRegistrationNumber,
            SafMemberStatus = source.SafMemberStatus,
            IncorporationDate = source.IncorporationDate,
            DissolutionDate = source.DissolutionDate,
            HasConflicts = source.HasConflicts,
            Notes = source.ConflictNotes,
            CreatedDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates an existing Dealer entity with data from a consolidated record.
    /// </summary>
    private void UpdateDealerRecord(Dealer existing, ConsolidatedRecord source)
    {
        existing.Name = source.Name;
        existing.LegalName = source.LegalName;
        existing.Address = source.Address;
        existing.AddressLine2 = source.AddressLine2;
        existing.Postcode = source.Postcode;
        existing.PhoneNumber = source.PhoneNumber;
        existing.WebsiteDomain = source.WebsiteDomain;
        existing.CompaniesHouseNumber = source.CompaniesHouseNumber;
        existing.FcaFirmRefNumber = source.FcaFirmRefNumber;
        existing.VatNumber = source.VatNumber;
        existing.IcoRegistrationNumber = source.IcoRegistrationNumber;
        existing.SafMemberStatus = source.SafMemberStatus;
        existing.IncorporationDate = source.IncorporationDate;
        existing.DissolutionDate = source.DissolutionDate;
        existing.HasConflicts = source.HasConflicts;
        existing.Notes = source.ConflictNotes;
        existing.LastModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Persists source records (DealerSource) for audit trail.
    /// Clears old sources and inserts new ones to maintain accurate lineage.
    /// </summary>
    private async Task PersistSourceRecordsAsync(int dealerId, ConsolidatedRecord consolidated)
    {
        // Remove old source records
        var oldSources = await _db.DealerSources
            .Where(s => s.DealerId == dealerId)
            .ToListAsync();

        _db.DealerSources.RemoveRange(oldSources);

        // Add new source records
        foreach (var sourceRecord in consolidated.SourceRecords)
        {
            var dealerSource = new DealerSource
            {
                DealerId = dealerId,
                SourceName = sourceRecord.Source,
                SourceId = sourceRecord.SourceId,
                SourceName_Value = sourceRecord.Name,
                SourceAddress = sourceRecord.Address,
                SourcePostcode = sourceRecord.Postcode,
                SourcePhoneNumber = sourceRecord.PhoneNumber,
                MatchConfidence = 100, // Source records are exact, not matched
                MatchReason = "Source record",
                ImportedDate = DateTime.UtcNow,
                SourceData = sourceRecord.RawData
            };

            _db.DealerSources.Add(dealerSource);
        }

        await _db.SaveChangesAsync();
    }
}
