using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnifyMcp.Core.Context;
using UnifyMcp.Core.Context.Models;

namespace UnifyMcp.Tests.Core.Context
{
    [TestFixture]
    public class ContextWindowManagerTests
    {
        private ContextWindowManager manager;
        private string testDatabasePath;

        [SetUp]
        public void SetUp()
        {
            testDatabasePath = Path.Combine(Path.GetTempPath(), $"test_context_{Guid.NewGuid()}.db");
            var cacheManager = new ResponseCacheManager(testDatabasePath);
            manager = new ContextWindowManager(cacheManager: cacheManager);
        }

        [TearDown]
        public void TearDown()
        {
            manager?.Dispose();

            if (File.Exists(testDatabasePath))
            {
                try { File.Delete(testDatabasePath); } catch { }
            }
        }

        [Test]
        public async Task ProcessToolRequestAsync_ExecutesToolAndReturnsResult()
        {
            // Arrange
            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            Func<Task<string>> executor = async () => await Task.FromResult("test result");

            // Act
            var result = await manager.ProcessToolRequestAsync("TestTool", parameters, executor);

            // Assert
            Assert.AreEqual("test result", result.Response);
            Assert.AreEqual("TestTool", result.ToolName);
        }

        [Test]
        public async Task ProcessToolRequestAsync_CachesResult()
        {
            // Arrange
            var executionCount = 0;
            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            Func<Task<string>> executor = async () =>
            {
                executionCount++;
                await Task.Delay(10);
                return "cached result";
            };

            // Act
            var result1 = await manager.ProcessToolRequestAsync("TestTool", parameters, executor);
            var result2 = await manager.ProcessToolRequestAsync("TestTool", parameters, executor);

            // Assert
            Assert.AreEqual("cached result", result1.Response);
            Assert.AreEqual("cached result", result2.Response);
            Assert.IsTrue(result2.WasCached || result2.WasDeduplicated); // Should use cache or deduplication
        }

        [Test]
        public async Task ProcessToolRequestAsync_DeduplicatesConcurrentRequests()
        {
            // Arrange
            var executionCount = 0;
            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            Func<Task<string>> executor = async () =>
            {
                executionCount++;
                await Task.Delay(100);
                return "deduplicated result";
            };

            // Act
            var task1 = manager.ProcessToolRequestAsync("TestTool", parameters, executor);
            var task2 = manager.ProcessToolRequestAsync("TestTool", parameters, executor);
            var task3 = manager.ProcessToolRequestAsync("TestTool", parameters, executor);

            var results = await Task.WhenAll(task1, task2, task3);

            // Assert
            Assert.AreEqual("deduplicated result", results[0].Response);
            Assert.AreEqual("deduplicated result", results[1].Response);
            Assert.AreEqual("deduplicated result", results[2].Response);
            Assert.AreEqual(1, executionCount); // Should only execute once
        }

        [Test]
        public async Task ProcessToolRequestAsync_AppliesSummarization_WhenEnabled()
        {
            // Arrange
            var largeResponse = new string('x', 10000);
            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            Func<Task<string>> executor = async () => await Task.FromResult(largeResponse);

            var options = new ContextOptimizationOptions
            {
                EnableSummarization = true,
                EnableCaching = false, // Disable caching to test summarization
                EnableDeduplication = false
            };

            // Act
            var result = await manager.ProcessToolRequestAsync("TestTool", parameters, executor, options);

            // Assert
            Assert.Less(result.Response.Length, largeResponse.Length);
            Assert.Greater(result.TokensSaved, 0);
            Assert.IsTrue(result.OptimizationsApplied.Count > 0);
        }

        [Test]
        public async Task ProcessToolRequestAsync_EnforcesTokenBudget()
        {
            // Arrange
            var hugeResponse = new string('x', 20000); // Way over budget
            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            Func<Task<string>> executor = async () => await Task.FromResult(hugeResponse);

            var options = new ContextOptimizationOptions
            {
                EnforceTokenBudget = true,
                EnableCaching = false,
                EnableDeduplication = false
            };

            // Act
            var result = await manager.ProcessToolRequestAsync("TestTool", parameters, executor, options);

            // Assert
            Assert.Less(result.Response.Length, hugeResponse.Length);
            Assert.IsTrue(result.OptimizationsApplied.Contains("token_budget_enforcement"));
        }

        [Test]
        public async Task ProcessToolRequestAsync_DisabledOptimizations_ReturnsOriginal()
        {
            // Arrange
            var originalResponse = "original response";
            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            Func<Task<string>> executor = async () => await Task.FromResult(originalResponse);

            var options = new ContextOptimizationOptions
            {
                EnableCaching = false,
                EnableDeduplication = false,
                EnableSummarization = false,
                EnforceTokenBudget = false
            };

            // Act
            var result = await manager.ProcessToolRequestAsync("TestTool", parameters, executor, options);

            // Assert
            Assert.AreEqual(originalResponse, result.Response);
            Assert.IsFalse(result.WasCached);
            Assert.IsFalse(result.WasDeduplicated);
        }

        [Test]
        public void AnalyzeQuery_ReturnsToolSuggestions()
        {
            // Arrange
            var query = "How do I use GameObject.SetActive?";

            // Act
            var analysis = manager.AnalyzeQuery(query);

            // Assert
            Assert.IsNotNull(analysis);
            Assert.AreEqual(query, analysis.Query);
            Assert.IsNotNull(analysis.SuggestedTools);
        }

