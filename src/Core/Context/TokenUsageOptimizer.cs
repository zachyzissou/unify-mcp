using System;
using System.Collections.Generic;
using System.Linq;
using UnifyMcp.Core.Context.Models;

namespace UnifyMcp.Core.Context
{
    /// <summary>
    /// Monitors and optimizes token usage across MCP operations.
    /// Implements FR-035: Token usage tracking and optimization.
    /// </summary>
    public class TokenUsageOptimizer
    {
        private readonly TokenUsageMetrics metrics;
        private readonly TokenBudgetConfig budgetConfig;
        private readonly ToolResultSummarizer summarizer;

        private const int EstimatedCharsPerToken = 4;

        public event Action<OptimizationRecommendation> OnRecommendationGenerated;
        public event Action<string> OnBudgetWarning;
        public event Action<string> OnBudgetExceeded;

        public TokenUsageOptimizer(
            TokenBudgetConfig config = null,
            ToolResultSummarizer summarizer = null)
        {
            metrics = new TokenUsageMetrics();
            budgetConfig = config ?? new TokenBudgetConfig();
            this.summarizer = summarizer ?? new ToolResultSummarizer();
        }

        /// <summary>
        /// Records token usage for a request/response pair.
        /// </summary>
        /// <param name="toolName">Name of the tool invoked.</param>
        /// <param name="inputContent">Input content (request).</param>
        /// <param name="outputContent">Output content (response).</param>
        public void RecordUsage(string toolName, string inputContent, string outputContent)
        {
            var inputTokens = EstimateTokenCount(inputContent);
            var outputTokens = EstimateTokenCount(outputContent);

            metrics.TotalInputTokens += inputTokens;
            metrics.TotalOutputTokens += outputTokens;
            metrics.RequestCount++;

            if (!metrics.ToolUsage.ContainsKey(toolName))
            {
                metrics.ToolUsage[toolName] = new ToolTokenUsage { ToolName = toolName };
            }

            var toolUsage = metrics.ToolUsage[toolName];
            toolUsage.InputTokens += inputTokens;
            toolUsage.OutputTokens += outputTokens;
            toolUsage.InvocationCount++;

            metrics.EndTime = DateTime.UtcNow;

            // Check budget thresholds
            CheckBudget(inputTokens, outputTokens);
        }

        /// <summary>
        /// Records token savings from optimization.
        /// </summary>
        /// <param name="toolName">Name of the tool.</param>
        /// <param name="savedTokens">Number of tokens saved.</param>
        public void RecordSavings(string toolName, long savedTokens)
        {
            metrics.TokensSaved += savedTokens;

            if (metrics.ToolUsage.ContainsKey(toolName))
            {
                metrics.ToolUsage[toolName].TokensSaved += savedTokens;
            }
        }

        /// <summary>
        /// Gets current token usage metrics.
        /// </summary>
        public TokenUsageMetrics GetMetrics()
        {
            return metrics;
        }

        /// <summary>
        /// Analyzes usage patterns and generates optimization recommendations.
        /// </summary>
        public List<OptimizationRecommendation> GenerateRecommendations()
        {
            var recommendations = new List<OptimizationRecommendation>();

            // Identify tools with high token usage
            var topTools = metrics.GetTopTools(5);

            foreach (var toolUsage in topTools)
            {
                var tool = toolUsage.Value;

                // Recommend caching for frequently used tools
                if (tool.InvocationCount > 10 && tool.AverageTokensPerInvocation > 500)
                {
                    recommendations.Add(new OptimizationRecommendation
                    {
                        Type = OptimizationType.Caching,
                        Target = tool.ToolName,
                        Description = $"Tool '{tool.ToolName}' is invoked frequently ({tool.InvocationCount} times) with high token usage ({tool.AverageTokensPerInvocation:F0} avg tokens/invocation).",
                        EstimatedSavings = tool.TotalTokens / 2, // Assume 50% cache hit rate
                        Priority = 1,
                        Actions = new List<string>
                        {
                            "Enable response caching for this tool",
                            "Set appropriate cache duration based on data freshness requirements",
                            "Monitor cache hit rate and adjust as needed"
                        }
                    });
                }

                // Recommend summarization for tools with large responses
                if (tool.OutputTokens / tool.InvocationCount > 1000)
                {
                    recommendations.Add(new OptimizationRecommendation
                    {
                        Type = OptimizationType.Summarization,
                        Target = tool.ToolName,
                        Description = $"Tool '{tool.ToolName}' generates large responses ({tool.OutputTokens / tool.InvocationCount:F0} avg output tokens).",
                        EstimatedSavings = tool.OutputTokens / 3, // Assume 33% reduction
                        Priority = 2,
                        Actions = new List<string>
                        {
                            "Implement result summarization",
                            "Remove unnecessary metadata from responses",
                            "Truncate long lists with pagination"
                        }
                    });
                }

                // Recommend deduplication if multiple identical requests detected
                if (tool.InvocationCount > 5)
                {
                    var potentialDuplicates = tool.InvocationCount * 0.2; // Assume 20% duplicates
                    if (potentialDuplicates > 1)
                    {
                        recommendations.Add(new OptimizationRecommendation
                        {
                            Type = OptimizationType.Deduplication,
                            Target = tool.ToolName,
                            Description = $"Tool '{tool.ToolName}' may have redundant invocations.",
                            EstimatedSavings = (long)(tool.TotalTokens * 0.2),
                            Priority = 3,
                            Actions = new List<string>
                            {
                                "Implement request deduplication",
                                "Track request hashes to identify duplicates",
                                "Queue duplicate requests to wait for in-flight responses"
                            }
                        });
                    }
                }
            }

            // Sort by priority and estimated savings
            recommendations = recommendations
                .OrderBy(r => r.Priority)
                .ThenByDescending(r => r.EstimatedSavings)
                .ToList();

            foreach (var recommendation in recommendations)
            {
                OnRecommendationGenerated?.Invoke(recommendation);
            }

            return recommendations;
        }

