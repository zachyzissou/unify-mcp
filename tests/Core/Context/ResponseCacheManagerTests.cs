using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnifyMcp.Core.Context;

namespace UnifyMcp.Tests.Core.Context
{
    [TestFixture]
    public class ResponseCacheManagerTests
    {
        private ResponseCacheManager cacheManager;
        private string testDatabasePath;

        [SetUp]
        public void SetUp()
        {
            testDatabasePath = Path.Combine(Path.GetTempPath(), $"test_cache_{Guid.NewGuid()}.db");
            cacheManager = new ResponseCacheManager(testDatabasePath);
        }

        [TearDown]
        public void TearDown()
        {
            cacheManager?.Dispose();

            if (File.Exists(testDatabasePath))
            {
                try { File.Delete(testDatabasePath); } catch { }
            }
        }

        [Test]
        public async Task CacheResponseAsync_StoresResponse()
        {
            // Arrange
            var toolName = "TestTool";
            var requestHash = "hash123";
            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            var response = "test response";
            var cacheDuration = TimeSpan.FromMinutes(5);

            // Act
            await cacheManager.CacheResponseAsync(toolName, requestHash, parameters, response, cacheDuration);
            var retrieved = await cacheManager.GetCachedResponseAsync(toolName, requestHash);

            // Assert
            Assert.AreEqual(response, retrieved);
        }

