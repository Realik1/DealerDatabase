using DealerDatabase.Import.Normalization;

namespace DealerDatabase.Import.Matching;

/// <summary>
/// Matches records across sources to identify duplicate dealership records.
/// Uses multiple heuristics: exact matches on registration numbers, fuzzy name matching, and address similarity.
/// </summary>
public class RecordMatcher
{
    // Matching thresholds
    private const int ExactMatchConfidence = 100;
    private const int RegistrationNumberMatchConfidence = 95; 
    private const int HighSimilarityThreshold = 80;
    private const int MediumSimilarityThreshold = 60;
    private const int LowSimilarityThreshold = 40;

    /// <summary>
    /// Matches all records into clusters of likely duplicates.
    /// </summary>
    public List<RecordCluster> MatchRecords(IEnumerable<NormalizedDealerRecord> records)
    {
        var recordList = records.ToList();
        var clusters = new List<RecordCluster>();
        var matchedRecords = new HashSet<string>();

        // Sort by quality score descending - process best records first
        var sortedRecords = recordList.OrderByDescending(r => r.QualityScore).ToList();

        // Build index for fast lookup
        var registrationIndex = BuildRegistrationIndex(recordList);
        var matchKeyIndex = BuildMatchKeyIndex(recordList);

        foreach (var record in sortedRecords)
        {
            var recordKey = $"{record.Source}:{record.SourceId}";

            // Skip if already matched
            if (matchedRecords.Contains(recordKey))
                continue;

            var cluster = new RecordCluster { Records = { record } };
            matchedRecords.Add(recordKey);

            // Find all potential matches for this record
            var potentialMatches = FindPotentialMatches(record, recordList, registrationIndex, matchKeyIndex);

            foreach (var potentialMatch in potentialMatches)
            {
                var matchKey = $"{potentialMatch.source.Source}:{potentialMatch.source.SourceId}";

                // Skip if already in another cluster
                if (matchedRecords.Contains(matchKey))
                    continue;

                if (potentialMatch.confidence >= 50)
                {
                    cluster.Records.Add(potentialMatch.source);
                    matchedRecords.Add(matchKey);
                }
            }

            // Calculate cluster average confidence
            var confidences = cluster.Records
                .Skip(1) // Skip the primary record
                .Select(r => CalculateMatchScore(record, r))
                .ToList();

            cluster.AverageConfidence = confidences.Count > 0 ? confidences.Average() : 100;
            clusters.Add(cluster);
        }

        // Merge clusters that should be together
        clusters = MergeClusters(clusters);

        return clusters;
    }

