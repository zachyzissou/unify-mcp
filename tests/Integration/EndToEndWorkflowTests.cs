using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnifyMcp.Core;
using UnifyMcp.Core.Context;
using UnifyMcp.Core.Context.Models;
using UnifyMcp.Tools.Documentation;

namespace UnifyMcp.Tests.Integration
{
    /// <summary>
    /// End-to-end integration tests for complete MCP workflows.
    /// Tests S063: Full request-response cycles with all components.
    /// </summary>
    [TestFixture]
    public class EndToEndWorkflowTests
    {
        private ContextWindowManager contextManager;
        private UnityDocumentationIndexer indexer;
        private DocumentationTools docTools;
        private string testDbPath;
        private string testCachePath;

        [SetUp]
        public void SetUp()
        {
            testDbPath = Path.Combine(Path.GetTempPath(), $"test_e2e_{Guid.NewGuid()}.db");
            testCachePath = Path.Combine(Path.GetTempPath(), $"test_cache_{Guid.NewGuid()}.db");

            indexer = new UnityDocumentationIndexer(testDbPath);
            indexer.CreateDatabase();

            var cacheManager = new ResponseCacheManager(testCachePath);
            contextManager = new ContextWindowManager(cacheManager: cacheManager);

            docTools = new DocumentationTools(indexer);
        }

        [TearDown]
        public void TearDown()
        {
            docTools?.Dispose();
            indexer?.Dispose();
            contextManager?.Dispose();

            CleanupTestFile(testDbPath);
            CleanupTestFile(testCachePath);
        }

        [Test]
        public async Task CompleteWorkflow_QueryAnalysis_ToolExecution_CacheHit()
        {
            // Arrange - Seed documentation
            SeedTestDocumentation();

            var query = "How do I use GameObject.SetActive?";

            // Act - Step 1: Analyze query
            var analysis = contextManager.AnalyzeQuery(query);

            Assert.IsNotNull(analysis);
            Assert.AreEqual(QueryIntent.Documentation, analysis.Intent);
            Assert.IsTrue(analysis.SuggestedTools.Count > 0);

            // Act - Step 2: Execute suggested tool
            var firstSuggestion = analysis.SuggestedTools[0];
            var parameters = new Dictionary<string, object>
            {
                { "query", "GameObject.SetActive" }
            };

            var result1 = await contextManager.ProcessToolRequestAsync(
                firstSuggestion.ToolName,
                parameters,
                async () => await docTools.QueryDocumentation("GameObject.SetActive")
            );

            Assert.IsNotNull(result1.Response);
            Assert.IsFalse(result1.WasCached); // First call should not be cached

            // Act - Step 3: Execute same request again (should hit cache)
            var result2 = await contextManager.ProcessToolRequestAsync(
                firstSuggestion.ToolName,
                parameters,
                async () => await docTools.QueryDocumentation("GameObject.SetActive")
            );

            // Assert
            Assert.AreEqual(result1.Response, result2.Response);
            Assert.IsTrue(result2.WasCached || result2.WasDeduplicated);
            Assert.Less(result2.Duration.TotalMilliseconds, result1.Duration.TotalMilliseconds * 2);
        }

        [Test]
        public async Task CompleteWorkflow_FuzzySearch_WithTypo()
        {
            // Arrange
            SeedTestDocumentation();

            var query = "GameObject.SetActiv"; // Typo

            // Act - Step 1: Analyze query
            var analysis = contextManager.AnalyzeQuery(query);

            // Act - Step 2: Execute fuzzy search
            var parameters = new Dictionary<string, object>
            {
                { "query", "GameObject.SetActiv" },
                { "threshold", 0.7 }
            };

            var result = await contextManager.ProcessToolRequestAsync(
                "SearchApiFuzzy",
                parameters,
                async () => await docTools.SearchApiFuzzy("GameObject.SetActiv", 0.7)
            );

            // Assert
            Assert.IsNotNull(result.Response);
            Assert.IsTrue(result.Response.Contains("GameObject.SetActive"));
        }

        [Test]
        public async Task CompleteWorkflow_MultipleTools_Sequential()
        {
            // Arrange
            SeedTestDocumentation();

            // Act - Execute multiple tools in sequence
            var params1 = new Dictionary<string, object> { { "query", "GameObject" } };
            var result1 = await contextManager.ProcessToolRequestAsync(
                "QueryDocumentation",
                params1,
                async () => await docTools.QueryDocumentation("GameObject")
            );

            var params2 = new Dictionary<string, object> { { "query", "Transform" } };
            var result2 = await contextManager.ProcessToolRequestAsync(
                "QueryDocumentation",
                params2,
                async () => await docTools.QueryDocumentation("Transform")
            );

            var params3 = new Dictionary<string, object> { { "apiName", "GameObject.SetActive" } };
            var result3 = await contextManager.ProcessToolRequestAsync(
                "CheckDeprecation",
                params3,
                async () => await docTools.CheckDeprecation("GameObject.SetActive")
            );

            // Assert
            Assert.IsNotNull(result1.Response);
            Assert.IsNotNull(result2.Response);
            Assert.IsNotNull(result3.Response);

            // Verify statistics
            var stats = await contextManager.GetStatisticsAsync();
            Assert.AreEqual(3, stats.TokenMetrics.RequestCount);
            Assert.Greater(stats.TokenMetrics.TotalTokens, 0);
        }

