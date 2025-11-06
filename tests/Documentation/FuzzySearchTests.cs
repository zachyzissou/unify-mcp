using NUnit.Framework;
using System.Linq;
using UnifyMcp.Tools.Documentation;

namespace UnifyMcp.Tests.Documentation
{
    /// <summary>
    /// Tests for fuzzy search using Fastenshtein Levenshtein distance.
    /// Tests typo tolerance with similarity scoring and threshold matching.
    /// Example: "Translte" → "Translate"
    /// </summary>
    [TestFixture]
    public class FuzzySearchTests
    {
        private FuzzyDocumentationSearch fuzzySearch;

        [SetUp]
        public void SetUp()
        {
            fuzzySearch = new FuzzyDocumentationSearch();
        }

        [Test]
        public void CalculateSimilarity_ExactMatch_ShouldReturn1()
        {
            // Arrange
            var source = "Translate";
            var target = "Translate";

            // Act
            var similarity = fuzzySearch.CalculateSimilarity(source, target);

            // Assert
            Assert.AreEqual(1.0, similarity, 0.001, "Exact match should have similarity of 1.0");
        }

        [Test]
        public void CalculateSimilarity_SingleTypo_ShouldReturnHighSimilarity()
        {
            // Arrange - Common typo: missing 'a'
            var source = "Translate";
            var target = "Translte";

            // Act
            var similarity = fuzzySearch.CalculateSimilarity(source, target);

            // Assert
            Assert.Greater(similarity, 0.7, "Single character typo should have >0.7 similarity");
        }

        [Test]
        public void CalculateSimilarity_TwoTypos_ShouldReturnModerateSimilarity()
        {
            // Arrange - Two typos
            var source = "GameObject";
            var target = "GameObjct";

            // Act
            var similarity = fuzzySearch.CalculateSimilarity(source, target);

            // Assert
            Assert.Greater(similarity, 0.6, "Two character typos should have >0.6 similarity");
            Assert.Less(similarity, 0.9, "Should be less similar than single typo");
        }

        [Test]
        public void CalculateSimilarity_CaseInsensitive_ShouldTreatSame()
        {
            // Arrange
            var source = "Translate";
            var target = "translate";

            // Act
            var similarity = fuzzySearch.CalculateSimilarity(source, target);

            // Assert
            Assert.AreEqual(1.0, similarity, 0.001, "Should be case-insensitive");
        }

        [Test]
        public void CalculateSimilarity_CompletelyDifferent_ShouldReturnLowSimilarity()
        {
            // Arrange
            var source = "Transform";
            var target = "GameObject";

            // Act
            var similarity = fuzzySearch.CalculateSimilarity(source, target);

            // Assert
            Assert.Less(similarity, 0.5, "Completely different words should have low similarity");
        }

        [Test]
        public void FindSimilarApis_WithTypo_ShouldSuggestCorrection()
        {
            // Arrange - Common typo scenarios
            var availableApis = new[]
            {
                "Transform.Translate",
                "Transform.Rotate",
                "GameObject.Find",
                "GameObject.FindGameObjectsWithTag"
            };
            var queryWithTypo = "Transform.Translte"; // Missing 'a'

            // Act
            var suggestions = fuzzySearch.FindSimilarApis(queryWithTypo, availableApis, threshold: 0.7);

            // Assert
            Assert.IsNotEmpty(suggestions, "Should find similar APIs");
            Assert.AreEqual("Transform.Translate", suggestions[0], "Should suggest correct spelling");
        }

        [Test]
        public void FindSimilarApis_MultipleMatches_ShouldOrderByRelevance()
        {
            // Arrange
            var availableApis = new[]
            {
                "Transform.Translate",
                "Transform.TransformPoint",
                "Transform.TransformDirection"
            };
            var query = "Translate";

            // Act
            var suggestions = fuzzySearch.FindSimilarApis(query, availableApis, threshold: 0.5);

            // Assert
            Assert.IsNotEmpty(suggestions);
            Assert.AreEqual("Transform.Translate", suggestions[0],
                "Exact match should be first");
        }

