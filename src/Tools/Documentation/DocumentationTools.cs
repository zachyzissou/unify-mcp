using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnifyMcp.Common.Threading;

namespace UnifyMcp.Tools.Documentation
{
    /// <summary>
    /// MCP tool implementation for Unity documentation queries.
    /// Implements FR-001 to FR-005: Full-text search, fuzzy search, local indexing, web fallback, deprecation warnings.
    /// [McpServerToolType] attribute for ModelContextProtocol SDK integration (to be added in final integration).
    /// </summary>
    // [McpServerToolType] // TODO: Add when integrating with ModelContextProtocol SDK
    public class DocumentationTools : IDisposable
    {
        private readonly UnityDocumentationIndexer indexer;
        private readonly FuzzyDocumentationSearch fuzzySearch;
        private readonly UnityVersionManager versionManager;
        private readonly DeprecationDetector deprecationDetector;
        private readonly DocumentationIndexingWorker indexingWorker;
        private readonly UnityInstallationDetector installationDetector;
        private readonly HtmlDocumentationParser htmlParser;

        public DocumentationTools(
            string databasePath,
            UnityDocumentationIndexer indexer = null,
            FuzzyDocumentationSearch fuzzySearch = null,
            UnityVersionManager versionManager = null,
            DeprecationDetector deprecationDetector = null)
        {
            // Use provided instances or create new ones
            this.indexer = indexer ?? new UnityDocumentationIndexer(databasePath);
            this.fuzzySearch = fuzzySearch ?? new FuzzyDocumentationSearch();
            this.versionManager = versionManager ?? new UnityVersionManager();
            this.deprecationDetector = deprecationDetector ?? new DeprecationDetector(this.versionManager);

            // Create supporting instances
            this.installationDetector = new UnityInstallationDetector();
            this.htmlParser = new HtmlDocumentationParser();
            this.indexingWorker = new DocumentationIndexingWorker(this.indexer, this.htmlParser, this.installationDetector);

            // Ensure database exists
            if (indexer == null)
            {
                this.indexer.CreateDatabase();
            }
        }

        /// <summary>
        /// Queries Unity documentation using full-text search with BM25 ranking (FR-001).
        /// Returns method signatures, parameters, descriptions, and code examples.
        /// Cached queries complete in <100ms.
        /// </summary>
        /// <param name="query">Search query (e.g., "Transform.Translate", "GameObject")</param>
        /// <returns>JSON array of matching documentation entries</returns>
        // [McpServerTool] // TODO: Add when integrating with ModelContextProtocol SDK
        public async Task<string> QueryDocumentation(string query)
        {
            return await Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return "[]"; // Empty result
                }

                var results = indexer.QueryDocumentation(query);

                // Convert to JSON
                var jsonResults = results.Select(entry => new
                {
                    className = entry.ClassName,
                    methodName = entry.MethodName,
                    returnType = entry.ReturnType,
                    parameters = entry.Parameters,
                    description = entry.Description,
                    codeExamples = entry.CodeExamples,
                    unityVersion = entry.UnityVersion,
                    documentationUrl = entry.DocumentationUrl,
                    isDeprecated = entry.IsDeprecated,
                    replacementApi = entry.ReplacementApi
                });

