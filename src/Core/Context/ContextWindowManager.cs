using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnifyMcp.Core.Context.Models;

namespace UnifyMcp.Core.Context
{
    /// <summary>
    /// Coordinates all context optimization techniques to manage token budget.
    /// Implements FR-036: Integrated context window management.
    /// </summary>
    public class ContextWindowManager : IDisposable
    {
        private readonly ContextAwareToolSuggester toolSuggester;
        private readonly ToolResultSummarizer resultSummarizer;
        private readonly RequestDeduplicator requestDeduplicator;
        private readonly ResponseCacheManager responseCacheManager;
        private readonly TokenUsageOptimizer tokenOptimizer;

        public event Action<string> OnOptimizationApplied;

        public ContextWindowManager(
            ContextAwareToolSuggester suggester = null,
            ToolResultSummarizer summarizer = null,
            RequestDeduplicator deduplicator = null,
            ResponseCacheManager cacheManager = null,
            TokenUsageOptimizer optimizer = null)
        {
            toolSuggester = suggester ?? new ContextAwareToolSuggester();
            resultSummarizer = summarizer ?? new ToolResultSummarizer();
            requestDeduplicator = deduplicator ?? new RequestDeduplicator();
            responseCacheManager = cacheManager ?? new ResponseCacheManager();
            tokenOptimizer = optimizer ?? new TokenUsageOptimizer();

            // Wire up events
            tokenOptimizer.OnRecommendationGenerated += HandleRecommendation;
            tokenOptimizer.OnBudgetWarning += HandleBudgetWarning;
            tokenOptimizer.OnBudgetExceeded += HandleBudgetExceeded;
        }

        /// <summary>
        /// Processes a tool request with full optimization pipeline.
        /// </summary>
        /// <param name="toolName">Name of the tool to invoke.</param>
        /// <param name="parameters">Parameters for the tool.</param>
        /// <param name="executor">Function to execute the tool if needed.</param>
        /// <param name="options">Optional optimization options.</param>
        /// <returns>Optimized tool result.</returns>
        public async Task<OptimizedToolResult> ProcessToolRequestAsync(
            string toolName,
            Dictionary<string, object> parameters,
            Func<Task<string>> executor,
            ContextOptimizationOptions options = null)
        {
            options ??= new ContextOptimizationOptions();

            var result = new OptimizedToolResult
            {
                ToolName = toolName,
                Parameters = parameters,
                RequestedAt = DateTime.UtcNow
            };

            try
            {
                // Step 1: Compute request hash for deduplication/caching
                var requestKey = new RequestKey(toolName, parameters);
                var requestHash = requestKey.Hash;

                // Step 2: Check persistent cache first
                if (options.EnableCaching)
                {
                    var cachedResponse = await responseCacheManager.GetCachedResponseAsync(toolName, requestHash);
                    if (cachedResponse != null)
                    {
                        result.Response = cachedResponse;
                        result.WasCached = true;
                        result.OptimizationsApplied.Add("persistent_cache_hit");
                        OnOptimizationApplied?.Invoke($"Cache hit for {toolName}");
                        return result;
                    }
                }

                // Step 3: Use request deduplicator (handles in-memory cache + in-flight requests)
                if (options.EnableDeduplication)
                {
                    result.Response = await requestDeduplicator.ProcessRequestAsync(
                        toolName,
                        parameters,
                        executor,
                        options.CacheDuration
                    );

                    // Check if it was deduplicated
                    var cachedEntry = requestDeduplicator.GetCachedResponse(toolName, parameters);
                    if (cachedEntry != null && cachedEntry.HitCount > 0)
                    {
                        result.WasDeduplicated = true;
                        result.OptimizationsApplied.Add("request_deduplication");
                        OnOptimizationApplied?.Invoke($"Deduplicated request for {toolName}");
                    }
                }
                else
                {
                    // Execute directly without deduplication
                    result.Response = await executor();
                }

                // Step 4: Record token usage
                var inputContent = System.Text.Json.JsonSerializer.Serialize(parameters);
                tokenOptimizer.RecordUsage(toolName, inputContent, result.Response);

                // Step 5: Apply summarization if needed
                if (options.EnableSummarization)
                {
                    var summarizationResult = resultSummarizer.Summarize(result.Response, options.SummarizationOptions);

                    if (summarizationResult.SummarizedLength < summarizationResult.OriginalLength)
                    {
                        result.Response = summarizationResult.SummarizedContent;
                        result.TokensSaved = summarizationResult.EstimatedTokenSavings;
                        result.OptimizationsApplied.AddRange(summarizationResult.AppliedTechniques);
                        tokenOptimizer.RecordSavings(toolName, result.TokensSaved);
                        OnOptimizationApplied?.Invoke($"Summarized result for {toolName}, saved {result.TokensSaved} tokens");
                    }
                }

                // Step 6: Check token budget and auto-optimize if needed
                if (options.EnforceTokenBudget)
                {
                    var (optimized, wasOptimized) = tokenOptimizer.CheckAndOptimizeResponse(result.Response);
                    if (wasOptimized)
                    {
                        var originalTokens = tokenOptimizer.EstimateTokenCount(result.Response);
                        var optimizedTokens = tokenOptimizer.EstimateTokenCount(optimized);
                        result.TokensSaved += originalTokens - optimizedTokens;
                        result.Response = optimized;
                        result.OptimizationsApplied.Add("token_budget_enforcement");
                        OnOptimizationApplied?.Invoke($"Auto-optimized {toolName} to fit token budget");
                    }
                }

                // Step 7: Store in persistent cache for future requests
                if (options.EnableCaching && !result.WasCached)
                {
                    await responseCacheManager.CacheResponseAsync(
                        toolName,
                        requestHash,
                        parameters,
                        result.Response,
                        options.CacheDuration
                    );
                }

                result.CompletedAt = DateTime.UtcNow;
                return result;
            }
            catch (Exception ex)
            {
                result.Error = ex;
                result.CompletedAt = DateTime.UtcNow;
                throw;
            }
        }

