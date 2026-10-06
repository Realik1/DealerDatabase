namespace DealerDatabase.Data.Entities;

/// <summary>
/// A consolidated dealership record bringing together information from multiple sources.
/// </summary>
public class Dealer
{
    public int Id { get; set; }

    /// <summary>The primary trading name of the dealership.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Legal company name if different from trading name.</summary>
    public string? LegalName { get; set; }

    /// <summary>Street address or business address line.</summary>
    public string? Address { get; set; }

    /// <summary>Additional address details (city, town, etc.).</summary>
    public string? AddressLine2 { get; set; }

    /// <summary>Postcode or ZIP code.</summary>
    public string? Postcode { get; set; }

    /// <summary>Primary phone number.</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>Website domain or URL.</summary>
    public string? WebsiteDomain { get; set; }

    /// <summary>Companies House registration number, if applicable.</summary>
    public string? CompaniesHouseNumber { get; set; }

    /// <summary>FCA firm reference number, if regulated.</summary>
    public string? FcaFirmRefNumber { get; set; }

    /// <summary>VAT registration number, if applicable.</summary>
    public string? VatNumber { get; set; }

    /// <summary>ICO data protection registration number, if applicable.</summary>
    public string? IcoRegistrationNumber { get; set; }

    /// <summary>SAF membership status, if member.</summary>
    public string? SafMemberStatus { get; set; }

    /// <summary>SAF membership expiry date, if applicable.</summary>
    public DateTime? SafExpiryDate { get; set; }

    /// <summary>ICO data protection registration expiry date, if applicable.</summary>
    public DateTime? IcoExpiryDate { get; set; }

    /// <summary>FCA status/classification (e.g., "Active", "Suspended", "Withdrawn").</summary>
    public string? FcaStatus { get; set; }

    /// <summary>VAT registration validation status (e.g., "Valid", "Pending", "Invalid").</summary>
    public string? VatValidationStatus { get; set; }

    /// <summary>Primary email address for the dealership.</summary>
    public string? EmailAddress { get; set; }

    /// <summary>When the dealership was first incorporated or established.</summary>
    public DateTime? IncorporationDate { get; set; }

    /// <summary>Dissolution or de-registration date, if applicable.</summary>
    public DateTime? DissolutionDate { get; set; }

    /// <summary>When this consolidated record was created.</summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>When this consolidated record was last updated.</summary>
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

    /// <summary>Flag indicating data quality concerns or unresolved conflicts.</summary>
    public bool HasConflicts { get; set; }

    /// <summary>Notes about consolidation, conflicts, or data quality issues.</summary>
    public string? Notes { get; set; }

    /// <summary>Source records that were matched and consolidated into this dealer.</summary>
    public ICollection<DealerSource> Sources { get; set; } = new List<DealerSource>();
}