        [Test]
        public async Task GetCachedResponseAsync_NonExistent_ReturnsNull()
        {
            // Act
            var result = await cacheManager.GetCachedResponseAsync("NonExistentTool", "nonexistenthash");

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetCachedResponseAsync_Expired_ReturnsNull()
        {
            // Arrange
            var toolName = "TestTool";
            var requestHash = "hash123";
            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            var response = "test response";
            var shortDuration = TimeSpan.FromMilliseconds(100);

            // Act
            await cacheManager.CacheResponseAsync(toolName, requestHash, parameters, response, shortDuration);
            await Task.Delay(150); // Wait for expiration
            var retrieved = await cacheManager.GetCachedResponseAsync(toolName, requestHash);

            // Assert
            Assert.IsNull(retrieved);
        }

        [Test]
        public async Task GetCachedResponseAsync_IncrementsHitCount()
        {
            // Arrange
            var toolName = "TestTool";
            var requestHash = "hash123";
            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            var response = "test response";
            var cacheDuration = TimeSpan.FromMinutes(5);

            // Act
            await cacheManager.CacheResponseAsync(toolName, requestHash, parameters, response, cacheDuration);
            await cacheManager.GetCachedResponseAsync(toolName, requestHash);
            await cacheManager.GetCachedResponseAsync(toolName, requestHash);
            await cacheManager.GetCachedResponseAsync(toolName, requestHash);

            var stats = await cacheManager.GetStatisticsAsync();

            // Assert
            Assert.AreEqual(3, stats.TotalHits);
        }

        [Test]
        public async Task InvalidateCacheAsync_ByTool_RemovesAllEntriesForTool()
        {
            // Arrange
            var toolName = "TestTool";
            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            var cacheDuration = TimeSpan.FromMinutes(5);

            await cacheManager.CacheResponseAsync(toolName, "hash1", parameters, "response1", cacheDuration);
            await cacheManager.CacheResponseAsync(toolName, "hash2", parameters, "response2", cacheDuration);
            await cacheManager.CacheResponseAsync("OtherTool", "hash3", parameters, "response3", cacheDuration);

            // Act
            await cacheManager.InvalidateCacheAsync(toolName);

            var retrieved1 = await cacheManager.GetCachedResponseAsync(toolName, "hash1");
            var retrieved2 = await cacheManager.GetCachedResponseAsync(toolName, "hash2");
            var retrieved3 = await cacheManager.GetCachedResponseAsync("OtherTool", "hash3");

            // Assert
            Assert.IsNull(retrieved1);
            Assert.IsNull(retrieved2);
            Assert.IsNotNull(retrieved3); // Other tool's cache should remain
        }

        [Test]
        public async Task InvalidateCacheAsync_Specific_RemovesOnlySpecifiedEntry()
        {
            // Arrange
            var toolName = "TestTool";
            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            var cacheDuration = TimeSpan.FromMinutes(5);

            await cacheManager.CacheResponseAsync(toolName, "hash1", parameters, "response1", cacheDuration);
            await cacheManager.CacheResponseAsync(toolName, "hash2", parameters, "response2", cacheDuration);

            // Act
            await cacheManager.InvalidateCacheAsync(toolName, "hash1");

            var retrieved1 = await cacheManager.GetCachedResponseAsync(toolName, "hash1");
            var retrieved2 = await cacheManager.GetCachedResponseAsync(toolName, "hash2");

            // Assert
            Assert.IsNull(retrieved1);
            Assert.IsNotNull(retrieved2);
        }

        [Test]
        public async Task CleanupExpiredEntriesAsync_RemovesExpiredOnly()
        {
            // Arrange
            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            var shortDuration = TimeSpan.FromMilliseconds(100);
            var longDuration = TimeSpan.FromMinutes(5);

            await cacheManager.CacheResponseAsync("Tool1", "hash1", parameters, "response1", shortDuration);
            await cacheManager.CacheResponseAsync("Tool2", "hash2", parameters, "response2", longDuration);

            // Act
            await Task.Delay(150); // Wait for first entry to expire
            var removedCount = await cacheManager.CleanupExpiredEntriesAsync();

            var stats = await cacheManager.GetStatisticsAsync();

            // Assert
            Assert.AreEqual(1, removedCount);
            Assert.AreEqual(1, stats.TotalEntries);
            Assert.AreEqual(0, stats.ExpiredEntries);
        }

        [Test]
        public async Task ClearAllAsync_RemovesAllEntries()
        {
            // Arrange
            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            var cacheDuration = TimeSpan.FromMinutes(5);

            await cacheManager.CacheResponseAsync("Tool1", "hash1", parameters, "response1", cacheDuration);
            await cacheManager.CacheResponseAsync("Tool2", "hash2", parameters, "response2", cacheDuration);
            await cacheManager.CacheResponseAsync("Tool3", "hash3", parameters, "response3", cacheDuration);

            // Act
            await cacheManager.ClearAllAsync();
            var stats = await cacheManager.GetStatisticsAsync();

            // Assert
            Assert.AreEqual(0, stats.TotalEntries);
        }

        [Test]
        public async Task GetStatisticsAsync_ReturnsAccurateStats()
        {
            // Arrange
            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            var cacheDuration = TimeSpan.FromMinutes(5);

            await cacheManager.CacheResponseAsync("Tool1", "hash1", parameters, "response1", cacheDuration);
            await cacheManager.CacheResponseAsync("Tool2", "hash2", parameters, "response2", cacheDuration);
            await cacheManager.CacheResponseAsync("Tool1", "hash3", parameters, "response3", cacheDuration);

            await cacheManager.GetCachedResponseAsync("Tool1", "hash1");
            await cacheManager.GetCachedResponseAsync("Tool1", "hash1");

            // Act
            var stats = await cacheManager.GetStatisticsAsync();

            // Assert
            Assert.AreEqual(3, stats.TotalEntries);
            Assert.AreEqual(2, stats.TotalHits);
            Assert.Greater(stats.CacheSizeBytes, 0);
            Assert.IsTrue(stats.ToolCacheCounts.ContainsKey("Tool1"));
            Assert.AreEqual(2, stats.ToolCacheCounts["Tool1"]);
        }

        [Test]
        public async Task GetTopEntriesAsync_ReturnsTopByHitCount()
        {
            // Arrange
            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            var cacheDuration = TimeSpan.FromMinutes(5);

            await cacheManager.CacheResponseAsync("Tool1", "hash1", parameters, "response1", cacheDuration);
            await cacheManager.CacheResponseAsync("Tool2", "hash2", parameters, "response2", cacheDuration);
            await cacheManager.CacheResponseAsync("Tool3", "hash3", parameters, "response3", cacheDuration);

            // Access hash1 the most
            await cacheManager.GetCachedResponseAsync("Tool1", "hash1");
            await cacheManager.GetCachedResponseAsync("Tool1", "hash1");
            await cacheManager.GetCachedResponseAsync("Tool1", "hash1");

            // Access hash2 less
            await cacheManager.GetCachedResponseAsync("Tool2", "hash2");

            // Act
            var topEntries = await cacheManager.GetTopEntriesAsync(2);

            // Assert
            Assert.AreEqual(2, topEntries.Count);
            Assert.AreEqual("hash1", topEntries[0].RequestHash);
            Assert.AreEqual(3, topEntries[0].HitCount);
            Assert.AreEqual("hash2", topEntries[1].RequestHash);
            Assert.AreEqual(1, topEntries[1].HitCount);
        }

        [Test]
        public async Task CacheResponseAsync_Replace_UpdatesExistingEntry()
        {
            // Arrange
            var toolName = "TestTool";
            var requestHash = "hash123";
            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            var cacheDuration = TimeSpan.FromMinutes(5);

            // Act
            await cacheManager.CacheResponseAsync(toolName, requestHash, parameters, "response1", cacheDuration);
            await cacheManager.CacheResponseAsync(toolName, requestHash, parameters, "response2", cacheDuration);

            var retrieved = await cacheManager.GetCachedResponseAsync(toolName, requestHash);
            var stats = await cacheManager.GetStatisticsAsync();

            // Assert
            Assert.AreEqual("response2", retrieved);
            Assert.AreEqual(1, stats.TotalEntries); // Should still be 1 entry, not 2
        }

        [Test]
        public async Task CacheStatistics_CacheSizeMB_CalculatesCorrectly()
        {
            // Arrange
            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            var cacheDuration = TimeSpan.FromMinutes(5);
            var largeResponse = new string('x', 1024 * 1024); // 1 MB

            // Act
            await cacheManager.CacheResponseAsync("Tool1", "hash1", parameters, largeResponse, cacheDuration);
            var stats = await cacheManager.GetStatisticsAsync();

            // Assert
            Assert.Greater(stats.CacheSizeMB, 0.9); // Should be approximately 1 MB
        }

        [Test]
        public async Task CacheEntry_IsExpired_ReflectsExpirationStatus()
        {
            // Arrange
            var parameters = new Dictionary<string, object> { { "param1", "value1" } };
            var shortDuration = TimeSpan.FromMilliseconds(100);

            await cacheManager.CacheResponseAsync("Tool1", "hash1", parameters, "response1", shortDuration);

            // Act
            var topEntries = await cacheManager.GetTopEntriesAsync(1);
            var entryBeforeExpiration = topEntries[0];

            await Task.Delay(150);

            topEntries = await cacheManager.GetTopEntriesAsync(1);
            var entryAfterExpiration = topEntries[0];

            // Assert
            Assert.IsFalse(entryBeforeExpiration.IsExpired);
            Assert.IsTrue(entryAfterExpiration.IsExpired);
        }
    }
}