        [Test]
        public async Task CompleteWorkflow_WithOptimization_ReducesTokens()
        {
            // Arrange
            SeedTestDocumentation();

            var largeQuery = new string('x', 1000) + " GameObject"; // Artificially large query
            var parameters = new Dictionary<string, object> { { "query", largeQuery } };

            // Act - Execute with optimization enabled
            var options = new ContextOptimizationOptions
            {
                EnableSummarization = true,
                EnforceTokenBudget = true
            };

            var result = await contextManager.ProcessToolRequestAsync(
                "QueryDocumentation",
                parameters,
                async () =>
                {
                    // Simulate large response
                    var docs = await docTools.QueryDocumentation("GameObject");
                    return docs + new string('y', 5000); // Pad with extra content
                },
                options
            );

            // Assert
            Assert.IsNotNull(result.Response);
            Assert.IsTrue(result.OptimizationsApplied.Count > 0);
            Assert.Greater(result.TokensSaved, 0);
        }

        [Test]
        public async Task CompleteWorkflow_ContextWindowManagement_Statistics()
        {
            // Arrange
            SeedTestDocumentation();

            // Act - Execute various operations
            var ops = new[]
            {
                ("QueryDocumentation", new Dictionary<string, object> { { "query", "GameObject" } }),
                ("QueryDocumentation", new Dictionary<string, object> { { "query", "Transform" } }),
                ("SearchApiFuzzy", new Dictionary<string, object> { { "query", "SetActive" }, { "threshold", 0.7 } }),
                ("CheckDeprecation", new Dictionary<string, object> { { "apiName", "GameObject.SetActive" } }),
                ("QueryDocumentation", new Dictionary<string, object> { { "query", "GameObject" } }) // Duplicate
            };

            foreach (var (toolName, parameters) in ops)
            {
                await contextManager.ProcessToolRequestAsync(
                    toolName,
                    parameters,
                    async () =>
                    {
                        switch (toolName)
                        {
                            case "QueryDocumentation":
                                return await docTools.QueryDocumentation((string)parameters["query"]);
                            case "SearchApiFuzzy":
                                return await docTools.SearchApiFuzzy((string)parameters["query"], (double)parameters["threshold"]);
                            case "CheckDeprecation":
                                return await docTools.CheckDeprecation((string)parameters["apiName"]);
                            default:
                                return "{}";
                        }
                    }
                );
            }

            // Assert - Check comprehensive statistics
            var stats = await contextManager.GetStatisticsAsync();

            Assert.AreEqual(5, stats.TokenMetrics.RequestCount);
            Assert.Greater(stats.TokenMetrics.TotalTokens, 0);
            Assert.Greater(stats.DeduplicationStats.TotalRequests, 0);

            // Should have at least one cache hit from the duplicate GameObject query
            Assert.IsTrue(stats.DeduplicationStats.DeduplicatedRequests > 0 ||
                         stats.CacheStatistics.TotalHits > 0);
        }

        [Test]
        public async Task CompleteWorkflow_MaintenanceOperations()
        {
            // Arrange
            SeedTestDocumentation();

            var parameters = new Dictionary<string, object> { { "query", "GameObject" } };
            var shortCacheOptions = new ContextOptimizationOptions
            {
                CacheDuration = TimeSpan.FromMilliseconds(100)
            };

            // Act - Create cache entry
            await contextManager.ProcessToolRequestAsync(
                "QueryDocumentation",
                parameters,
                async () => await docTools.QueryDocumentation("GameObject"),
                shortCacheOptions
            );

            // Wait for expiration
            await Task.Delay(150);

            // Perform maintenance
            await contextManager.PerformMaintenanceAsync();

            // Assert - Statistics should reflect cleanup
            var stats = await contextManager.GetStatisticsAsync();
            Assert.AreEqual(0, stats.CacheStatistics.ExpiredEntries);
        }

