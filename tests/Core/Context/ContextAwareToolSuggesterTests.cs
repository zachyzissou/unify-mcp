using NUnit.Framework;
using UnifyMcp.Core.Context;
using UnifyMcp.Core.Context.Models;

namespace UnifyMcp.Tests.Core.Context
{
    [TestFixture]
    public class ContextAwareToolSuggesterTests
    {
        private ContextAwareToolSuggester suggester;

        [SetUp]
        public void SetUp()
        {
            suggester = new ContextAwareToolSuggester();
        }

        [Test]
        public void AnalyzeQuery_DocumentationIntent_SuggestsDocumentationTools()
        {
            // Arrange
            var query = "How do I use the GameObject.SetActive method?";

            // Act
            var result = suggester.AnalyzeQuery(query);

            // Assert
            Assert.AreEqual(QueryIntent.Documentation, result.Intent);
            Assert.IsTrue(result.SuggestedTools.Count > 0);
            Assert.IsTrue(result.SuggestedTools.Any(t => t.ToolName == "QueryDocumentation"));
        }

        [Test]
        public void AnalyzeQuery_PerformanceIntent_SuggestsProfilerTools()
        {
            // Arrange
            var query = "Why is my game running slow? Need to check for performance bottlenecks.";

            // Act
            var result = suggester.AnalyzeQuery(query);

            // Assert
            Assert.AreEqual(QueryIntent.Performance, result.Intent);
            Assert.IsTrue(result.SuggestedTools.Any(t => t.ToolName == "CaptureProfilerSnapshot" || t.ToolName == "AnalyzeBottlenecks"));
        }

        [Test]
        public void AnalyzeQuery_ExtractsUnityApiReferences()
        {
            // Arrange
            var query = "What's the difference between Transform.position and Transform.localPosition?";

            // Act
            var result = suggester.AnalyzeQuery(query);

            // Assert
            Assert.IsTrue(result.ExtractedEntities.ContainsKey("apis"));
            Assert.IsTrue(result.ExtractedEntities["apis"].Contains("Transform.position"));
            Assert.IsTrue(result.ExtractedEntities["apis"].Contains("Transform.localPosition"));
        }

        [Test]
        public void AnalyzeQuery_ExtractsUnityVersions()
        {
            // Arrange
            var query = "Is this API available in Unity 2021.3?";

            // Act
            var result = suggester.AnalyzeQuery(query);

            // Assert
            Assert.IsTrue(result.ExtractedEntities.ContainsKey("versions"));
            Assert.IsTrue(result.ExtractedEntities["versions"].Contains("2021.3"));
        }

        [Test]
        public void AnalyzeQuery_SuggestsParametersFromEntities()
        {
            // Arrange
            var query = "Show me documentation for GameObject.SetActive";

            // Act
            var result = suggester.AnalyzeQuery(query);

            // Assert
            var docTool = result.SuggestedTools.FirstOrDefault(t => t.ToolName == "QueryDocumentation");
            Assert.IsNotNull(docTool);
            Assert.IsTrue(docTool.SuggestedParameters.ContainsKey("query"));
        }

        [Test]
        public void AnalyzeQuery_ConfidenceThreshold_FiltersLowConfidenceSuggestions()
        {
            // Arrange
            var query = "generic question";
            var highThreshold = 0.9;

            // Act
            var result = suggester.AnalyzeQuery(query, confidenceThreshold: highThreshold);

            // Assert
            Assert.IsTrue(result.SuggestedTools.All(t => t.ConfidenceScore >= highThreshold));
        }

        [Test]
        public void AnalyzeQuery_MaxSuggestions_LimitsResults()
        {
            // Arrange
            var query = "How do I use Unity API for performance profiling?";
            var maxSuggestions = 2;

            // Act
            var result = suggester.AnalyzeQuery(query, maxSuggestions: maxSuggestions);

            // Assert
            Assert.LessOrEqual(result.SuggestedTools.Count, maxSuggestions);
        }

