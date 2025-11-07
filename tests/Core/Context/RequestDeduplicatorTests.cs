using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnifyMcp.Core.Context;
using UnifyMcp.Core.Context.Models;

namespace UnifyMcp.Tests.Core.Context
{
    [TestFixture]
    public class RequestDeduplicatorTests
    {
        private RequestDeduplicator deduplicator;

        [SetUp]
        public void SetUp()
        {
            deduplicator = new RequestDeduplicator(
                cacheDuration: TimeSpan.FromSeconds(10),
                maxCacheSize: 100
            );
        }

        [TearDown]
        public void TearDown()
        {
            deduplicator?.Dispose();
        }

        [Test]
        public async Task ProcessRequestAsync_FirstCall_ExecutesFunction()
        {
            // Arrange
            var executionCount = 0;
            Func<Task<string>> executor = async () =>
            {
                executionCount++;
                await Task.Delay(10);
                return "result";
            };

            var parameters = new Dictionary<string, object> { { "param1", "value1" } };

            // Act
            var result = await deduplicator.ProcessRequestAsync("TestTool", parameters, executor);

            // Assert
            Assert.AreEqual("result", result);
            Assert.AreEqual(1, executionCount);
        }

        [Test]
        public async Task ProcessRequestAsync_DuplicateCall_UsesCachedResult()
        {
            // Arrange
            var executionCount = 0;
            Func<Task<string>> executor = async () =>
            {
                executionCount++;
                await Task.Delay(10);
                return "result";
            };

            var parameters = new Dictionary<string, object> { { "param1", "value1" } };

            // Act
            var result1 = await deduplicator.ProcessRequestAsync("TestTool", parameters, executor);
            var result2 = await deduplicator.ProcessRequestAsync("TestTool", parameters, executor);

            // Assert
            Assert.AreEqual("result", result1);
            Assert.AreEqual("result", result2);
            Assert.AreEqual(1, executionCount); // Should only execute once
        }

        [Test]
        public async Task ProcessRequestAsync_DifferentParameters_ExecutesBothTimes()
        {
            // Arrange
            var executionCount = 0;
            Func<Task<string>> executor = async () =>
            {
                executionCount++;
                await Task.Delay(10);
                return $"result{executionCount}";
            };

            var parameters1 = new Dictionary<string, object> { { "param1", "value1" } };
            var parameters2 = new Dictionary<string, object> { { "param1", "value2" } };

            // Act
            var result1 = await deduplicator.ProcessRequestAsync("TestTool", parameters1, executor);
            var result2 = await deduplicator.ProcessRequestAsync("TestTool", parameters2, executor);

            // Assert
            Assert.AreEqual("result1", result1);
            Assert.AreEqual("result2", result2);
            Assert.AreEqual(2, executionCount); // Should execute both times
        }

        [Test]
        public async Task ProcessRequestAsync_ConcurrentDuplicates_ExecutesOnce()
        {
            // Arrange
            var executionCount = 0;
            Func<Task<string>> executor = async () =>
            {
                executionCount++;
                await Task.Delay(100); // Longer delay to ensure concurrent requests
                return "result";
            };

            var parameters = new Dictionary<string, object> { { "param1", "value1" } };

            // Act
            var task1 = deduplicator.ProcessRequestAsync("TestTool", parameters, executor);
            var task2 = deduplicator.ProcessRequestAsync("TestTool", parameters, executor);
            var task3 = deduplicator.ProcessRequestAsync("TestTool", parameters, executor);

            var results = await Task.WhenAll(task1, task2, task3);

            // Assert
            Assert.AreEqual("result", results[0]);
            Assert.AreEqual("result", results[1]);
            Assert.AreEqual("result", results[2]);
            Assert.AreEqual(1, executionCount); // Should only execute once despite concurrent requests
        }