        /// <summary>
        /// Suggests relevant tools based on query analysis.
        /// </summary>
        public QueryAnalysisResult AnalyzeQuery(string query, int maxSuggestions = 3)
        {
            return toolSuggester.AnalyzeQuery(query, maxSuggestions);
        }

        /// <summary>
        /// Records feedback about tool relevance to improve suggestions.
        /// </summary>
        public void RecordToolFeedback(string toolName, bool wasRelevant)
        {
            toolSuggester.RecordToolInvocation(toolName, wasRelevant);
        }

        /// <summary>
        /// Gets comprehensive optimization statistics.
        /// </summary>
        public async Task<OptimizationStatistics> GetStatisticsAsync()
        {
            var stats = new OptimizationStatistics
            {
                TokenMetrics = tokenOptimizer.GetMetrics(),
                DeduplicationStats = requestDeduplicator.GetStats(),
                CacheStatistics = await responseCacheManager.GetStatisticsAsync(),
                ToolInvocationHistory = toolSuggester.GetInvocationHistory(),
                EfficiencyScore = tokenOptimizer.GetEfficiencyScore()
            };

            return stats;
        }

        /// <summary>
        /// Generates optimization recommendations based on usage patterns.
        /// </summary>
        public List<OptimizationRecommendation> GenerateRecommendations()
        {
            return tokenOptimizer.GenerateRecommendations();
        }

        /// <summary>
        /// Clears all caches and resets metrics.
        /// </summary>
        public async Task ResetAsync()
        {
            requestDeduplicator.ClearCache();
            await responseCacheManager.ClearAllAsync();
            tokenOptimizer.ResetMetrics();
        }

