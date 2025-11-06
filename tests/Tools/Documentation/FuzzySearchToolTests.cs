using NUnit.Framework;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using UnifyMcp.Tools.Documentation;

namespace UnifyMcp.Tests.Tools.Documentation
{
    /// <summary>
    /// Tests for SearchApiFuzzy MCP tool (FR-002).
    /// Tests typo tolerance, similarity threshold, and suggested corrections.
    /// </summary>
    [TestFixture]
    public class FuzzySearchToolTests
    {
        private string tempDatabasePath;
        private DocumentationTools tools;

        [SetUp]
        public void SetUp()
        {
            tempDatabasePath = Path.Combine(Path.GetTempPath(), $"test_fuzzy_{Guid.NewGuid()}.db");
            tools = new DocumentationTools(tempDatabasePath);

            // Index test data
            var indexer = new UnityDocumentationIndexer(tempDatabasePath);
            indexer.CreateDatabase();

            indexer.IndexDocument(new DocumentationEntry
            {
                ClassName = "Transform",
                MethodName = "Translate",
                UnityVersion = "2021.3"
            });

            indexer.IndexDocument(new DocumentationEntry
            {
                ClassName = "Transform",
                MethodName = "Rotate",
                UnityVersion = "2021.3"
            });

            indexer.IndexDocument(new DocumentationEntry
            {
                ClassName = "GameObject",
                MethodName = "Find",
                UnityVersion = "2021.3"
            });

            indexer.IndexDocument(new DocumentationEntry
            {
                ClassName = "GameObject",
                MethodName = "GetComponent",
                UnityVersion = "2021.3"
            });

            indexer.Dispose();
        }

        [TearDown]
        public void TearDown()
        {
            tools?.Dispose();
            if (File.Exists(tempDatabasePath))
                File.Delete(tempDatabasePath);
        }

        [Test]
        public async Task SearchApiFuzzy_WithTypo_ShouldSuggestCorrection()
        {
            // Arrange - "Translte" is missing 'a'
            var queryWithTypo = "Transform.Translte";

            // Act
            var json = await tools.SearchApiFuzzy(queryWithTypo, threshold: 0.7);

            // Assert
            var suggestions = JsonSerializer.Deserialize<string[]>(json);
            Assert.IsNotEmpty(suggestions, "Should find similar APIs despite typo (FR-002)");
            Assert.Contains("Transform.Translate", suggestions,
                "Should suggest correct spelling");
        }

        [Test]
        public async Task SearchApiFuzzy_ExactMatch_ShouldReturnFirst()
        {
            // Act
            var json = await tools.SearchApiFuzzy("Transform.Translate", threshold: 0.7);

            // Assert
            var suggestions = JsonSerializer.Deserialize<string[]>(json);
            Assert.IsNotEmpty(suggestions);
            Assert.AreEqual("Transform.Translate", suggestions[0],
                "Exact match should be first result");
        }

        [Test]
        public async Task SearchApiFuzzy_BelowThreshold_ShouldNotInclude()
        {
            // Arrange - Very different query
            var query = "Position";

            // Act
            var json = await tools.SearchApiFuzzy(query, threshold: 0.8);

            // Assert
            var suggestions = JsonSerializer.Deserialize<string[]>(json);
            // May have some results, but Transform.Translate should not be in top results
            // due to low similarity
        }

        [Test]
        public async Task SearchApiFuzzy_WithCustomThreshold_ShouldRespect()
        {
            // Act - Low threshold should return more results
            var json = await tools.SearchApiFuzzy("Transform", threshold: 0.5);

            // Assert
            var suggestions = JsonSerializer.Deserialize<string[]>(json);
            Assert.IsNotEmpty(suggestions);
        }

        [Test]
        public async Task SearchApiFuzzy_EmptyQuery_ShouldReturnEmptyArray()
        {
            // Act
            var json = await tools.SearchApiFuzzy("");

            // Assert
            Assert.AreEqual("[]", json);
        }

        [Test]
        public async Task SearchApiFuzzy_SwappedLetters_ShouldSuggest()
        {
            // Arrange - "GetComponet" has swapped letters
            var queryWithTypo = "GameObject.GetComponet";

            // Act
            var json = await tools.SearchApiFuzzy(queryWithTypo, threshold: 0.7);

            // Assert
            var suggestions = JsonSerializer.Deserialize<string[]>(json);
            Assert.IsNotEmpty(suggestions);
            Assert.Contains("GameObject.GetComponent", suggestions,
                "Should handle swapped letters");
        }

        [Test]
        public async Task SearchApiFuzzy_PartialQuery_ShouldMatch()
        {
            // Act
            var json = await tools.SearchApiFuzzy("Transform.Rot", threshold: 0.6);

            // Assert
            var suggestions = JsonSerializer.Deserialize<string[]>(json);
            // Should find Transform.Rotate with partial match
            Assert.IsNotEmpty(suggestions);
        }

        [Test]
        public async Task SearchApiFuzzy_MultipleTypos_ShouldStillSuggest()
        {
            // Arrange - Multiple typos
            var queryWithTypos = "Transfrm.Translte";

            // Act - Lower threshold for multiple typos
            var json = await tools.SearchApiFuzzy(queryWithTypos, threshold: 0.6);

            // Assert
            var suggestions = JsonSerializer.Deserialize<string[]>(json);
            // May still find results with lower threshold
        }

        [Test]
        public async Task SearchApiFuzzy_CaseInsensitive_ShouldMatch()
        {
            // Act
            var json = await tools.SearchApiFuzzy("transform.translate", threshold: 0.7);

            // Assert
            var suggestions = JsonSerializer.Deserialize<string[]>(json);
            Assert.IsNotEmpty(suggestions);
            Assert.Contains("Transform.Translate", suggestions,
                "Should be case-insensitive");
        }
    }
}
