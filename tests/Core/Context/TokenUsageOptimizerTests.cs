using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnifyMcp.Core.Context;
using UnifyMcp.Core.Context.Models;

namespace UnifyMcp.Tests.Core.Context
{
    [TestFixture]
    public class TokenUsageOptimizerTests
    {
        private TokenUsageOptimizer optimizer;

        [SetUp]
        public void SetUp()
        {
            var config = new TokenBudgetConfig
            {
                MaxTokensPerRequest = 1000,
                MaxTokensPerResponse = 500,
                WarningThreshold = 0.8,
                AutoOptimize = true
            };
            optimizer = new TokenUsageOptimizer(config);
        }

        [Test]
        public void RecordUsage_TracksTokens()
        {
            // Arrange
            var input = new string('a', 400); // ~100 tokens
            var output = new string('b', 800); // ~200 tokens

            // Act
            optimizer.RecordUsage("TestTool", input, output);
            var metrics = optimizer.GetMetrics();

            // Assert
            Assert.AreEqual(100, metrics.TotalInputTokens);
            Assert.AreEqual(200, metrics.TotalOutputTokens);
            Assert.AreEqual(300, metrics.TotalTokens);
            Assert.AreEqual(1, metrics.RequestCount);
        }

        [Test]
        public void RecordUsage_TracksPerToolUsage()
        {
            // Arrange
            var input = new string('a', 400);
            var output = new string('b', 400);

            // Act
            optimizer.RecordUsage("Tool1", input, output);
            optimizer.RecordUsage("Tool1", input, output);
            optimizer.RecordUsage("Tool2", input, output);

            var metrics = optimizer.GetMetrics();

            // Assert
            Assert.AreEqual(2, metrics.ToolUsage["Tool1"].InvocationCount);
            Assert.AreEqual(1, metrics.ToolUsage["Tool2"].InvocationCount);
            Assert.AreEqual(200, metrics.ToolUsage["Tool1"].TotalTokens);
        }

        [Test]
        public void RecordSavings_TracksSavings()
        {
            // Act
            optimizer.RecordSavings("TestTool", 100);
            optimizer.RecordSavings("TestTool", 50);

            var metrics = optimizer.GetMetrics();

            // Assert
            Assert.AreEqual(150, metrics.TokensSaved);
        }

        [Test]
        public void GenerateRecommendations_RecommendsCaching_ForFrequentHighUsage()
        {
            // Arrange
            var input = new string('a', 400);
            var output = new string('b', 2000); // ~500 tokens

            for (int i = 0; i < 15; i++)
            {
                optimizer.RecordUsage("FrequentTool", input, output);
            }

            // Act
            var recommendations = optimizer.GenerateRecommendations();

            // Assert
            var cachingRec = recommendations.FirstOrDefault(r => r.Type == OptimizationType.Caching);
            Assert.IsNotNull(cachingRec);
            Assert.AreEqual("FrequentTool", cachingRec.Target);
            Assert.Greater(cachingRec.EstimatedSavings, 0);
        }

        [Test]
        public void GenerateRecommendations_RecommendsSummarization_ForLargeResponses()
        {
            // Arrange
            var input = new string('a', 400);
            var output = new string('b', 5000); // ~1250 tokens

            optimizer.RecordUsage("LargeResponseTool", input, output);

            // Act
            var recommendations = optimizer.GenerateRecommendations();

            // Assert
            var summarizationRec = recommendations.FirstOrDefault(r => r.Type == OptimizationType.Summarization);
            Assert.IsNotNull(summarizationRec);
            Assert.AreEqual("LargeResponseTool", summarizationRec.Target);
        }

        [Test]
        public void GenerateRecommendations_RecommendsDeduplication_ForMultipleInvocations()
        {
            // Arrange
            var input = new string('a', 400);
            var output = new string('b', 400);

            for (int i = 0; i < 10; i++)
            {
                optimizer.RecordUsage("RepeatedTool", input, output);
            }

            // Act
            var recommendations = optimizer.GenerateRecommendations();

            // Assert
            var deduplicationRec = recommendations.FirstOrDefault(r => r.Type == OptimizationType.Deduplication);
            Assert.IsNotNull(deduplicationRec);
            Assert.AreEqual("RepeatedTool", deduplicationRec.Target);
        }

        [Test]
        public void OptimizeContent_ReducesTokenCount()
        {
            // Arrange
            var content = new string('a', 4000); // ~1000 tokens
            var targetTokens = 500;

            // Act
            var (optimized, saved) = optimizer.OptimizeContent(content, targetTokens);
            var optimizedTokens = optimized.Length / 4; // Approximate

            // Assert
            Assert.Less(optimizedTokens, 1000);
            Assert.Greater(saved, 0);
        }

        [Test]
        public void OptimizeContent_NoChangeIfWithinBudget()
        {
            // Arrange
            var content = new string('a', 400); // ~100 tokens
            var targetTokens = 500;

            // Act
            var (optimized, saved) = optimizer.OptimizeContent(content, targetTokens);

            // Assert
            Assert.AreEqual(content, optimized);
            Assert.AreEqual(0, saved);
        }

        [Test]
        public void CheckAndOptimizeRequest_OptimizesIfExceedsBudget()
        {
            // Arrange
            var largeRequest = new string('a', 5000); // ~1250 tokens (exceeds 1000)

            // Act
            var (optimized, wasOptimized) = optimizer.CheckAndOptimizeRequest(largeRequest);

            // Assert
            Assert.IsTrue(wasOptimized);
            Assert.Less(optimized.Length, largeRequest.Length);
        }