        /// <summary>
        /// Optimizes content to fit within token budget.
        /// </summary>
        /// <param name="content">Content to optimize.</param>
        /// <param name="targetTokens">Target token count.</param>
        /// <returns>Optimized content and actual token savings.</returns>
        public (string optimizedContent, int tokensSaved) OptimizeContent(string content, int targetTokens)
        {
            var currentTokens = EstimateTokenCount(content);

            if (currentTokens <= targetTokens)
            {
                return (content, 0); // Already within budget
            }

            // Calculate required compression ratio
            var ratio = (double)targetTokens / currentTokens;

            var options = summarizer.OptimizeOptionsForTokenBudget(targetTokens, currentTokens);
            var result = summarizer.Summarize(content, options);

            var tokensSaved = currentTokens - EstimateTokenCount(result.SummarizedContent);

            return (result.SummarizedContent, tokensSaved);
        }

        /// <summary>
        /// Checks if a request would exceed budget and applies optimization if configured.
        /// </summary>
        /// <param name="requestContent">Request content.</param>
        /// <returns>Potentially optimized content and whether it was modified.</returns>
        public (string content, bool wasOptimized) CheckAndOptimizeRequest(string requestContent)
        {
            var tokenCount = EstimateTokenCount(requestContent);

            if (tokenCount <= budgetConfig.MaxTokensPerRequest)
            {
                return (requestContent, false);
            }

            if (budgetConfig.AutoOptimize)
            {
                var (optimized, saved) = OptimizeContent(requestContent, budgetConfig.MaxTokensPerRequest);
                RecordSavings("AutoOptimization", saved);
                return (optimized, true);
            }

            OnBudgetExceeded?.Invoke($"Request exceeds budget: {tokenCount} tokens (max: {budgetConfig.MaxTokensPerRequest})");
            return (requestContent, false);
        }

        /// <summary>
        /// Checks if a response would exceed budget and applies optimization if configured.
        /// </summary>
        /// <param name="responseContent">Response content.</param>
        /// <returns>Potentially optimized content and whether it was modified.</returns>
        public (string content, bool wasOptimized) CheckAndOptimizeResponse(string responseContent)
        {
            var tokenCount = EstimateTokenCount(responseContent);

            if (tokenCount <= budgetConfig.MaxTokensPerResponse)
            {
                return (responseContent, false);
            }

            if (budgetConfig.AutoOptimize)
            {
                var (optimized, saved) = OptimizeContent(responseContent, budgetConfig.MaxTokensPerResponse);
                RecordSavings("AutoOptimization", saved);
                return (optimized, true);
            }

            OnBudgetExceeded?.Invoke($"Response exceeds budget: {tokenCount} tokens (max: {budgetConfig.MaxTokensPerResponse})");
            return (responseContent, false);
        }

        /// <summary>
        /// Resets metrics to start fresh tracking.
        /// </summary>
        public void ResetMetrics()
        {
            metrics.TotalInputTokens = 0;
            metrics.TotalOutputTokens = 0;
            metrics.RequestCount = 0;
            metrics.TokensSaved = 0;
            metrics.ToolUsage.Clear();
            metrics.StartTime = DateTime.UtcNow;
            metrics.EndTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets efficiency score (0.0 to 1.0, higher is better).
        /// </summary>
        public double GetEfficiencyScore()
        {
            if (metrics.TotalTokens == 0)
                return 1.0;

            // Efficiency = tokens saved / (total tokens + tokens saved)
            var totalWithoutOptimization = metrics.TotalTokens + metrics.TokensSaved;
            return (double)metrics.TokensSaved / totalWithoutOptimization;
        }

        private int EstimateTokenCount(string content)
        {
            if (string.IsNullOrEmpty(content))
                return 0;

            return content.Length / EstimatedCharsPerToken;
        }

        private void CheckBudget(long inputTokens, long outputTokens)
        {
            var requestWarningThreshold = (long)(budgetConfig.MaxTokensPerRequest * budgetConfig.WarningThreshold);
            var responseWarningThreshold = (long)(budgetConfig.MaxTokensPerResponse * budgetConfig.WarningThreshold);

            if (inputTokens >= requestWarningThreshold)
            {
                OnBudgetWarning?.Invoke($"Request approaching budget limit: {inputTokens} tokens ({(double)inputTokens / budgetConfig.MaxTokensPerRequest:P0} of max)");
            }

            if (outputTokens >= responseWarningThreshold)
            {
                OnBudgetWarning?.Invoke($"Response approaching budget limit: {outputTokens} tokens ({(double)outputTokens / budgetConfig.MaxTokensPerResponse:P0} of max)");
            }

            if (inputTokens > budgetConfig.MaxTokensPerRequest)
            {
                OnBudgetExceeded?.Invoke($"Request exceeded budget: {inputTokens} tokens (max: {budgetConfig.MaxTokensPerRequest})");
            }

            if (outputTokens > budgetConfig.MaxTokensPerResponse)
            {
                OnBudgetExceeded?.Invoke($"Response exceeded budget: {outputTokens} tokens (max: {budgetConfig.MaxTokensPerResponse})");
            }
        }
    }
}