                return System.Text.Json.JsonSerializer.Serialize(jsonResults, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
            });
        }

        /// <summary>
        /// Performs fuzzy search with typo tolerance (FR-002).
        /// Returns similar API names ranked by similarity using Levenshtein distance.
        /// Example: "Transform.Translte" → "Transform.Translate"
        /// </summary>
        /// <param name="query">Query string (may contain typos)</param>
        /// <param name="threshold">Similarity threshold (0.0-1.0, default 0.7)</param>
        /// <returns>JSON array of suggested API names</returns>
        // [McpServerTool] // TODO: Add when integrating with ModelContextProtocol SDK
        public async Task<string> SearchApiFuzzy(string query, double threshold = 0.7)
        {
            return await Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return "[]";
                }

                // Get all indexed APIs
                var allResults = indexer.QueryDocumentation("*"); // Query all
                var allApis = allResults
                    .Select(e => $"{e.ClassName}.{e.MethodName}")
                    .Distinct()
                    .ToArray();

                // Fuzzy search
                var suggestions = fuzzySearch.FindSimilarApis(query, allApis, threshold);

                // Return as JSON array
                return System.Text.Json.JsonSerializer.Serialize(suggestions, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
            });
        }

        /// <summary>
        /// Gets the current Unity Editor version and maps to documentation version.
        /// Example: "2021.3.25f1" → "2021.3"
        /// </summary>
        /// <returns>JSON object with currentVersion and documentationVersion</returns>
        // [McpServerTool] // TODO: Add when integrating with ModelContextProtocol SDK
        public async Task<string> GetUnityVersion()
        {
            return await Task.Run(() =>
            {
                var currentVersion = versionManager.GetCurrentUnityVersion();
                var docVersion = versionManager.MapToDocumentationVersion(currentVersion);

                var result = new
                {
                    currentVersion = currentVersion,
                    documentationVersion = docVersion
                };

                return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
            });
        }

        /// <summary>
        /// Refreshes the documentation index by detecting Unity installations and indexing ScriptReference HTML files (FR-003).
        /// Returns progress updates via callback.
        /// </summary>
        /// <param name="progressCallback">Optional callback for progress updates</param>
        /// <returns>JSON object with indexing summary</returns>
        // [McpServerTool] // TODO: Add when integrating with ModelContextProtocol SDK
        public async Task<string> RefreshDocumentationIndex(Action<string> progressCallback = null)
        {
            return await Task.Run(() =>
            {
                // Detect Unity installations
                var installations = installationDetector.DetectUnityInstallations();

                if (installations.Count == 0)
                {
                    return System.Text.Json.JsonSerializer.Serialize(new
                    {
                        success = false,
                        message = "No Unity installations found"
                    });
                }

                // Use the first (newest) installation
                var installation = installations[0];
                progressCallback?.Invoke($"Indexing Unity {installation.Version}...");

                // Subscribe to worker events
                indexingWorker.OnProgress += (processed, total) =>
                {
                    progressCallback?.Invoke($"Progress: {processed}/{total} files");
                };

                // Start indexing (synchronously for this tool)
                indexingWorker.StartIndexing(installation);

                // Wait for completion
                while (indexingWorker.IsRunning)
                {
                    System.Threading.Thread.Sleep(100);
                }

                var summary = indexingWorker.GetSummary();

                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    success = true,
                    unityVersion = installation.Version,
                    totalFiles = summary.TotalFiles,
                    processedFiles = summary.ProcessedFiles,
                    successfullyIndexed = summary.SuccessfullyIndexed,
                    failed = summary.Failed,
                    durationSeconds = summary.Duration.TotalSeconds
                }, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
            });
        }

        /// <summary>
        /// Checks if an API is deprecated and returns migration suggestions (FR-005).
        /// </summary>
        /// <param name="apiName">API name (e.g., "Transform.OldMethod")</param>
        /// <returns>JSON object with deprecation info</returns>
        // [McpServerTool] // TODO: Add when integrating with ModelContextProtocol SDK
        public async Task<string> CheckApiDeprecation(string apiName)
        {
            return await Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(apiName))
                {
                    return "{}";
                }

                var results = indexer.QueryDocumentation(apiName);
                var entry = results.FirstOrDefault();

                if (entry == null)
                {
                    return System.Text.Json.JsonSerializer.Serialize(new
                    {
                        found = false
                    });
                }

                var currentVersion = versionManager.GetCurrentUnityVersion();
                var warning = deprecationDetector.GetDeprecationWarning(entry, currentVersion);

                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    found = true,
                    className = entry.ClassName,
                    methodName = entry.MethodName,
                    isDeprecated = entry.IsDeprecated,
                    replacementApi = entry.ReplacementApi,
                    warning = warning
                }, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
            });
        }

        public void Dispose()
        {
            indexer?.Dispose();
        }
    }
}
