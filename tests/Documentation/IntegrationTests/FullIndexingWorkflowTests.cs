using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using UnifyMcp.Tools.Documentation;

namespace UnifyMcp.Tests.Documentation.IntegrationTests
{
    /// <summary>
    /// Integration tests for the complete documentation indexing workflow.
    /// Tests end-to-end: detect Unity install → parse documentation → index to SQLite → query with fuzzy search → verify results.
    /// </summary>
    [TestFixture]
    public class FullIndexingWorkflowTests
    {
        private string tempTestDir;
        private string tempDatabasePath;
        private string tempDocPath;
        private string tempCacheDir;

        [SetUp]
        public void SetUp()
        {
            tempTestDir = Path.Combine(Path.GetTempPath(), $"UnifyMcpIntegrationTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempTestDir);

            tempDatabasePath = Path.Combine(tempTestDir, "test_integration.db");
            tempDocPath = Path.Combine(tempTestDir, "Documentation");
            tempCacheDir = Path.Combine(tempTestDir, "Cache");

            // Create mock Unity documentation structure
            CreateMockDocumentationStructure();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(tempTestDir))
            {
                Directory.Delete(tempTestDir, recursive: true);
            }
        }

        [Test]
        public void FullWorkflow_DetectInstall_ParseDocs_Index_Query_ShouldSucceed()
        {
            // Step 1: Create mock Unity installation
            var mockInstallation = new UnityInstallation
            {
                Version = "2021.3.25f1",
                EditorPath = Path.Combine(tempTestDir, "Unity"),
                DocumentationPath = tempDocPath,
                Platform = "Windows"
            };

            // Step 2: Create indexer and parser
            var indexer = new UnityDocumentationIndexer(tempDatabasePath);
            indexer.CreateDatabase();

            var parser = new HtmlDocumentationParser();

            // Step 3: Get documentation files
            var detector = new UnityInstallationDetector();
            var htmlFiles = detector.GetScriptReferenceFiles(mockInstallation.DocumentationPath);

            Assert.IsNotEmpty(htmlFiles, "Should find HTML files in mock documentation");
            Assert.GreaterOrEqual(htmlFiles.Count, 3, "Should have at least 3 test HTML files");

            // Step 4: Parse and index documents
            int successfullyIndexed = 0;
            foreach (var htmlFile in htmlFiles)
            {
                var html = File.ReadAllText(htmlFile);
                var entry = parser.ParseHtml(html, $"file:///{htmlFile}");

                if (entry != null)
                {
                    entry.UnityVersion = mockInstallation.Version;
                    indexer.IndexDocument(entry);
                    successfullyIndexed++;
                }
            }

            Assert.GreaterOrEqual(successfullyIndexed, 3, "Should successfully index at least 3 documents");

            // Step 5: Query the indexed documentation
            var results = indexer.QueryDocumentation("Transform");

            Assert.IsNotEmpty(results, "Should find Transform in indexed documentation");
            Assert.IsTrue(results.Any(r => r.ClassName == "Transform"), "Should find Transform class");

            // Step 6: Test fuzzy search
            var fuzzySearch = new FuzzyDocumentationSearch();
            var allApis = results.Select(r => $"{r.ClassName}.{r.MethodName}").ToArray();
            var fuzzyResults = fuzzySearch.FindSimilarApis("Translte", allApis, threshold: 0.7); // Typo: missing 'a'

            Assert.IsNotEmpty(fuzzyResults, "Fuzzy search should find similar APIs despite typo");

            // Cleanup
            indexer.Dispose();
        }

        [Test]
        public void Workflow_UnityVersionManager_ShouldMapVersionCorrectly()
        {
            // Arrange
            var versionManager = new UnityVersionManager();

            // Act & Assert
            Assert.AreEqual("2021.3", versionManager.MapToDocumentationVersion("2021.3.25f1"));
            Assert.AreEqual("2022.3", versionManager.MapToDocumentationVersion("2022.3.10f1"));
            Assert.AreEqual("6.0", versionManager.MapToDocumentationVersion("6000.0.1f1"));
        }

