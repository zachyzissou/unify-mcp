using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Unity.EditorCoroutines.Editor;

namespace UnifyMcp.Tools.Documentation
{
    /// <summary>
    /// Background worker for indexing Unity documentation files using EditorCoroutine.
    /// Processes HTML files in batches with progress reporting and cancellation support.
    /// </summary>
    public class DocumentationIndexingWorker
    {
        private readonly UnityDocumentationIndexer indexer;
        private readonly HtmlDocumentationParser parser;
        private readonly UnityInstallationDetector detector;

        private EditorCoroutine currentCoroutine;
        private CancellationTokenSource cancellationTokenSource;

        // Progress tracking
        public int TotalFiles { get; private set; }
        public int ProcessedFiles { get; private set; }
        public int SuccessfullyIndexed { get; private set; }
        public int Failed { get; private set; }
        public DateTime StartTime { get; private set; }
        public bool IsRunning { get; private set; }

        // Configuration
        public int BatchSize { get; set; } = 10;
        public int MillisecondsBetweenBatches { get; set; } = 100;

        // Events for progress reporting
        public event Action<string> OnFileProcessed;
        public event Action<int, int> OnProgress; // (processed, total)
        public event Action<string> OnCompleted;
        public event Action<string> OnError;

        public DocumentationIndexingWorker(
            UnityDocumentationIndexer indexer,
            HtmlDocumentationParser parser,
            UnityInstallationDetector detector)
        {
            this.indexer = indexer ?? throw new ArgumentNullException(nameof(indexer));
            this.parser = parser ?? throw new ArgumentNullException(nameof(parser));
            this.detector = detector ?? throw new ArgumentNullException(nameof(detector));
        }

        /// <summary>
        /// Starts the indexing process for a specific Unity installation.
        /// </summary>
        /// <param name="installation">Unity installation to index</param>
        public void StartIndexing(UnityInstallation installation)
        {
            if (IsRunning)
            {
                OnError?.Invoke("Indexing already in progress");
                return;
            }

            if (installation == null)
            {
                OnError?.Invoke("No Unity installation provided");
                return;
            }

            // Get all ScriptReference HTML files
            var htmlFiles = detector.GetScriptReferenceFiles(installation.DocumentationPath);
            if (htmlFiles.Count == 0)
            {
                OnError?.Invoke("No HTML files found in Unity installation");
                return;
            }

            // Initialize state
            TotalFiles = htmlFiles.Count;
            ProcessedFiles = 0;
            SuccessfullyIndexed = 0;
            Failed = 0;
            StartTime = DateTime.UtcNow;
            IsRunning = true;

            // Create cancellation token
            cancellationTokenSource = new CancellationTokenSource();

            // Start coroutine
            currentCoroutine = EditorCoroutineUtility.StartCoroutine(
                IndexFilesCoroutine(htmlFiles, installation.Version, cancellationTokenSource.Token),
                this
            );
        }

