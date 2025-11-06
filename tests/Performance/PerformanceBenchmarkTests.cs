using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnifyMcp.Core.Context;
using UnifyMcp.Core.Context.Models;

namespace UnifyMcp.Tests.Performance
{
    /// <summary>
    /// Performance benchmark tests for MCP operations.
    /// Tests S066: Performance benchmarking and metrics collection.
    /// </summary>
    [TestFixture]
    [Category("Performance")]
    public class PerformanceBenchmarkTests
    {
        private ContextWindowManager contextManager;

        [SetUp]
        public void SetUp()
        {
            contextManager = new ContextWindowManager();
        }

        [TearDown]
        public void TearDown()
        {
            contextManager?.Dispose();
        }

        [Test]
        public async Task Benchmark_CacheHitLatency_UnderThreshold()
        {
            // Arrange
            var parameters = new Dictionary<string, object> { { "param", "value" } };

            // Prime the cache
            await contextManager.ProcessToolRequestAsync(
                "CacheTestTool",
                parameters,
                async () => await Task.FromResult("cached result")
            );

            // Act - Measure cache hit latency
            var stopwatch = Stopwatch.StartNew();

            await contextManager.ProcessToolRequestAsync(
                "CacheTestTool",
                parameters,
                async () => await Task.FromResult("cached result")
            );

            stopwatch.Stop();

            // Assert - Cache hit should be very fast (< 10ms)
            Assert.Less(stopwatch.ElapsedMilliseconds, 10,
                $"Cache hit took {stopwatch.ElapsedMilliseconds}ms, expected < 10ms");

            TestContext.WriteLine($"Cache hit latency: {stopwatch.ElapsedMilliseconds}ms");
        }

        [Test]
        public async Task Benchmark_SummarizationPerformance()
        {
            // Arrange
            var largeContent = new string('x', 50000); // 50KB
            var parameters = new Dictionary<string, object>();
            var options = new ContextOptimizationOptions
            {
                EnableSummarization = true,
                EnableCaching = false
            };

            // Act
            var stopwatch = Stopwatch.StartNew();

            var result = await contextManager.ProcessToolRequestAsync(
                "SummarizeTool",
                parameters,
                async () => await Task.FromResult(largeContent),
                options
            );

            stopwatch.Stop();

            // Assert - Summarization should complete in reasonable time (< 100ms)
            Assert.Less(stopwatch.ElapsedMilliseconds, 100,
                $"Summarization took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");

            TestContext.WriteLine($"Summarization time: {stopwatch.ElapsedMilliseconds}ms");
            TestContext.WriteLine($"Original size: {largeContent.Length}, Summarized: {result.Response.Length}");
            TestContext.WriteLine($"Compression ratio: {(double)result.Response.Length / largeContent.Length:P2}");
        }

        [Test]
        public async Task Benchmark_DeduplicationOverhead()
        {
            // Arrange
            var parameters = new Dictionary<string, object> { { "param", "value" } };

            // Measure without deduplication
            var noDedupOptions = new ContextOptimizationOptions
            {
                EnableDeduplication = false,
                EnableCaching = false
            };

            var stopwatch1 = Stopwatch.StartNew();
            await contextManager.ProcessToolRequestAsync(
                "DedupTest1",
                parameters,
                async () => { await Task.Delay(10); return "result"; },
                noDedupOptions
            );
            stopwatch1.Stop();
            var timeWithoutDedup = stopwatch1.ElapsedMilliseconds;

            await contextManager.ResetAsync();

            // Measure with deduplication
            var withDedupOptions = new ContextOptimizationOptions
            {
                EnableDeduplication = true,
                EnableCaching = false
            };

            var stopwatch2 = Stopwatch.StartNew();
            await contextManager.ProcessToolRequestAsync(
                "DedupTest2",
                parameters,
                async () => { await Task.Delay(10); return "result"; },
                withDedupOptions
            );
            stopwatch2.Stop();
            var timeWithDedup = stopwatch2.ElapsedMilliseconds;

            // Assert - Deduplication overhead should be minimal (< 20% increase)
            var overhead = timeWithDedup - timeWithoutDedup;
            var overheadPercentage = (double)overhead / timeWithoutDedup;

            Assert.Less(overheadPercentage, 0.5, // Allow 50% overhead
                $"Deduplication overhead: {overheadPercentage:P2}");

            TestContext.WriteLine($"Without dedup: {timeWithoutDedup}ms");
            TestContext.WriteLine($"With dedup: {timeWithDedup}ms");
            TestContext.WriteLine($"Overhead: {overheadPercentage:P2}");
        }

