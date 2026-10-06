using DealerDatabase.Import.Staging;

namespace DealerDatabase.Import.Consolidation;

/// <summary>
/// Defines which sources are authoritative for which fields.
/// Regulators (Companies House, FCA, ICO) are trusted for legal/regulatory details.
/// Marketplace/Operational sources (MarketCheck, crawled, SAF) are trusted for current trading details.
/// </summary>
public static class SourceHierarchy
{
    /// <summary>
    /// Field-level authority map.
    /// For each field, define which sources are most trustworthy, in order of preference.
    /// Lower SourceAuthorityRank = higher trust.
    /// </summary>
    public static class FieldAuthority
    {
        /// <summary>Legal company name from Companies House (most trustworthy).</summary>
        public static readonly HashSet<SourceAuthorityRank> LegalName = new()
        {
            SourceAuthorityRank.CompaniesHouse, 
            SourceAuthorityRank.FcaRegister,
            SourceAuthorityRank.VatLookups
        };

        /// <summary>Trading name from marketplace/operational sources.</summary>
        public static readonly HashSet<SourceAuthorityRank> TradingName = new()
        {
            SourceAuthorityRank.MarketCheck,
            SourceAuthorityRank.SafMembers,
            SourceAuthorityRank.CrawledDealers,
            SourceAuthorityRank.CompaniesHouse
        };

        /// <summary>Registered office address from Companies House (most trustworthy).</summary>
        public static readonly HashSet<SourceAuthorityRank> RegisteredAddress = new()
        {
            SourceAuthorityRank.CompaniesHouse,
            SourceAuthorityRank.VatLookups,
            SourceAuthorityRank.IcoRegister
        };

        /// <summary>Operational/current address from marketplace or crawled data.</summary>
        public static readonly HashSet<SourceAuthorityRank> OperationalAddress = new()
        {
            SourceAuthorityRank.MarketCheck,
            SourceAuthorityRank.SafMembers,
            SourceAuthorityRank.CrawledDealers
        };

        /// <summary>Postcode from any reliable source.</summary>
        public static readonly HashSet<SourceAuthorityRank> Postcode = new()
        {
            SourceAuthorityRank.CompaniesHouse,
            SourceAuthorityRank.VatLookups,
            SourceAuthorityRank.MarketCheck,
            SourceAuthorityRank.IcoRegister,
            SourceAuthorityRank.SafMembers,
            SourceAuthorityRank.CrawledDealers
        };

        /// <summary>Phone number from operational/current sources.</summary>
        public static readonly HashSet<SourceAuthorityRank> PhoneNumber = new()
        {
            SourceAuthorityRank.MarketCheck,
            SourceAuthorityRank.SafMembers,
            SourceAuthorityRank.CrawledDealers
        };

        /// <summary>Website domain from operational sources.</summary>
        public static readonly HashSet<SourceAuthorityRank> Website = new()
        {
            SourceAuthorityRank.MarketCheck,
            SourceAuthorityRank.CrawledDealers,
            SourceAuthorityRank.SafMembers
        };

        /// <summary>Companies House registration number - only from Companies House.</summary>
        public static readonly HashSet<SourceAuthorityRank> CompaniesHouseNumber = new()
        {
            SourceAuthorityRank.CompaniesHouse
        };

        /// <summary>FCA firm reference number - only from FCA.</summary>
        public static readonly HashSet<SourceAuthorityRank> FcaFirmRefNumber = new()
        {
            SourceAuthorityRank.FcaRegister
        };

        /// <summary>VAT registration number - preferably from VAT register, or Companies House.</summary>
        public static readonly HashSet<SourceAuthorityRank> VatNumber = new()
        {
            SourceAuthorityRank.VatLookups,
            SourceAuthorityRank.CompaniesHouse
        };

        /// <summary>ICO data protection registration - only from ICO.</summary>
        public static readonly HashSet<SourceAuthorityRank> IcoRegistrationNumber = new()
        {
            SourceAuthorityRank.IcoRegister
        };

        /// <summary>SAF membership status - only from SAF.</summary>
        public static readonly HashSet<SourceAuthorityRank> SafMemberStatus = new()
        {
            SourceAuthorityRank.SafMembers
        };

        /// <summary>Incorporation date from Companies House (most authoritative).</summary>
        public static readonly HashSet<SourceAuthorityRank> IncorporationDate = new()
        {
            SourceAuthorityRank.CompaniesHouse,
            SourceAuthorityRank.VatLookups
        };

        /// <summary>Dissolution date from Companies House (most authoritative).</summary>
        public static readonly HashSet<SourceAuthorityRank> DissolutionDate = new()
        {
            SourceAuthorityRank.CompaniesHouse
        };
    }

    /// <summary>
    /// Determines if a source is authoritative for a given field.
    /// </summary>
    public static bool IsAuthoritative(SourceAuthorityRank source, HashSet<SourceAuthorityRank> authoritySet)
    {
        return authoritySet.Contains(source);
    }

    /// <summary>
    /// Selects the most authoritative value from a set of options.
    /// </summary>
    public static (string? value, SourceAuthorityRank source)? SelectBest(
        IEnumerable<(string? value, SourceAuthorityRank source)> options,
        HashSet<SourceAuthorityRank> authoritySet)
    {
        var authoritative = options
            .Where(o => !string.IsNullOrEmpty(o.value) && IsAuthoritative(o.source, authoritySet))
            .OrderBy(o => o.source) // Lower rank = higher authority
            .FirstOrDefault();

        if (authoritative != default)
            return authoritative;

        // Fallback: return first non-empty value
        return options.FirstOrDefault(o => !string.IsNullOrEmpty(o.value));
    }

    /// <summary>
    /// Selects the most authoritative value from a set of options (for dates).
    /// </summary>
    public static (DateTime? value, SourceAuthorityRank source)? SelectBestDate(
        IEnumerable<(DateTime? value, SourceAuthorityRank source)> options,
        HashSet<SourceAuthorityRank> authoritySet)
    {
        var authoritative = options
            .Where(o => o.value.HasValue && IsAuthoritative(o.source, authoritySet))
            .OrderBy(o => o.source) // Lower rank = higher authority
            .FirstOrDefault();

        if (authoritative != default)
            return authoritative;

        // Fallback: return first non-null value
        return options.FirstOrDefault(o => o.value.HasValue);
    }
}
