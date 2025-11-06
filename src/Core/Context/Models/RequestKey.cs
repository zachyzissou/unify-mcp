using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace UnifyMcp.Core.Context.Models
{
    /// <summary>
    /// Represents a unique key for a tool request used for deduplication.
    /// </summary>
    public class RequestKey : IEquatable<RequestKey>
    {
        /// <summary>
        /// Name of the tool being invoked.
        /// </summary>
        public string ToolName { get; set; }

        /// <summary>
        /// Parameters passed to the tool (sorted for consistency).
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; }

        /// <summary>
        /// Hash of the request for efficient comparison.
        /// </summary>
        public string Hash { get; private set; }

        public RequestKey(string toolName, Dictionary<string, object> parameters)
        {
            ToolName = toolName ?? throw new ArgumentNullException(nameof(toolName));
            Parameters = parameters ?? new Dictionary<string, object>();
            Hash = ComputeHash();
        }

        private string ComputeHash()
        {
            var sb = new StringBuilder();
            sb.Append(ToolName);
            sb.Append("|");

            // Sort parameters for consistent hashing
            var sortedParams = Parameters
                .OrderBy(kvp => kvp.Key)
                .ToList();

            foreach (var kvp in sortedParams)
            {
                sb.Append(kvp.Key);
                sb.Append("=");
                sb.Append(JsonSerializer.Serialize(kvp.Value));
                sb.Append(";");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        public bool Equals(RequestKey other)
        {
            if (other == null) return false;
            return Hash == other.Hash;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RequestKey);
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        public override string ToString()
        {
            return $"{ToolName}({string.Join(", ", Parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"))})";
        }
    }

    /// <summary>
    /// Represents a cached response for a deduplicated request.
    /// </summary>
    public class CachedResponse
    {
        /// <summary>
        /// The request key this response corresponds to.
        /// </summary>
        public RequestKey RequestKey { get; set; }

        /// <summary>
        /// The cached response content.
        /// </summary>
        public string Response { get; set; }

        /// <summary>
        /// When this response was cached.
        /// </summary>
        public DateTime CachedAt { get; set; }

        /// <summary>
        /// When this cache entry expires.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Number of times this cached response has been reused.
        /// </summary>
        public int HitCount { get; set; }

        /// <summary>
        /// Whether this cache entry has expired.
        /// </summary>
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;

        /// <summary>
        /// Age of this cache entry.
        /// </summary>
        public TimeSpan Age => DateTime.UtcNow - CachedAt;
    }

    /// <summary>
    /// Statistics about request deduplication.
    /// </summary>
    public class DeduplicationStats
    {
        /// <summary>
        /// Total number of requests processed.
        /// </summary>
        public int TotalRequests { get; set; }

        /// <summary>
        /// Number of requests that were deduplicated (cache hits).
        /// </summary>
        public int DeduplicatedRequests { get; set; }

        /// <summary>
        /// Number of unique requests (cache misses).
        /// </summary>
        public int UniqueRequests { get; set; }

        /// <summary>
        /// Deduplication rate (0.0 to 1.0).
        /// </summary>
        public double DeduplicationRate =>
            TotalRequests > 0 ? (double)DeduplicatedRequests / TotalRequests : 0.0;

        /// <summary>
        /// Number of currently cached responses.
        /// </summary>
        public int CacheSize { get; set; }

        /// <summary>
        /// Number of cache entries that have expired.
        /// </summary>
        public int ExpiredEntries { get; set; }
    }
}