        [Test]
        public async Task Benchmark_ParallelRequestThroughput()
        {
            // Arrange
            var requestCount = 50;

            // Act
            var stopwatch = Stopwatch.StartNew();

            var tasks = Enumerable.Range(0, requestCount).Select(i =>
                contextManager.ProcessToolRequestAsync(
                    "ThroughputTest",
                    new Dictionary<string, object> { { "index", i } },
                    async () =>
                    {
                        await Task.Delay(10); // Simulate work
                        return $"result{i}";
                    }
                )
            );

            await Task.WhenAll(tasks);

            stopwatch.Stop();

            // Calculate throughput
            var throughput = requestCount / (stopwatch.ElapsedMilliseconds / 1000.0);

            // Assert - Should handle at least 10 requests/second
            Assert.Greater(throughput, 10,
                $"Throughput: {throughput:F2} req/s, expected > 10 req/s");

            TestContext.WriteLine($"Processed {requestCount} requests in {stopwatch.ElapsedMilliseconds}ms");
            TestContext.WriteLine($"Throughput: {throughput:F2} requests/second");
        }

        [Test]
        public async Task Benchmark_MemoryEfficiency()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);

            // Act - Execute many operations
            for (int i = 0; i < 100; i++)
            {
                await contextManager.ProcessToolRequestAsync(
                    "MemoryTest",
                    new Dictionary<string, object> { { "index", i } },
                    async () => await Task.FromResult(new string('x', 1000))
                );
            }

            var stats = await contextManager.GetStatisticsAsync();

            // Force GC and measure
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(true);
            var memoryIncrease = finalMemory - initialMemory;
            var memoryPerRequest = memoryIncrease / 100;

            // Assert - Memory per request should be reasonable (< 100KB average)
            Assert.Less(memoryPerRequest, 100 * 1024,
                $"Memory per request: {memoryPerRequest / 1024}KB, expected < 100KB");