        /// <summary>
        /// Cancels the current indexing operation.
        /// </summary>
        public void CancelIndexing()
        {
            if (!IsRunning)
                return;

            cancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Coroutine that processes HTML files in batches.
        /// </summary>
        private IEnumerator IndexFilesCoroutine(List<string> htmlFiles, string unityVersion, CancellationToken cancellationToken)
        {
            for (int i = 0; i < htmlFiles.Count; i += BatchSize)
            {
                // Check for cancellation
                if (cancellationToken.IsCancellationRequested)
                {
                    IsRunning = false;
                    OnCompleted?.Invoke("Indexing cancelled");
                    yield break;
                }

                // Process batch
                int batchEnd = Math.Min(i + BatchSize, htmlFiles.Count);
                for (int j = i; j < batchEnd; j++)
                {
                    // Check for cancellation before each file
                    if (cancellationToken.IsCancellationRequested)
                    {
                        IsRunning = false;
                        OnCompleted?.Invoke("Indexing cancelled");
                        yield break;
                    }

                    ProcessFile(htmlFiles[j], unityVersion);
                }

                // Report progress
                OnProgress?.Invoke(ProcessedFiles, TotalFiles);

                // Yield to keep editor responsive
                yield return null;

                // Delay between batches to avoid overwhelming the editor
                if (i + BatchSize < htmlFiles.Count && MillisecondsBetweenBatches > 0)
                {
                    var waitUntil = DateTime.UtcNow.AddMilliseconds(MillisecondsBetweenBatches);
                    while (DateTime.UtcNow < waitUntil)
                    {
                        yield return null;
                    }
                }
            }

            // Complete
            IsRunning = false;
            var duration = DateTime.UtcNow - StartTime;
            var message = $"Indexing completed: {SuccessfullyIndexed} indexed, {Failed} failed, " +
                         $"took {duration.TotalSeconds:F1} seconds";
            OnCompleted?.Invoke(message);
        }

        /// <summary>
        /// Processes a single HTML file and indexes it.
        /// </summary>
        private void ProcessFile(string filePath, string unityVersion)
        {
            try
            {
                // Read HTML content
                var html = File.ReadAllText(filePath);

                // Generate documentation URL based on file path
                var fileName = Path.GetFileName(filePath);
                var documentationUrl = $"file:///{filePath}";

                // Parse HTML
                var entry = parser.ParseHtml(html, documentationUrl);

                if (entry != null)
                {
                    // Set Unity version
                    entry.UnityVersion = unityVersion;

                    // Index document
                    indexer.IndexDocument(entry);

                    SuccessfullyIndexed++;
                    OnFileProcessed?.Invoke($"Indexed: {entry.ClassName}.{entry.MethodName}");
                }
                else
                {
                    Failed++;
                    OnFileProcessed?.Invoke($"Failed to parse: {fileName}");
                }
            }
            catch (Exception ex)
            {
                Failed++;
                OnFileProcessed?.Invoke($"Error processing {Path.GetFileName(filePath)}: {ex.Message}");
            }
            finally
            {
                ProcessedFiles++;
            }
        }

        /// <summary>
        /// Gets the current progress percentage (0-100).
        /// </summary>
        public float GetProgressPercentage()
        {
            if (TotalFiles == 0)
                return 0f;

            return (ProcessedFiles / (float)TotalFiles) * 100f;
        }

        /// <summary>
        /// Gets estimated time remaining based on current progress.
        /// </summary>
        public TimeSpan GetEstimatedTimeRemaining()
        {
            if (ProcessedFiles == 0 || TotalFiles == 0)
                return TimeSpan.Zero;

            var elapsed = DateTime.UtcNow - StartTime;
            var averageTimePerFile = elapsed.TotalSeconds / ProcessedFiles;
            var remainingFiles = TotalFiles - ProcessedFiles;
            var estimatedSecondsRemaining = averageTimePerFile * remainingFiles;

            return TimeSpan.FromSeconds(estimatedSecondsRemaining);
        }

        /// <summary>
        /// Gets a summary of the indexing operation.
        /// </summary>
        public IndexingSummary GetSummary()
        {
            return new IndexingSummary
            {
                TotalFiles = TotalFiles,
                ProcessedFiles = ProcessedFiles,
                SuccessfullyIndexed = SuccessfullyIndexed,
                Failed = Failed,
                StartTime = StartTime,
                Duration = DateTime.UtcNow - StartTime,
                IsRunning = IsRunning,
                ProgressPercentage = GetProgressPercentage(),
                EstimatedTimeRemaining = GetEstimatedTimeRemaining()
            };
        }
    }

    /// <summary>
    /// Summary of an indexing operation.
    /// </summary>
    public class IndexingSummary
    {
        public int TotalFiles { get; set; }
        public int ProcessedFiles { get; set; }
        public int SuccessfullyIndexed { get; set; }
        public int Failed { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public bool IsRunning { get; set; }
        public float ProgressPercentage { get; set; }
        public TimeSpan EstimatedTimeRemaining { get; set; }

        public override string ToString()
        {
            return $"{ProcessedFiles}/{TotalFiles} files ({ProgressPercentage:F1}%) - " +
                   $"{SuccessfullyIndexed} indexed, {Failed} failed - " +
                   $"Duration: {Duration.TotalSeconds:F1}s, ETA: {EstimatedTimeRemaining.TotalSeconds:F0}s";
        }
    }
}
