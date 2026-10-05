using DealerDatabase.Import.Normalization;
using DealerDatabase.Import.Staging;

namespace DealerDatabase.Import.Matching;

/// <summary>
/// Result of a single match attempt.
/// </summary>
public class PassMatchResult
{
    public int PassNumber { get; set; }
    public NormalizedDealerRecord CandidateRecord { get; set; } = null!;
    public int ConfidenceScore { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Multi-pass deterministic matcher that prioritizes explicit identifiers,
/// then fuzzy matching on postcode + normalized name.
/// </summary>
public class MultiPassMatcher
{
    // Confidence thresholds
    private const int Pass1HighThreshold = 100; // Exact ID match
    private const int Pass2MediumThreshold = 80;  // Strong fuzzy + postcode
    private const int Pass3LowThreshold = 60;    // Secondary signals (domain/phone)

    /// <summary>
    /// Executes multi-pass matching on all records.
    /// Returns clusters of matched records.
    /// </summary>
    public List<RecordCluster> MatchRecords(IEnumerable<NormalizedDealerRecord> records)
    {
        var recordList = records.ToList();
        var clusters = new List<RecordCluster>();
        var matchedRecordKeys = new HashSet<string>();

        // Sort by quality score descending - process best records first
        var sortedRecords = recordList.OrderByDescending(r => r.QualityScore).ToList();

        // Build indices for fast lookup
        var idIndex = BuildIdIndex(recordList);
        var postcodeIndex = BuildPostcodeIndex(recordList);
        var domainIndex = BuildDomainIndex(recordList);
        var phoneIndex = BuildPhoneIndex(recordList);

        foreach (var record in sortedRecords)
        {
            var recordKey = GetRecordKey(record);

            // Skip if already claimed by another cluster
            if (matchedRecordKeys.Contains(recordKey))
                continue;

            // Create new cluster with this record as primary
            var cluster = new RecordCluster
            {
                ClusterId = Guid.NewGuid().ToString(),
                Records = { record }
            };

            matchedRecordKeys.Add(recordKey);

            // Run matching passes
            var allMatches = new List<PassMatchResult>();

            // PASS 1: Explicit identifier matches (highest confidence)
            allMatches.AddRange(RunPass1_ExplicitIds(record, recordList, idIndex, matchedRecordKeys));

            // PASS 2: Postcode + Fuzzy name matching (medium confidence)
            allMatches.AddRange(RunPass2_PostcodeFuzzyName(record, recordList, postcodeIndex, matchedRecordKeys));

            // PASS 3: Secondary signals (domain, phone)
            allMatches.AddRange(RunPass3_SecondarySignals(record, recordList, domainIndex, phoneIndex, matchedRecordKeys, allMatches));

            // Merge matches into cluster
            foreach (var match in allMatches.OrderByDescending(m => m.ConfidenceScore))
            {
                var matchKey = GetRecordKey(match.CandidateRecord);
                if (!matchedRecordKeys.Contains(matchKey) && match.ConfidenceScore >= 50)
                {
                    cluster.Records.Add(match.CandidateRecord);
                    matchedRecordKeys.Add(matchKey);
                }
            }

            clusters.Add(cluster);
        }

        return clusters;
    }

    /// <summary>
    /// PASS 1: Match on explicit identifiers (Company Number, FCA, VAT).
    /// Highest confidence (100 or 95 points).
    /// </summary>
    private List<PassMatchResult> RunPass1_ExplicitIds(
        NormalizedDealerRecord targetRecord,
        List<NormalizedDealerRecord> allRecords,
        Dictionary<string, List<NormalizedDealerRecord>> idIndex,
        HashSet<string> alreadyMatched)
    {
        var matches = new List<PassMatchResult>();

        // Match on Companies House Number (100% confidence, definitive)
        if (!string.IsNullOrEmpty(targetRecord.CompaniesHouseNumber))
        {
            var key = $"CH:{targetRecord.CompaniesHouseNumber}";
            if (idIndex.TryGetValue(key, out var candidates))
            {
                foreach (var candidate in candidates)
                {
                    if (candidate.Source != targetRecord.Source && !alreadyMatched.Contains(GetRecordKey(candidate)))
                    {
                        matches.Add(new PassMatchResult
                        {
                            PassNumber = 1,
                            CandidateRecord = candidate,
                            ConfidenceScore = 100,
                            Reason = $"Exact Companies House Number match: {targetRecord.CompaniesHouseNumber}"
                        });
                    }
                }
            }
        }

        // Match on VAT Number (100% confidence)
        if (!string.IsNullOrEmpty(targetRecord.VatNumber))
        {
            var key = $"VAT:{targetRecord.VatNumber}";
            if (idIndex.TryGetValue(key, out var candidates))
            {
                foreach (var candidate in candidates)
                {
                    if (candidate.Source != targetRecord.Source && !alreadyMatched.Contains(GetRecordKey(candidate)))
                    {
                        matches.Add(new PassMatchResult
                        {
                            PassNumber = 1,
                            CandidateRecord = candidate,
                            ConfidenceScore = 100,
                            Reason = $"Exact VAT Number match: {targetRecord.VatNumber}"
                        });
                    }
                }
            }
        }

        // Match on FCA Firm Reference Number (100% confidence)
        if (!string.IsNullOrEmpty(targetRecord.FcaFirmRefNumber))
        {
            var key = $"FCA:{targetRecord.FcaFirmRefNumber}";
            if (idIndex.TryGetValue(key, out var candidates))
            {
                foreach (var candidate in candidates)
                {
                    if (candidate.Source != targetRecord.Source && !alreadyMatched.Contains(GetRecordKey(candidate)))
                    {
                        matches.Add(new PassMatchResult
                        {
                            PassNumber = 1,
                            CandidateRecord = candidate,
                            ConfidenceScore = 100,
                            Reason = $"Exact FCA Firm Ref match: {targetRecord.FcaFirmRefNumber}"
                        });
                    }
                }
            }
        }

        return matches;
    }

    /// <summary>
    /// PASS 2: Match on normalized postcode + fuzzy string matching on name.
    /// Medium confidence (60-85 points).
    /// </summary>
    private List<PassMatchResult> RunPass2_PostcodeFuzzyName(
        NormalizedDealerRecord targetRecord,
        List<NormalizedDealerRecord> allRecords,
        Dictionary<string, List<NormalizedDealerRecord>> postcodeIndex,
        HashSet<string> alreadyMatched)
    {
        var matches = new List<PassMatchResult>();

        // Skip if no postcode - can't do reliable fuzzy match
        if (string.IsNullOrEmpty(targetRecord.NormalizedPostcode))
            return matches;

        // Get candidates with same postcode
        if (postcodeIndex.TryGetValue(targetRecord.NormalizedPostcode, out var candidates))
        {
            foreach (var candidate in candidates)
            {
                if (candidate.Source == targetRecord.Source || alreadyMatched.Contains(GetRecordKey(candidate)))
                    continue;

                // Calculate name similarity after normalization and legal suffix removal
                var nameSimilarity = CalculateNameSimilarity(
                    targetRecord.NormalizedName ?? targetRecord.Name,
                    candidate.NormalizedName ?? candidate.Name);

                // High confidence if names are very similar (85+ similarity)
                if (nameSimilarity >= 0.85)
                {
                    matches.Add(new PassMatchResult
                    {
                        PassNumber = 2,
                        CandidateRecord = candidate,
                        ConfidenceScore = 85,
                        Reason = $"Postcode match + Name similarity {(int)(nameSimilarity * 100)}%: '{targetRecord.NormalizedName}' vs '{candidate.NormalizedName}'"
                    });
                }
                // Medium confidence if names are reasonably similar (70-84 similarity)
                else if (nameSimilarity >= 0.70)
                {
                    matches.Add(new PassMatchResult
                    {
                        PassNumber = 2,
                        CandidateRecord = candidate,
                        ConfidenceScore = 75,
                        Reason = $"Postcode match + Partial name similarity {(int)(nameSimilarity * 100)}%"
                    });
                }
                // Lower confidence for partial matches (60-69 similarity)
                else if (nameSimilarity >= 0.60)
                {
                    matches.Add(new PassMatchResult
                    {
                        PassNumber = 2,
                        CandidateRecord = candidate,
                        ConfidenceScore = 65,
                        Reason = $"Postcode match + Weak name similarity {(int)(nameSimilarity * 100)}%"
                    });
                }
            }
        }

        return matches;
    }

    /// <summary>
    /// PASS 3: Match on secondary signals - shared domain or phone number.
    /// Low confidence (50-65 points), only if no Pass 1/2 matches exist.
    /// </summary>
    private List<PassMatchResult> RunPass3_SecondarySignals(
        NormalizedDealerRecord targetRecord,
        List<NormalizedDealerRecord> allRecords,
        Dictionary<string, List<NormalizedDealerRecord>> domainIndex,
        Dictionary<string, List<NormalizedDealerRecord>> phoneIndex,
        HashSet<string> alreadyMatched,
        List<PassMatchResult> existingMatches)
    {
        var matches = new List<PassMatchResult>();

        // Only use Pass 3 if we don't already have high-confidence matches
        if (existingMatches.Any(m => m.ConfidenceScore >= 70))
            return matches;

        // Match on shared website domain
        if (!string.IsNullOrEmpty(targetRecord.NormalizedWebsite))
        {
            if (domainIndex.TryGetValue(targetRecord.NormalizedWebsite, out var candidates))
            {
                foreach (var candidate in candidates)
                {
                    if (candidate.Source == targetRecord.Source || alreadyMatched.Contains(GetRecordKey(candidate)))
                        continue;

                    matches.Add(new PassMatchResult
                    {
                        PassNumber = 3,
                        CandidateRecord = candidate,
                        ConfidenceScore = 60,
                        Reason = $"Shared website domain: {targetRecord.NormalizedWebsite}"
                    });
                }
            }
        }

        // Match on shared phone number
        if (!string.IsNullOrEmpty(targetRecord.NormalizedPhoneNumber))
        {
            if (phoneIndex.TryGetValue(targetRecord.NormalizedPhoneNumber, out var candidates))
            {
                foreach (var candidate in candidates)
                {
                    if (candidate.Source == targetRecord.Source || alreadyMatched.Contains(GetRecordKey(candidate)))
                        continue;

                    matches.Add(new PassMatchResult
                    {
                        PassNumber = 3,
                        CandidateRecord = candidate,
                        ConfidenceScore = 55,
                        Reason = $"Shared phone number: {targetRecord.NormalizedPhoneNumber}"
                    });
                }
            }
        }

        return matches;
    }

    /// <summary>
    /// Calculates normalized string similarity (0.0 to 1.0).
    /// Strips legal suffixes (Ltd, Limited, PLC, etc.) before comparison.
    /// </summary>
    private double CalculateNameSimilarity(string name1, string name2)
    {
        if (name1 == name2)
            return 1.0;

        if (string.IsNullOrEmpty(name1) || string.IsNullOrEmpty(name2))
            return 0.0;

        var n1 = StripLegalSuffixes(name1).ToLowerInvariant().Trim();
        var n2 = StripLegalSuffixes(name2).ToLowerInvariant().Trim();

        if (n1 == n2)
            return 1.0;

        // Check containment
        if (n1.Contains(n2) || n2.Contains(n1))
            return 0.85;

        // Levenshtein distance
        var distance = LevenshteinDistance(n1, n2);
        var maxLength = Math.Max(n1.Length, n2.Length);

        return 1.0 - ((double)distance / maxLength);
    }

    /// <summary>
    /// Strips common legal suffixes from company names.
    /// E.g., "Apex Cars Ltd" → "Apex Cars", "ABC PLC Limited" → "ABC"
    /// </summary>
    private string StripLegalSuffixes(string name)
    {
        var suffixes = new[] { " Ltd", " Limited", " Inc", " Incorporated", " PLC", " plc", " Company", " Co.", " Corp", " Corporation", " LLC" };

        var result = name;
        foreach (var suffix in suffixes)
        {
            if (result.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                result = result.Substring(0, result.Length - suffix.Length);
            }
        }

        return result.Trim();
    }

    /// <summary>
    /// Calculates Levenshtein distance between two strings (edit distance).
    /// </summary>
    private int LevenshteinDistance(string s1, string s2)
    {
        var len1 = s1.Length;
        var len2 = s2.Length;
        var d = new int[len1 + 1, len2 + 1];

        for (int i = 0; i <= len1; i++) d[i, 0] = i;
        for (int j = 0; j <= len2; j++) d[0, j] = j;

        for (int i = 1; i <= len1; i++)
        {
            for (int j = 1; j <= len2; j++)
            {
                int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[len1, len2];
    }

    private Dictionary<string, List<NormalizedDealerRecord>> BuildIdIndex(List<NormalizedDealerRecord> records)
    {
        var index = new Dictionary<string, List<NormalizedDealerRecord>>();

        foreach (var record in records)
        {
            if (!string.IsNullOrEmpty(record.CompaniesHouseNumber))
                AddToIndex(index, $"CH:{record.CompaniesHouseNumber}", record);

            if (!string.IsNullOrEmpty(record.VatNumber))
                AddToIndex(index, $"VAT:{record.VatNumber}", record);

            if (!string.IsNullOrEmpty(record.FcaFirmRefNumber))
                AddToIndex(index, $"FCA:{record.FcaFirmRefNumber}", record);
        }

        return index;
    }

    private Dictionary<string, List<NormalizedDealerRecord>> BuildPostcodeIndex(List<NormalizedDealerRecord> records)
    {
        var index = new Dictionary<string, List<NormalizedDealerRecord>>();

        foreach (var record in records)
        {
            if (!string.IsNullOrEmpty(record.NormalizedPostcode))
                AddToIndex(index, record.NormalizedPostcode, record);
        }

        return index;
    }

    private Dictionary<string, List<NormalizedDealerRecord>> BuildDomainIndex(List<NormalizedDealerRecord> records)
    {
        var index = new Dictionary<string, List<NormalizedDealerRecord>>();

        foreach (var record in records)
        {
            if (!string.IsNullOrEmpty(record.NormalizedWebsite))
                AddToIndex(index, record.NormalizedWebsite, record);
        }

        return index;
    }

    private Dictionary<string, List<NormalizedDealerRecord>> BuildPhoneIndex(List<NormalizedDealerRecord> records)
    {
        var index = new Dictionary<string, List<NormalizedDealerRecord>>();

        foreach (var record in records)
        {
            if (!string.IsNullOrEmpty(record.NormalizedPhoneNumber))
                AddToIndex(index, record.NormalizedPhoneNumber, record);
        }

        return index;
    }

    private void AddToIndex(Dictionary<string, List<NormalizedDealerRecord>> index, string key, NormalizedDealerRecord record)
    {
        if (!index.TryGetValue(key, out var list))
        {
            list = new List<NormalizedDealerRecord>();
            index[key] = list;
        }

        list.Add(record);
    }

    private string GetRecordKey(NormalizedDealerRecord record)
    {
        return $"{record.Source}:{record.SourceId}";
    }
}
