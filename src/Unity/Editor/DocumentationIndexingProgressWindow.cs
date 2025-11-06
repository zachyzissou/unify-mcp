using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnifyMcp.Tools.Documentation;

namespace UnifyMcp.Unity.Editor
{
    /// <summary>
    /// EditorWindow displaying real-time progress for documentation indexing.
    /// Shows files processed, time elapsed, ETA, success/failure counts, and cancellation button.
    /// </summary>
    public class DocumentationIndexingProgressWindow : EditorWindow
    {
        private DocumentationIndexingWorker worker;
        private UnityInstallation selectedInstallation;
        private List<string> logMessages = new List<string>();
        private Vector2 scrollPosition;
        private const int MaxLogMessages = 100;

        // UI State
        private bool isIndexing;
        private DateTime lastUpdateTime;
        private string statusMessage = "Ready to index documentation";

        [MenuItem("Tools/Unify MCP/Documentation Indexer")]
        public static void ShowWindow()
        {
            var window = GetWindow<DocumentationIndexingProgressWindow>("Documentation Indexer");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnEnable()
        {
            lastUpdateTime = DateTime.UtcNow;
        }

        private void OnDisable()
        {
            // Clean up worker if window is closed
            if (worker != null && worker.IsRunning)
            {
                worker.CancelIndexing();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            // Header
            EditorGUILayout.LabelField("Unity Documentation Indexer", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Installation Selection
            DrawInstallationSelection();

            EditorGUILayout.Space(10);

            // Progress Display
            if (worker != null && isIndexing)
            {
                DrawProgressDisplay();
            }
            else
            {
                DrawReadyState();
            }

            EditorGUILayout.Space(10);

            // Log Display
            DrawLogDisplay();
        }

        private void DrawInstallationSelection()
        {
            EditorGUILayout.LabelField("Unity Installation", EditorStyles.boldLabel);

            if (isIndexing)
            {
                EditorGUI.BeginDisabledGroup(true);
                if (selectedInstallation != null)
                {
                    EditorGUILayout.TextField("Version", selectedInstallation.Version);
                    EditorGUILayout.TextField("Path", selectedInstallation.DocumentationPath);
                }
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                // Detect installations button
                if (GUILayout.Button("Detect Unity Installations", GUILayout.Height(30)))
                {
                    DetectInstallations();
                }

                if (selectedInstallation != null)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField($"Selected: Unity {selectedInstallation.Version}");
                    EditorGUILayout.LabelField($"Platform: {selectedInstallation.Platform}");
                    EditorGUILayout.LabelField($"Path: {selectedInstallation.DocumentationPath}", EditorStyles.wordWrappedLabel);
                }
            }
        }

        private void DrawReadyState()
        {
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(statusMessage, MessageType.Info);

            EditorGUILayout.Space(5);

            // Start Indexing Button
            EditorGUI.BeginDisabledGroup(selectedInstallation == null);
            if (GUILayout.Button("Start Indexing", GUILayout.Height(40)))
            {
                StartIndexing();
            }
            EditorGUI.EndDisabledGroup();

            if (selectedInstallation == null)
            {
                EditorGUILayout.HelpBox("Please detect and select a Unity installation first.", MessageType.Warning);
            }
        }

        private void DrawProgressDisplay()
        {
            var summary = worker.GetSummary();

            // Status
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Indexing in progress...", MessageType.Info);

            EditorGUILayout.Space(5);

            // Progress Bar
            var progress = summary.ProgressPercentage / 100f;
            var progressLabel = $"{summary.ProcessedFiles}/{summary.TotalFiles} files ({summary.ProgressPercentage:F1}%)";
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 25), progress, progressLabel);

            EditorGUILayout.Space(10);

            // Statistics
            EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Successfully Indexed:", GUILayout.Width(150));
            EditorGUILayout.LabelField($"{summary.SuccessfullyIndexed}", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Failed:", GUILayout.Width(150));
            EditorGUILayout.LabelField($"{summary.Failed}", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Time Elapsed:", GUILayout.Width(150));
            EditorGUILayout.LabelField($"{summary.Duration.TotalSeconds:F1} seconds");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Estimated Time Remaining:", GUILayout.Width(150));
            EditorGUILayout.LabelField($"{summary.EstimatedTimeRemaining.TotalSeconds:F0} seconds");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Cancel Button
            if (GUILayout.Button("Cancel Indexing", GUILayout.Height(30)))
            {
                CancelIndexing();
            }

            // Force repaint to update progress
            if ((DateTime.UtcNow - lastUpdateTime).TotalMilliseconds > 100)
            {
                Repaint();
                lastUpdateTime = DateTime.UtcNow;
            }
        }

        private void DrawLogDisplay()
        {
            EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));

            foreach (var message in logMessages)
            {
                EditorGUILayout.LabelField(message, EditorStyles.wordWrappedLabel);
            }

            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Clear Log"))
            {
                logMessages.Clear();
            }
        }

