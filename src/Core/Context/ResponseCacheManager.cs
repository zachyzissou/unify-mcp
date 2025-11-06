using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace UnifyMcp.Core.Context
{
    /// <summary>
    /// Manages persistent caching of MCP responses to disk.
    /// Implements FR-034: Response caching.
    /// </summary>
    public class ResponseCacheManager : IDisposable
    {
        private readonly string databasePath;
        private SQLiteConnection connection;
        private readonly object connectionLock = new object();

        public ResponseCacheManager(string cachePath = null)
        {
            if (string.IsNullOrWhiteSpace(cachePath))
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var cacheDir = Path.Combine(appDataPath, "UnifyMcp", "ResponseCache");
                Directory.CreateDirectory(cacheDir);
                databasePath = Path.Combine(cacheDir, "response_cache.db");
            }
            else
            {
                databasePath = cachePath;
                var directory = Path.GetDirectoryName(cachePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
            }

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            lock (connectionLock)
            {
                connection = new SQLiteConnection($"Data Source={databasePath};Version=3;");
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS response_cache (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            tool_name TEXT NOT NULL,
                            request_hash TEXT NOT NULL,
                            parameters TEXT NOT NULL,
                            response TEXT NOT NULL,
                            cached_at TEXT NOT NULL,
                            expires_at TEXT NOT NULL,
                            hit_count INTEGER DEFAULT 0,
                            last_accessed TEXT NOT NULL,
                            UNIQUE(tool_name, request_hash)
                        );

                        CREATE INDEX IF NOT EXISTS idx_tool_name ON response_cache(tool_name);
                        CREATE INDEX IF NOT EXISTS idx_expires_at ON response_cache(expires_at);
                        CREATE INDEX IF NOT EXISTS idx_request_hash ON response_cache(request_hash);
                    ";
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Retrieves a cached response if available and not expired.
        /// </summary>
        public async Task<string> GetCachedResponseAsync(string toolName, string requestHash)
        {
            return await Task.Run(() =>
            {
                lock (connectionLock)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT response, expires_at, hit_count
                            FROM response_cache
                            WHERE tool_name = @toolName AND request_hash = @requestHash
                        ";
                        command.Parameters.AddWithValue("@toolName", toolName);
                        command.Parameters.AddWithValue("@requestHash", requestHash);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var expiresAt = DateTime.Parse(reader.GetString(1));

                                if (DateTime.UtcNow < expiresAt)
                                {
                                    var response = reader.GetString(0);
                                    var hitCount = reader.GetInt32(2);

                                    // Update hit count and last accessed
                                    UpdateHitCount(toolName, requestHash, hitCount + 1);

                                    return response;
                                }
                                else
                                {
                                    // Expired - remove it
                                    DeleteCachedResponse(toolName, requestHash);
                                }
                            }
                        }
                    }

                    return null;
                }
            });
        }

        /// <summary>
        /// Stores a response in the cache.
        /// </summary>
        public async Task CacheResponseAsync(
            string toolName,
            string requestHash,
            Dictionary<string, object> parameters,
            string response,
            TimeSpan cacheDuration)
        {
            await Task.Run(() =>
            {
                lock (connectionLock)
                {
                    var now = DateTime.UtcNow;
                    var expiresAt = now.Add(cacheDuration);

                    var parametersJson = JsonSerializer.Serialize(parameters);

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            INSERT OR REPLACE INTO response_cache
                            (tool_name, request_hash, parameters, response, cached_at, expires_at, hit_count, last_accessed)
                            VALUES (@toolName, @requestHash, @parameters, @response, @cachedAt, @expiresAt, 0, @lastAccessed)
                        ";
                        command.Parameters.AddWithValue("@toolName", toolName);
                        command.Parameters.AddWithValue("@requestHash", requestHash);
                        command.Parameters.AddWithValue("@parameters", parametersJson);
                        command.Parameters.AddWithValue("@response", response);
                        command.Parameters.AddWithValue("@cachedAt", now.ToString("o"));
                        command.Parameters.AddWithValue("@expiresAt", expiresAt.ToString("o"));
                        command.Parameters.AddWithValue("@lastAccessed", now.ToString("o"));

                        command.ExecuteNonQuery();
                    }
                }
            });
        }

        /// <summary>
        /// Invalidates all cached responses for a specific tool.
        /// </summary>
        public async Task InvalidateCacheAsync(string toolName)
        {
            await Task.Run(() =>
            {
                lock (connectionLock)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "DELETE FROM response_cache WHERE tool_name = @toolName";
                        command.Parameters.AddWithValue("@toolName", toolName);
                        command.ExecuteNonQuery();
                    }
                }
            });
        }

        /// <summary>
        /// Invalidates a specific cached response.
        /// </summary>
        public async Task InvalidateCacheAsync(string toolName, string requestHash)
        {
            await Task.Run(() => DeleteCachedResponse(toolName, requestHash));
        }

        /// <summary>
        /// Clears all expired cache entries.
        /// </summary>
        public async Task<int> CleanupExpiredEntriesAsync()
        {
            return await Task.Run(() =>
            {
                lock (connectionLock)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            DELETE FROM response_cache
                            WHERE expires_at < @now
                        ";
                        command.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
                        return command.ExecuteNonQuery();
                    }
                }
            });
        }

        /// <summary>
        /// Clears all cached responses.
        /// </summary>
        public async Task ClearAllAsync()
        {
            await Task.Run(() =>
            {
                lock (connectionLock)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "DELETE FROM response_cache";
                        command.ExecuteNonQuery();
                    }
                }
            });
        }

        /// <summary>
        /// Gets cache statistics.
        /// </summary>
        public async Task<CacheStatistics> GetStatisticsAsync()
        {
            return await Task.Run(() =>
            {
                lock (connectionLock)
                {
                    var stats = new CacheStatistics();

                    using (var command = connection.CreateCommand())
                    {
                        // Total entries
                        command.CommandText = "SELECT COUNT(*) FROM response_cache";
                        stats.TotalEntries = Convert.ToInt32(command.ExecuteScalar());

                        // Expired entries
                        command.CommandText = @"
                            SELECT COUNT(*) FROM response_cache
                            WHERE expires_at < @now
                        ";
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
                        stats.ExpiredEntries = Convert.ToInt32(command.ExecuteScalar());

                        // Total hits
                        command.CommandText = "SELECT COALESCE(SUM(hit_count), 0) FROM response_cache";
                        stats.TotalHits = Convert.ToInt64(command.ExecuteScalar());

                        // Cache size in bytes
                        command.CommandText = "SELECT COALESCE(SUM(LENGTH(response)), 0) FROM response_cache";
                        stats.CacheSizeBytes = Convert.ToInt64(command.ExecuteScalar());

                        // Most cached tools
                        command.CommandText = @"
                            SELECT tool_name, COUNT(*) as count
                            FROM response_cache
                            GROUP BY tool_name
                            ORDER BY count DESC
                            LIMIT 10
                        ";
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                stats.ToolCacheCounts[reader.GetString(0)] = reader.GetInt32(1);
                            }
                        }
                    }

                    return stats;
                }
            });
        }

        /// <summary>
        /// Gets the most frequently accessed cache entries.
        /// </summary>
        public async Task<List<CacheEntry>> GetTopEntriesAsync(int limit = 10)
        {
            return await Task.Run(() =>
            {
                var entries = new List<CacheEntry>();

                lock (connectionLock)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT tool_name, request_hash, parameters, cached_at, expires_at, hit_count, last_accessed
                            FROM response_cache
                            ORDER BY hit_count DESC
                            LIMIT @limit
                        ";
                        command.Parameters.AddWithValue("@limit", limit);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                entries.Add(new CacheEntry
                                {
                                    ToolName = reader.GetString(0),
                                    RequestHash = reader.GetString(1),
                                    Parameters = reader.GetString(2),
                                    CachedAt = DateTime.Parse(reader.GetString(3)),
                                    ExpiresAt = DateTime.Parse(reader.GetString(4)),
                                    HitCount = reader.GetInt32(5),
                                    LastAccessed = DateTime.Parse(reader.GetString(6))
                                });
                            }
                        }
                    }
                }

                return entries;
            });
        }

        private void UpdateHitCount(string toolName, string requestHash, int newHitCount)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    UPDATE response_cache
                    SET hit_count = @hitCount, last_accessed = @lastAccessed
                    WHERE tool_name = @toolName AND request_hash = @requestHash
                ";
                command.Parameters.AddWithValue("@hitCount", newHitCount);
                command.Parameters.AddWithValue("@lastAccessed", DateTime.UtcNow.ToString("o"));
                command.Parameters.AddWithValue("@toolName", toolName);
                command.Parameters.AddWithValue("@requestHash", requestHash);
                command.ExecuteNonQuery();
            }
        }

        private void DeleteCachedResponse(string toolName, string requestHash)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    DELETE FROM response_cache
                    WHERE tool_name = @toolName AND request_hash = @requestHash
                ";
                command.Parameters.AddWithValue("@toolName", toolName);
                command.Parameters.AddWithValue("@requestHash", requestHash);
                command.ExecuteNonQuery();
            }
        }

        public void Dispose()
        {
            lock (connectionLock)
            {
                connection?.Close();
                connection?.Dispose();
            }
        }
    }

    /// <summary>
    /// Statistics about the response cache.
    /// </summary>
    public class CacheStatistics
    {
        public int TotalEntries { get; set; }
        public int ExpiredEntries { get; set; }
        public long TotalHits { get; set; }
        public long CacheSizeBytes { get; set; }
        public Dictionary<string, int> ToolCacheCounts { get; set; }

        public int ActiveEntries => TotalEntries - ExpiredEntries;
        public double CacheSizeMB => CacheSizeBytes / (1024.0 * 1024.0);

        public CacheStatistics()
        {
            ToolCacheCounts = new Dictionary<string, int>();
        }
    }

    /// <summary>
    /// Represents a cache entry for reporting purposes.
    /// </summary>
    public class CacheEntry
    {
        public string ToolName { get; set; }
        public string RequestHash { get; set; }
        public string Parameters { get; set; }
        public DateTime CachedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int HitCount { get; set; }
        public DateTime LastAccessed { get; set; }

        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        public TimeSpan Age => DateTime.UtcNow - CachedAt;
    }
}
