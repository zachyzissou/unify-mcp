using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnifyMcp.Core.Context.Models;

namespace UnifyMcp.Core.Context
{
    /// <summary>
    /// Analyzes queries and suggests relevant MCP tools to minimize unnecessary invocations.
    /// Implements FR-031: Context-aware tool suggestions.
    /// </summary>
    public class ContextAwareToolSuggester
    {
        private readonly Dictionary<QueryIntent, List<string>> intentToToolsMap;
        private readonly Dictionary<string, List<string>> keywordToToolsMap;
        private readonly Dictionary<string, double> toolInvocationHistory;

        public ContextAwareToolSuggester()
        {
            intentToToolsMap = InitializeIntentMapping();
            keywordToToolsMap = InitializeKeywordMapping();
            toolInvocationHistory = new Dictionary<string, double>();
        }

        /// <summary>
        /// Analyzes a query and suggests relevant tools.
        /// </summary>
        /// <param name="query">User query text.</param>
        /// <param name="maxSuggestions">Maximum number of tools to suggest.</param>
        /// <param name="confidenceThreshold">Minimum confidence score (0.0 to 1.0).</param>
        /// <returns>Query analysis result with tool suggestions.</returns>
        public QueryAnalysisResult AnalyzeQuery(string query, int maxSuggestions = 3, double confidenceThreshold = 0.5)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be null or empty", nameof(query));

            var result = new QueryAnalysisResult { Query = query };

            // Extract intent
            result.Intent = ExtractIntent(query);

            // Extract entities (API names, version numbers, etc.)
            result.ExtractedEntities = ExtractEntities(query);

            // Generate tool suggestions
            var suggestions = GenerateSuggestions(query, result.Intent, result.ExtractedEntities);

            // Filter by confidence threshold and limit results
            result.SuggestedTools = suggestions
                .Where(s => s.ConfidenceScore >= confidenceThreshold)
                .OrderByDescending(s => s.ConfidenceScore)
                .Take(maxSuggestions)
                .ToList();

            return result;
        }

        /// <summary>
        /// Records the result of a tool invocation to improve future suggestions.
        /// </summary>
        /// <param name="toolName">Name of the tool that was invoked.</param>
        /// <param name="wasRelevant">Whether the tool result was relevant to the query.</param>
        public void RecordToolInvocation(string toolName, bool wasRelevant)
        {
            if (string.IsNullOrWhiteSpace(toolName))
                return;

            if (!toolInvocationHistory.ContainsKey(toolName))
                toolInvocationHistory[toolName] = 0.5; // Start with neutral score

            // Adjust score based on relevance (simple learning mechanism)
            var currentScore = toolInvocationHistory[toolName];
            toolInvocationHistory[toolName] = wasRelevant
                ? Math.Min(1.0, currentScore + 0.1)
                : Math.Max(0.0, currentScore - 0.1);
        }

        /// <summary>
        /// Gets the invocation history for analytics.
        /// </summary>
        public Dictionary<string, double> GetInvocationHistory()
        {
            return new Dictionary<string, double>(toolInvocationHistory);
        }

        private QueryIntent ExtractIntent(string query)
        {
            var lowerQuery = query.ToLowerInvariant();

            // Documentation keywords
            if (ContainsAny(lowerQuery, "api", "documentation", "docs", "reference", "method", "class", "function"))
                return QueryIntent.Documentation;

            // Performance keywords
            if (ContainsAny(lowerQuery, "performance", "profiler", "fps", "memory", "bottleneck", "slow", "lag"))
                return QueryIntent.Performance;

            // Build keywords
            if (ContainsAny(lowerQuery, "build", "compile", "deploy", "platform", "bundle", "asset bundle"))
                return QueryIntent.Build;

            // Asset keywords
            if (ContainsAny(lowerQuery, "asset", "texture", "model", "prefab", "material", "shader", "unused"))
                return QueryIntent.Assets;

            // Scene keywords
            if (ContainsAny(lowerQuery, "scene", "hierarchy", "gameobject", "component", "lighting", "reference"))
                return QueryIntent.Scene;

            // Package keywords
            if (ContainsAny(lowerQuery, "package", "dependency", "version", "install", "compatibility"))
                return QueryIntent.Packages;

            return QueryIntent.Unknown;
        }

