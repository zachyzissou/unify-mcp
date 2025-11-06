using NUnit.Framework;
using System.Collections.Generic;
using System.Text.Json;
using UnifyMcp.Core.Context;
using UnifyMcp.Core.Context.Models;

namespace UnifyMcp.Tests.Core.Context
{
    [TestFixture]
    public class ToolResultSummarizerTests
    {
        private ToolResultSummarizer summarizer;

        [SetUp]
        public void SetUp()
        {
            summarizer = new ToolResultSummarizer();
        }

        [Test]
        public void Summarize_JsonContent_TruncatesLongLists()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                items = Enumerable.Range(1, 20).Select(i => $"Item {i}").ToArray()
            });

            var options = new SummarizationOptions { MaxListItems = 5 };

            // Act
            var result = summarizer.Summarize(json, options);

            // Assert
            Assert.Less(result.SummarizedLength, result.OriginalLength);
            Assert.IsTrue(result.SummarizedContent.Contains("...and"));
            Assert.IsTrue(result.AppliedTechniques.Contains("list_truncation"));
        }

        [Test]
        public void Summarize_RemovesMetadata_WhenNotIncluded()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                data = "important data",
                timestamp = "2025-01-01T00:00:00Z",
                id = "12345",
                url = "https://example.com"
            });

            var options = new SummarizationOptions { IncludeMetadata = false };

            // Act
            var result = summarizer.Summarize(json, options);

            // Assert
            Assert.IsFalse(result.SummarizedContent.Contains("timestamp"));
            Assert.IsFalse(result.SummarizedContent.Contains("url"));
            Assert.IsTrue(result.SummarizedContent.Contains("data"));
            Assert.IsTrue(result.AppliedTechniques.Contains("metadata_removal"));
        }

        [Test]
        public void Summarize_PreservesCodeExamples()
        {
            // Arrange
            var codeExample = @"public class Example
{
    void Start()
    {
        Debug.Log(""Hello"");
    }
}";

            var json = JsonSerializer.Serialize(new
            {
                description = "A very long description that should be truncated because it exceeds the maximum length specified in the summarization options",
                codeExamples = new[] { codeExample }
            });

            var options = new SummarizationOptions
            {
                PreserveCodeExamples = true,
                MaxLength = 50
            };

            // Act
            var result = summarizer.Summarize(json, options);

            // Assert
            Assert.IsTrue(result.SummarizedContent.Contains("Debug.Log"));
            Assert.IsTrue(result.SummarizedContent.Contains("void Start"));
        }

        [Test]
        public void Summarize_LimitsDepth()
        {
            // Arrange
            var deeplyNested = new
            {
                level1 = new
                {
                    level2 = new
                    {
                        level3 = new
                        {
                            level4 = new
                            {
                                level5 = "too deep"
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(deeplyNested);
            var options = new SummarizationOptions { MaxDepth = 3 };

            // Act
            var result = summarizer.Summarize(json, options);

            // Assert
            Assert.IsTrue(result.SummarizedContent.Contains("[...truncated]"));
            Assert.IsTrue(result.AppliedTechniques.Contains("depth_limiting"));
        }

        [Test]
        public void Summarize_TruncatesLongText()
        {
            // Arrange
            var longText = new string('a', 1000);
            var json = JsonSerializer.Serialize(new { text = longText });
            var options = new SummarizationOptions { MaxLength = 100 };

            // Act
            var result = summarizer.Summarize(json, options);

            // Assert
            Assert.Less(result.SummarizedLength, result.OriginalLength);
            Assert.IsTrue(result.AppliedTechniques.Contains("text_truncation"));
        }

        [Test]
        public void Summarize_PlainText_ExtractsKeySentences()
        {
            // Arrange
            var text = "This is sentence one. This is sentence two. This is sentence three. This is sentence four. This is sentence five.";
            var options = new SummarizationOptions { MaxLength = 50 };

            // Act
            var result = summarizer.Summarize(text, options);

            // Assert
            Assert.Less(result.SummarizedLength, result.OriginalLength);
            Assert.IsTrue(result.SummarizedContent.EndsWith("..."));
        }

        [Test]
        public void Summarize_CalculatesCompressionRatio()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                items = Enumerable.Range(1, 100).ToArray()
            });

            var options = new SummarizationOptions { MaxListItems = 5 };

            // Act
            var result = summarizer.Summarize(json, options);

            // Assert
            Assert.Greater(result.OriginalLength, result.SummarizedLength);
            Assert.Less(result.CompressionRatio, 1.0);
            Assert.Greater(result.CompressionRatio, 0.0);
        }

        [Test]
        public void Summarize_EstimatesTokenSavings()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                data = new string('x', 1000)
            });

            var options = new SummarizationOptions { MaxLength = 100 };

            // Act
            var result = summarizer.Summarize(json, options);

            // Assert
            Assert.Greater(result.EstimatedTokenSavings, 0);
        }

        [Test]
        public void SummarizeMultiple_CombinesResults()
        {
            // Arrange
            var results = new Dictionary<string, string>
            {
                { "Tool1", JsonSerializer.Serialize(new { data = "result1" }) },
                { "Tool2", JsonSerializer.Serialize(new { data = "result2" }) }
            };

            // Act
            var result = summarizer.SummarizeMultiple(results);

            // Assert
            Assert.IsTrue(result.SummarizedContent.Contains("=== Tool1 ==="));
            Assert.IsTrue(result.SummarizedContent.Contains("=== Tool2 ==="));
            Assert.IsTrue(result.SummarizedContent.Contains("result1"));
            Assert.IsTrue(result.SummarizedContent.Contains("result2"));
        }

        [Test]
        public void EstimateTokenCount_ReturnsReasonableEstimate()
        {
            // Arrange
            var text = new string('a', 400); // ~100 tokens

            // Act
            var tokenCount = summarizer.EstimateTokenCount(text);

            // Assert
            Assert.AreEqual(100, tokenCount);
        }

        [Test]
        public void OptimizeOptionsForTokenBudget_ReturnsMinimal_WhenBudgetIsHigh()
        {
            // Arrange
            var targetTokens = 1000;
            var currentTokens = 1100;

            // Act
            var options = summarizer.OptimizeOptionsForTokenBudget(targetTokens, currentTokens);

            // Assert
            Assert.AreEqual(SummarizationMode.Minimal, options.Mode);
        }

        [Test]
        public void OptimizeOptionsForTokenBudget_ReturnsBalanced_WhenBudgetIsModerate()
        {
            // Arrange
            var targetTokens = 500;
            var currentTokens = 900;

            // Act
            var options = summarizer.OptimizeOptionsForTokenBudget(targetTokens, currentTokens);

            // Assert
            Assert.AreEqual(SummarizationMode.Balanced, options.Mode);
        }

        [Test]
        public void OptimizeOptionsForTokenBudget_ReturnsAggressive_WhenBudgetIsLow()
        {
            // Arrange
            var targetTokens = 200;
            var currentTokens = 1000;

            // Act
            var options = summarizer.OptimizeOptionsForTokenBudget(targetTokens, currentTokens);

            // Assert
            Assert.AreEqual(SummarizationMode.Aggressive, options.Mode);
            Assert.AreEqual(200, options.MaxLength);
            Assert.AreEqual(3, options.MaxListItems);
            Assert.IsFalse(options.IncludeMetadata);
        }

        [Test]
        public void Summarize_NullOrEmpty_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => summarizer.Summarize(null));
            Assert.Throws<ArgumentException>(() => summarizer.Summarize(""));
        }

        [Test]
        public void SummarizeMultiple_NullOrEmpty_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => summarizer.SummarizeMultiple(null));
            Assert.Throws<ArgumentException>(() => summarizer.SummarizeMultiple(new Dictionary<string, string>()));
        }
    }
}
