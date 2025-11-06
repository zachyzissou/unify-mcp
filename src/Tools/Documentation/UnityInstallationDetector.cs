using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UnifyMcp.Tools.Documentation
{
    /// <summary>
    /// Detects Unity installation paths and documentation folders across Windows, macOS, and Linux.
    /// Searches Unity Hub default installation paths and validates documentation folder existence.
    /// </summary>
    public class UnityInstallationDetector
    {
        /// <summary>
        /// Detects all Unity installations with documentation on the current platform.
        /// Returns installations ordered by version (newest first).
        /// </summary>
        /// <returns>List of Unity installations with documentation paths</returns>
        public List<UnityInstallation> DetectUnityInstallations()
        {
            var installations = new List<UnityInstallation>();

            // Detect platform and use appropriate detection logic
            if (IsWindows())
            {
                installations.AddRange(DetectWindowsInstallations());
            }
            else if (IsMacOS())
            {
                installations.AddRange(DetectMacOSInstallations());
            }
            else if (IsLinux())
            {
                installations.AddRange(DetectLinuxInstallations());
            }

            // Sort by version descending (newest first)
            return installations.OrderByDescending(i => i.Version).ToList();
        }

        /// <summary>
        /// Detects Unity installations on Windows.
        /// Standard path: C:\Program Files\Unity\Hub\Editor\{version}\Editor\Data\Documentation
        /// </summary>
        private List<UnityInstallation> DetectWindowsInstallations()
        {
            var installations = new List<UnityInstallation>();
            var basePath = @"C:\Program Files\Unity\Hub\Editor";

            if (!Directory.Exists(basePath))
                return installations;

            // Iterate through version directories
            foreach (var versionDir in Directory.GetDirectories(basePath))
            {
                var version = Path.GetFileName(versionDir);
                var docPath = Path.Combine(versionDir, "Editor", "Data", "Documentation");

                if (Directory.Exists(docPath))
                {
                    installations.Add(new UnityInstallation
                    {
                        Version = version,
                        EditorPath = versionDir,
                        DocumentationPath = docPath,
                        Platform = "Windows"
                    });
                }
            }

            return installations;
        }

        /// <summary>
        /// Detects Unity installations on macOS.
        /// Standard path: /Applications/Unity/Hub/Editor/{version}/Unity.app/Contents/Documentation
        /// </summary>
        private List<UnityInstallation> DetectMacOSInstallations()
        {
            var installations = new List<UnityInstallation>();
            var basePath = "/Applications/Unity/Hub/Editor";

            if (!Directory.Exists(basePath))
                return installations;

            // Iterate through version directories
            foreach (var versionDir in Directory.GetDirectories(basePath))
            {
                var version = Path.GetFileName(versionDir);
                var docPath = Path.Combine(versionDir, "Unity.app", "Contents", "Documentation");

                if (Directory.Exists(docPath))
                {
                    installations.Add(new UnityInstallation
                    {
                        Version = version,
                        EditorPath = versionDir,
                        DocumentationPath = docPath,
                        Platform = "macOS"
                    });
                }
            }

            return installations;
        }

        /// <summary>
        /// Detects Unity installations on Linux.
        /// Standard path: ~/Unity/Hub/Editor/{version}/Editor/Data/Documentation
        /// </summary>
        private List<UnityInstallation> DetectLinuxInstallations()
        {
            var installations = new List<UnityInstallation>();
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var basePath = Path.Combine(homeDir, "Unity", "Hub", "Editor");

            if (!Directory.Exists(basePath))
                return installations;

            // Iterate through version directories
            foreach (var versionDir in Directory.GetDirectories(basePath))
            {
                var version = Path.GetFileName(versionDir);
                var docPath = Path.Combine(versionDir, "Editor", "Data", "Documentation");

                if (Directory.Exists(docPath))
                {
                    installations.Add(new UnityInstallation
                    {
                        Version = version,
                        EditorPath = versionDir,
                        DocumentationPath = docPath,
                        Platform = "Linux"
                    });
                }
            }

            return installations;
        }

        /// <summary>
        /// Detects if the current platform is Windows
        /// </summary>
        private bool IsWindows()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT ||
                   Environment.OSVersion.Platform == PlatformID.Win32Windows;
        }

        /// <summary>
        /// Detects if the current platform is macOS
        /// </summary>
        private bool IsMacOS()
        {
            // macOS reports as Unix, but we can distinguish by checking for specific directories
            return Environment.OSVersion.Platform == PlatformID.Unix &&
                   Directory.Exists("/Applications");
        }

        /// <summary>
        /// Detects if the current platform is Linux
        /// </summary>
        private bool IsLinux()
        {
            // Linux reports as Unix, but lacks /Applications directory (macOS-specific)
            return Environment.OSVersion.Platform == PlatformID.Unix &&
                   !Directory.Exists("/Applications");
        }

        /// <summary>
        /// Gets all ScriptReference HTML files from a Unity documentation path.
        /// Returns paths to all .html files in the ScriptReference folder.
        /// </summary>
        /// <param name="documentationPath">Path to Unity documentation folder</param>
        /// <returns>List of paths to ScriptReference HTML files</returns>
        public List<string> GetScriptReferenceFiles(string documentationPath)
        {
            if (string.IsNullOrWhiteSpace(documentationPath) || !Directory.Exists(documentationPath))
                return new List<string>();

            var scriptRefPath = Path.Combine(documentationPath, "ScriptReference");

            if (!Directory.Exists(scriptRefPath))
                return new List<string>();

            // Recursively find all HTML files
            return Directory.GetFiles(scriptRefPath, "*.html", SearchOption.AllDirectories).ToList();
        }

        /// <summary>
        /// Validates that a Unity installation has accessible documentation.
        /// </summary>
        /// <param name="installation">Unity installation to validate</param>
        /// <returns>True if documentation exists and is accessible</returns>
        public bool ValidateInstallation(UnityInstallation installation)
        {
            if (installation == null)
                return false;

            // Check that documentation path exists
            if (!Directory.Exists(installation.DocumentationPath))
                return false;

            // Check that ScriptReference folder exists
            var scriptRefPath = Path.Combine(installation.DocumentationPath, "ScriptReference");
            if (!Directory.Exists(scriptRefPath))
                return false;

            // Check that there are HTML files in ScriptReference
            var htmlFiles = GetScriptReferenceFiles(installation.DocumentationPath);
            return htmlFiles.Count > 0;
        }
    }

    /// <summary>
    /// Represents a detected Unity installation with documentation.
    /// </summary>
    public class UnityInstallation
    {
        /// <summary>
        /// Unity version (e.g., "2021.3.25f1", "2022.3.10f1")
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Path to Unity Editor installation folder
        /// </summary>
        public string EditorPath { get; set; }

        /// <summary>
        /// Path to documentation folder
        /// </summary>
        public string DocumentationPath { get; set; }

        /// <summary>
        /// Platform name ("Windows", "macOS", "Linux")
        /// </summary>
        public string Platform { get; set; }

        public override string ToString()
        {
            return $"Unity {Version} ({Platform}) - {DocumentationPath}";
        }
    }
}
