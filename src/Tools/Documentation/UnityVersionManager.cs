using System;

namespace UnifyMcp.Tools.Documentation
{
    /// <summary>
    /// Manages Unity version detection and documentation version mapping.
    /// Maps Unity versions (2021.3.25f1) to documentation versions (2021.3).
    /// Flags deprecated APIs and provides migration suggestions.
    /// </summary>
    public class UnityVersionManager
    {
        /// <summary>
        /// Gets the current Unity Editor version.
        /// In actual Unity context, this would use UnityEditor.ApplicationInfo.unityVersion
        /// </summary>
        /// <returns>Unity version string (e.g., "2021.3.25f1")</returns>
        public string GetCurrentUnityVersion()
        {
#if UNITY_EDITOR
            // When running in Unity Editor, get actual version
            return UnityEditor.ApplicationInfo.unityVersion;
#else
            // For testing outside Unity Editor, return test version
            return "2021.3.25f1";
#endif
        }

        /// <summary>
        /// Maps Unity version to documentation version.
        /// Examples:
        /// - "2021.3.25f1" → "2021.3"
        /// - "2022.3.10f1" → "2022.3"
        /// - "6000.0.1f1" → "6.0" (Unity 6)
        /// </summary>
        /// <param name="unityVersion">Full Unity version string</param>
        /// <returns>Documentation version string</returns>
        public string MapToDocumentationVersion(string unityVersion)
        {
            if (string.IsNullOrWhiteSpace(unityVersion))
                throw new ArgumentException("Unity version cannot be null or empty", nameof(unityVersion));

            // Parse version components
            var versionParts = unityVersion.Split('.');
            if (versionParts.Length < 2)
                throw new ArgumentException($"Invalid Unity version format: {unityVersion}", nameof(unityVersion));

            var majorVersion = versionParts[0];
            var minorVersion = versionParts[1];

            // Handle Unity 6 (6000.0.x format)
            if (majorVersion == "6000")
            {
                return "6.0";
            }

            // Standard format: major.minor (e.g., "2021.3", "2022.3")
            return $"{majorVersion}.{minorVersion}";
        }

        /// <summary>
        /// Checks if an API is deprecated in the current Unity version.
        /// </summary>
        /// <param name="apiEntry">Documentation entry to check</param>
        /// <param name="currentVersion">Current Unity version</param>
        /// <returns>True if API is deprecated in current version</returns>
        public bool IsDeprecatedInVersion(DocumentationEntry apiEntry, string currentVersion)
        {
            if (apiEntry == null)
                return false;

            // If entry is marked as deprecated, check version compatibility
            if (apiEntry.IsDeprecated)
            {
                // If no specific Unity version info, assume deprecated in all versions
                if (string.IsNullOrEmpty(apiEntry.UnityVersion))
                    return true;

                // If the API's documented version matches or is older than current, it's deprecated
                return CompareVersions(apiEntry.UnityVersion, currentVersion) <= 0;
            }

            return false;
        }

        /// <summary>
        /// Compares two Unity versions.
        /// </summary>
        /// <param name="version1">First version</param>
        /// <param name="version2">Second version</param>
        /// <returns>-1 if version1 < version2, 0 if equal, 1 if version1 > version2</returns>
        public int CompareVersions(string version1, string version2)
        {
            if (string.IsNullOrWhiteSpace(version1) || string.IsNullOrWhiteSpace(version2))
                return 0;

            try
            {
                var v1Parts = ParseVersionComponents(version1);
                var v2Parts = ParseVersionComponents(version2);

                // Compare major version
                if (v1Parts.Major != v2Parts.Major)
                    return v1Parts.Major.CompareTo(v2Parts.Major);

                // Compare minor version
                if (v1Parts.Minor != v2Parts.Minor)
                    return v1Parts.Minor.CompareTo(v2Parts.Minor);

                // Compare patch version
                return v1Parts.Patch.CompareTo(v2Parts.Patch);
            }
            catch
            {
                // If parsing fails, consider versions equal
                return 0;
            }
        }

        /// <summary>
        /// Parses version components from Unity version string.
        /// </summary>
        private VersionComponents ParseVersionComponents(string version)
        {
            // Remove suffix (f1, b1, etc.)
            var versionWithoutSuffix = System.Text.RegularExpressions.Regex.Replace(version, @"[a-zA-Z]\d+$", "");

            var parts = versionWithoutSuffix.Split('.');
            var components = new VersionComponents();

            if (parts.Length > 0)
                int.TryParse(parts[0], out components.Major);

            if (parts.Length > 1)
                int.TryParse(parts[1], out components.Minor);

            if (parts.Length > 2)
                int.TryParse(parts[2], out components.Patch);

            return components;
        }

        /// <summary>
        /// Gets migration suggestions for deprecated APIs.
        /// </summary>
        /// <param name="apiEntry">Deprecated API entry</param>
        /// <returns>Migration suggestion or null if none available</returns>
        public string GetMigrationSuggestion(DocumentationEntry apiEntry)
        {
            if (apiEntry == null || !apiEntry.IsDeprecated)
                return null;

            if (!string.IsNullOrWhiteSpace(apiEntry.ReplacementApi))
            {
                return $"Use {apiEntry.ReplacementApi} instead";
            }

            // Generic migration message if no specific replacement
            return "This API is deprecated. Check Unity documentation for recommended alternatives.";
        }

        /// <summary>
        /// Gets the documentation URL for a specific Unity version.
        /// </summary>
        /// <param name="version">Unity version (e.g., "2021.3.25f1")</param>
        /// <param name="apiPath">API path (e.g., "Transform.Translate")</param>
        /// <returns>Full documentation URL</returns>
        public string GetDocumentationUrl(string version, string apiPath)
        {
            var docVersion = MapToDocumentationVersion(version);
            var sanitizedPath = apiPath.Replace(".", "-");

            return $"https://docs.unity3d.com/{docVersion}/Documentation/ScriptReference/{sanitizedPath}.html";
        }

        private struct VersionComponents
        {
            public int Major;
            public int Minor;
            public int Patch;
        }
    }
}
