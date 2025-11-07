using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnifyMcp.Unity.Editor
{
    /// <summary>
    /// Automatically installs NuGet dependencies when the package is imported.
    /// </summary>
    [InitializeOnLoad]
    public static class DependencyInstaller
    {
        private const string INSTALLED_FLAG = "UnifyMcp_DependenciesInstalled";

        static DependencyInstaller()
        {
            // Check if dependencies are already installed
            if (SessionState.GetBool(INSTALLED_FLAG, false))
            {
                return;
            }

            // Run installation on next editor update
            EditorApplication.delayCall += InstallDependencies;
        }

        private static void InstallDependencies()
        {
            try
            {
                string packagePath = GetPackagePath();
                if (string.IsNullOrEmpty(packagePath))
                {
                    UnityEngine.Debug.LogError("[UnifyMcp] Could not find package path");
                    return;
                }

                string scriptPath = Path.Combine(packagePath, "scripts", "install-dependencies.sh");
                if (!File.Exists(scriptPath))
                {
                    UnityEngine.Debug.LogError($"[UnifyMcp] Install script not found at: {scriptPath}");
                    return;
                }

                UnityEngine.Debug.Log("[UnifyMcp] Installing NuGet dependencies...");

                // Run the install script
                var processInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"\"{scriptPath}\"",
                    WorkingDirectory = packagePath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processInfo))
                {
                    if (process == null)
                    {
                        UnityEngine.Debug.LogError("[UnifyMcp] Failed to start install process");
                        return;
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        UnityEngine.Debug.Log($"[UnifyMcp] Dependencies installed successfully!\n{output}");
                        SessionState.SetBool(INSTALLED_FLAG, true);

                        // Refresh asset database to detect new DLLs
                        AssetDatabase.Refresh();
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"[UnifyMcp] Dependency installation failed (exit code {process.ExitCode}):\n{error}\n{output}");
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[UnifyMcp] Exception during dependency installation: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static string GetPackagePath()
        {
            // Try to find the package in Library/PackageCache
            string[] guids = AssetDatabase.FindAssets("DependencyInstaller");
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (assetPath.Contains("com.anthropic.unify-mcp"))
                {
                    // Get package root (go up from src/Unity/Editor/)
                    string dir = Path.GetDirectoryName(assetPath);
                    dir = Path.GetDirectoryName(dir); // Unity
                    dir = Path.GetDirectoryName(dir); // src
                    dir = Path.GetDirectoryName(dir); // package root
                    return dir;
                }
            }

            return null;
        }

        [MenuItem("Tools/UnifyMCP/Reinstall Dependencies")]
        private static void ReinstallDependencies()
        {
            SessionState.SetBool(INSTALLED_FLAG, false);
            InstallDependencies();
        }
    }
}
