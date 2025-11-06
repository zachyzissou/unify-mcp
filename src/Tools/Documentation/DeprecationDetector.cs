using System;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;

namespace UnifyMcp.Tools.Documentation
{
    /// <summary>
    /// Detects deprecation warnings in Unity documentation HTML.
    /// Parses for "Deprecated" or "Obsolete" markers and extracts replacement API suggestions.
    /// </summary>
    public class DeprecationDetector
    {
        private readonly HtmlParser htmlParser;
        private readonly UnityVersionManager versionManager;

        public DeprecationDetector(UnityVersionManager versionManager = null)
        {
            this.htmlParser = new HtmlParser();
            this.versionManager = versionManager ?? new UnityVersionManager();
        }

        /// <summary>
        /// Detects deprecation information from HTML content.
        /// </summary>
        /// <param name="html">Documentation HTML content</param>
        /// <returns>DeprecationInfo or null if not deprecated</returns>
        public DeprecationInfo DetectDeprecation(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return null;

            try
            {
                var document = htmlParser.ParseDocument(html);

                // Check for explicit deprecation message elements
                var deprecatedElement = document.QuerySelector(".deprecated-message, div.deprecated-message, .obsolete-message, div.obsolete-message");
                if (deprecatedElement != null)
                {
                    return ParseDeprecationMessage(deprecatedElement.TextContent);
                }

                // Check for deprecation in description text
                var descriptionElements = document.QuerySelectorAll(".description p, div.description p");
                foreach (var elem in descriptionElements)
                {
                    var text = elem.TextContent;
                    if (ContainsDeprecationKeywords(text))
                    {
                        return ParseDeprecationMessage(text);
                    }
                }

                // Check signature for [Obsolete] attribute
                var signature = document.QuerySelector(".signature code, div.signature code")?.TextContent ?? "";
                if (signature.Contains("[Obsolete]") || signature.Contains("[System.Obsolete]"))
                {
                    // Try to extract message from Obsolete attribute
                    var obsoleteMatch = Regex.Match(signature, @"\[Obsolete\(""([^""]+)""\)\]");
                    if (obsoleteMatch.Success)
                    {
                        return ParseDeprecationMessage(obsoleteMatch.Groups[1].Value);
                    }

                    return new DeprecationInfo
                    {
                        IsDeprecated = true,
                        Message = "This API is marked as obsolete"
                    };
                }

                return null; // Not deprecated
            }
            catch (Exception)
            {
                // If parsing fails, assume not deprecated
                return null;
            }
        }

        /// <summary>
        /// Checks if text contains deprecation keywords.
        /// </summary>
        private bool ContainsDeprecationKeywords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var lowerText = text.ToLowerInvariant();
            return lowerText.Contains("deprecated") ||
                   lowerText.Contains("obsolete") ||
                   lowerText.Contains("no longer supported") ||
                   lowerText.Contains("use instead");
        }

        /// <summary>
        /// Parses deprecation message to extract replacement API and version info.
        /// </summary>
        private DeprecationInfo ParseDeprecationMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return null;

            var info = new DeprecationInfo
            {
                IsDeprecated = true,
                Message = message.Trim()
            };

            // Extract replacement API
            // Patterns: "Use XYZ instead", "Please use ABC", "Replaced by DEF"
            var replacementPatterns = new[]
            {
                @"[Uu]se\s+([A-Za-z0-9_.]+)\s+instead",
                @"[Pp]lease\s+use\s+([A-Za-z0-9_.]+)",
                @"[Rr]eplaced\s+by\s+([A-Za-z0-9_.]+)",
                @"[Ss]ee\s+([A-Za-z0-9_.]+)\s+for",
                @"[Uu]se\s+([A-Za-z0-9_.]+)\s+or",
            };

            foreach (var pattern in replacementPatterns)
            {
                var match = Regex.Match(message, pattern);
                if (match.Success)
                {
                    info.ReplacementApi = match.Groups[1].Value;
                    break;
                }
            }

            // Extract version info if present
            // Pattern: "since Unity 2021.3", "as of version 2022.2"
            var versionPattern = @"(?:since|from|as of|in)\s+(?:Unity\s+)?(\d+\.\d+(?:\.\d+)?(?:[a-z]\d+)?)";
            var versionMatch = Regex.Match(message, versionPattern, RegexOptions.IgnoreCase);
            if (versionMatch.Success)
            {
                info.DeprecatedSinceVersion = versionMatch.Groups[1].Value;
            }

            return info;
        }

        /// <summary>
        /// Enriches a DocumentationEntry with deprecation information.
        /// </summary>
        /// <param name="entry">Documentation entry to enrich</param>
        /// <param name="html">HTML content to analyze</param>
        public void EnrichWithDeprecationInfo(DocumentationEntry entry, string html)
        {
            if (entry == null)
                return;

            var deprecationInfo = DetectDeprecation(html);
            if (deprecationInfo != null)
            {
                entry.IsDeprecated = true;
                entry.ReplacementApi = deprecationInfo.ReplacementApi;

                // Append deprecation message to description
                if (!string.IsNullOrEmpty(deprecationInfo.Message) && !entry.Description?.Contains(deprecationInfo.Message) == true)
                {
                    entry.Description = $"[DEPRECATED] {deprecationInfo.Message}\n\n{entry.Description}";
                }
            }
        }

        /// <summary>
        /// Checks if an API should show a deprecation warning for the current Unity version.
        /// </summary>
        /// <param name="entry">Documentation entry</param>
        /// <param name="currentVersion">Current Unity version</param>
        /// <returns>Deprecation warning message or null</returns>
        public string GetDeprecationWarning(DocumentationEntry entry, string currentVersion)
        {
            if (entry == null || !entry.IsDeprecated)
                return null;

            var warning = $"⚠️ {entry.ClassName}.{entry.MethodName} is deprecated";

            if (!string.IsNullOrEmpty(entry.ReplacementApi))
            {
                warning += $". Use {entry.ReplacementApi} instead";
            }

            return warning;
        }
    }

    /// <summary>
    /// Information about API deprecation.
    /// </summary>
    public class DeprecationInfo
    {
        /// <summary>
        /// Whether the API is deprecated
        /// </summary>
        public bool IsDeprecated { get; set; }

        /// <summary>
        /// Deprecation message from documentation
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Suggested replacement API
        /// </summary>
        public string ReplacementApi { get; set; }

        /// <summary>
        /// Unity version when API was deprecated
        /// </summary>
        public string DeprecatedSinceVersion { get; set; }
    }
}