        private Dictionary<string, List<string>> ExtractEntities(string query)
        {
            var entities = new Dictionary<string, List<string>>();

            // Extract Unity API references (e.g., GameObject.SetActive, Transform.position)
            var apiPattern = @"\b([A-Z][a-zA-Z]+)\.([a-zA-Z_][a-zA-Z0-9_]*)\b";
            var apiMatches = Regex.Matches(query, apiPattern);
            if (apiMatches.Count > 0)
            {
                entities["apis"] = apiMatches
                    .Cast<Match>()
                    .Select(m => m.Value)
                    .Distinct()
                    .ToList();
            }

            // Extract Unity version numbers (e.g., 2021.3, 2022.1.5)
            var versionPattern = @"\b20\d{2}\.\d+(?:\.\d+)?(?:[a-z]\d+)?\b";
            var versionMatches = Regex.Matches(query, versionPattern);
            if (versionMatches.Count > 0)
            {
                entities["versions"] = versionMatches
                    .Cast<Match>()
                    .Select(m => m.Value)
                    .Distinct()
                    .ToList();
            }

            // Extract file paths
            var pathPattern = @"\b(?:[A-Za-z]:[/\\]|[/\\])?(?:[A-Za-z0-9_\-]+[/\\])+[A-Za-z0-9_\-]+\.[a-z]{2,4}\b";
            var pathMatches = Regex.Matches(query, pathPattern);
            if (pathMatches.Count > 0)
            {
                entities["paths"] = pathMatches
                    .Cast<Match>()
                    .Select(m => m.Value)
                    .Distinct()
                    .ToList();
            }

            return entities;
        }

        private List<ToolSuggestion> GenerateSuggestions(
            string query,
            QueryIntent intent,
            Dictionary<string, List<string>> entities)
        {
            var suggestions = new Dictionary<string, ToolSuggestion>();
            var lowerQuery = query.ToLowerInvariant();

            // Intent-based suggestions
            if (intentToToolsMap.ContainsKey(intent))
            {
                foreach (var toolName in intentToToolsMap[intent])
                {
                    if (!suggestions.ContainsKey(toolName))
                    {
                        suggestions[toolName] = new ToolSuggestion
                        {
                            ToolName = toolName,
                            ConfidenceScore = 0.6,
                            Reason = $"Matches {intent} intent"
                        };
                    }
                }
            }

            // Keyword-based suggestions
            foreach (var kvp in keywordToToolsMap)
            {
                if (lowerQuery.Contains(kvp.Key))
                {
                    foreach (var toolName in kvp.Value)
                    {
                        if (!suggestions.ContainsKey(toolName))
                        {
                            suggestions[toolName] = new ToolSuggestion
                            {
                                ToolName = toolName,
                                ConfidenceScore = 0.5,
                                Reason = $"Matches keyword '{kvp.Key}'"
                            };
                        }
                        else
                        {
                            // Boost confidence for multiple matches
                            suggestions[toolName].ConfidenceScore += 0.1;
                            suggestions[toolName].MatchedKeywords.Add(kvp.Key);
                        }
                    }
                }
            }

            // Boost confidence based on historical success
            foreach (var suggestion in suggestions.Values)
            {
                if (toolInvocationHistory.ContainsKey(suggestion.ToolName))
                {
                    var historicalScore = toolInvocationHistory[suggestion.ToolName];
                    suggestion.ConfidenceScore = (suggestion.ConfidenceScore + historicalScore) / 2.0;
                }
            }

            // Add suggested parameters based on extracted entities
            foreach (var suggestion in suggestions.Values)
            {
                AddSuggestedParameters(suggestion, entities);
            }

            // Normalize confidence scores to [0.0, 1.0]
            foreach (var suggestion in suggestions.Values)
            {
                suggestion.ConfidenceScore = Math.Max(0.0, Math.Min(1.0, suggestion.ConfidenceScore));
            }

            return suggestions.Values.ToList();
        }