        [Test]
        public async Task ProcessRequestAsync_ExpiredCache_ReExecutes()
        {
            // Arrange
            var shortCacheDeduplicator = new RequestDeduplicator(
                cacheDuration: TimeSpan.FromMilliseconds(100)
            );

            var executionCount = 0;
            Func<Task<string>> executor = async () =>
            {
                executionCount++;
                await Task.Delay(10);
                return $"result{executionCount}";
            };

            var parameters = new Dictionary<string, object> { { "param1", "value1" } };

            // Act
            var result1 = await shortCacheDeduplicator.ProcessRequestAsync("TestTool", parameters, executor);
            await Task.Delay(150); // Wait for cache to expire
            var result2 = await shortCacheDeduplicator.ProcessRequestAsync("TestTool", parameters, executor);

            // Assert
            Assert.AreEqual("result1", result1);
            Assert.AreEqual("result2", result2);
            Assert.AreEqual(2, executionCount); // Should execute twice due to expiration

            shortCacheDeduplicator.Dispose();
        }

        [Test]
        public async Task InvalidateCache_RemovesCachedResponse()
        {
            // Arrange
            var executionCount = 0;
            Func<Task<string>> executor = async () =>
            {
                executionCount++;
                await Task.Delay(10);
                return $"result{executionCount}";
            };

            var parameters = new Dictionary<string, object> { { "param1", "value1" } };

            // Act
            await deduplicator.ProcessRequestAsync("TestTool", parameters, executor);
            deduplicator.InvalidateCache("TestTool");
            await deduplicator.ProcessRequestAsync("TestTool", parameters, executor);

            // Assert
            Assert.AreEqual(2, executionCount); // Should execute twice after invalidation
        }

        [Test]
        public async Task InvalidateCache_SpecificRequest_RemovesOnlyThatCache()
        {
            // Arrange
            var executionCount = 0;
            Func<Task<string>> executor = async () =>
            {
                executionCount++;
                await Task.Delay(10);
                return $"result{executionCount}";
            };

            var parameters1 = new Dictionary<string, object> { { "param1", "value1" } };
            var parameters2 = new Dictionary<string, object> { { "param1", "value2" } };

            // Act
            await deduplicator.ProcessRequestAsync("TestTool", parameters1, executor);
            await deduplicator.ProcessRequestAsync("TestTool", parameters2, executor);

            deduplicator.InvalidateCache("TestTool", parameters1);

            await deduplicator.ProcessRequestAsync("TestTool", parameters1, executor);
            await deduplicator.ProcessRequestAsync("TestTool", parameters2, executor);

            // Assert
            Assert.AreEqual(3, executionCount); // parameters1 re-executed, parameters2 cached
        }

        [Test]
        public void ClearCache_RemovesAllEntries()
        {
            // Arrange & Act
            deduplicator.ClearCache();
            var stats = deduplicator.GetStats();

            // Assert
            Assert.AreEqual(0, stats.CacheSize);
        }

        [Test]
        public async Task GetStats_ReturnsAccurateStatistics()
        {
            // Arrange
            Func<Task<string>> executor = async () =>
            {
                await Task.Delay(10);
                return "result";
            };

            var parameters = new Dictionary<string, object> { { "param1", "value1" } };

            // Act
            await deduplicator.ProcessRequestAsync("TestTool", parameters, executor);
            await deduplicator.ProcessRequestAsync("TestTool", parameters, executor);
            await deduplicator.ProcessRequestAsync("TestTool", parameters, executor);

            var stats = deduplicator.GetStats();

            // Assert
            Assert.AreEqual(3, stats.TotalRequests);
            Assert.AreEqual(2, stats.DeduplicatedRequests);
            Assert.AreEqual(1, stats.UniqueRequests);
            Assert.AreEqual(0.666, stats.DeduplicationRate, 0.01);
            Assert.AreEqual(1, stats.CacheSize);
        }

        [Test]
        public async Task GetCachedResponse_ReturnsCorrectCacheEntry()
        {
            // Arrange
            Func<Task<string>> executor = async () =>
            {
                await Task.Delay(10);
                return "result";
            };

            var parameters = new Dictionary<string, object> { { "param1", "value1" } };

            // Act
            await deduplicator.ProcessRequestAsync("TestTool", parameters, executor);
            var cached = deduplicator.GetCachedResponse("TestTool", parameters);

            // Assert
            Assert.IsNotNull(cached);
            Assert.AreEqual("result", cached.Response);
            Assert.AreEqual("TestTool", cached.RequestKey.ToolName);
        }