        [Test]
        public async Task CompleteWorkflow_ToolFeedback_ImprovesSuggestions()
        {
            // Arrange
            var query = "How do I use GameObject API?";

            // Act - Record multiple positive feedbacks for QueryDocumentation
            for (int i = 0; i < 5; i++)
            {
                contextManager.RecordToolFeedback("QueryDocumentation", wasRelevant: true);
            }

            // Record negative feedback for SearchApiFuzzy
            for (int i = 0; i < 3; i++)
            {
                contextManager.RecordToolFeedback("SearchApiFuzzy", wasRelevant: false);
            }

            // Analyze query
            var analysis = contextManager.AnalyzeQuery(query);

            // Assert - QueryDocumentation should rank higher due to positive feedback
            var queryDocTool = analysis.SuggestedTools.Find(t => t.ToolName == "QueryDocumentation");
            var fuzzySearchTool = analysis.SuggestedTools.Find(t => t.ToolName == "SearchApiFuzzy");

            if (queryDocTool != null && fuzzySearchTool != null)
            {
                Assert.Greater(queryDocTool.ConfidenceScore, fuzzySearchTool.ConfidenceScore);
            }
        }

        [Test]
        public async Task CompleteWorkflow_OptimizationRecommendations()
        {
            // Arrange
            SeedTestDocumentation();

            var parameters = new Dictionary<string, object> { { "query", "GameObject" } };

            // Act - Execute many requests to trigger recommendations
            for (int i = 0; i < 15; i++)
            {
                await contextManager.ProcessToolRequestAsync(
                    "QueryDocumentation",
                    parameters,
                    async () =>
                    {
                        // Simulate large response
                        var docs = await docTools.QueryDocumentation("GameObject");
                        return docs + new string('x', 3000);
                    }
                );
            }

            // Generate recommendations
            var recommendations = contextManager.GenerateRecommendations();

            // Assert
            Assert.IsNotNull(recommendations);
            // Should recommend caching and/or summarization due to high usage
            Assert.IsTrue(recommendations.Exists(r =>
                r.Type == OptimizationType.Caching ||
                r.Type == OptimizationType.Summarization));
        }

        [Test]
        public async Task CompleteWorkflow_ResetAndRestart()
        {
            // Arrange
            SeedTestDocumentation();

            var parameters = new Dictionary<string, object> { { "query", "GameObject" } };

            // Act - Execute some operations
            await contextManager.ProcessToolRequestAsync(
                "QueryDocumentation",
                parameters,
                async () => await docTools.QueryDocumentation("GameObject")
            );

            var statsBefore = await contextManager.GetStatisticsAsync();
            Assert.Greater(statsBefore.TokenMetrics.RequestCount, 0);

            // Reset
            await contextManager.ResetAsync();

            // Execute again after reset
            await contextManager.ProcessToolRequestAsync(
                "QueryDocumentation",
                parameters,
                async () => await docTools.QueryDocumentation("GameObject")
            );

            var statsAfter = await contextManager.GetStatisticsAsync();

            // Assert
            Assert.AreEqual(1, statsAfter.TokenMetrics.RequestCount);
            Assert.AreEqual(0, statsAfter.CacheStatistics.TotalEntries); // Cache should be cleared
        }

        private void SeedTestDocumentation()
        {
            var testDocs = new[]
            {
                new DocumentationEntry
                {
                    ClassName = "GameObject",
                    MethodName = "SetActive",
                    ReturnType = "void",
                    Parameters = new[] { "bool value" },
                    Description = "Activates/Deactivates the GameObject, depending on the given true or false value.",
                    CodeExamples = new[] { "myGameObject.SetActive(true);" },
                    UnityVersion = "2021.3",
                    DocumentationUrl = "https://docs.unity3d.com/ScriptReference/GameObject.SetActive.html",
                    LastUpdated = DateTime.UtcNow,
                    IsDeprecated = false
                },
                new DocumentationEntry
                {
                    ClassName = "Transform",
                    MethodName = "position",
                    ReturnType = "Vector3",
                    Parameters = new string[0],
                    Description = "The position of the transform in world space.",
                    CodeExamples = new[] { "transform.position = new Vector3(0, 0, 0);" },
                    UnityVersion = "2021.3",
                    DocumentationUrl = "https://docs.unity3d.com/ScriptReference/Transform-position.html",
                    LastUpdated = DateTime.UtcNow,
                    IsDeprecated = false
                },
                new DocumentationEntry
                {
                    ClassName = "GameObject",
                    MethodName = "Find",
                    ReturnType = "GameObject",
                    Parameters = new[] { "string name" },
                    Description = "Finds a GameObject by name and returns it.",
                    CodeExamples = new[] { "GameObject myObject = GameObject.Find(\"MyObjectName\");" },
                    UnityVersion = "2021.3",
                    DocumentationUrl = "https://docs.unity3d.com/ScriptReference/GameObject.Find.html",
                    LastUpdated = DateTime.UtcNow,
                    IsDeprecated = false
                }
            };

            foreach (var doc in testDocs)
            {
                indexer.IndexDocument(doc);
            }
        }

        private void CleanupTestFile(string path)
        {
            if (File.Exists(path))
            {
                try { File.Delete(path); } catch { }
            }
        }
    }
}