        [Test]
        public void Workflow_DeprecationDetector_ShouldDetectDeprecated()
        {
            // Arrange
            var deprecationDetector = new DeprecationDetector();
            var deprecatedHtml = @"
                <html>
                <body>
                    <div class='deprecated-message'>
                        <p>This method is deprecated. Use NewMethod instead.</p>
                    </div>
                    <div class='signature'>
                        <code>public void OldMethod();</code>
                    </div>
                </body>
                </html>";

            // Act
            var deprecationInfo = deprecationDetector.DetectDeprecation(deprecatedHtml);

            // Assert
            Assert.IsNotNull(deprecationInfo);
            Assert.IsTrue(deprecationInfo.IsDeprecated);
            Assert.AreEqual("NewMethod", deprecationInfo.ReplacementApi);
        }

        [Test]
        public void Workflow_CacheManager_ShouldManageCache()
        {
            // Arrange
            var cacheManager = new DocumentationCacheManager(tempCacheDir);
            var testUrl = "https://docs.unity3d.com/ScriptReference/Transform.html";
            var testFilePath = Path.Combine(tempCacheDir, "test.html");
            File.WriteAllText(testFilePath, "<html>Test</html>");

            // Act - Record cached URL
            cacheManager.RecordCachedUrl(testUrl, testFilePath);

            // Assert - Should be cached
            Assert.IsTrue(cacheManager.IsCached(testUrl));
            Assert.AreEqual(testFilePath, cacheManager.GetCacheFilePath(testUrl));

            // Act - Get statistics
            var stats = cacheManager.GetStatistics();
            Assert.AreEqual(1, stats.TotalEntries);
            Assert.AreEqual(1, stats.ValidEntries);
        }

        [Test]
        public void Workflow_IndexingWorker_ShouldTrackProgress()
        {
            // Arrange
            var indexer = new UnityDocumentationIndexer(tempDatabasePath);
            indexer.CreateDatabase();

            var parser = new HtmlDocumentationParser();
            var detector = new UnityInstallationDetector();
            var worker = new DocumentationIndexingWorker(indexer, parser, detector);

            // Assert - Initial state
            Assert.AreEqual(0, worker.ProcessedFiles);
            Assert.AreEqual(0, worker.SuccessfullyIndexed);
            Assert.IsFalse(worker.IsRunning);

            // Cleanup
            indexer.Dispose();
        }

        [Test]
        public void Workflow_FullTextSearch_WithBM25_ShouldRankCorrectly()
        {
            // Arrange
            var indexer = new UnityDocumentationIndexer(tempDatabasePath);
            indexer.CreateDatabase();

            // Index multiple entries
            var entries = new[]
            {
                new DocumentationEntry
                {
                    ClassName = "Transform",
                    MethodName = "Translate",
                    Description = "Moves the transform in the direction and distance of translation.",
                    UnityVersion = "2021.3"
                },
                new DocumentationEntry
                {
                    ClassName = "Transform",
                    MethodName = "Rotate",
                    Description = "Rotates the transform around axis.",
                    UnityVersion = "2021.3"
                },
                new DocumentationEntry
                {
                    ClassName = "GameObject",
                    MethodName = "Find",
                    Description = "Finds a GameObject by name.",
                    UnityVersion = "2021.3"
                }
            };

            foreach (var entry in entries)
            {
                indexer.IndexDocument(entry);
            }

            // Act - Query for "transform"
            var results = indexer.QueryDocumentation("transform");

            // Assert - Should prioritize Transform class results
            Assert.IsNotEmpty(results);
            Assert.AreEqual("Transform", results[0].ClassName, "BM25 should rank Transform higher");

            // Cleanup
            indexer.Dispose();
        }

