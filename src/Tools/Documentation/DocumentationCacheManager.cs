using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UnifyMcp.Tools.Documentation
{
    /// <summary>
    /// Manages documentation cache with 30-day expiration tracking.
    /// Stores cache metadata (URL, fetch timestamp, expiration) and implements automatic cleanup.
    /// </summary>
    public class DocumentationCacheManager
    {
        private readonly string cacheDirectory;
        private readonly string metadataFilePath;
        private Dictionary<string, CacheMetadata> cacheMetadata;

        private const int DefaultExpirationDays = 30;

        public DocumentationCacheManager(string cacheDirectory)
        {
            this.cacheDirectory = cacheDirectory ?? throw new ArgumentNullException(nameof(cacheDirectory));
            this.metadataFilePath = Path.Combine(cacheDirectory, "cache_metadata.json");

            // Ensure cache directory exists
            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }

            // Load metadata
            LoadMetadata();
        }

        /// <summary>
        /// Records that a URL was cached.
        /// </summary>
        /// <param name="url">Documentation URL</param>
        /// <param name="cacheFilePath">Path to cached HTML file</param>
        public void RecordCachedUrl(string url, string cacheFilePath)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be null or empty", nameof(url));

            var metadata = new CacheMetadata
            {
                Url = url,
                CacheFilePath = cacheFilePath,
                FetchTimestamp = DateTime.UtcNow,
                ExpirationTimestamp = DateTime.UtcNow.AddDays(DefaultExpirationDays)
            };

            cacheMetadata[url] = metadata;
            SaveMetadata();
        }

        /// <summary>
        /// Checks if a URL is cached and not expired.
        /// </summary>
        /// <param name="url">Documentation URL</param>
        /// <returns>True if cached and not expired, false otherwise</returns>
        public bool IsCached(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            if (!cacheMetadata.TryGetValue(url, out var metadata))
                return false;

            // Check if expired
            if (metadata.ExpirationTimestamp < DateTime.UtcNow)
                return false;

            // Check if file still exists
            if (!File.Exists(metadata.CacheFilePath))
                return false;

            return true;
        }

        /// <summary>
        /// Gets cache file path for a URL if cached and not expired.
        /// </summary>
        /// <param name="url">Documentation URL</param>
        /// <returns>Cache file path, or null if not cached or expired</returns>
        public string GetCacheFilePath(string url)
        {
            if (!IsCached(url))
                return null;

            return cacheMetadata[url].CacheFilePath;
        }

        /// <summary>
        /// Cleans up expired cache entries.
        /// Should be called on startup or periodically.
        /// </summary>
        public CacheCleanupResult CleanupExpiredCache()
        {
            var result = new CacheCleanupResult();
            var expiredEntries = new List<string>();

            foreach (var kvp in cacheMetadata)
            {
                var url = kvp.Key;
                var metadata = kvp.Value;

                // Check if expired
                if (metadata.ExpirationTimestamp < DateTime.UtcNow)
                {
                    expiredEntries.Add(url);

                    // Delete cached file
                    try
                    {
                        if (File.Exists(metadata.CacheFilePath))
                        {
                            File.Delete(metadata.CacheFilePath);
                            result.FilesDeleted++;
                        }
                    }
                    catch (Exception)
                    {
                        result.FailedDeletions++;
                    }
                }
                else if (!File.Exists(metadata.CacheFilePath))
                {
                    // File missing but not expired - mark as orphaned
                    expiredEntries.Add(url);
                    result.OrphanedEntries++;
                }
            }

            // Remove expired entries from metadata
            foreach (var url in expiredEntries)
            {
                cacheMetadata.Remove(url);
            }

            // Save updated metadata
            if (expiredEntries.Count > 0)
            {
                SaveMetadata();
            }

            return result;
        }

        /// <summary>
        /// Clears all cached entries and deletes all files.
        /// </summary>
        public void ClearAllCache()
        {
            // Delete all HTML files
            if (Directory.Exists(cacheDirectory))
            {
                var htmlFiles = Directory.GetFiles(cacheDirectory, "*.html");
                foreach (var file in htmlFiles)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception)
                    {
                        // Continue even if deletion fails
                    }
                }
            }

            // Clear metadata
            cacheMetadata.Clear();
            SaveMetadata();
        }

        /// <summary>
        /// Gets cache statistics.
        /// </summary>
        public CacheStatistics GetStatistics()
        {
            var now = DateTime.UtcNow;
            var stats = new CacheStatistics
            {
                TotalEntries = cacheMetadata.Count,
                ExpiredEntries = cacheMetadata.Count(kvp => kvp.Value.ExpirationTimestamp < now),
                ValidEntries = cacheMetadata.Count(kvp => kvp.Value.ExpirationTimestamp >= now && File.Exists(kvp.Value.CacheFilePath))
            };

            // Calculate total cache size
            long totalSize = 0;
            foreach (var metadata in cacheMetadata.Values)
            {
                if (File.Exists(metadata.CacheFilePath))
                {
                    try
                    {
                        totalSize += new FileInfo(metadata.CacheFilePath).Length;
                    }
                    catch (Exception)
                    {
                        // Skip files we can't read
                    }
                }
            }
            stats.TotalSizeBytes = totalSize;

            return stats;
        }

        /// <summary>
        /// Loads cache metadata from disk.
        /// </summary>
        private void LoadMetadata()
        {
            cacheMetadata = new Dictionary<string, CacheMetadata>();

            if (!File.Exists(metadataFilePath))
                return;

            try
            {
                var json = File.ReadAllText(metadataFilePath);
                var entries = System.Text.Json.JsonSerializer.Deserialize<List<CacheMetadata>>(json);

                if (entries != null)
                {
                    foreach (var entry in entries)
                    {
                        cacheMetadata[entry.Url] = entry;
                    }
                }
            }
            catch (Exception)
            {
                // If loading fails, start with empty metadata
                cacheMetadata = new Dictionary<string, CacheMetadata>();
            }
        }

        /// <summary>
        /// Saves cache metadata to disk.
        /// </summary>
        private void SaveMetadata()
        {
            try
            {
                var entries = cacheMetadata.Values.ToList();
                var json = System.Text.Json.JsonSerializer.Serialize(entries, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(metadataFilePath, json);
            }
            catch (Exception)
            {
                // Silently fail metadata save - not critical
            }
        }
    }

    /// <summary>
    /// Metadata for a cached documentation entry.
    /// </summary>
    public class CacheMetadata
    {
        public string Url { get; set; }
        public string CacheFilePath { get; set; }
        public DateTime FetchTimestamp { get; set; }
        public DateTime ExpirationTimestamp { get; set; }
    }

    /// <summary>
    /// Result of cache cleanup operation.
    /// </summary>
    public class CacheCleanupResult
    {
        public int FilesDeleted { get; set; }
        public int FailedDeletions { get; set; }
        public int OrphanedEntries { get; set; }

        public override string ToString()
        {
            return $"Cleanup: {FilesDeleted} deleted, {FailedDeletions} failed, {OrphanedEntries} orphaned";
        }
    }

    /// <summary>
    /// Cache statistics.
    /// </summary>
    public class CacheStatistics
    {
        public int TotalEntries { get; set; }
        public int ExpiredEntries { get; set; }
        public int ValidEntries { get; set; }
        public long TotalSizeBytes { get; set; }

        public double TotalSizeMB => TotalSizeBytes / (1024.0 * 1024.0);

        public override string ToString()
        {
            return $"{ValidEntries}/{TotalEntries} valid entries, {TotalSizeMB:F2} MB total";
        }
    }
}
