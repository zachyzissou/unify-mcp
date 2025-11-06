using System;

namespace UnifyMcp.Tools.Documentation
{
    /// <summary>
    /// Represents a Unity API documentation entry with version tracking.
    /// Fields: ClassName, MethodName, ReturnType, Parameters[], Description,
    /// CodeExamples[], UnityVersion, DocumentationUrl, LastUpdated
    /// </summary>
    public class DocumentationEntry
    {
        /// <summary>
        /// The Unity class name (e.g., "Transform", "GameObject")
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// The method or property name (e.g., "Translate", "position")
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// The return type of the method or property type (e.g., "void", "Vector3")
        /// </summary>
        public string ReturnType { get; set; }

        /// <summary>
        /// Array of parameter descriptions (e.g., ["Vector3 translation", "Space relativeTo"])
        /// </summary>
        public string[] Parameters { get; set; }

        /// <summary>
        /// Description of what the API does
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Code examples demonstrating usage
        /// </summary>
        public string[] CodeExamples { get; set; }

        /// <summary>
        /// Unity version this documentation applies to (e.g., "2021.3", "2022.3", "6.0")
        /// </summary>
        public string UnityVersion { get; set; }

        /// <summary>
        /// URL to the official Unity documentation
        /// </summary>
        public string DocumentationUrl { get; set; }

        /// <summary>
        /// Timestamp when this entry was last updated in the index
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Whether this API is marked as deprecated in Unity documentation
        /// </summary>
        public bool IsDeprecated { get; set; }

        /// <summary>
        /// If deprecated, the recommended replacement API
        /// </summary>
        public string ReplacementApi { get; set; }

        public DocumentationEntry()
        {
            Parameters = Array.Empty<string>();
            CodeExamples = Array.Empty<string>();
            LastUpdated = DateTime.UtcNow;
            IsDeprecated = false;
        }

        /// <summary>
        /// Gets the full API signature (e.g., "Transform.Translate")
        /// </summary>
        public string GetFullSignature()
        {
            return string.IsNullOrEmpty(MethodName)
                ? ClassName
                : $"{ClassName}.{MethodName}";
        }

        /// <summary>
        /// Gets a formatted parameter list for display
        /// </summary>
        public string GetParameterList()
        {
            return Parameters?.Length > 0
                ? string.Join(", ", Parameters)
                : string.Empty;
        }

        /// <summary>
        /// Creates a search-optimized text representation for FTS5 indexing
        /// </summary>
        public string GetSearchableText()
        {
            var parts = new[]
            {
                ClassName,
                MethodName,
                ReturnType,
                GetParameterList(),
                Description
            };

            return string.Join(" ", parts).ToLowerInvariant();
        }
    }
}
