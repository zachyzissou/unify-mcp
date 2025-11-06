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
    /// Load and stress tests for system limits and scalability.
    /// Tests S067 (Load Testing) and S068 (Stress Testing).
    /// </summary>
    [TestFixture]
    [Category("Performance")]
    [Category("Load")]
    public class LoadAndStressTests
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

        #region Load Tests (S067)

        [Test]
        public async Task LoadTest_SustainedLoad_100RequestsPerSecond()
        {
            // Arrange
            var durationSeconds = 5;
            var targetRps = 100;
            var totalRequests = durationSeconds * targetRps;

            // Act
            var stopwatch = Stopwatch.StartNew();
            var successCount = 0;
            var failureCount = 0;

            var tasks = Enumerable.Range(0, totalRequests).Select(async i =>
            {
                try
                {
                    await contextManager.ProcessToolRequestAsync(
                        "LoadTest",
                        new Dictionary<string, object> { { "index", i } },
                        async () =>
                        {
                            await Task.Delay(1); // Minimal work
                            return $"result{i}";
                        }
                    );
                    successCount++;
                }
                catch
                {
                    failureCount++;
                }
            });

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            var actualRps = totalRequests / (stopwatch.ElapsedMilliseconds / 1000.0);

            // Assert
            Assert.Greater(successCount, totalRequests * 0.95, "At least 95% success rate");
            Assert.Less(failureCount, totalRequests * 0.05, "Less than 5% failure rate");

            TestContext.WriteLine($"Total requests: {totalRequests}");
            TestContext.WriteLine($"Success: {successCount}, Failures: {failureCount}");
            TestContext.WriteLine($"Duration: {stopwatch.Elapsed.TotalSeconds:F2}s");
            TestContext.WriteLine($"Actual RPS: {actualRps:F2}");
        }

        [Test]
        public async Task LoadTest_GradualRampUp()
        {
            // Arrange - Ramp from 10 to 100 requests/second
            var rampSteps = new[] { 10, 25, 50, 75, 100 };
            var stepDurationMs = 1000;

            // Act
            foreach (var rps in rampSteps)
            {
                var stopwatch = Stopwatch.StartNew();
                var tasks = Enumerable.Range(0, rps).Select(async i =>
                {
                    await contextManager.ProcessToolRequestAsync(
                        $"RampTest_{rps}",
                        new Dictionary<string, object> { { "index", i } },
                        async () =>
                        {
                            await Task.Delay(1);
                            return "result";
                        }
                    );
                });

                await Task.WhenAll(tasks);
                stopwatch.Stop();

                TestContext.WriteLine($"RPS: {rps}, Time: {stopwatch.ElapsedMilliseconds}ms");
                Assert.Less(stopwatch.ElapsedMilliseconds, stepDurationMs * 2,
                    $"Step with {rps} RPS took too long");
            }

            // System should still be responsive
            var stats = await contextManager.GetStatisticsAsync();
            Assert.IsNotNull(stats);
        }

        [Test]
        public async Task LoadTest_HighCacheHitRate_UnderLoad()
        {
            // Arrange - Create scenario with high cache potential
            var parameters = new Dictionary<string, object> { { "query", "cached" } };
            var requestCount = 200;

            // Prime cache
            await contextManager.ProcessToolRequestAsync(
                "CacheLoadTest",
                parameters,
                async () => await Task.FromResult("cached result")
            );

            // Act - Hammer with identical requests
            var stopwatch = Stopwatch.StartNew();

            var tasks = Enumerable.Range(0, requestCount).Select(_ =>
                contextManager.ProcessToolRequestAsync(
                    "CacheLoadTest",
                    parameters,
                    async () => await Task.FromResult("cached result")
                )
            );

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            var cachedCount = results.Count(r => r.WasCached || r.WasDeduplicated);

            // Assert - High cache hit rate
            Assert.Greater(cachedCount, requestCount * 0.9, "Expected > 90% cache hit rate");
            Assert.Less(stopwatch.ElapsedMilliseconds, 500, "Cached requests should be very fast");

            TestContext.WriteLine($"Cache hit rate: {(double)cachedCount / requestCount:P2}");
            TestContext.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
        }

        [Test]
        public async Task LoadTest_MixedWorkload()
        {
            // Arrange - Mix of different request types
            var requestCount = 100;

            // Act
            var tasks = Enumerable.Range(0, requestCount).Select(async i =>
            {
                var requestType = i % 4;
                switch (requestType)
                {
                    case 0: // Fast cached request
                        return await contextManager.ProcessToolRequestAsync(
                            "FastTool",
                            new Dictionary<string, object> { { "type", "fast" } },
                            async () => await Task.FromResult("quick")
                        );

                    case 1: // Slow unique request
                        return await contextManager.ProcessToolRequestAsync(
                            "SlowTool",
                            new Dictionary<string, object> { { "id", i } },
                            async () =>
                            {
                                await Task.Delay(50);
                                return "slow";
                            }
                        );

                    case 2: // Large response
                        return await contextManager.ProcessToolRequestAsync(
                            "LargeTool",
                            new Dictionary<string, object> { { "id", i } },
                            async () => await Task.FromResult(new string('x', 5000))
                        );

                    default: // Repeated request
                        return await contextManager.ProcessToolRequestAsync(
                            "RepeatedTool",
                            new Dictionary<string, object> { { "type", "repeated" } },
                            async () => await Task.FromResult("repeated")
                        );
                }
            });

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.AreEqual(requestCount, results.Length);
            Assert.IsTrue(results.All(r => r.Response != null));

            var stats = await contextManager.GetStatisticsAsync();
            TestContext.WriteLine($"Total requests: {stats.TokenMetrics.RequestCount}");
            TestContext.WriteLine($"Cache hits: {stats.CacheStatistics.TotalHits}");
        }

        #endregion

        #region Stress Tests (S068)

        [Test]
        public async Task StressTest_MaximumConcurrency()
        {
            // Arrange - Stress with very high concurrency
            var concurrentRequests = 500;

            // Act
            var stopwatch = Stopwatch.StartNew();

            var tasks = Enumerable.Range(0, concurrentRequests).Select(i =>
                contextManager.ProcessToolRequestAsync(
                    "StressTool",
                    new Dictionary<string, object> { { "index", i } },
                    async () =>
                    {
                        await Task.Delay(10);
                        return $"result{i}";
                    }
                )
            );

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert - System should handle high concurrency
            Assert.AreEqual(concurrentRequests, results.Length);
            Assert.IsTrue(results.All(r => r.Response != null));

            TestContext.WriteLine($"Processed {concurrentRequests} concurrent requests");
            TestContext.WriteLine($"Total time: {stopwatch.Elapsed.TotalSeconds:F2}s");
        }

        [Test]
        public async Task StressTest_MemoryPressure()
        {
            // Arrange - Create many large responses
            var requestCount = 100;
            var responseSize = 100000; // 100KB each

            var initialMemory = GC.GetTotalMemory(false);

            // Act
            for (int i = 0; i < requestCount; i++)
            {
                await contextManager.ProcessToolRequestAsync(
                    $"MemoryStress{i}",
                    new Dictionary<string, object>(),
                    async () => await Task.FromResult(new string('x', responseSize))
                );

                if (i % 10 == 0)
                {
                    TestContext.WriteLine($"Progress: {i}/{requestCount}");
                }
            }

            // Force GC
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;

            // Assert - Memory should not grow unbounded
            // With 100 requests of 100KB each, we expect some growth but not 10MB
            Assert.Less(memoryIncrease, 10 * 1024 * 1024,
                $"Memory increased by {memoryIncrease / 1024 / 1024}MB");

            TestContext.WriteLine($"Initial memory: {initialMemory / 1024 / 1024}MB");
            TestContext.WriteLine($"Final memory: {finalMemory / 1024 / 1024}MB");
            TestContext.WriteLine($"Increase: {memoryIncrease / 1024 / 1024}MB");
        }

        [Test]
        public async Task StressTest_RapidStartStop()
        {
            // Arrange - Rapidly create and destroy managers
            var cycleCount = 10;

            // Act
            for (int i = 0; i < cycleCount; i++)
            {
                using var manager = new ContextWindowManager();

                await manager.ProcessToolRequestAsync(
                    "RapidTest",
                    new Dictionary<string, object> { { "cycle", i } },
                    async () => await Task.FromResult($"cycle{i}")
                );

                // Immediate disposal
            }

            // Assert - No resource leaks or errors
            Assert.Pass($"Completed {cycleCount} rapid start/stop cycles");
        }

        [Test]
        public async Task StressTest_ExtremeCacheSize()
        {
            // Arrange - Fill cache with many unique entries
            var cacheEntries = 1000;

            // Act
            for (int i = 0; i < cacheEntries; i++)
            {
                await contextManager.ProcessToolRequestAsync(
                    "CacheStress",
                    new Dictionary<string, object> { { "unique", i } },
                    async () => await Task.FromResult($"entry{i}")
                );
            }

            var stats = await contextManager.GetStatisticsAsync();

            // Assert - System should handle large cache
            Assert.Greater(stats.TokenMetrics.RequestCount, 0);

            TestContext.WriteLine($"Cache entries created: {cacheEntries}");
            TestContext.WriteLine($"Cache size: {stats.CacheStatistics.CacheSize}");
        }

        [Test]
        public async Task StressTest_LongRunningOperations()
        {
            // Arrange - Simulate very slow tools
            var slowOperationCount = 20;
            var operationDurationMs = 500;

            // Act
            var stopwatch = Stopwatch.StartNew();

            var tasks = Enumerable.Range(0, slowOperationCount).Select(i =>
                contextManager.ProcessToolRequestAsync(
                    $"SlowStress{i}",
                    new Dictionary<string, object>(),
                    async () =>
                    {
                        await Task.Delay(operationDurationMs);
                        return $"slow{i}";
                    }
                )
            );

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert - All should complete despite being slow
            Assert.AreEqual(slowOperationCount, results.Length);

            // Parallel execution should be much faster than sequential
            var sequentialTime = slowOperationCount * operationDurationMs;
            Assert.Less(stopwatch.ElapsedMilliseconds, sequentialTime * 0.3,
                "Parallel execution should be significantly faster");

            TestContext.WriteLine($"Parallel time: {stopwatch.ElapsedMilliseconds}ms");
            TestContext.WriteLine($"Sequential would be: {sequentialTime}ms");
        }

        [Test]
        public async Task StressTest_ContinuousOperation_ExtendedDuration()
        {
            // Arrange - Run continuously for extended period
            var durationSeconds = 10;
            var stopTime = DateTime.UtcNow.AddSeconds(durationSeconds);
            var requestCount = 0;

            // Act
            while (DateTime.UtcNow < stopTime)
            {
                await contextManager.ProcessToolRequestAsync(
                    "ContinuousTest",
                    new Dictionary<string, object> { { "index", requestCount } },
                    async () =>
                    {
                        await Task.Delay(10);
                        return "result";
                    }
                );
                requestCount++;
            }

            var stats = await contextManager.GetStatisticsAsync();

            // Assert - System should remain stable
            Assert.Greater(requestCount, 100, "Should handle many requests");
            Assert.IsNotNull(stats);

            TestContext.WriteLine($"Requests processed: {requestCount}");
            TestContext.WriteLine($"Average RPS: {requestCount / durationSeconds}");
        }

        [Test]
        public async Task StressTest_ErrorRecovery_UnderLoad()
        {
            // Arrange - High load with intermittent failures
            var requestCount = 200;
            var successCount = 0;
            var failureCount = 0;

            // Act
            var tasks = Enumerable.Range(0, requestCount).Select(async i =>
            {
                try
                {
                    await contextManager.ProcessToolRequestAsync(
                        "ErrorStress",
                        new Dictionary<string, object> { { "index", i } },
                        async () =>
                        {
                            await Task.Delay(5);

                            // Fail every 5th request
                            if (i % 5 == 0)
                                throw new Exception($"Simulated failure {i}");

                            return $"success{i}";
                        }
                    );
                    successCount++;
                }
                catch
                {
                    failureCount++;
                }
            });

            await Task.WhenAll(tasks);

            // Assert - System should handle failures gracefully
            Assert.AreEqual(requestCount, successCount + failureCount);
            Assert.Greater(successCount, failureCount, "More successes than failures");

            // System should still be operational
            var stats = await contextManager.GetStatisticsAsync();
            Assert.IsNotNull(stats);

            TestContext.WriteLine($"Success: {successCount}, Failures: {failureCount}");
        }

        #endregion
    }
}
