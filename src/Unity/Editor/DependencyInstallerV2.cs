using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using UnityEditor;
using UnityEngine;

namespace UnifyMcp.Unity.Editor
{
    /// <summary>
    /// Cross-platform dependency installer that downloads and extracts NuGet packages.
    /// Installs to Assets/Plugins/UnifyMcp/Dependencies (writable location).
    /// </summary>
    [InitializeOnLoad]
    public static class DependencyInstallerV2
    {
        private const string INSTALLED_FLAG = "UnifyMcp_DependenciesInstalled_V2";
        private const string DEPENDENCIES_PATH = "Assets/Plugins/UnifyMcp/Dependencies";

        // NuGet package definitions
        private static readonly DependencyInfo[] Dependencies = new[]
        {
            new DependencyInfo
            {
                Name = "ModelContextProtocol",
                Version = "0.4.0-preview.3",
                DllName = "ModelContextProtocol.dll"
            },
            new DependencyInfo
            {
                Name = "System.Data.SQLite.Core",
                Version = "1.0.118.0",
                DllName = "System.Data.SQLite.dll"
            },
            new DependencyInfo
            {
                Name = "NJsonSchema",
                Version = "11.0.0",
                DllName = "NJsonSchema.dll"
            },
            new DependencyInfo
            {
                Name = "Fastenshtein",
                Version = "1.0.0.8",
                DllName = "Fastenshtein.dll"
            },
            new DependencyInfo
            {
                Name = "AngleSharp",
                Version = "1.1.2",
                DllName = "AngleSharp.dll"
            }
        };

        static DependencyInstallerV2()
        {
            // Check if dependencies are already installed
            if (SessionState.GetBool(INSTALLED_FLAG, false))
            {
                return;
            }

            // Check if Dependencies folder already exists with DLLs
            if (AreDependenciesInstalled())
            {
                Debug.Log("[UnifyMcp] Dependencies already installed.");
                SessionState.SetBool(INSTALLED_FLAG, true);
                return;
            }

            // Run installation on next editor update
            EditorApplication.delayCall += InstallDependencies;
        }

        private static bool AreDependenciesInstalled()
        {
            if (!Directory.Exists(DEPENDENCIES_PATH))
                return false;

            foreach (var dep in Dependencies)
            {
                string dllPath = Path.Combine(DEPENDENCIES_PATH, dep.DllName);
                if (!File.Exists(dllPath))
                    return false;
            }

            return true;
        }

        private static void InstallDependencies()
        {
            try
            {
                Debug.Log("[UnifyMcp] Installing NuGet dependencies...");
                Debug.Log("[UnifyMcp] This may take a minute on first run.");

                // Create dependencies directory
                if (!Directory.Exists(DEPENDENCIES_PATH))
                {
                    Directory.CreateDirectory(DEPENDENCIES_PATH);
                }

                // Download and extract each dependency
                int successCount = 0;
                foreach (var dep in Dependencies)
                {
                    if (InstallDependency(dep))
                    {
                        successCount++;
                    }
                }

                if (successCount == Dependencies.Length)
                {
                    Debug.Log($"[UnifyMcp] Successfully installed {successCount}/{Dependencies.Length} dependencies!");
                    SessionState.SetBool(INSTALLED_FLAG, true);

                    // Refresh asset database to detect new DLLs
                    AssetDatabase.Refresh();

                    Debug.Log("[UnifyMcp] Dependencies installed. Unity is recompiling...");
                }
                else
                {
                    Debug.LogError($"[UnifyMcp] Failed to install some dependencies ({successCount}/{Dependencies.Length} succeeded)");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnifyMcp] Exception during dependency installation: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static bool InstallDependency(DependencyInfo dep)
        {
            try
            {
                string dllPath = Path.Combine(DEPENDENCIES_PATH, dep.DllName);

                // Skip if already exists
                if (File.Exists(dllPath))
                {
                    Debug.Log($"[UnifyMcp] {dep.Name} already installed, skipping.");
                    return true;
                }

                Debug.Log($"[UnifyMcp] Downloading {dep.Name} v{dep.Version}...");

                // Download NuGet package
                string nupkgUrl = $"https://www.nuget.org/api/v2/package/{dep.Name}/{dep.Version}";
                string tempNupkg = Path.Combine(Path.GetTempPath(), $"{dep.Name}.{dep.Version}.nupkg");

                using (var client = new WebClient())
                {
                    client.DownloadFile(nupkgUrl, tempNupkg);
                }

                Debug.Log($"[UnifyMcp] Extracting {dep.Name}...");

                // Extract DLL from .nupkg (it's just a ZIP file)
                ExtractDllFromNupkg(tempNupkg, dep.DllName, dllPath);

                // Cleanup
                if (File.Exists(tempNupkg))
                {
                    File.Delete(tempNupkg);
                }

                Debug.Log($"[UnifyMcp] ✓ {dep.Name} installed successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnifyMcp] Failed to install {dep.Name}: {ex.Message}");
                return false;
            }
        }

        private static void ExtractDllFromNupkg(string nupkgPath, string dllName, string outputPath)
        {
            using (var archive = ZipFile.OpenRead(nupkgPath))
            {
                // Look for DLL in common NuGet paths
                string[] searchPaths = new[]
                {
                    $"lib/netstandard2.1/{dllName}",
                    $"lib/netstandard2.0/{dllName}",
                    $"lib/net462/{dllName}",
                    $"lib/net461/{dllName}",
                    $"lib/net45/{dllName}",
                    $"lib/{dllName}",
                    $"runtimes/any/lib/netstandard2.0/{dllName}",
                    $"runtimes/win/lib/netstandard2.0/{dllName}"
                };

                foreach (var searchPath in searchPaths)
                {
                    var entry = archive.GetEntry(searchPath);
                    if (entry != null)
                    {
                        entry.ExtractToFile(outputPath, true);
                        return;
                    }
                }

                // If not found in standard paths, search all entries
                foreach (var entry in archive.Entries)
                {
                    if (entry.Name.Equals(dllName, StringComparison.OrdinalIgnoreCase))
                    {
                        entry.ExtractToFile(outputPath, true);
                        return;
                    }
                }

                throw new FileNotFoundException($"Could not find {dllName} in NuGet package");
            }
        }

        [MenuItem("Tools/UnifyMCP/Reinstall Dependencies")]
        private static void ReinstallDependencies()
        {
            ReinstallDependenciesPublic();
        }

        /// <summary>
        /// Public method to reinstall dependencies. Called from Control Panel.
        /// </summary>
        public static void ReinstallDependenciesPublic()
        {
            SessionState.SetBool(INSTALLED_FLAG, false);

            // Delete existing dependencies
            if (Directory.Exists(DEPENDENCIES_PATH))
            {
                Directory.Delete(DEPENDENCIES_PATH, true);
                AssetDatabase.Refresh();
            }

            InstallDependencies();
        }

        private class DependencyInfo
        {
            public string Name;
            public string Version;
            public string DllName;
        }
    }
}
