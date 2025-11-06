using System.Collections.Generic;

namespace UnifyMcp.Core.Context.Models
{
    /// <summary>
    /// Represents a suggested tool based on query analysis.
    /// </summary>
    public class ToolSuggestion
    {
        /// <summary>
        /// Name of the suggested tool.
        /// </summary>
        public string ToolName { get; set; }

        /// <summary>
        /// Confidence score (0.0 to 1.0) for this suggestion.
        /// </summary>
        public double ConfidenceScore { get; set; }

        /// <summary>
        /// Reason why this tool was suggested.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Suggested parameters for the tool invocation.
        /// </summary>
        public Dictionary<string, object> SuggestedParameters { get; set; }

        /// <summary>
        /// Keywords from the query that matched this tool.
        /// </summary>
        public List<string> MatchedKeywords { get; set; }

        public ToolSuggestion()
        {
            SuggestedParameters = new Dictionary<string, object>();
            MatchedKeywords = new List<string>();
        }
    }

    /// <summary>
    /// Represents the result of query analysis.
    /// </summary>
    public class QueryAnalysisResult
    {
        /// <summary>
        /// Original query text.
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// List of suggested tools ranked by confidence.
        /// </summary>
        public List<ToolSuggestion> SuggestedTools { get; set; }

        /// <summary>
        /// Extracted intent from the query.
        /// </summary>
        public QueryIntent Intent { get; set; }

        /// <summary>
        /// Extracted entities (API names, file paths, etc.).
        /// </summary>
        public Dictionary<string, List<string>> ExtractedEntities { get; set; }

        public QueryAnalysisResult()
        {
            SuggestedTools = new List<ToolSuggestion>();
            ExtractedEntities = new Dictionary<string, List<string>>();
        }
    }

    /// <summary>
    /// Categorization of query intent.
    /// </summary>
    public enum QueryIntent
    {
        /// <summary>
        /// Query is asking about Unity API documentation.
        /// </summary>
        Documentation,

        /// <summary>
        /// Query is asking about performance or profiling.
        /// </summary>
        Performance,

        /// <summary>
        /// Query is asking about build or deployment.
        /// </summary>
        Build,

        /// <summary>
        /// Query is asking about assets or resources.
        /// </summary>
        Assets,

        /// <summary>
        /// Query is asking about scene or hierarchy.
        /// </summary>
        Scene,

        /// <summary>
        /// Query is asking about packages or dependencies.
        /// </summary>
        Packages,

        /// <summary>
        /// Query intent is unclear or mixed.
        /// </summary>
        Unknown
    }
}