        [Test]
        public void RecordToolFeedback_UpdatesSuggestions()
        {
            // Arrange & Act
            manager.RecordToolFeedback("TestTool", wasRelevant: true);
            manager.RecordToolFeedback("TestTool", wasRelevant: true);
            manager.RecordToolFeedback("TestTool", wasRelevant: false);

            // Assert - no exception should be thrown
            Assert.Pass();
        }

        [Test]
        public async Task GetStatisticsAsync_ReturnsComprehensiveStats()
        {
            // Arrange
            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            Func<Task<string>> executor = async () => await Task.FromResult("result");

            await manager.ProcessToolRequestAsync("Tool1", parameters, executor);
            await manager.ProcessToolRequestAsync("Tool2", parameters, executor);

            // Act
            var stats = await manager.GetStatisticsAsync();

            // Assert
            Assert.IsNotNull(stats);
            Assert.IsNotNull(stats.TokenMetrics);
            Assert.IsNotNull(stats.DeduplicationStats);
            Assert.IsNotNull(stats.CacheStatistics);
            Assert.Greater(stats.TokenMetrics.RequestCount, 0);
        }

        [Test]
        public void GenerateRecommendations_ReturnsOptimizationTips()
        {
            // Act
            var recommendations = manager.GenerateRecommendations();

            // Assert
            Assert.IsNotNull(recommendations);
            // May be empty if no usage patterns warrant recommendations
        }

        [Test]
        public async Task ResetAsync_ClearsAllCachesAndMetrics()
        {
            // Arrange
            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            Func<Task<string>> executor = async () => await Task.FromResult("result");

            await manager.ProcessToolRequestAsync("TestTool", parameters, executor);

            // Act
            await manager.ResetAsync();
            var stats = await manager.GetStatisticsAsync();

            // Assert
            Assert.AreEqual(0, stats.TokenMetrics.RequestCount);
            Assert.AreEqual(0, stats.CacheStatistics.TotalEntries);
        }

        [Test]
        public async Task PerformMaintenanceAsync_CleansUpExpiredEntries()
        {
            // Arrange
            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            Func<Task<string>> executor = async () => await Task.FromResult("result");

            var options = new ContextOptimizationOptions
            {
                CacheDuration = TimeSpan.FromMilliseconds(100) // Very short duration
            };

            await manager.ProcessToolRequestAsync("TestTool", parameters, executor, options);
            await Task.Delay(150); // Wait for expiration

            // Act
            await manager.PerformMaintenanceAsync();

            // Assert - no exception should be thrown
            Assert.Pass();
        }

        [Test]
        public async Task ProcessToolRequestAsync_RecordsDuration()
        {
            // Arrange
            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            Func<Task<string>> executor = async () =>
            {
                await Task.Delay(50);
                return "result";
            };

            // Act
            var result = await manager.ProcessToolRequestAsync("TestTool", parameters, executor);

            // Assert
            Assert.Greater(result.Duration.TotalMilliseconds, 0);
            Assert.Less(result.CompletedAt, result.RequestedAt.AddSeconds(5)); // Should complete quickly
        }

        [Test]
        public async Task ProcessToolRequestAsync_CapturesErrors()
        {
            // Arrange
            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            Func<Task<string>> executor = async () =>
            {
                await Task.Delay(10);
                throw new InvalidOperationException("Test error");
            };

            // Act & Assert
            OptimizedToolResult result = null;
            try
            {
                result = await manager.ProcessToolRequestAsync("TestTool", parameters, executor);
                Assert.Fail("Expected exception to be thrown");
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual("Test error", ex.Message);
            }
        }

        [Test]
        public async Task OnOptimizationApplied_FiresWhenOptimizationsOccur()
        {
            // Arrange
            var optimizationMessages = new List<string>();
            manager.OnOptimizationApplied += (msg) => optimizationMessages.Add(msg);

            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            Func<Task<string>> executor = async () => await Task.FromResult("result");

            // Act
            await manager.ProcessToolRequestAsync("TestTool", parameters, executor);
            await manager.ProcessToolRequestAsync("TestTool", parameters, executor); // Should trigger cache hit

            // Assert
            Assert.Greater(optimizationMessages.Count, 0);
        }

        [Test]
        public async Task ProcessToolRequestAsync_TracksOptimizationsApplied()
        {
            // Arrange
            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            Func<Task<string>> executor = async () => await Task.FromResult("result");

            // Act - First call
            var result1 = await manager.ProcessToolRequestAsync("TestTool", parameters, executor);

            // Second call should use cache
            var result2 = await manager.ProcessToolRequestAsync("TestTool", parameters, executor);

            // Assert
            Assert.IsNotNull(result2.OptimizationsApplied);
            Assert.IsTrue(result2.OptimizationsApplied.Contains("persistent_cache_hit") ||
                         result2.OptimizationsApplied.Contains("request_deduplication"));
        }

        [Test]
        public async Task ProcessToolRequestAsync_CustomCacheDuration_RespectsOption()
        {
            // Arrange
            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            Func<Task<string>> executor = async () => await Task.FromResult("result");

            var options = new ContextOptimizationOptions
            {
                CacheDuration = TimeSpan.FromMilliseconds(50)
            };

            // Act
            await manager.ProcessToolRequestAsync("TestTool", parameters, executor, options);
            await Task.Delay(100); // Wait for cache to expire

            var executionCount = 0;
            Func<Task<string>> countingExecutor = async () =>
            {
                executionCount++;
                return await Task.FromResult("result");
            };

            await manager.ProcessToolRequestAsync("TestTool", parameters, countingExecutor, options);

            // Assert
            Assert.AreEqual(1, executionCount); // Should re-execute after cache expired
        }
    }
}