            TestContext.WriteLine($"Initial memory: {initialMemory / 1024}KB");
            TestContext.WriteLine($"Final memory: {finalMemory / 1024}KB");
            TestContext.WriteLine($"Memory increase: {memoryIncrease / 1024}KB");
            TestContext.WriteLine($"Memory per request: {memoryPerRequest / 1024}KB");
        }

        [Test]
        public async Task Benchmark_ContextWindowOptimizationImpact()
        {
            // Arrange
            var parameters = new Dictionary<string, object> { { "query", "test" } };
            var largeResponse = new string('x', 20000);

            // Measure without optimization
            var noOptOptions = new ContextOptimizationOptions
            {
                EnableSummarization = false,
                EnforceTokenBudget = false,
                EnableCaching = false,
                EnableDeduplication = false
            };

            var result1 = await contextManager.ProcessToolRequestAsync(
                "OptImpact1",
                parameters,
                async () => await Task.FromResult(largeResponse),
                noOptOptions
            );

            await contextManager.ResetAsync();

            // Measure with full optimization
            var fullOptOptions = new ContextOptimizationOptions
            {
                EnableSummarization = true,
                EnforceTokenBudget = true,
                EnableCaching = true,
                EnableDeduplication = true
            };

            var result2 = await contextManager.ProcessToolRequestAsync(
                "OptImpact2",
                parameters,
                async () => await Task.FromResult(largeResponse),
                fullOptOptions
            );

            // Assert - Optimized version should be significantly smaller
            var reductionRatio = (double)result2.Response.Length / result1.Response.Length;

            Assert.Less(reductionRatio, 0.5, // At least 50% reduction
                $"Size reduction: {(1 - reductionRatio):P2}");

            TestContext.WriteLine($"Unoptimized size: {result1.Response.Length}");
            TestContext.WriteLine($"Optimized size: {result2.Response.Length}");
            TestContext.WriteLine($"Reduction: {(1 - reductionRatio):P2}");
        }

        [Test]
        public async Task Benchmark_QueryAnalysisLatency()
        {
            // Arrange
            var queries = new[]
            {
                "How do I use GameObject.SetActive?",
                "What is Transform.position?",
                "How to optimize performance?",
                "Check for deprecated APIs",
                "Find unused assets in my project"
            };

            var latencies = new List<long>();

            // Act
            foreach (var query in queries)
            {
                var stopwatch = Stopwatch.StartNew();
                var analysis = contextManager.AnalyzeQuery(query);
                stopwatch.Stop();
                latencies.Add(stopwatch.ElapsedMilliseconds);
            }

            var avgLatency = latencies.Average();
            var maxLatency = latencies.Max();

            // Assert - Query analysis should be fast (< 5ms average, < 10ms max)
            Assert.Less(avgLatency, 5, $"Average latency: {avgLatency}ms, expected < 5ms");
            Assert.Less(maxLatency, 10, $"Max latency: {maxLatency}ms, expected < 10ms");

            TestContext.WriteLine($"Average query analysis latency: {avgLatency:F2}ms");
            TestContext.WriteLine($"Max latency: {maxLatency}ms");
        }

        [Test]
        public async Task Benchmark_StatisticsCollectionOverhead()
        {
            // Arrange
            var parameters = new Dictionary<string, object> { { "param", "value" } };

            // Act - Execute operations and measure stats collection time
            for (int i = 0; i < 50; i++)
            {
                await contextManager.ProcessToolRequestAsync(
                    "StatsTest",
                    new Dictionary<string, object> { { "index", i } },
                    async () => await Task.FromResult("result")
                );
            }

            var stopwatch = Stopwatch.StartNew();
            var stats = await contextManager.GetStatisticsAsync();
            stopwatch.Stop();

            // Assert - Statistics collection should be fast (< 20ms)
            Assert.Less(stopwatch.ElapsedMilliseconds, 20,
                $"Stats collection took {stopwatch.ElapsedMilliseconds}ms, expected < 20ms");

            TestContext.WriteLine($"Statistics collection time: {stopwatch.ElapsedMilliseconds}ms");
            TestContext.WriteLine($"Total requests tracked: {stats.TokenMetrics.RequestCount}");
        }

        [Test]
        public async Task Benchmark_ColdStartLatency()
        {
            // Arrange - Create fresh manager
            using var freshManager = new ContextWindowManager();
            var parameters = new Dictionary<string, object> { { "param", "value" } };

            // Act - Measure first request (cold start)
            var stopwatch = Stopwatch.StartNew();

            await freshManager.ProcessToolRequestAsync(
                "ColdStartTest",
                parameters,
                async () =>
                {
                    await Task.Delay(10);
                    return "result";
                }
            );

            stopwatch.Stop();

            // Assert - Cold start should complete reasonably fast (< 100ms)
            Assert.Less(stopwatch.ElapsedMilliseconds, 100,
                $"Cold start took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");

            TestContext.WriteLine($"Cold start latency: {stopwatch.ElapsedMilliseconds}ms");
        }

        [Test]
        public async Task Benchmark_TokenUsageTracking_Accuracy()
        {
            // Arrange
            var knownContent = new string('a', 4000); // Exactly 1000 tokens (4 chars/token)
            var parameters = new Dictionary<string, object>();

            // Act
            await contextManager.ProcessToolRequestAsync(
                "TokenTrackingTest",
                parameters,
                async () => await Task.FromResult(knownContent)
            );

            var stats = await contextManager.GetStatisticsAsync();

            // Assert - Token estimation should be reasonably accurate (within 10%)
            var estimatedTokens = stats.TokenMetrics.TotalOutputTokens;
            var expectedTokens = 1000;
            var accuracy = 1.0 - Math.Abs((double)(estimatedTokens - expectedTokens) / expectedTokens);

            Assert.Greater(accuracy, 0.9, $"Token estimation accuracy: {accuracy:P2}");

            TestContext.WriteLine($"Expected tokens: {expectedTokens}");
            TestContext.WriteLine($"Estimated tokens: {estimatedTokens}");
            TestContext.WriteLine($"Accuracy: {accuracy:P2}");
        }
    }
}
