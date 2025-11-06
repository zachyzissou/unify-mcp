using System;
using System.Collections.Generic;
using System.Linq;

namespace UnifyMcp.Core.Context.Models
{
    /// <summary>
    /// Metrics about token usage for optimization.
    /// </summary>
    public class TokenUsageMetrics
    {
        /// <summary>
        /// Total input tokens consumed.
        /// </summary>
        public long TotalInputTokens { get; set; }

        /// <summary>
        /// Total output tokens generated.
        /// </summary>
        public long TotalOutputTokens { get; set; }

        /// <summary>
        /// Total tokens (input + output).
        /// </summary>
        public long TotalTokens => TotalInputTokens + TotalOutputTokens;

        /// <summary>
        /// Number of requests processed.
        /// </summary>
        public int RequestCount { get; set; }

        /// <summary>
        /// Average tokens per request.
        /// </summary>
        public double AverageTokensPerRequest =>
            RequestCount > 0 ? (double)TotalTokens / RequestCount : 0;

        /// <summary>
        /// Token usage by tool.
        /// </summary>
        public Dictionary<string, ToolTokenUsage> ToolUsage { get; set; }

        /// <summary>
        /// Token savings from optimization techniques.
        /// </summary>
        public long TokensSaved { get; set; }

        /// <summary>
        /// Start time for these metrics.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End time for these metrics.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Duration of the measurement period.
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;

        public TokenUsageMetrics()
        {
            ToolUsage = new Dictionary<string, ToolTokenUsage>();
            StartTime = DateTime.UtcNow;
            EndTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the most token-intensive tools.
        /// </summary>
        public List<KeyValuePair<string, ToolTokenUsage>> GetTopTools(int limit = 5)
        {
            return ToolUsage
                .OrderByDescending(kvp => kvp.Value.TotalTokens)
                .Take(limit)
                .ToList();
        }
    }

    /// <summary>
    /// Token usage statistics for a specific tool.
    /// </summary>
    public class ToolTokenUsage
    {
        /// <summary>
        /// Name of the tool.
        /// </summary>
        public string ToolName { get; set; }

        /// <summary>
        /// Total input tokens for this tool.
        /// </summary>
        public long InputTokens { get; set; }

        /// <summary>
        /// Total output tokens for this tool.
        /// </summary>
        public long OutputTokens { get; set; }

        /// <summary>
        /// Total tokens for this tool.
        /// </summary>
        public long TotalTokens => InputTokens + OutputTokens;

        /// <summary>
        /// Number of invocations.
        /// </summary>
        public int InvocationCount { get; set; }

        /// <summary>
        /// Average tokens per invocation.
        /// </summary>
        public double AverageTokensPerInvocation =>
            InvocationCount > 0 ? (double)TotalTokens / InvocationCount : 0;

        /// <summary>
        /// Tokens saved through optimization.
        /// </summary>
        public long TokensSaved { get; set; }
    }

    /// <summary>
    /// Optimization recommendations based on token usage analysis.
    /// </summary>
    public class OptimizationRecommendation
    {
        /// <summary>
        /// Type of optimization recommended.
        /// </summary>
        public OptimizationType Type { get; set; }

        /// <summary>
        /// Tool or area this recommendation applies to.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// Description of the recommendation.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Estimated token savings if implemented.
        /// </summary>
        public long EstimatedSavings { get; set; }

        /// <summary>
        /// Priority of this recommendation (1 = highest).
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Specific actions to take.
        /// </summary>
        public List<string> Actions { get; set; }

        public OptimizationRecommendation()
        {
            Actions = new List<string>();
        }
    }

    /// <summary>
    /// Types of optimization techniques.
    /// </summary>
    public enum OptimizationType
    {
        /// <summary>
        /// Implement or improve caching.
        /// </summary>
        Caching,

        /// <summary>
        /// Summarize or compress results.
        /// </summary>
        Summarization,

        /// <summary>
        /// Reduce redundant requests.
        /// </summary>
        Deduplication,

        /// <summary>
        /// Paginate large result sets.
        /// </summary>
        Pagination,

        /// <summary>
        /// Filter results to reduce payload size.
        /// </summary>
        Filtering,

        /// <summary>
        /// Use incremental updates instead of full data.
        /// </summary>
        IncrementalUpdates,

        /// <summary>
        /// Request only necessary fields.
        /// </summary>
        SelectiveFields
    }

    /// <summary>
    /// Configuration for token budget management.
    /// </summary>
    public class TokenBudgetConfig
    {
        /// <summary>
        /// Maximum tokens allowed per request.
        /// </summary>
        public int MaxTokensPerRequest { get; set; } = 4000;

        /// <summary>
        /// Maximum tokens allowed per response.
        /// </summary>
        public int MaxTokensPerResponse { get; set; } = 2000;

        /// <summary>
        /// Warning threshold (percentage of max).
        /// </summary>
        public double WarningThreshold { get; set; } = 0.8;

        /// <summary>
        /// Whether to automatically apply optimization when budget is exceeded.
        /// </summary>
        public bool AutoOptimize { get; set; } = true;

        /// <summary>
        /// Target compression ratio when optimizing (0.0 to 1.0).
        /// </summary>
        public double TargetCompressionRatio { get; set; } = 0.5;
    }
}
