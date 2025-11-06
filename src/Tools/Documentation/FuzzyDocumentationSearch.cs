using System;
using System.Collections.Generic;
using System.Linq;
using Fastenshtein;

namespace UnifyMcp.Tools.Documentation
{
    /// <summary>
    /// Implements fuzzy search using Fastenshtein Levenshtein distance with configurable similarity threshold.
    /// Normalizes strings (lowercase, trim), calculates Levenshtein distance, converts to similarity score.
    /// Default threshold: 0.7
    /// </summary>
    public class FuzzyDocumentationSearch
    {
        private const double DefaultSimilarityThreshold = 0.7;

        /// <summary>
        /// Calculates similarity score between two strings using Levenshtein distance.
        /// Similarity = 1.0 - (distance / maxLength)
        /// </summary>
        /// <param name="source">Source string</param>
        /// <param name="target">Target string to compare</param>
        /// <returns>Similarity score between 0.0 (completely different) and 1.0 (identical)</returns>
        public double CalculateSimilarity(string source, string target)
        {
            // Normalize inputs
            source = NormalizeString(source);
            target = NormalizeString(target);

            // Handle empty strings
            if (string.IsNullOrEmpty(source) && string.IsNullOrEmpty(target))
                return 1.0; // Both empty = identical

            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
                return 0.0; // One empty = completely different

            // Calculate Levenshtein distance using Fastenshtein
            var levenshtein = new Levenshtein(source);
            int distance = levenshtein.DistanceFrom(target);

            // Convert distance to similarity score
            // Similarity = 1.0 - (distance / maxLength)
            int maxLength = Math.Max(source.Length, target.Length);
            double similarity = 1.0 - ((double)distance / maxLength);

            // Ensure similarity is in valid range [0.0, 1.0]
            return Math.Max(0.0, Math.Min(1.0, similarity));
        }

        /// <summary>
        /// Finds similar API names from available APIs based on fuzzy matching.
        /// Returns results ordered by similarity (most similar first).
        /// </summary>
        /// <param name="query">Query string (may contain typos)</param>
        /// <param name="availableApis">List of available API names</param>
        /// <param name="threshold">Minimum similarity threshold (default 0.7)</param>
        /// <returns>List of matching API names ordered by similarity</returns>
        public List<string> FindSimilarApis(string query, string[] availableApis, double threshold = DefaultSimilarityThreshold)
        {
            if (string.IsNullOrWhiteSpace(query) || availableApis == null || availableApis.Length == 0)
                return new List<string>();

            // Calculate similarity for each API
            var results = new List<ApiSimilarity>();

            foreach (var api in availableApis)
            {
                if (string.IsNullOrWhiteSpace(api))
                    continue;

                double similarity = CalculateSimilarity(query, api);

                // Only include results above threshold
                if (similarity >= threshold)
                {
                    results.Add(new ApiSimilarity
                    {
                        ApiName = api,
                        Similarity = similarity
                    });
                }
            }

            // Sort by similarity descending (most similar first)
            return results
                .OrderByDescending(r => r.Similarity)
                .Select(r => r.ApiName)
                .ToList();
        }

        /// <summary>
        /// Normalizes a string for comparison: lowercase and trim.
        /// </summary>
        private string NormalizeString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return input.Trim().ToLowerInvariant();
        }

        /// <summary>
        /// Internal class for tracking API similarity scores.
        /// </summary>
        private class ApiSimilarity
        {
            public string ApiName { get; set; }
            public double Similarity { get; set; }
        }
    }
}
