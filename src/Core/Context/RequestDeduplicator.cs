using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnifyMcp.Core.Context.Models;

namespace UnifyMcp.Core.Context
{
    /// <summary>
    /// Detects and prevents redundant tool invocations.
    /// Implements FR-033: Request deduplication.
    /// </summary>
    public class RequestDeduplicator : IDisposable
    {
        private readonly ConcurrentDictionary<RequestKey, CachedResponse> cache;
        private readonly ConcurrentDictionary<RequestKey, SemaphoreSlim> inFlightRequests;
        private readonly ConcurrentDictionary<RequestKey, DateTime> semaphoreAccessTimes;
        private readonly TimeSpan defaultCacheDuration;
        private readonly int maxCacheSize;
        private readonly Timer cleanupTimer;
        private readonly Timer semaphoreCleanupTimer;

        private int totalRequests;
        private int deduplicatedRequests;
        private int uniqueRequests;

        /// <summary>
        /// Creates a new RequestDeduplicator instance.
        /// </summary>
        /// <param name="cacheDuration">Duration to cache responses (default: 5 minutes)</param>
        /// <param name="maxCacheSize">Maximum number of cached responses (default: 1000)</param>
        /// <param name="cleanupInterval">Interval for cache cleanup (default: 1 minute)</param>
        /// <param name="semaphoreCleanupInterval">Interval for semaphore cleanup (default: 5 minutes)</param>
        public RequestDeduplicator(
            TimeSpan? cacheDuration = null,
            int maxCacheSize = 1000,
            TimeSpan? cleanupInterval = null,
            TimeSpan? semaphoreCleanupInterval = null)
        {
            cache = new ConcurrentDictionary<RequestKey, CachedResponse>();
            inFlightRequests = new ConcurrentDictionary<RequestKey, SemaphoreSlim>();
            semaphoreAccessTimes = new ConcurrentDictionary<RequestKey, DateTime>();
            defaultCacheDuration = cacheDuration ?? TimeSpan.FromMinutes(5);
            this.maxCacheSize = maxCacheSize;

            var interval = cleanupInterval ?? TimeSpan.FromMinutes(1);
            cleanupTimer = new Timer(CleanupExpiredEntries, null, interval, interval);

            var semCleanupInterval = semaphoreCleanupInterval ?? TimeSpan.FromMinutes(5);
            semaphoreCleanupTimer = new Timer(CleanupOldSemaphores, null, semCleanupInterval, semCleanupInterval);
        }

        /// <summary>
        /// Processes a request, either executing it or returning a cached result.
        /// </summary>
        /// <param name="toolName">Name of the tool to invoke.</param>
        /// <param name="parameters">Parameters for the tool.</param>
        /// <param name="executor">Function to execute if no cached result exists.</param>
        /// <param name="cacheDuration">Optional override for cache duration.</param>
        /// <returns>Tool result (either cached or newly executed).</returns>
        public async Task<string> ProcessRequestAsync(
            string toolName,
            Dictionary<string, object> parameters,
            Func<Task<string>> executor,
            TimeSpan? cacheDuration = null)
        {
            if (string.IsNullOrWhiteSpace(toolName))
                throw new ArgumentException("Tool name cannot be null or empty", nameof(toolName));

            if (executor == null)
                throw new ArgumentNullException(nameof(executor));

            Interlocked.Increment(ref totalRequests);

            var requestKey = new RequestKey(toolName, parameters);

            // Check cache first
            if (cache.TryGetValue(requestKey, out var cachedResponse))
            {
                if (!cachedResponse.IsExpired)
                {
                    cachedResponse.HitCount++;
                    Interlocked.Increment(ref deduplicatedRequests);
                    return cachedResponse.Response;
                }
                else
                {
                    // Remove expired entry
                    cache.TryRemove(requestKey, out _);
                }
            }

            // Get or create semaphore for this request
            var semaphore = inFlightRequests.GetOrAdd(requestKey, _ => new SemaphoreSlim(1, 1));
            semaphoreAccessTimes[requestKey] = DateTime.UtcNow;

            try
            {
                await semaphore.WaitAsync();

                // Double-check cache (another thread might have completed the request)
                if (cache.TryGetValue(requestKey, out cachedResponse) && !cachedResponse.IsExpired)
                {
                    cachedResponse.HitCount++;
                    Interlocked.Increment(ref deduplicatedRequests);
                    return cachedResponse.Response;
                }

                // Execute the request
                Interlocked.Increment(ref uniqueRequests);
                var result = await executor();

                // Cache the result
                var duration = cacheDuration ?? defaultCacheDuration;
                var newCachedResponse = new CachedResponse
                {
                    RequestKey = requestKey,
                    Response = result,
                    CachedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.Add(duration),
                    HitCount = 0
                };

                // Enforce max cache size
                if (cache.Count >= maxCacheSize)
                {
                    EvictLeastRecentlyUsed();
                }

                cache[requestKey] = newCachedResponse;

                return result;
            }
            finally
            {
                semaphore.Release();
                // Don't remove semaphore immediately - keep it for a short time
                // to handle rapid duplicate requests
            }
        }

