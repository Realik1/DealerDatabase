namespace DealerDatabase.Import.DataReaders;

/// <summary>
/// Represents a raw record from a data source before normalization and matching.
/// </summary>
public class RawDealerRecord
{
    /// <summary>Name of the source system this record came from.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Unique identifier within the source system.</summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>The name/trading name of the dealership as recorded in the source.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Legal company name, if different from trading name.</summary>
    public string? LegalName { get; set; }

    /// <summary>Address line 1 (street address).</summary>
    public string? Address { get; set; }

    /// <summary>Address line 2 (city, locality, etc.).</summary>
    public string? AddressLine2 { get; set; }

    /// <summary>Postcode or ZIP code, as recorded in source.</summary>
    public string? Postcode { get; set; }

    /// <summary>Phone number as recorded in source.</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>Website or domain name.</summary>
    public string? Website { get; set; }

    /// <summary>Companies House registration number.</summary>
    public string? CompaniesHouseNumber { get; set; }

    /// <summary>FCA firm reference number.</summary>
    public string? FcaFirmRefNumber { get; set; }

    /// <summary>VAT registration number.</summary>
    public string? VatNumber { get; set; }

    /// <summary>ICO data protection registration number.</summary>
    public string? IcoRegistrationNumber { get; set; }

    /// <summary>SAF membership status.</summary>
    public string? SafMemberStatus { get; set; }

    /// <summary>Company incorporation/establishment date.</summary>
    public DateTime? IncorporationDate { get; set; }

    /// <summary>Company dissolution/closure date.</summary>
    public DateTime? DissolutionDate { get; set; }

    /// <summary>Raw data as JSON for audit purposes.</summary>
    public string? RawData { get; set; }
}
