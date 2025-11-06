using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace UnifyMcp.Tools.Documentation
{
    /// <summary>
    /// Fetches Unity documentation from docs.unity3d.com with exponential backoff and rate limiting.
    /// Caches HTML locally to reduce network requests and respect Unity's servers.
    /// </summary>
    public class WebDocumentationFetcher
    {
        private readonly string cacheDirectory;
        private readonly HttpClient httpClient;
        private DateTime lastRequestTime = DateTime.MinValue;

        // Rate limiting configuration
        public int MinimumDelayMilliseconds { get; set; } = 1000; // 1 second default
        public int MaximumDelayMilliseconds { get; set; } = 2000; // 2 seconds default

        // Retry configuration
        public int MaxRetries { get; set; } = 3;
        public int InitialRetryDelayMilliseconds { get; set; } = 1000;

        public WebDocumentationFetcher(string cacheDirectory)
        {
            this.cacheDirectory = cacheDirectory ?? throw new ArgumentNullException(nameof(cacheDirectory));

            // Ensure cache directory exists
            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }

            // Configure HttpClient with realistic user agent
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent",
                "UnifyMCP/1.0 (Unity Documentation Indexer; +https://github.com/anthropics/unify-mcp)");
            httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Fetches documentation from URL with caching and rate limiting.
        /// </summary>
        /// <param name="url">Documentation URL (e.g., https://docs.unity3d.com/ScriptReference/Transform.html)</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>HTML content from cache or web fetch</returns>
        public async Task<string> FetchDocumentationAsync(string url, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be null or empty", nameof(url));

            // Check cache first
            var cachedHtml = GetCachedHtml(url);
            if (cachedHtml != null)
            {
                return cachedHtml;
            }

            // Implement rate limiting
            await ApplyRateLimitingAsync(cancellationToken);

            // Fetch from web with retries
            var html = await FetchFromWebWithRetriesAsync(url, cancellationToken);

            // Cache the result
            if (!string.IsNullOrEmpty(html))
            {
                CacheHtml(url, html);
            }

            return html;
        }

        /// <summary>
        /// Gets cached HTML if available and not expired (managed by DocumentationCacheManager).
        /// </summary>
        private string GetCachedHtml(string url)
        {
            var cacheFilePath = GetCacheFilePath(url);

            if (!File.Exists(cacheFilePath))
                return null;

            try
            {
                return File.ReadAllText(cacheFilePath);
            }
            catch (Exception)
            {
                // If reading fails, return null to trigger web fetch
                return null;
            }
        }

        /// <summary>
        /// Caches HTML content to disk.
        /// </summary>
        private void CacheHtml(string url, string html)
        {
            var cacheFilePath = GetCacheFilePath(url);

            try
            {
                File.WriteAllText(cacheFilePath, html);
            }
            catch (Exception)
            {
                // Silently fail cache write - not critical
            }
        }

        /// <summary>
        /// Gets cache file path for a URL.
        /// Uses URL hash to create safe filename.
        /// </summary>
        private string GetCacheFilePath(string url)
        {
            // Create a safe filename from URL hash
            var urlHash = GetUrlHash(url);
            return Path.Combine(cacheDirectory, $"{urlHash}.html");
        }

        /// <summary>
        /// Creates a hash of the URL for cache filename.
        /// </summary>
        private string GetUrlHash(string url)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(url));
                return BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 32);
            }
        }

        /// <summary>
        /// Applies rate limiting between requests.
        /// Ensures minimum delay between consecutive requests.
        /// </summary>
        private async Task ApplyRateLimitingAsync(CancellationToken cancellationToken)
        {
            var timeSinceLastRequest = DateTime.UtcNow - lastRequestTime;
            var randomDelay = new Random().Next(MinimumDelayMilliseconds, MaximumDelayMilliseconds + 1);
            var requiredDelay = TimeSpan.FromMilliseconds(randomDelay);

            if (timeSinceLastRequest < requiredDelay)
            {
                var remainingDelay = requiredDelay - timeSinceLastRequest;
                await Task.Delay(remainingDelay, cancellationToken);
            }

            lastRequestTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Fetches HTML from web with exponential backoff retry logic.
        /// </summary>
        private async Task<string> FetchFromWebWithRetriesAsync(string url, CancellationToken cancellationToken)
        {
            int retryCount = 0;
            int retryDelay = InitialRetryDelayMilliseconds;

            while (retryCount <= MaxRetries)
            {
                try
                {
                    var response = await httpClient.GetAsync(url, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }

                    // Handle rate limiting (429) and service unavailable (503)
                    if (response.StatusCode == HttpStatusCode.TooManyRequests ||
                        response.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        if (retryCount >= MaxRetries)
                        {
                            throw new HttpRequestException(
                                $"Failed after {MaxRetries} retries. Status: {response.StatusCode}");
                        }

                        // Exponential backoff
                        await Task.Delay(retryDelay, cancellationToken);
                        retryDelay *= 2;
                        retryCount++;
                        continue;
                    }

                    // Other errors
                    throw new HttpRequestException($"HTTP request failed with status: {response.StatusCode}");
                }
                catch (TaskCanceledException)
                {
                    throw; // Propagate cancellation
                }
                catch (Exception ex)
                {
                    if (retryCount >= MaxRetries)
                    {
                        throw new Exception($"Failed to fetch documentation after {MaxRetries} retries", ex);
                    }

                    // Exponential backoff for network errors
                    await Task.Delay(retryDelay, cancellationToken);
                    retryDelay *= 2;
                    retryCount++;
                }
            }

            throw new Exception("Unexpected retry loop exit");
        }

        /// <summary>
        /// Clears the entire cache directory.
        /// </summary>
        public void ClearCache()
        {
            if (!Directory.Exists(cacheDirectory))
                return;

            try
            {
                var files = Directory.GetFiles(cacheDirectory, "*.html");
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
            catch (Exception)
            {
                // Silently fail cache clear - not critical
            }
        }

        /// <summary>
        /// Gets the number of cached HTML files.
        /// </summary>
        public int GetCachedFileCount()
        {
            if (!Directory.Exists(cacheDirectory))
                return 0;

            try
            {
                return Directory.GetFiles(cacheDirectory, "*.html").Length;
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}
