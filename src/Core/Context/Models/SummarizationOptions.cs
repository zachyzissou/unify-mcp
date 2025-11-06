namespace UnifyMcp.Core.Context.Models
{
    /// <summary>
    /// Options for controlling result summarization.
    /// </summary>
    public class SummarizationOptions
    {
        /// <summary>
        /// Maximum length of summarized text in characters.
        /// </summary>
        public int MaxLength { get; set; } = 500;

        /// <summary>
        /// Maximum number of items to include in lists.
        /// </summary>
        public int MaxListItems { get; set; } = 5;

        /// <summary>
        /// Maximum depth for nested object summarization.
        /// </summary>
        public int MaxDepth { get; set; } = 3;

        /// <summary>
        /// Whether to preserve code examples in full.
        /// </summary>
        public bool PreserveCodeExamples { get; set; } = true;

        /// <summary>
        /// Whether to include metadata (timestamps, URLs, etc.).
        /// </summary>
        public bool IncludeMetadata { get; set; } = false;

        /// <summary>
        /// Summarization mode determining aggressiveness of compression.
        /// </summary>
        public SummarizationMode Mode { get; set; } = SummarizationMode.Balanced;
    }

    /// <summary>
    /// Summarization aggressiveness levels.
    /// </summary>
    public enum SummarizationMode
    {
        /// <summary>
        /// Minimal summarization, preserve most details.
        /// </summary>
        Minimal,

        /// <summary>
        /// Balanced summarization for typical use cases.
        /// </summary>
        Balanced,

        /// <summary>
        /// Aggressive summarization for maximum token savings.
        /// </summary>
        Aggressive
    }

    /// <summary>
    /// Result of summarization with metrics.
    /// </summary>
    public class SummarizationResult
    {
        /// <summary>
        /// Original content.
        /// </summary>
        public string OriginalContent { get; set; }

        /// <summary>
        /// Summarized content.
        /// </summary>
        public string SummarizedContent { get; set; }

        /// <summary>
        /// Original content length in characters.
        /// </summary>
        public int OriginalLength { get; set; }

        /// <summary>
        /// Summarized content length in characters.
        /// </summary>
        public int SummarizedLength { get; set; }

        /// <summary>
        /// Compression ratio (0.0 to 1.0, lower is better).
        /// </summary>
        public double CompressionRatio => SummarizedLength / (double)OriginalLength;

        /// <summary>
        /// Estimated token savings (assuming ~4 chars per token).
        /// </summary>
        public int EstimatedTokenSavings => (OriginalLength - SummarizedLength) / 4;

        /// <summary>
        /// List of applied summarization techniques.
        /// </summary>
        public List<string> AppliedTechniques { get; set; }

        public SummarizationResult()
        {
            AppliedTechniques = new List<string>();
        }
    }
}
