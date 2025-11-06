using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UnifyMcp.Core.Context.Models;

namespace UnifyMcp.Core.Context
{
    /// <summary>
    /// Summarizes verbose tool results to reduce token consumption.
    /// Implements FR-032: Tool result summarization.
    /// </summary>
    public class ToolResultSummarizer
    {
        private const int EstimatedCharsPerToken = 4;

        /// <summary>
        /// Summarizes a tool result based on provided options.
        /// </summary>
        /// <param name="content">Original tool result content (typically JSON).</param>
        /// <param name="options">Summarization options.</param>
        /// <returns>Summarization result with metrics.</returns>
        public SummarizationResult Summarize(string content, SummarizationOptions options = null)
        {
            if (string.IsNullOrEmpty(content))
                throw new ArgumentException("Content cannot be null or empty", nameof(content));

            options ??= new SummarizationOptions();

            var result = new SummarizationResult
            {
                OriginalContent = content,
                OriginalLength = content.Length
            };

            try
            {
                // Try to parse as JSON
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(content);
                result.SummarizedContent = SummarizeJson(jsonElement, options, result.AppliedTechniques);
            }
            catch (JsonException)
            {
                // Not JSON, treat as plain text
                result.SummarizedContent = SummarizePlainText(content, options, result.AppliedTechniques);
            }

            result.SummarizedLength = result.SummarizedContent.Length;
            return result;
        }

        /// <summary>
        /// Summarizes multiple tool results into a single response.
        /// </summary>
        /// <param name="toolResults">Dictionary of tool name to result content.</param>
        /// <param name="options">Summarization options.</param>
        /// <returns>Combined summarization result.</returns>
        public SummarizationResult SummarizeMultiple(
            Dictionary<string, string> toolResults,
            SummarizationOptions options = null)
        {
            if (toolResults == null || toolResults.Count == 0)
                throw new ArgumentException("Tool results cannot be null or empty", nameof(toolResults));

            options ??= new SummarizationOptions();

            var sb = new StringBuilder();
            var originalLength = 0;
            var appliedTechniques = new HashSet<string>();

            foreach (var kvp in toolResults)
            {
                originalLength += kvp.Value.Length;
                var individualResult = Summarize(kvp.Value, options);

                sb.AppendLine($"=== {kvp.Key} ===");
                sb.AppendLine(individualResult.SummarizedContent);
                sb.AppendLine();

                foreach (var technique in individualResult.AppliedTechniques)
                    appliedTechniques.Add(technique);
            }

            var summarizedContent = sb.ToString().TrimEnd();

            return new SummarizationResult
            {
                OriginalContent = string.Join("\n\n", toolResults.Values),
                SummarizedContent = summarizedContent,
                OriginalLength = originalLength,
                SummarizedLength = summarizedContent.Length,
                AppliedTechniques = appliedTechniques.ToList()
            };
        }

        private string SummarizeJson(JsonElement json, SummarizationOptions options, List<string> techniques)
        {
            var sb = new StringBuilder();
            SummarizeJsonElement(json, sb, options, techniques, depth: 0);
            return sb.ToString();
        }

        private void SummarizeJsonElement(
            JsonElement element,
            StringBuilder sb,
            SummarizationOptions options,
            List<string> techniques,
            int depth)
        {
            if (depth > options.MaxDepth)
            {
                sb.Append("[...truncated]");
                if (!techniques.Contains("depth_limiting"))
                    techniques.Add("depth_limiting");
                return;
            }

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    SummarizeJsonObject(element, sb, options, techniques, depth);
                    break;

                case JsonValueKind.Array:
                    SummarizeJsonArray(element, sb, options, techniques, depth);
                    break;

                case JsonValueKind.String:
                    var str = element.GetString();
                    if (str.Length > options.MaxLength && !IsCodeExample(str, options))
                    {
                        sb.Append(TruncateText(str, options.MaxLength));
                        if (!techniques.Contains("text_truncation"))
                            techniques.Add("text_truncation");
                    }
                    else
                    {
                        sb.Append(str);
                    }
                    break;

                default:
                    sb.Append(element.ToString());
                    break;
            }
        }

        private void SummarizeJsonObject(
            JsonElement obj,
            StringBuilder sb,
            SummarizationOptions options,
            List<string> techniques,
            int depth)
        {
            sb.Append("{");

            var properties = obj.EnumerateObject().ToList();
            var includedCount = 0;

            foreach (var prop in properties)
            {
                // Skip metadata if not included
                if (!options.IncludeMetadata && IsMetadataField(prop.Name))
                {
                    if (!techniques.Contains("metadata_removal"))
                        techniques.Add("metadata_removal");
                    continue;
                }

                if (includedCount > 0)
                    sb.Append(", ");

                sb.Append($"\"{prop.Name}\": ");

                // Preserve code examples
                if (options.PreserveCodeExamples && IsCodeExampleField(prop.Name))
                {
                    sb.Append(JsonSerializer.Serialize(prop.Value));
                }
                else
                {
                    SummarizeJsonElement(prop.Value, sb, options, techniques, depth + 1);
                }

                includedCount++;
            }

            sb.Append("}");
        }

