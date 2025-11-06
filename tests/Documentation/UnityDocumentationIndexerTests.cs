using NUnit.Framework;
using System;
using System.IO;
using System.Data.SQLite;
using UnityMcp.Tools.Documentation;

namespace UnifyMcp.Tests.Documentation
{
    /// <summary>
    /// Tests for UnityDocumentationIndexer with SQLite FTS5 schema.
    /// Tests database creation, FTS5 table schema, index/query operations.
    /// </summary>
    [TestFixture]
    public class UnityDocumentationIndexerTests
    {
        private string testDatabasePath;
        private UnityDocumentationIndexer indexer;

        [SetUp]
        public void SetUp()
        {
            // Create temporary database for testing
            testDatabasePath = Path.Combine(Path.GetTempPath(), $"test_unity_docs_{Guid.NewGuid()}.db");
            indexer = new UnityDocumentationIndexer(testDatabasePath);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test database
            indexer?.Dispose();
            if (File.Exists(testDatabasePath))
            {
                File.Delete(testDatabasePath);
            }
        }

        [Test]
        public void CreateDatabase_ShouldCreateDatabaseFile()
        {
            // Act
            indexer.CreateDatabase();

            // Assert
            Assert.IsTrue(File.Exists(testDatabasePath), "Database file should be created");
        }

        [Test]
        public void CreateDatabase_ShouldCreateFTS5Table()
        {
            // Act
            indexer.CreateDatabase();

            // Assert
            using (var connection = new SQLiteConnection($"Data Source={testDatabasePath};Version=3;"))
            {
                connection.Open();
                using (var command = new SQLiteCommand(
                    "SELECT name FROM sqlite_master WHERE type='table' AND name='documentation_fts5';",
                    connection))
                {
                    var result = command.ExecuteScalar();
                    Assert.IsNotNull(result, "FTS5 table 'documentation_fts5' should exist");
                    Assert.AreEqual("documentation_fts5", result.ToString());
                }
            }
        }

        [Test]
        public void CreateDatabase_ShouldUseFTS5WithPorterTokenizer()
        {
            // Act
            indexer.CreateDatabase();

            // Assert
            using (var connection = new SQLiteConnection($"Data Source={testDatabasePath};Version=3;"))
            {
                connection.Open();
                using (var command = new SQLiteCommand(
                    "SELECT sql FROM sqlite_master WHERE type='table' AND name='documentation_fts5';",
                    connection))
                {
                    var sql = command.ExecuteScalar()?.ToString();
                    Assert.IsNotNull(sql, "Table definition should exist");
                    StringAssert.Contains("USING fts5", sql, "Should use FTS5 virtual table");
                    StringAssert.Contains("tokenize", sql, "Should specify tokenizer");
                }
            }
        }

        [Test]
        public void IndexDocument_ShouldInsertDocumentIntoDatabase()
        {
            // Arrange
            indexer.CreateDatabase();
            var entry = new DocumentationEntry
            {
                ClassName = "Transform",
                MethodName = "Translate",
                ReturnType = "void",
                Parameters = new[] { "Vector3 translation", "Space relativeTo" },
                Description = "Moves the transform in the direction and distance of translation.",
                CodeExamples = new[] { "transform.Translate(Vector3.forward * Time.deltaTime);" },
                UnityVersion = "2021.3",
                DocumentationUrl = "https://docs.unity3d.com/ScriptReference/Transform.Translate.html"
            };

            // Act
            indexer.IndexDocument(entry);

            // Assert
            using (var connection = new SQLiteConnection($"Data Source={testDatabasePath};Version=3;"))
            {
                connection.Open();
                using (var command = new SQLiteCommand(
                    "SELECT COUNT(*) FROM documentation_fts5 WHERE class_name='Transform';",
                    connection))
                {
                    var count = Convert.ToInt32(command.ExecuteScalar());
                    Assert.AreEqual(1, count, "Should have one indexed document");
                }
            }
        }