        /// <summary>
        /// Performs maintenance operations (cleanup expired cache entries, etc.).
        /// </summary>
        public async Task PerformMaintenanceAsync()
        {
            var expiredCount = await responseCacheManager.CleanupExpiredEntriesAsync();
            if (expiredCount > 0)
            {
                OnOptimizationApplied?.Invoke($"Cleaned up {expiredCount} expired cache entries");
            }
        }

        private void HandleRecommendation(OptimizationRecommendation recommendation)
        {
            OnOptimizationApplied?.Invoke($"Recommendation: {recommendation.Description}");
        }

        private void HandleBudgetWarning(string message)
        {
            OnOptimizationApplied?.Invoke($"Warning: {message}");
        }

        private void HandleBudgetExceeded(string message)
        {
            OnOptimizationApplied?.Invoke($"Budget Exceeded: {message}");
        }

        public void Dispose()
        {
            requestDeduplicator?.Dispose();
            responseCacheManager?.Dispose();
        }
    }

    /// <summary>
    /// Options for context optimization behavior.
    /// </summary>
    public class ContextOptimizationOptions
    {
        /// <summary>
        /// Enable persistent caching.
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Enable request deduplication.
        /// </summary>
        public bool EnableDeduplication { get; set; } = true;

        /// <summary>
        /// Enable result summarization.
        /// </summary>
        public bool EnableSummarization { get; set; } = true;

        /// <summary>
        /// Enforce token budget limits.
        /// </summary>
        public bool EnforceTokenBudget { get; set; } = true;

        /// <summary>
        /// Cache duration for responses.
        /// </summary>
        public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Summarization options.
        /// </summary>
        public SummarizationOptions SummarizationOptions { get; set; } = new SummarizationOptions();
    }

    /// <summary>
    /// Result of an optimized tool request.
    /// </summary>
    public class OptimizedToolResult
    {
        /// <summary>
        /// Tool name.
        /// </summary>
        public string ToolName { get; set; }

        /// <summary>
        /// Parameters used.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; }

        /// <summary>
        /// Tool response.
        /// </summary>
        public string Response { get; set; }

        /// <summary>
        /// Whether the response was served from cache.
        /// </summary>
        public bool WasCached { get; set; }

        /// <summary>
        /// Whether the request was deduplicated.
        /// </summary>
        public bool WasDeduplicated { get; set; }

        /// <summary>
        /// Tokens saved through optimization.
        /// </summary>
        public int TokensSaved { get; set; }

        /// <summary>
        /// List of optimizations applied.
        /// </summary>
        public List<string> OptimizationsApplied { get; set; }

        /// <summary>
        /// When the request was made.
        /// </summary>
        public DateTime RequestedAt { get; set; }

        /// <summary>
        /// When the request completed.
        /// </summary>
        public DateTime CompletedAt { get; set; }

        /// <summary>
        /// Request duration.
        /// </summary>
        public TimeSpan Duration => CompletedAt - RequestedAt;

        /// <summary>
        /// Error if request failed.
        /// </summary>
        public Exception Error { get; set; }

        public OptimizedToolResult()
        {
            Parameters = new Dictionary<string, object>();
            OptimizationsApplied = new List<string>();
        }
    }

    /// <summary>
    /// Comprehensive optimization statistics.
    /// </summary>
    public class OptimizationStatistics
    {
        /// <summary>
        /// Token usage metrics.
        /// </summary>
        public TokenUsageMetrics TokenMetrics { get; set; }

        /// <summary>
        /// Deduplication statistics.
        /// </summary>
        public DeduplicationStats DeduplicationStats { get; set; }

        /// <summary>
        /// Cache statistics.
        /// </summary>
        public CacheStatistics CacheStatistics { get; set; }

        /// <summary>
        /// Tool invocation history.
        /// </summary>
        public Dictionary<string, double> ToolInvocationHistory { get; set; }

        /// <summary>
        /// Overall efficiency score (0.0 to 1.0).
        /// </summary>
        public double EfficiencyScore { get; set; }
    }
}