    /// <summary>
    /// Finds potential matches for a given record.
    /// </summary>
    private List<(NormalizedDealerRecord source, int confidence)> FindPotentialMatches(
        NormalizedDealerRecord targetRecord,
        List<NormalizedDealerRecord> allRecords,
        Dictionary<string, List<NormalizedDealerRecord>> registrationIndex,
        Dictionary<string, List<NormalizedDealerRecord>> matchKeyIndex)
    {
        var candidates = new List<(NormalizedDealerRecord, int)>();

        // 1. Check for exact registration number matches (highest confidence)
        var registrationMatches = FindRegistrationMatches(targetRecord, registrationIndex);
        candidates.AddRange(registrationMatches);

        // 2. Check for match key similarity
        if (!string.IsNullOrEmpty(targetRecord.MatchKey) && matchKeyIndex.TryGetValue(targetRecord.MatchKey, out var matchKeyCandidates))
        {
            foreach (var candidate in matchKeyCandidates)
            {
                if (candidate.Source != targetRecord.Source)
                {
                    var confidence = CalculateMatchScore(targetRecord, candidate);
                    if (confidence >= 50)
                    {
                        candidates.Add((candidate, confidence));
                    }
                }
            }
        }

        // 3. Check for fuzzy name + postcode matches
        var fuzzyMatches = FindFuzzyMatches(targetRecord, allRecords);
        candidates.AddRange(fuzzyMatches);

        // Deduplicate and return top matches
        return candidates
            .DistinctBy(c => $"{c.Item1.Source}:{c.Item1.SourceId}")
            .OrderByDescending(c => c.Item2)  // c.Item2 is the confidence score
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Finds matches based on registration numbers (Companies House, VAT, FCA, etc.).
    /// </summary>
    private List<(NormalizedDealerRecord, int)> FindRegistrationMatches(
        NormalizedDealerRecord targetRecord,
        Dictionary<string, List<NormalizedDealerRecord>> registrationIndex)
    {
        var matches = new List<(NormalizedDealerRecord, int)>();

        // Check Companies House number
        if (!string.IsNullOrEmpty(targetRecord.CompaniesHouseNumber))
        {
            var key = $"companies_house:{targetRecord.CompaniesHouseNumber}";
            if (registrationIndex.TryGetValue(key, out var candidates))
            {
                matches.AddRange(candidates
                    .Where(c => c.Source != targetRecord.Source)
                    .Select(c => (c, RegistrationNumberMatchConfidence)));
            }
        }

        // Check VAT number
        if (!string.IsNullOrEmpty(targetRecord.VatNumber))
        {
            var key = $"vat:{targetRecord.VatNumber}";
            if (registrationIndex.TryGetValue(key, out var candidates))
            {
                matches.AddRange(candidates
                    .Where(c => c.Source != targetRecord.Source)
                    .Select(c => (c, RegistrationNumberMatchConfidence)));
            }
        }

        // Check FCA firm number
        if (!string.IsNullOrEmpty(targetRecord.FcaFirmRefNumber))
        {
            var key = $"fca:{targetRecord.FcaFirmRefNumber}";
            if (registrationIndex.TryGetValue(key, out var candidates))
            {
                matches.AddRange(candidates
                    .Where(c => c.Source != targetRecord.Source)
                    .Select(c => (c, RegistrationNumberMatchConfidence)));
            }
        }

        return matches;
    }

    /// <summary>
    /// Finds matches based on fuzzy name and postcode similarity.
    /// </summary>
    private List<(NormalizedDealerRecord, int)> FindFuzzyMatches(
        NormalizedDealerRecord targetRecord,
        List<NormalizedDealerRecord> allRecords)
    {
        var matches = new List<(NormalizedDealerRecord, int)>();

        foreach (var candidate in allRecords)
        {
            // Skip same source
            if (candidate.Source == targetRecord.Source)
                continue;

            // Calculate similarity
            var score = CalculateMatchScore(targetRecord, candidate);

            if (score >= MediumSimilarityThreshold)
            {
                matches.Add((candidate, score));
            }
        }

        return matches;
    }

    /// <summary>
    /// Calculates a match confidence score between two records (0-100).
    /// </summary>
    private int CalculateMatchScore(NormalizedDealerRecord record1, NormalizedDealerRecord record2)
    {
        var score = 0;
        var maxScore = 100;
        var factors = 0;

        // Name similarity (40 points max)
        var nameSimilarity = CalculateSimilarity(
            record1.NormalizedName ?? record1.Name,
            record2.NormalizedName ?? record2.Name);
        score += (int)(nameSimilarity * 40);
        factors++;

        // Postcode match (40 points)
        if (!string.IsNullOrEmpty(record1.NormalizedPostcode) && !string.IsNullOrEmpty(record2.NormalizedPostcode))
        {
            if (record1.NormalizedPostcode == record2.NormalizedPostcode)
            {
                score += 40;
            }
            else if (record1.NormalizedPostcode.StartsWith(record2.NormalizedPostcode.Substring(0, Math.Min(4, record2.NormalizedPostcode.Length))))
            {
                score += 20;
            }
        }
        factors++;

        // Phone number match (10 points)
        if (!string.IsNullOrEmpty(record1.NormalizedPhoneNumber) && record1.NormalizedPhoneNumber == record2.NormalizedPhoneNumber)
            score += 10;

        // Website match (10 points)
        if (!string.IsNullOrEmpty(record1.NormalizedWebsite) && record1.NormalizedWebsite == record2.NormalizedWebsite)
            score += 10;

        // Registration numbers (bonus)
        if (!string.IsNullOrEmpty(record1.CompaniesHouseNumber) && record1.CompaniesHouseNumber == record2.CompaniesHouseNumber)
            score = 100; // Definite match

        if (!string.IsNullOrEmpty(record1.VatNumber) && record1.VatNumber == record2.VatNumber)
            score = 100; // Definite match

        return Math.Min(score, maxScore);
    }

    /// <summary>
    /// Calculates string similarity using a simple algorithm (0.0-1.0).
    /// </summary>
    private double CalculateSimilarity(string str1, string str2)
    {
        if (str1 == str2)
            return 1.0;

        if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
            return 0.0;

        var s1 = str1.ToLowerInvariant();
        var s2 = str2.ToLowerInvariant();

        // Check if one contains the other
        if (s1.Contains(s2) || s2.Contains(s1))
            return 0.8;

        // Levenshtein distance based similarity
        int distance = LevenshteinDistance(s1, s2);
        int maxLength = Math.Max(s1.Length, s2.Length);

        return 1.0 - (double)distance / maxLength;
    }

    /// <summary>
    /// Calculates Levenshtein distance between two strings.
    /// </summary>
    private int LevenshteinDistance(string s1, string s2)
    {
        var length1 = s1.Length;
        var length2 = s2.Length;
        var distances = new int[length1 + 1, length2 + 1];

        for (int i = 0; i <= length1; distances[i, 0] = i++) ;
        for (int j = 0; j <= length2; distances[0, j] = j++) ;

        for (int i = 1; i <= length1; i++)
        {
            for (int j = 1; j <= length2; j++)
            {
                int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                distances[i, j] = Math.Min(
                    Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                    distances[i - 1, j - 1] + cost);
            }
        }

        return distances[length1, length2];
    }

    /// <summary>
    /// Merges clusters that represent the same dealership.
    /// </summary>
    private List<RecordCluster> MergeClusters(List<RecordCluster> clusters)
    {
        // Could implement cluster merging logic here
        // For now, keep clusters separate to avoid over-merging
        return clusters;
    }

    /// <summary>
    /// Builds an index for fast registration number lookups.
    /// </summary>
    private Dictionary<string, List<NormalizedDealerRecord>> BuildRegistrationIndex(
        List<NormalizedDealerRecord> records)
    {
        var index = new Dictionary<string, List<NormalizedDealerRecord>>();

        foreach (var record in records)
        {
            if (!string.IsNullOrEmpty(record.CompaniesHouseNumber))
                AddToIndex(index, $"companies_house:{record.CompaniesHouseNumber}", record);

            if (!string.IsNullOrEmpty(record.VatNumber))
                AddToIndex(index, $"vat:{record.VatNumber}", record);

            if (!string.IsNullOrEmpty(record.FcaFirmRefNumber))
                AddToIndex(index, $"fca:{record.FcaFirmRefNumber}", record);
        }

        return index;
    }

    /// <summary>
    /// Builds an index for fast match key lookups.
    /// </summary>
    private Dictionary<string, List<NormalizedDealerRecord>> BuildMatchKeyIndex(
        List<NormalizedDealerRecord> records)
    {
        var index = new Dictionary<string, List<NormalizedDealerRecord>>();

        foreach (var record in records)
        {
            if (!string.IsNullOrEmpty(record.MatchKey))
                AddToIndex(index, record.MatchKey, record);
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
}