        private void SummarizeJsonArray(
            JsonElement array,
            StringBuilder sb,
            SummarizationOptions options,
            List<string> techniques,
            int depth)
        {
            var items = array.EnumerateArray().ToList();
            var totalItems = items.Count;

            sb.Append("[");

            if (totalItems > options.MaxListItems)
            {
                // Include first MaxListItems items
                for (int i = 0; i < options.MaxListItems; i++)
                {
                    if (i > 0) sb.Append(", ");
                    SummarizeJsonElement(items[i], sb, options, techniques, depth + 1);
                }

                var remaining = totalItems - options.MaxListItems;
                sb.Append($", ...and {remaining} more");

                if (!techniques.Contains("list_truncation"))
                    techniques.Add("list_truncation");
            }
            else
            {
                for (int i = 0; i < totalItems; i++)
                {
                    if (i > 0) sb.Append(", ");
                    SummarizeJsonElement(items[i], sb, options, techniques, depth + 1);
                }
            }

            sb.Append("]");
        }

        private string SummarizePlainText(string text, SummarizationOptions options, List<string> techniques)
        {
            if (text.Length <= options.MaxLength)
                return text;

            // Check if it's code
            if (IsCodeExample(text, options))
                return text;

            // Extract key sentences
            var sentences = SplitIntoSentences(text);
            var sb = new StringBuilder();
            var currentLength = 0;

            foreach (var sentence in sentences)
            {
                if (currentLength + sentence.Length > options.MaxLength)
                {
                    sb.Append("...");
                    if (!techniques.Contains("sentence_truncation"))
                        techniques.Add("sentence_truncation");
                    break;
                }

                sb.Append(sentence);
                currentLength += sentence.Length;
            }

            return sb.ToString();
        }

        private string TruncateText(string text, int maxLength)
        {
            if (text.Length <= maxLength)
                return text;

            var truncated = text.Substring(0, maxLength - 3);
            var lastSpace = truncated.LastIndexOf(' ');

            if (lastSpace > maxLength / 2)
                truncated = truncated.Substring(0, lastSpace);

            return truncated + "...";
        }

        private List<string> SplitIntoSentences(string text)
        {
            // Simple sentence splitting (can be improved with NLP libraries)
            var pattern = @"(?<=[.!?])\s+";
            return Regex.Split(text, pattern).ToList();
        }

        private bool IsMetadataField(string fieldName)
        {
            var metadataFields = new[]
            {
                "timestamp", "createdAt", "updatedAt", "lastModified",
                "id", "guid", "uuid", "url", "documentationUrl",
                "metadata", "version"
            };

            return metadataFields.Any(f => fieldName.Equals(f, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsCodeExampleField(string fieldName)
        {
            var codeFields = new[]
            {
                "codeExamples", "code", "example", "snippet", "sample"
            };

            return codeFields.Any(f => fieldName.Equals(f, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsCodeExample(string text, SummarizationOptions options)
        {
            if (!options.PreserveCodeExamples)
                return false;

            // Heuristics to detect code
            var codeIndicators = new[]
            {
                "using ", "namespace ", "class ", "public ", "private ",
                "void ", "return ", "if (", "for (", "while (",
                "{", "}", "//", "/*"
            };

            var indicatorCount = codeIndicators.Count(indicator => text.Contains(indicator));
            return indicatorCount >= 3; // At least 3 code indicators
        }

        /// <summary>
        /// Estimates token count from character count.
        /// </summary>
        public int EstimateTokenCount(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            return text.Length / EstimatedCharsPerToken;
        }

        /// <summary>
        /// Adjusts summarization options based on target token budget.
        /// </summary>
        public SummarizationOptions OptimizeOptionsForTokenBudget(int targetTokens, int currentTokens)
        {
            var ratio = (double)targetTokens / currentTokens;

            if (ratio >= 0.8)
                return new SummarizationOptions { Mode = SummarizationMode.Minimal };

            if (ratio >= 0.5)
                return new SummarizationOptions { Mode = SummarizationMode.Balanced };

            return new SummarizationOptions
            {
                Mode = SummarizationMode.Aggressive,
                MaxLength = 200,
                MaxListItems = 3,
                MaxDepth = 2,
                IncludeMetadata = false
            };
        }
    }
}
