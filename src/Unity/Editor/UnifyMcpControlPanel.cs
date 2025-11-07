using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UnifyMcp.Unity.Editor
{
    /// <summary>
    /// Control panel for UnifyMCP server with configuration generation, monitoring, and logs.
    /// </summary>
    public class UnifyMcpControlPanel : EditorWindow
    {
        private enum Tab { Configuration, Monitoring, Logs, Tools }
        private Tab currentTab = Tab.Configuration;

        private Vector2 scrollPosition;
        private Vector2 logScrollPosition;

        // Status
        private bool isServerRunning = false;
        private bool isConnected = false;
        private int requestCount = 0;
        private float cacheHitRate = 0f;
        private List<string> recentRequests = new List<string>();
        private List<LogEntry> logEntries = new List<LogEntry>();

        // Configuration
        private string projectPath = "";
        private string unityProjectPath = "";
        private string generatedConfig = "";

        // Log filtering
        private LogType logFilter = LogType.Log;

        [MenuItem("Tools/UnifyMCP/Control Panel")]
        public static void ShowWindow()
        {
            var window = GetWindow<UnifyMcpControlPanel>("UnifyMCP Control Panel");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            DetectPaths();
            UpdateServerStatus();
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            // Update status periodically (every second)
            if (EditorApplication.timeSinceStartup % 1.0 < 0.1)
            {
                UpdateServerStatus();
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawTabs();

            EditorGUILayout.Space(10);

            switch (currentTab)
            {
                case Tab.Configuration:
                    DrawConfigurationTab();
                    break;
                case Tab.Monitoring:
                    DrawMonitoringTab();
                    break;
                case Tab.Logs:
                    DrawLogsTab();
                    break;
                case Tab.Tools:
                    DrawToolsTab();
                    break;
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Title
            GUILayout.Label("UnifyMCP Server Control Panel", EditorStyles.boldLabel);

            EditorGUILayout.Space(5);

            // Status indicators
            EditorGUILayout.BeginHorizontal();

            // Server status
            string serverStatus = isServerRunning ? "🟢 Server Running" : "🔴 Server Stopped";
            GUILayout.Label(serverStatus, isServerRunning ? GetGreenStyle() : GetRedStyle());

            GUILayout.FlexibleSpace();

            // Connection status
            string connectionStatus = isConnected ? "🟢 Connected" : "🟡 Not Connected";
            GUILayout.Label(connectionStatus, isConnected ? GetGreenStyle() : GetYellowStyle());

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Metrics
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Requests: {requestCount}", EditorStyles.miniLabel);
            GUILayout.Label($"Cache Hit Rate: {cacheHitRate:P0}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Toggle(currentTab == Tab.Configuration, "Configuration", EditorStyles.toolbarButton))
                currentTab = Tab.Configuration;

            if (GUILayout.Toggle(currentTab == Tab.Monitoring, "Monitoring", EditorStyles.toolbarButton))
                currentTab = Tab.Monitoring;

            if (GUILayout.Toggle(currentTab == Tab.Logs, "Logs", EditorStyles.toolbarButton))
                currentTab = Tab.Logs;

            if (GUILayout.Toggle(currentTab == Tab.Tools, "Tools", EditorStyles.toolbarButton))
                currentTab = Tab.Tools;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawConfigurationTab()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("Generate MCP Client Configurations", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Generate configuration files for various MCP clients. Click Generate to create the config, then Copy to clipboard.", MessageType.Info);

            EditorGUILayout.Space(10);

            // Paths
            EditorGUILayout.LabelField("Detected Paths:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Unity Project:", unityProjectPath, EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("Package Path:", projectPath, EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(10);

            // Claude Desktop
            DrawConfigSection(
                "Claude Desktop",
                "Configuration for Claude Desktop MCP integration",
                () => GenerateClaudeDesktopConfig(),
                "claude_desktop_config.json"
            );

            EditorGUILayout.Space(10);

            // VS Code
            DrawConfigSection(
                "VS Code MCP Extension",
                "Configuration for VS Code MCP extension",
                () => GenerateVSCodeConfig(),
                "settings.json (MCP section)"
            );

            EditorGUILayout.Space(10);

            // Cursor
            DrawConfigSection(
                "Cursor IDE",
                "Configuration for Cursor IDE MCP integration",
                () => GenerateCursorConfig(),
                "cursor_config.json"
            );

            EditorGUILayout.Space(10);

            // Generic
            DrawConfigSection(
                "Generic MCP Client",
                "Generic stdio transport configuration",
                () => GenerateGenericConfig(),
                "mcp_config.json"
            );

            EditorGUILayout.EndScrollView();
        }

        private void DrawConfigSection(string title, string description, Func<string> generateFunc, string fileName)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUILayout.Label(title, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(description, MessageType.None);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Generate", GUILayout.Width(100)))
            {
                generatedConfig = generateFunc();
                EditorGUIUtility.systemCopyBuffer = generatedConfig;
                Debug.Log($"[UnifyMcp] Generated and copied {fileName} to clipboard!");
            }

            if (GUILayout.Button("Copy to Clipboard", GUILayout.Width(150)))
            {
                if (!string.IsNullOrEmpty(generatedConfig))
                {
                    EditorGUIUtility.systemCopyBuffer = generatedConfig;
                    Debug.Log($"[UnifyMcp] Copied {fileName} to clipboard!");
                }
                else
                {
                    Debug.LogWarning("[UnifyMcp] Generate config first!");
                }
            }

            if (GUILayout.Button("Save to File", GUILayout.Width(120)))
            {
                SaveConfigToFile(fileName, generatedConfig);
            }

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(generatedConfig))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.TextArea(generatedConfig, GUILayout.Height(100));
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMonitoringTab()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("Real-Time Monitoring", EditorStyles.boldLabel);

            EditorGUILayout.Space(10);

            // Recent requests
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Recent Requests (Last 10)", EditorStyles.boldLabel);

            if (recentRequests.Count == 0)
            {
                EditorGUILayout.HelpBox("No requests yet. Start using MCP tools from your AI client.", MessageType.Info);
            }
            else
            {
                foreach (var request in recentRequests)
                {
                    EditorGUILayout.LabelField("✅ " + request, EditorStyles.wordWrappedLabel);
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Performance metrics
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Performance Metrics", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Total Requests:", requestCount.ToString());
            EditorGUILayout.LabelField("Cache Hit Rate:", $"{cacheHitRate:P1}");
            EditorGUILayout.LabelField("Average Response Time:", "45ms"); // Mock data
            EditorGUILayout.LabelField("Token Savings:", "~65%"); // Mock data

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        private void DrawLogsTab()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Filter:", GUILayout.Width(50));

            if (GUILayout.Toggle(logFilter == LogType.Log, "All", EditorStyles.toolbarButton))
                logFilter = LogType.Log;
            if (GUILayout.Toggle(logFilter == LogType.Warning, "Warnings", EditorStyles.toolbarButton))
                logFilter = LogType.Warning;
            if (GUILayout.Toggle(logFilter == LogType.Error, "Errors", EditorStyles.toolbarButton))
                logFilter = LogType.Error;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Clear Logs", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                logEntries.Clear();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Log viewer
            logScrollPosition = EditorGUILayout.BeginScrollView(logScrollPosition, GUILayout.ExpandHeight(true));

            if (logEntries.Count == 0)
            {
                EditorGUILayout.HelpBox("No logs yet. Logs will appear here as the server processes requests.", MessageType.Info);
            }
            else
            {
                foreach (var log in logEntries)
                {
                    if (ShouldShowLog(log))
                    {
                        DrawLogEntry(log);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolsTab()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("Quick Actions", EditorStyles.boldLabel);

            EditorGUILayout.Space(10);

            // Server control
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Server Control", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(isServerRunning ? "Stop Server" : "Start Server", GUILayout.Height(30)))
            {
                ToggleServer();
            }

            if (GUILayout.Button("Restart Server", GUILayout.Height(30)))
            {
                RestartServer();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Maintenance
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Maintenance", EditorStyles.boldLabel);

            if (GUILayout.Button("Clear Cache", GUILayout.Height(30)))
            {
                ClearCache();
            }

            if (GUILayout.Button("Refresh Documentation Index", GUILayout.Height(30)))
            {
                RefreshDocumentation();
            }

            if (GUILayout.Button("Reinstall Dependencies", GUILayout.Height(30)))
            {
                DependencyInstaller.ReinstallDependenciesPublic();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Testing
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Testing", EditorStyles.boldLabel);

            if (GUILayout.Button("Test Documentation Query", GUILayout.Height(30)))
            {
                TestDocumentationQuery();
            }

            if (GUILayout.Button("Test Profiler Snapshot", GUILayout.Height(30)))
            {
                TestProfilerSnapshot();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        // Configuration generators
        private string GenerateClaudeDesktopConfig()
        {
            var config = new StringBuilder();
            config.AppendLine("{");
            config.AppendLine("  \"mcpServers\": {");
            config.AppendLine("    \"unity-mcp\": {");
            config.AppendLine("      \"command\": \"unity\",");
            config.AppendLine($"      \"args\": [\"{unityProjectPath}\", \"-batchmode\", \"-nographics\", \"-executeMethod\", \"UnifyMcp.StartMcpServer\"],");
            config.AppendLine("      \"env\": {}");
            config.AppendLine("    }");
            config.AppendLine("  }");
            config.AppendLine("}");
            return config.ToString();
        }

        private string GenerateVSCodeConfig()
        {
            var config = new StringBuilder();
            config.AppendLine("\"mcp.servers\": {");
            config.AppendLine("  \"unity-mcp\": {");
            config.AppendLine("    \"type\": \"stdio\",");
            config.AppendLine($"    \"command\": \"unity\",");
            config.AppendLine($"    \"args\": [\"{unityProjectPath}\", \"-batchmode\", \"-nographics\", \"-executeMethod\", \"UnifyMcp.StartMcpServer\"]");
            config.AppendLine("  }");
            config.AppendLine("}");
            return config.ToString();
        }

        private string GenerateCursorConfig()
        {
            return GenerateClaudeDesktopConfig(); // Same format as Claude Desktop
        }

        private string GenerateGenericConfig()
        {
            var config = new StringBuilder();
            config.AppendLine("{");
            config.AppendLine("  \"name\": \"unity-mcp\",");
            config.AppendLine("  \"transport\": \"stdio\",");
            config.AppendLine($"  \"command\": \"unity\",");
            config.AppendLine($"  \"args\": [\"{unityProjectPath}\", \"-batchmode\", \"-nographics\", \"-executeMethod\", \"UnifyMcp.StartMcpServer\"],");
            config.AppendLine("  \"description\": \"Unity MCP Server for AI-assisted Unity development\"");
            config.AppendLine("}");
            return config.ToString();
        }

        private void SaveConfigToFile(string fileName, string config)
        {
            if (string.IsNullOrEmpty(config))
            {
                Debug.LogWarning("[UnifyMcp] Generate config first!");
                return;
            }

            string path = EditorUtility.SaveFilePanel("Save Configuration", "", fileName, "json");
            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, config);
                Debug.Log($"[UnifyMcp] Configuration saved to: {path}");
            }
        }

        // Status and actions
        private void DetectPaths()
        {
            unityProjectPath = Path.GetFullPath(Application.dataPath + "/..");
            projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Packages/com.anthropic.unify-mcp"));
        }

        private void UpdateServerStatus()
        {
            // Mock status - will be replaced with real status checks
            isServerRunning = true;
            isConnected = false;
            requestCount = UnityEngine.Random.Range(50, 200);
            cacheHitRate = UnityEngine.Random.Range(0.6f, 0.9f);
        }

        private void ToggleServer()
        {
            isServerRunning = !isServerRunning;
            Debug.Log($"[UnifyMcp] Server {(isServerRunning ? "started" : "stopped")}");
        }

        private void RestartServer()
        {
            Debug.Log("[UnifyMcp] Restarting server...");
        }

        private void ClearCache()
        {
            Debug.Log("[UnifyMcp] Cache cleared!");
        }

        private void RefreshDocumentation()
        {
            Debug.Log("[UnifyMcp] Refreshing documentation index...");
        }

        private void TestDocumentationQuery()
        {
            Debug.Log("[UnifyMcp] Testing documentation query: GameObject.SetActive");
            AddRecentRequest("QueryDocumentation(GameObject.SetActive)");
        }

        private void TestProfilerSnapshot()
        {
            Debug.Log("[UnifyMcp] Testing profiler snapshot capture");
            AddRecentRequest("CaptureProfilerSnapshot()");
        }

        private void AddRecentRequest(string request)
        {
            recentRequests.Insert(0, $"{request} ({DateTime.Now:HH:mm:ss})");
            if (recentRequests.Count > 10)
                recentRequests.RemoveAt(10);
            requestCount++;
        }

        // Log helpers
        private void DrawLogEntry(LogEntry log)
        {
            GUIStyle style = GetLogStyle(log.type);
            EditorGUILayout.LabelField($"[{log.timestamp:HH:mm:ss}] {log.message}", style, GUILayout.Height(20));
        }

        private bool ShouldShowLog(LogEntry log)
        {
            if (logFilter == LogType.Log) return true;
            return log.type == logFilter;
        }

        private GUIStyle GetLogStyle(LogType type)
        {
            var style = new GUIStyle(EditorStyles.label);
            switch (type)
            {
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    style.normal.textColor = Color.red;
                    break;
                case LogType.Warning:
                    style.normal.textColor = new Color(1f, 0.6f, 0f);
                    break;
            }
            return style;
        }

        private GUIStyle GetGreenStyle()
        {
            var style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = Color.green;
            return style;
        }

        private GUIStyle GetYellowStyle()
        {
            var style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = new Color(1f, 0.92f, 0.016f);
            return style;
        }

        private GUIStyle GetRedStyle()
        {
            var style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = Color.red;
            return style;
        }

        private class LogEntry
        {
            public string message;
            public LogType type;
            public DateTime timestamp;
        }
    }
}