        [Test]
        public void RecordToolInvocation_RelevantTool_IncreasesScore()
        {
            // Arrange
            var toolName = "QueryDocumentation";

            // Act
            suggester.RecordToolInvocation(toolName, wasRelevant: true);
            suggester.RecordToolInvocation(toolName, wasRelevant: true);
            var history = suggester.GetInvocationHistory();

            // Assert
            Assert.IsTrue(history.ContainsKey(toolName));
            Assert.Greater(history[toolName], 0.5); // Should be higher than initial score
        }

        [Test]
        public void RecordToolInvocation_IrrelevantTool_DecreasesScore()
        {
            // Arrange
            var toolName = "QueryDocumentation";

            // Act
            suggester.RecordToolInvocation(toolName, wasRelevant: false);
            suggester.RecordToolInvocation(toolName, wasRelevant: false);
            var history = suggester.GetInvocationHistory();

            // Assert
            Assert.IsTrue(history.ContainsKey(toolName));
            Assert.Less(history[toolName], 0.5); // Should be lower than initial score
        }

        [Test]
        public void AnalyzeQuery_BuildIntent_SuggestsBuildTools()
        {
            // Arrange
            var query = "How do I build for multiple platforms?";

            // Act
            var result = suggester.AnalyzeQuery(query);

            // Assert
            Assert.AreEqual(QueryIntent.Build, result.Intent);
            Assert.IsTrue(result.SuggestedTools.Any(t => t.ToolName.Contains("Build")));
        }

        [Test]
        public void AnalyzeQuery_AssetsIntent_SuggestsAssetTools()
        {
            // Arrange
            var query = "Find unused textures in my project";

            // Act
            var result = suggester.AnalyzeQuery(query);

            // Assert
            Assert.AreEqual(QueryIntent.Assets, result.Intent);
            Assert.IsTrue(result.SuggestedTools.Any(t => t.ToolName == "FindUnusedAssets"));
        }

        [Test]
        public void AnalyzeQuery_SceneIntent_SuggestsSceneTools()
        {
            // Arrange
            var query = "Check for missing references in my scene";

            // Act
            var result = suggester.AnalyzeQuery(query);

            // Assert
            Assert.AreEqual(QueryIntent.Scene, result.Intent);
            Assert.IsTrue(result.SuggestedTools.Any(t => t.ToolName == "FindMissingReferences"));
        }

        [Test]
        public void AnalyzeQuery_PackagesIntent_SuggestsPackageTools()
        {
            // Arrange
            var query = "Check package compatibility for Unity 2022.1";

            // Act
            var result = suggester.AnalyzeQuery(query);

            // Assert
            Assert.AreEqual(QueryIntent.Packages, result.Intent);
            Assert.IsTrue(result.SuggestedTools.Any(t => t.ToolName.Contains("Package")));
        }

        [Test]
        public void AnalyzeQuery_NullOrEmpty_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => suggester.AnalyzeQuery(null));
            Assert.Throws<ArgumentException>(() => suggester.AnalyzeQuery(""));
            Assert.Throws<ArgumentException>(() => suggester.AnalyzeQuery("   "));
        }

        [Test]
        public void AnalyzeQuery_MultipleKeywords_BoostsConfidence()
        {
            // Arrange
            var query = "Show me code examples for deprecated Unity APIs";

            // Act
            var result = suggester.AnalyzeQuery(query);

            // Assert
            var suggestions = result.SuggestedTools;
            Assert.IsTrue(suggestions.Count > 0);

            // Tools matching multiple keywords should have higher confidence
            var multiMatchTool = suggestions.FirstOrDefault(s => s.MatchedKeywords.Count > 1);
            if (multiMatchTool != null)
            {
                var singleMatchTool = suggestions.FirstOrDefault(s => s.MatchedKeywords.Count == 1);
                if (singleMatchTool != null)
                {
                    Assert.Greater(multiMatchTool.ConfidenceScore, singleMatchTool.ConfidenceScore);
                }
            }
        }
    }
}