        private void DetectInstallations()
        {
            AddLogMessage("Detecting Unity installations...");

            var detector = new UnityInstallationDetector();
            var installations = detector.DetectUnityInstallations();

            if (installations.Count == 0)
            {
                AddLogMessage("No Unity installations found.");
                statusMessage = "No Unity installations detected. Please ensure Unity Hub is installed.";
                EditorUtility.DisplayDialog("No Installations Found",
                    "No Unity installations with documentation were detected.\n\n" +
                    "Please ensure Unity Hub is installed and you have at least one Unity version with documentation.",
                    "OK");
                return;
            }

            AddLogMessage($"Found {installations.Count} Unity installation(s).");

            // If only one installation, select it automatically
            if (installations.Count == 1)
            {
                selectedInstallation = installations[0];
                AddLogMessage($"Selected Unity {selectedInstallation.Version}");
                statusMessage = $"Ready to index Unity {selectedInstallation.Version} documentation";
            }
            else
            {
                // Show selection menu
                var menu = new GenericMenu();
                foreach (var installation in installations)
                {
                    var install = installation; // Capture for closure
                    menu.AddItem(
                        new GUIContent($"Unity {install.Version} ({install.Platform})"),
                        false,
                        () => SelectInstallation(install)
                    );
                }
                menu.ShowAsContext();
            }
        }

        private void SelectInstallation(UnityInstallation installation)
        {
            selectedInstallation = installation;
            AddLogMessage($"Selected Unity {installation.Version}");
            statusMessage = $"Ready to index Unity {installation.Version} documentation";
            Repaint();
        }

        private void StartIndexing()
        {
            if (selectedInstallation == null)
            {
                EditorUtility.DisplayDialog("No Installation Selected",
                    "Please detect and select a Unity installation first.",
                    "OK");
                return;
            }

            AddLogMessage($"Starting indexing for Unity {selectedInstallation.Version}...");

            // Create database path
            var databasePath = System.IO.Path.Combine(
                Application.dataPath,
                "..",
                "Library",
                "UnifyMcp",
                $"documentation_{selectedInstallation.Version}.db"
            );

            // Ensure directory exists
            var directory = System.IO.Path.GetDirectoryName(databasePath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // Create indexer and worker
            var indexer = new UnityDocumentationIndexer(databasePath);
            indexer.CreateDatabase();

            var parser = new HtmlDocumentationParser();
            var detector = new UnityInstallationDetector();

            worker = new DocumentationIndexingWorker(indexer, parser, detector)
            {
                BatchSize = 10,
                MillisecondsBetweenBatches = 50
            };

            // Subscribe to events
            worker.OnFileProcessed += OnFileProcessed;
            worker.OnProgress += OnProgress;
            worker.OnCompleted += OnCompleted;
            worker.OnError += OnError;

            // Start indexing
            isIndexing = true;
            worker.StartIndexing(selectedInstallation);

            statusMessage = "Indexing in progress...";
            Repaint();
        }

        private void CancelIndexing()
        {
            if (worker != null && worker.IsRunning)
            {
                AddLogMessage("Cancelling indexing...");
                worker.CancelIndexing();
            }
        }

        private void OnFileProcessed(string message)
        {
            // Only log errors and milestones to avoid flooding
            if (message.Contains("Failed") || message.Contains("Error"))
            {
                AddLogMessage($"⚠ {message}");
            }
            else if (worker.ProcessedFiles % 100 == 0)
            {
                AddLogMessage($"✓ Processed {worker.ProcessedFiles} files...");
            }

            Repaint();
        }

        private void OnProgress(int processed, int total)
        {
            // Progress bar updates automatically via DrawProgressDisplay
            Repaint();
        }

        private void OnCompleted(string message)
        {
            isIndexing = false;
            statusMessage = message;
            AddLogMessage($"✓ {message}");

            EditorUtility.DisplayDialog("Indexing Complete",
                message,
                "OK");

            Repaint();
        }

        private void OnError(string message)
        {
            AddLogMessage($"✗ Error: {message}");
            statusMessage = $"Error: {message}";

            EditorUtility.DisplayDialog("Indexing Error",
                message,
                "OK");

            Repaint();
        }

        private void AddLogMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            logMessages.Add($"[{timestamp}] {message}");

            // Keep log size manageable
            if (logMessages.Count > MaxLogMessages)
            {
                logMessages.RemoveAt(0);
            }

            // Auto-scroll to bottom
            scrollPosition.y = float.MaxValue;
        }
    }
}