        [Test]
        public void FindSimilarApis_BelowThreshold_ShouldNotInclude()
        {
            // Arrange
            var availableApis = new[]
            {
                "Transform.Translate",
                "GameObject.Find"
            };
            var query = "Position"; // Very different from available APIs

            // Act
            var suggestions = fuzzySearch.FindSimilarApis(query, availableApis, threshold: 0.7);

            // Assert
            Assert.IsEmpty(suggestions, "Should not suggest APIs below similarity threshold");
        }

        [Test]
        public void FindSimilarApis_PartialMatch_ShouldWork()
        {
            // Arrange
            var availableApis = new[]
            {
                "Transform.position",
                "Transform.rotation",
                "GameObject.transform"
            };
            var query = "transform.pos"; // Partial query

            // Act
            var suggestions = fuzzySearch.FindSimilarApis(query, availableApis, threshold: 0.6);

            // Assert
            Assert.IsNotEmpty(suggestions);
            CollectionAssert.Contains(suggestions, "Transform.position",
                "Should match partial queries");
        }

        [Test]
        public void FindSimilarApis_CommonTypos_ShouldHandleGracefully()
        {
            // Arrange - Common typos developers make
            var availableApis = new[]
            {
                "GetComponent",
                "AddComponent",
                "RemoveComponent"
            };
            var commonTypos = new[]
            {
                "GetComponet",   // Swapped letters
                "GetCmoponent",  // Missing letter
                "GetComponentt"  // Extra letter
            };

            // Act & Assert
            foreach (var typo in commonTypos)
            {
                var suggestions = fuzzySearch.FindSimilarApis(typo, availableApis, threshold: 0.7);
                Assert.IsNotEmpty(suggestions, $"Should handle typo: {typo}");
                Assert.AreEqual("GetComponent", suggestions[0],
                    $"Should suggest GetComponent for typo: {typo}");
            }
        }

        [Test]
        public void FindSimilarApis_WithDefaultThreshold_ShouldUse07()
        {
            // Arrange
            var availableApis = new[] { "Transform.Translate" };
            var query = "Translte"; // 1 character difference from 9 chars = 0.889 similarity

            // Act - Use default threshold (should be 0.7)
            var suggestions = fuzzySearch.FindSimilarApis(query, availableApis);

            // Assert
            Assert.IsNotEmpty(suggestions, "Default threshold (0.7) should allow this match");
        }

        [Test]
        public void CalculateSimilarity_EmptyStrings_ShouldHandle()
        {
            // Arrange
            var source = "";
            var target = "Transform";

            // Act
            var similarity = fuzzySearch.CalculateSimilarity(source, target);

            // Assert
            Assert.AreEqual(0.0, similarity, "Empty string should have 0 similarity");
        }

        [Test]
        public void CalculateSimilarity_BothEmpty_ShouldReturn1()
        {
            // Arrange
            var source = "";
            var target = "";

            // Act
            var similarity = fuzzySearch.CalculateSimilarity(source, target);

            // Assert
            Assert.AreEqual(1.0, similarity, 0.001, "Two empty strings should be identical");
        }

        [Test]
        public void FindSimilarApis_NamespaceTypo_ShouldMatchCorrectly()
        {
            // Arrange
            var availableApis = new[]
            {
                "UnityEngine.Transform",
                "UnityEngine.GameObject",
                "UnityEditor.EditorWindow"
            };
            var query = "UnityEngie.Transform"; // Typo in namespace

            // Act
            var suggestions = fuzzySearch.FindSimilarApis(query, availableApis, threshold: 0.8);

            // Assert
            Assert.IsNotEmpty(suggestions);
            Assert.AreEqual("UnityEngine.Transform", suggestions[0],
                "Should handle namespace typos");
        }

        [Test]
        public void CalculateSimilarity_LongStrings_ShouldPerformWell()
        {
            // Arrange - Performance test with long API names
            var source = "GameObject.FindGameObjectsWithTag";
            var target = "GameObject.FindGameObjctsWithTag"; // Typo in middle

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var similarity = fuzzySearch.CalculateSimilarity(source, target);
            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 10,
                "Fuzzy search should be fast (<10ms for reasonable strings)");
            Assert.Greater(similarity, 0.7, "Should still detect similarity despite typo");
        }
    }
}