        [Test]
        public void CheckAndOptimizeRequest_NoChangeIfWithinBudget()
        {
            // Arrange
            var smallRequest = new string('a', 400); // ~100 tokens

            // Act
            var (optimized, wasOptimized) = optimizer.CheckAndOptimizeRequest(smallRequest);

            // Assert
            Assert.IsFalse(wasOptimized);
            Assert.AreEqual(smallRequest, optimized);
        }

        [Test]
        public void CheckAndOptimizeResponse_OptimizesIfExceedsBudget()
        {
            // Arrange
            var largeResponse = new string('a', 3000); // ~750 tokens (exceeds 500)

            // Act
            var (optimized, wasOptimized) = optimizer.CheckAndOptimizeResponse(largeResponse);

            // Assert
            Assert.IsTrue(wasOptimized);
            Assert.Less(optimized.Length, largeResponse.Length);
        }

        [Test]
        public void ResetMetrics_ClearsAllTracking()
        {
            // Arrange
            optimizer.RecordUsage("Tool1", new string('a', 400), new string('b', 400));
            optimizer.RecordSavings("Tool1", 100);

            // Act
            optimizer.ResetMetrics();
            var metrics = optimizer.GetMetrics();

            // Assert
            Assert.AreEqual(0, metrics.TotalTokens);
            Assert.AreEqual(0, metrics.TokensSaved);
            Assert.AreEqual(0, metrics.RequestCount);
            Assert.AreEqual(0, metrics.ToolUsage.Count);
        }

        [Test]
        public void GetEfficiencyScore_ReturnsCorrectScore()
        {
            // Arrange
            optimizer.RecordUsage("Tool1", new string('a', 400), new string('b', 400));
            optimizer.RecordSavings("Tool1", 50); // 50 tokens saved out of 200 total

            // Act
            var score = optimizer.GetEfficiencyScore();

            // Assert
            Assert.Greater(score, 0.0);
            Assert.Less(score, 1.0);
            Assert.AreEqual(0.2, score, 0.01); // 50 / (200 + 50) = 0.2
        }

        [Test]
        public void GetEfficiencyScore_ReturnsOne_WhenNoUsage()
        {
            // Act
            var score = optimizer.GetEfficiencyScore();

            // Assert
            Assert.AreEqual(1.0, score);
        }

        [Test]
        public void OnBudgetWarning_FiresWhenApproachingLimit()
        {
            // Arrange
            var warningFired = false;
            optimizer.OnBudgetWarning += (msg) => warningFired = true;

            var largeInput = new string('a', 3400); // ~850 tokens (85% of 1000)
            var output = new string('b', 400);

            // Act
            optimizer.RecordUsage("TestTool", largeInput, output);

            // Assert
            Assert.IsTrue(warningFired);
        }

        [Test]
        public void OnBudgetExceeded_FiresWhenExceedingLimit()
        {
            // Arrange
            var exceededFired = false;
            optimizer.OnBudgetExceeded += (msg) => exceededFired = true;

            var tooLargeInput = new string('a', 4500); // ~1125 tokens (exceeds 1000)
            var output = new string('b', 400);

            // Act
            optimizer.RecordUsage("TestTool", tooLargeInput, output);

            // Assert
            Assert.IsTrue(exceededFired);
        }

        [Test]
        public void GetMetrics_AverageTokensPerRequest_CalculatesCorrectly()
        {
            // Arrange
            optimizer.RecordUsage("Tool1", new string('a', 400), new string('b', 400)); // 200 tokens
            optimizer.RecordUsage("Tool2", new string('a', 800), new string('b', 800)); // 400 tokens

            // Act
            var metrics = optimizer.GetMetrics();

            // Assert
            Assert.AreEqual(300, metrics.AverageTokensPerRequest); // (200 + 400) / 2
        }

        [Test]
        public void GetMetrics_GetTopTools_ReturnsHighestUsage()
        {
            // Arrange
            optimizer.RecordUsage("Tool1", new string('a', 400), new string('b', 400)); // 200 tokens
            optimizer.RecordUsage("Tool2", new string('a', 800), new string('b', 800)); // 400 tokens
            optimizer.RecordUsage("Tool3", new string('a', 1200), new string('b', 1200)); // 600 tokens

            // Act
            var metrics = optimizer.GetMetrics();
            var topTools = metrics.GetTopTools(2);

            // Assert
            Assert.AreEqual(2, topTools.Count);
            Assert.AreEqual("Tool3", topTools[0].Key);
            Assert.AreEqual("Tool2", topTools[1].Key);
        }

        [Test]
        public void OnRecommendationGenerated_FiresWhenRecommendationCreated()
        {
            // Arrange
            var recommendationsFired = new List<OptimizationRecommendation>();
            optimizer.OnRecommendationGenerated += (rec) => recommendationsFired.Add(rec);

            var input = new string('a', 400);
            var output = new string('b', 2000);

            for (int i = 0; i < 15; i++)
            {
                optimizer.RecordUsage("FrequentTool", input, output);
            }

            // Act
            optimizer.GenerateRecommendations();

            // Assert
            Assert.Greater(recommendationsFired.Count, 0);
        }
    }
}
