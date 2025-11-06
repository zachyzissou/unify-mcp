using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityMcp.Tools.Documentation;

namespace UnifyMcp.Tests.Documentation
{
    /// <summary>
    /// Tests for DocumentationIndexingWorker background processing with EditorCoroutine.
    /// Tests batch processing, progress tracking, cancellation, and error handling.
    /// </summary>
    [TestFixture]
    public class DocumentationIndexingWorkerTests
    {
        private string tempDatabasePath;
        private string tempDocPath;
        private UnityDocumentationIndexer indexer;
        private HtmlDocumentationParser parser;
        private UnityInstallationDetector detector;
        private DocumentationIndexingWorker worker;

        [SetUp]
        public void SetUp()
        {
            // Create temporary database
            tempDatabasePath = Path.Combine(Path.GetTempPath(), $"test_indexing_{Guid.NewGuid()}.db");
            indexer = new UnityDocumentationIndexer(tempDatabasePath);
            indexer.CreateDatabase();

            parser = new HtmlDocumentationParser();
            detector = new UnityInstallationDetector();

            worker = new DocumentationIndexingWorker(indexer, parser, detector)
            {
                BatchSize = 5,
                MillisecondsBetweenBatches = 10
            };

            // Create temporary documentation structure
            tempDocPath = Path.Combine(Path.GetTempPath(), $"UnityDoc_{Guid.NewGuid()}");
            var scriptRefPath = Path.Combine(tempDocPath, "ScriptReference");
            Directory.CreateDirectory(scriptRefPath);
        }

        [TearDown]
        public void TearDown()
        {
            indexer?.Dispose();

            if (File.Exists(tempDatabasePath))
                File.Delete(tempDatabasePath);

            if (Directory.Exists(tempDocPath))
                Directory.Delete(tempDocPath, recursive: true);
        }