        [Test]
        public void QueryDocumentation_ExactMatch_ShouldReturnResults()
        {
            // Arrange
            indexer.CreateDatabase();
            indexer.IndexDocument(new DocumentationEntry
            {
                ClassName = "Transform",
                MethodName = "Translate",
                Description = "Moves the transform in the direction and distance of translation."
            });

            // Act
            var results = indexer.QueryDocumentation("Transform.Translate");

            // Assert
            Assert.IsNotNull(results, "Results should not be null");
            Assert.IsNotEmpty(results, "Should return at least one result");
            Assert.AreEqual("Transform", results[0].ClassName);
            Assert.AreEqual("Translate", results[0].MethodName);
        }

        [Test]
        public void QueryDocumentation_PartialMatch_ShouldReturnResults()
        {
            // Arrange
            indexer.CreateDatabase();
            indexer.IndexDocument(new DocumentationEntry
            {
                ClassName = "GameObject",
                MethodName = "FindGameObjectsWithTag",
                Description = "Returns an array of active GameObjects tagged with tag."
            });

            // Act
            var results = indexer.QueryDocumentation("FindGameObjects");

            // Assert
            Assert.IsNotEmpty(results, "Should return results for partial match");
            Assert.AreEqual("FindGameObjectsWithTag", results[0].MethodName);
        }

        [Test]
        public void QueryDocumentation_BM25Ranking_ShouldOrderByRelevance()
        {
            // Arrange
            indexer.CreateDatabase();
            indexer.IndexDocument(new DocumentationEntry
            {
                ClassName = "Transform",
                MethodName = "Translate",
                Description = "Moves the transform."
            });
            indexer.IndexDocument(new DocumentationEntry
            {
                ClassName = "Transform",
                MethodName = "position",
                Description = "The position of the transform in world space."
            });

            // Act
            var results = indexer.QueryDocumentation("transform position");

            // Assert
            Assert.IsNotEmpty(results, "Should return results");
            // First result should be 'position' property as it matches both terms better
            Assert.AreEqual("position", results[0].MethodName, "Should rank by relevance (BM25)");
        }

        [Test]
        public void QueryDocumentation_NoMatches_ShouldReturnEmptyList()
        {
            // Arrange
            indexer.CreateDatabase();
            indexer.IndexDocument(new DocumentationEntry
            {
                ClassName = "Transform",
                MethodName = "Translate"
            });

            // Act
            var results = indexer.QueryDocumentation("NonExistentMethod");

            // Assert
            Assert.IsNotNull(results, "Results should not be null");
            Assert.IsEmpty(results, "Should return empty list for no matches");
        }

        [Test]
        public void IndexDocument_MultipleDocuments_ShouldIndexAll()
        {
            // Arrange
            indexer.CreateDatabase();
            var entries = new[]
            {
                new DocumentationEntry { ClassName = "Transform", MethodName = "Translate" },
                new DocumentationEntry { ClassName = "Transform", MethodName = "Rotate" },
                new DocumentationEntry { ClassName = "GameObject", MethodName = "Find" }
            };

            // Act
            foreach (var entry in entries)
            {
                indexer.IndexDocument(entry);
            }

            // Assert
            var results = indexer.QueryDocumentation("Transform");
            Assert.AreEqual(2, results.Count, "Should return 2 Transform methods");
        }

        [Test]
        public void CreateDatabase_ShouldCreateVersionMetadataTable()
        {
            // Act
            indexer.CreateDatabase();

            // Assert
            using (var connection = new SQLiteConnection($"Data Source={testDatabasePath};Version=3;"))
            {
                connection.Open();
                using (var command = new SQLiteCommand(
                    "SELECT name FROM sqlite_master WHERE type='table' AND name='metadata';",
                    connection))
                {
                    var result = command.ExecuteScalar();
                    Assert.IsNotNull(result, "Metadata table should exist for version tracking");
                }
            }
        }

        [Test]
        public void QueryDocumentation_WithPerformanceTest_ShouldCompleteWithin100ms()
        {
            // Arrange
            indexer.CreateDatabase();
            for (int i = 0; i < 100; i++)
            {
                indexer.IndexDocument(new DocumentationEntry
                {
                    ClassName = $"Class{i}",
                    MethodName = $"Method{i}",
                    Description = $"Description for method {i}"
                });
            }

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var results = indexer.QueryDocumentation("Method50");
            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 100,
                "Cached query should complete within 100ms (FR-001 requirement)");
            Assert.IsNotEmpty(results, "Should find the queried method");
        }
    }
}