        [Test]
        public async Task GetCachedResponsesForTool_ReturnsAllForTool()
        {
            // Arrange
            Func<Task<string>> executor = async () =>
            {
                await Task.Delay(10);
                return "result";
            };

            var parameters1 = new Dictionary<string, object> { { "param1", "value1" } };
            var parameters2 = new Dictionary<string, object> { { "param1", "value2" } };

            // Act
            await deduplicator.ProcessRequestAsync("TestTool", parameters1, executor);
            await deduplicator.ProcessRequestAsync("TestTool", parameters2, executor);
            await deduplicator.ProcessRequestAsync("OtherTool", parameters1, executor);

            var cachedResponses = deduplicator.GetCachedResponsesForTool("TestTool");

            // Assert
            Assert.AreEqual(2, cachedResponses.Count);
            Assert.IsTrue(cachedResponses.All(r => r.RequestKey.ToolName == "TestTool"));
        }

        [Test]
        public async Task ProcessRequestAsync_HitCount_IncrementsOnCacheHit()
        {
            // Arrange
            Func<Task<string>> executor = async () =>
            {
                await Task.Delay(10);
                return "result";
            };

            var parameters = new Dictionary<string, object> { { "param1", "value1" } };

            // Act
            await deduplicator.ProcessRequestAsync("TestTool", parameters, executor);
            await deduplicator.ProcessRequestAsync("TestTool", parameters, executor);
            await deduplicator.ProcessRequestAsync("TestTool", parameters, executor);

            var cached = deduplicator.GetCachedResponse("TestTool", parameters);

            // Assert
            Assert.AreEqual(2, cached.HitCount); // 2 cache hits after initial execution
        }

        [Test]
        public void ProcessRequestAsync_NullToolName_ThrowsArgumentException()
        {
            // Arrange
            Func<Task<string>> executor = async () => await Task.FromResult("result");
            var parameters = new Dictionary<string, object>();

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await deduplicator.ProcessRequestAsync(null, parameters, executor));
        }

        [Test]
        public void ProcessRequestAsync_NullExecutor_ThrowsArgumentNullException()
        {
            // Arrange
            var parameters = new Dictionary<string, object>();

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await deduplicator.ProcessRequestAsync("TestTool", parameters, null));
        }

        [Test]
        public async Task ProcessRequestAsync_MaxCacheSize_EvictsOldEntries()
        {
            // Arrange
            var smallCacheDeduplicator = new RequestDeduplicator(maxCacheSize: 10);

            Func<Task<string>> executor = async () =>
            {
                await Task.Delay(10);
                return "result";
            };

            // Act - Add more than max cache size
            for (int i = 0; i < 15; i++)
            {
                var parameters = new Dictionary<string, object> { { "param", i } };
                await smallCacheDeduplicator.ProcessRequestAsync("TestTool", parameters, executor);
            }

            var stats = smallCacheDeduplicator.GetStats();

            // Assert
            Assert.LessOrEqual(stats.CacheSize, 10);

            smallCacheDeduplicator.Dispose();
        }

        [Test]
        public async Task RequestDeduplicator_OldSemaphores_GetCleanedUp()
        {
            // Arrange
            var deduplicator = new RequestDeduplicator(
                cacheDuration: TimeSpan.FromMilliseconds(50),
                semaphoreCleanupInterval: TimeSpan.FromMilliseconds(100)
            );

            var parameters = new Dictionary<string, object> { { "param", "value" } };

            // Create a request that will add a semaphore
            await deduplicator.ProcessRequestAsync(
                "TestTool",
                parameters,
                async () => await Task.FromResult("result")
            );

            var initialSemaphoreCount = deduplicator.GetSemaphoreCount();
            Assert.AreEqual(1, initialSemaphoreCount);

            // Act - Wait for cache to expire and cleanup to run
            await Task.Delay(200);

            // Assert - Semaphore should be cleaned up
            var finalSemaphoreCount = deduplicator.GetSemaphoreCount();
            Assert.AreEqual(0, finalSemaphoreCount);

            deduplicator.Dispose();
        }
    }
}