        private void AddSuggestedParameters(ToolSuggestion suggestion, Dictionary<string, List<string>> entities)
        {
            // Documentation tools
            if (suggestion.ToolName == "QueryDocumentation" && entities.ContainsKey("apis"))
            {
                var firstApi = entities["apis"].FirstOrDefault();
                if (firstApi != null)
                    suggestion.SuggestedParameters["query"] = firstApi;
            }
            else if (suggestion.ToolName == "SearchApiFuzzy" && entities.ContainsKey("apis"))
            {
                var firstApi = entities["apis"].FirstOrDefault();
                if (firstApi != null)
                    suggestion.SuggestedParameters["query"] = firstApi;
            }

            // Scene tools
            if (suggestion.ToolName == "ValidateScene" && entities.ContainsKey("paths"))
            {
                var scenePath = entities["paths"].FirstOrDefault(p => p.EndsWith(".unity"));
                if (scenePath != null)
                    suggestion.SuggestedParameters["scenePath"] = scenePath;
            }

            // Asset tools
            if (suggestion.ToolName == "AnalyzeAssetDependencies" && entities.ContainsKey("paths"))
            {
                var assetPath = entities["paths"].FirstOrDefault();
                if (assetPath != null)
                    suggestion.SuggestedParameters["assetPath"] = assetPath;
            }

            // Build tools
            if (suggestion.ToolName == "ValidateBuildConfiguration")
            {
                // Try to extract platform from query
                var platforms = new[] { "Windows", "macOS", "Linux", "iOS", "Android", "WebGL" };
                foreach (var platform in platforms)
                {
                    if (suggestion.Reason.Contains(platform, StringComparison.OrdinalIgnoreCase))
                    {
                        suggestion.SuggestedParameters["platform"] = platform;
                        break;
                    }
                }
            }
        }

        private bool ContainsAny(string text, params string[] keywords)
        {
            return keywords.Any(keyword => text.Contains(keyword));
        }

        private Dictionary<QueryIntent, List<string>> InitializeIntentMapping()
        {
            return new Dictionary<QueryIntent, List<string>>
            {
                {
                    QueryIntent.Documentation,
                    new List<string> { "QueryDocumentation", "SearchApiFuzzy", "CheckDeprecation", "GetCodeExamples" }
                },
                {
                    QueryIntent.Performance,
                    new List<string> { "CaptureProfilerSnapshot", "AnalyzeBottlenecks", "DetectAntipatterns", "CompareSnapshots" }
                },
                {
                    QueryIntent.Build,
                    new List<string> { "ValidateBuildConfiguration", "StartMultiPlatformBuild", "GetBuildSizeAnalysis" }
                },
                {
                    QueryIntent.Assets,
                    new List<string> { "FindUnusedAssets", "AnalyzeAssetDependencies", "OptimizeTextureSettings" }
                },
                {
                    QueryIntent.Scene,
                    new List<string> { "ValidateScene", "FindMissingReferences", "AnalyzeLightingSetup" }
                },
                {
                    QueryIntent.Packages,
                    new List<string> { "ListInstalledPackages", "CheckPackageCompatibility", "ResolveDependencies" }
                }
            };
        }

        private Dictionary<string, List<string>> InitializeKeywordMapping()
        {
            return new Dictionary<string, List<string>>
            {
                // Documentation keywords
                { "deprecated", new List<string> { "CheckDeprecation" } },
                { "example", new List<string> { "GetCodeExamples", "QueryDocumentation" } },
                { "fuzzy", new List<string> { "SearchApiFuzzy" } },
                { "typo", new List<string> { "SearchApiFuzzy" } },

                // Performance keywords
                { "bottleneck", new List<string> { "AnalyzeBottlenecks" } },
                { "antipattern", new List<string> { "DetectAntipatterns" } },
                { "compare", new List<string> { "CompareSnapshots" } },
                { "snapshot", new List<string> { "CaptureProfilerSnapshot" } },

                // Build keywords
                { "multi-platform", new List<string> { "StartMultiPlatformBuild" } },
                { "size", new List<string> { "GetBuildSizeAnalysis" } },
                { "validate build", new List<string> { "ValidateBuildConfiguration" } },

                // Asset keywords
                { "unused", new List<string> { "FindUnusedAssets" } },
                { "dependencies", new List<string> { "AnalyzeAssetDependencies", "ResolveDependencies" } },
                { "optimize texture", new List<string> { "OptimizeTextureSettings" } },

                // Scene keywords
                { "missing reference", new List<string> { "FindMissingReferences" } },
                { "lighting", new List<string> { "AnalyzeLightingSetup" } },
                { "validate scene", new List<string> { "ValidateScene" } },

                // Package keywords
                { "installed packages", new List<string> { "ListInstalledPackages" } },
                { "compatibility", new List<string> { "CheckPackageCompatibility" } },
                { "resolve", new List<string> { "ResolveDependencies" } }
            };
        }
    }
}