        [Test]
        public void Workflow_FuzzySearch_WithCommonTypos_ShouldSuggest()
        {
            // Arrange
            var fuzzySearch = new FuzzyDocumentationSearch();
            var availableApis = new[]
            {
                "Transform.Translate",
                "Transform.Rotate",
                "GameObject.Find",
                "GetComponent"
            };

            // Act & Assert - Various typo scenarios
            var results1 = fuzzySearch.FindSimilarApis("Translte", availableApis, 0.7); // Missing 'a'
            Assert.Contains("Transform.Translate", results1);

            var results2 = fuzzySearch.FindSimilarApis("GetComponet", availableApis, 0.7); // Swapped letters
            Assert.Contains("GetComponent", results2);

            var results3 = fuzzySearch.FindSimilarApis("Roate", availableApis, 0.6); // Missing 't'
            Assert.Contains("Transform.Rotate", results3);
        }

        [Test]
        public void Workflow_EndToEnd_WithDeprecation_ShouldWarn()
        {
            // Arrange - Index a deprecated API
            var indexer = new UnityDocumentationIndexer(tempDatabasePath);
            indexer.CreateDatabase();

            var deprecatedEntry = new DocumentationEntry
            {
                ClassName = "Transform",
                MethodName = "OldMethod",
                Description = "This method is deprecated.",
                IsDeprecated = true,
                ReplacementApi = "NewMethod",
                UnityVersion = "2021.3"
            };

            indexer.IndexDocument(deprecatedEntry);

            // Act - Query and check deprecation
            var results = indexer.QueryDocumentation("OldMethod");

            // Assert
            Assert.IsNotEmpty(results);
            Assert.IsTrue(results[0].IsDeprecated);
            Assert.AreEqual("NewMethod", results[0].ReplacementApi);

            var deprecationDetector = new DeprecationDetector();
            var warning = deprecationDetector.GetDeprecationWarning(results[0], "2021.3");

            Assert.IsNotNull(warning);
            StringAssert.Contains("deprecated", warning.ToLower());
            StringAssert.Contains("NewMethod", warning);

            // Cleanup
            indexer.Dispose();
        }

        /// <summary>
        /// Creates a mock Unity documentation structure for testing.
        /// </summary>
        private void CreateMockDocumentationStructure()
        {
            var scriptRefPath = Path.Combine(tempDocPath, "ScriptReference");
            Directory.CreateDirectory(scriptRefPath);

            // Create Transform.Translate.html
            File.WriteAllText(Path.Combine(scriptRefPath, "Transform.Translate.html"), @"
<html>
<head><title>Unity - Scripting API: Transform.Translate</title></head>
<body>
    <h1>Transform.Translate</h1>
    <div class='signature'>
        <code>public void Translate(Vector3 translation, Space relativeTo = Space.Self);</code>
    </div>
    <div class='description'>
        <p>Moves the transform in the direction and distance of translation.</p>
    </div>
    <div class='example'>
        <pre><code>transform.Translate(Vector3.forward * Time.deltaTime);</code></pre>
    </div>
</body>
</html>");

            // Create Transform.Rotate.html
            File.WriteAllText(Path.Combine(scriptRefPath, "Transform.Rotate.html"), @"
<html>
<head><title>Unity - Scripting API: Transform.Rotate</title></head>
<body>
    <h1>Transform.Rotate</h1>
    <div class='signature'>
        <code>public void Rotate(Vector3 eulers, Space relativeTo = Space.Self);</code>
    </div>
    <div class='description'>
        <p>Rotates the transform around each axis by the specified angle.</p>
    </div>
</body>
</html>");

            // Create GameObject.Find.html
            File.WriteAllText(Path.Combine(scriptRefPath, "GameObject.Find.html"), @"
<html>
<head><title>Unity - Scripting API: GameObject.Find</title></head>
<body>
    <h1>GameObject.Find</h1>
    <div class='signature'>
        <code>public static GameObject Find(string name);</code>
    </div>
    <div class='description'>
        <p>Finds a GameObject by name and returns it.</p>
    </div>
</body>
</html>");
        }
    }
}