        /// <summary>
        /// Invalidates cached responses for a specific tool.
        /// </summary>
        /// <param name="toolName">Name of the tool whose cache to invalidate.</param>
        public void InvalidateCache(string toolName)
        {
            if (string.IsNullOrWhiteSpace(toolName))
                return;

            var keysToRemove = cache.Keys
                .Where(key => key.ToolName == toolName)
                .ToList();

            foreach (var key in keysToRemove)
            {
                cache.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Invalidates a specific cached response.
        /// </summary>
        /// <param name="toolName">Name of the tool.</param>
        /// <param name="parameters">Parameters of the request to invalidate.</param>
        public void InvalidateCache(string toolName, Dictionary<string, object> parameters)
        {
            var requestKey = new RequestKey(toolName, parameters);
            cache.TryRemove(requestKey, out _);
        }

        /// <summary>
        /// Clears all cached responses.
        /// </summary>
        public void ClearCache()
        {
            cache.Clear();
            inFlightRequests.Clear();
        }

        /// <summary>
        /// Gets deduplication statistics.
        /// </summary>
        public DeduplicationStats GetStats()
        {
            var expiredCount = cache.Values.Count(c => c.IsExpired);

            return new DeduplicationStats
            {
                TotalRequests = totalRequests,
                DeduplicatedRequests = deduplicatedRequests,
                UniqueRequests = uniqueRequests,
                CacheSize = cache.Count,
                ExpiredEntries = expiredCount
            };
        }

        /// <summary>
        /// Gets information about a specific cached response.
        /// </summary>
        public CachedResponse GetCachedResponse(string toolName, Dictionary<string, object> parameters)
        {
            var requestKey = new RequestKey(toolName, parameters);
            cache.TryGetValue(requestKey, out var response);
            return response;
        }

        /// <summary>
        /// Gets all cached responses for a specific tool.
        /// </summary>
        public List<CachedResponse> GetCachedResponsesForTool(string toolName)
        {
            return cache.Values
                .Where(r => r.RequestKey.ToolName == toolName)
                .ToList();
        }

        /// <summary>
        /// Gets the current number of active semaphores.
        /// Used for testing to verify semaphore cleanup.
        /// </summary>
        public int GetSemaphoreCount()
        {
            return inFlightRequests.Count;
        }

        private void CleanupExpiredEntries(object state)
        {
            var expiredKeys = cache
                .Where(kvp => kvp.Value.IsExpired)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                cache.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Cleans up semaphores that haven't been accessed recently.
        /// Prevents memory leaks from abandoned semaphores.
        /// </summary>
        private void CleanupOldSemaphores(object state)
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-5);

            var oldKeys = semaphoreAccessTimes
                .Where(kvp => kvp.Value < cutoffTime && !cache.ContainsKey(kvp.Key))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in oldKeys)
            {
                if (inFlightRequests.TryRemove(key, out var semaphore))
                {
                    semaphore.Dispose();
                }
                semaphoreAccessTimes.TryRemove(key, out _);
            }
        }

        private void EvictLeastRecentlyUsed()
        {
            // Simple LRU: remove entries with lowest hit count and oldest cache time
            var toEvict = cache
                .OrderBy(kvp => kvp.Value.HitCount)
                .ThenBy(kvp => kvp.Value.CachedAt)
                .Take(maxCacheSize / 10) // Evict 10% of cache
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in toEvict)
            {
                cache.TryRemove(key, out _);
            }
        }

        public void Dispose()
        {
            cleanupTimer?.Dispose();
            semaphoreCleanupTimer?.Dispose();

            foreach (var semaphore in inFlightRequests.Values)
            {
                semaphore?.Dispose();
            }

            cache.Clear();
            inFlightRequests.Clear();
            semaphoreAccessTimes.Clear();
        }
    }
}