        [Test]
        public void Constructor_WithNullIndexer_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DocumentationIndexingWorker(null, parser, detector));
        }

        [Test]
        public void Constructor_WithNullParser_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DocumentationIndexingWorker(indexer, null, detector));
        }

        [Test]
        public void Constructor_WithNullDetector_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DocumentationIndexingWorker(indexer, parser, null));
        }

        [Test]
        public void InitialState_ShouldHaveZeroProgress()
        {
            // Assert
            Assert.AreEqual(0, worker.TotalFiles);
            Assert.AreEqual(0, worker.ProcessedFiles);
            Assert.AreEqual(0, worker.SuccessfullyIndexed);
            Assert.AreEqual(0, worker.Failed);
            Assert.IsFalse(worker.IsRunning);
        }

        [Test]
        public void GetProgressPercentage_WithNoFiles_ShouldReturnZero()
        {
            // Act
            var percentage = worker.GetProgressPercentage();

            // Assert
            Assert.AreEqual(0f, percentage);
        }

        [Test]
        public void GetEstimatedTimeRemaining_WithNoProgress_ShouldReturnZero()
        {
            // Act
            var eta = worker.GetEstimatedTimeRemaining();

            // Assert
            Assert.AreEqual(TimeSpan.Zero, eta);
        }

        [Test]
        public void StartIndexing_WithNullInstallation_ShouldInvokeOnError()
        {
            // Arrange
            string errorMessage = null;
            worker.OnError += (msg) => errorMessage = msg;

            // Act
            worker.StartIndexing(null);

            // Assert
            Assert.IsNotNull(errorMessage);
            StringAssert.Contains("No Unity installation", errorMessage);
            Assert.IsFalse(worker.IsRunning);
        }

        [Test]
        public void StartIndexing_WithNoHtmlFiles_ShouldInvokeOnError()
        {
            // Arrange
            var installation = new UnityInstallation
            {
                Version = "2021.3.25f1",
                DocumentationPath = tempDocPath // Has ScriptReference folder but no HTML files
            };

            string errorMessage = null;
            worker.OnError += (msg) => errorMessage = msg;

            // Act
            worker.StartIndexing(installation);

            // Assert
            Assert.IsNotNull(errorMessage);
            StringAssert.Contains("No HTML files found", errorMessage);
            Assert.IsFalse(worker.IsRunning);
        }

        [Test]
        public void GetSummary_ShouldReturnCorrectInformation()
        {
            // Act
            var summary = worker.GetSummary();

            // Assert
            Assert.IsNotNull(summary);
            Assert.AreEqual(0, summary.TotalFiles);
            Assert.AreEqual(0, summary.ProcessedFiles);
            Assert.AreEqual(0, summary.SuccessfullyIndexed);
            Assert.AreEqual(0, summary.Failed);
            Assert.IsFalse(summary.IsRunning);
            Assert.AreEqual(0f, summary.ProgressPercentage);
        }

        [Test]
        public void IndexingSummary_ToString_ShouldFormatCorrectly()
        {
            // Arrange
            var summary = new IndexingSummary
            {
                TotalFiles = 100,
                ProcessedFiles = 50,
                SuccessfullyIndexed = 48,
                Failed = 2,
                Duration = TimeSpan.FromSeconds(30),
                EstimatedTimeRemaining = TimeSpan.FromSeconds(30),
                ProgressPercentage = 50f
            };

            // Act
            var text = summary.ToString();

            // Assert
            StringAssert.Contains("50/100", text);
            StringAssert.Contains("50.0%", text);
            StringAssert.Contains("48 indexed", text);
            StringAssert.Contains("2 failed", text);
        }

        [Test]
        public void CancelIndexing_WhenNotRunning_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => worker.CancelIndexing());
        }

        [Test]
        public void BatchSize_ShouldBeConfigurable()
        {
            // Arrange
            worker.BatchSize = 20;

            // Assert
            Assert.AreEqual(20, worker.BatchSize);
        }

        [Test]
        public void MillisecondsBetweenBatches_ShouldBeConfigurable()
        {
            // Arrange
            worker.MillisecondsBetweenBatches = 500;

            // Assert
            Assert.AreEqual(500, worker.MillisecondsBetweenBatches);
        }

        [Test]
        public void OnFileProcessed_Event_ShouldBeInvokable()
        {
            // Arrange
            var eventCalled = false;
            worker.OnFileProcessed += (msg) => eventCalled = true;

            // Act - Trigger the event manually (in real usage, StartIndexing triggers it)
            worker.OnFileProcessed?.Invoke("Test file");

            // Assert
            Assert.IsTrue(eventCalled, "OnFileProcessed event should be invokable");
        }

        [Test]
        public void OnProgress_Event_ShouldBeInvokable()
        {
            // Arrange
            int reportedProcessed = 0;
            int reportedTotal = 0;
            worker.OnProgress += (processed, total) =>
            {
                reportedProcessed = processed;
                reportedTotal = total;
            };

            // Act
            worker.OnProgress?.Invoke(10, 100);

            // Assert
            Assert.AreEqual(10, reportedProcessed);
            Assert.AreEqual(100, reportedTotal);
        }

        [Test]
        public void OnCompleted_Event_ShouldBeInvokable()
        {
            // Arrange
            string completionMessage = null;
            worker.OnCompleted += (msg) => completionMessage = msg;

            // Act
            worker.OnCompleted?.Invoke("Indexing complete");

            // Assert
            Assert.IsNotNull(completionMessage);
            StringAssert.Contains("complete", completionMessage);
        }

        [Test]
        public void OnError_Event_ShouldBeInvokable()
        {
            // Arrange
            string errorMessage = null;
            worker.OnError += (msg) => errorMessage = msg;

            // Act
            worker.OnError?.Invoke("Test error");

            // Assert
            Assert.IsNotNull(errorMessage);
            Assert.AreEqual("Test error", errorMessage);
        }

        // Note: Full integration tests with EditorCoroutine would require Unity Editor test mode
        // These tests validate the worker's initialization, configuration, and event system
        // Actual coroutine execution is tested in Unity Editor integration tests
    }
}
