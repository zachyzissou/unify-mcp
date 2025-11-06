using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UnifyMcp.Tools.Documentation;

namespace UnifyMcp.Tests.Tools.Documentation
{
    /// <summary>
    /// Tests for QueryDocumentation MCP tool (FR-001).
    /// Tests exact match queries, JSON response format, method signatures/parameters/examples, and <100ms cached queries.
    /// </summary>
    [TestFixture]
    public class QueryDocumentationTests
    {
        private string tempDatabasePath;
        private DocumentationTools tools;

        [SetUp]
        public void SetUp()
        {
            tempDatabasePath = Path.Combine(Path.GetTempPath(), $"test_query_{Guid.NewGuid()}.db");
            tools = new DocumentationTools(tempDatabasePath);

            // Index test data
            var indexer = new UnityDocumentationIndexer(tempDatabasePath);
            indexer.CreateDatabase();

            indexer.IndexDocument(new DocumentationEntry
            {
                ClassName = "Transform",
                MethodName = "Translate",
                ReturnType = "void",
                Parameters = new[] { "Vector3 translation", "Space relativeTo" },
                Description = "Moves the transform in the direction and distance of translation.",
                CodeExamples = new[] { "transform.Translate(Vector3.forward * Time.deltaTime);" },
                UnityVersion = "2021.3",
                DocumentationUrl = "https://docs.unity3d.com/ScriptReference/Transform.Translate.html",
                IsDeprecated = false
            });

            indexer.IndexDocument(new DocumentationEntry
            {
                ClassName = "Transform",
                MethodName = "Rotate",
                ReturnType = "void",
                Parameters = new[] { "Vector3 eulers", "Space relativeTo" },
                Description = "Rotates the transform around each axis.",
                CodeExamples = new[] { "transform.Rotate(Vector3.up * Time.deltaTime);" },
                UnityVersion = "2021.3",
                DocumentationUrl = "https://docs.unity3d.com/ScriptReference/Transform.Rotate.html",
                IsDeprecated = false
            });

            indexer.Dispose();
        }

        [TearDown]
        public void TearDown()
        {
            tools?.Dispose();
            if (File.Exists(tempDatabasePath))
                File.Delete(tempDatabasePath);
        }

        [Test]
        public async Task QueryDocumentation_ExactMatch_ShouldReturnResults()
        {
            // Act
            var json = await tools.QueryDocumentation("Transform");

            // Assert
            Assert.IsNotNull(json);
            Assert.IsNotEmpty(json);

            var results = JsonSerializer.Deserialize<JsonElement[]>(json);
            Assert.IsNotEmpty(results);
            Assert.AreEqual("Transform", results[0].GetProperty("className").GetString());
        }

        [Test]
        public async Task QueryDocumentation_ShouldReturnMethodSignature()
        {
            // Act
            var json = await tools.QueryDocumentation("Transform.Translate");

            // Assert
            var results = JsonSerializer.Deserialize<JsonElement[]>(json);
            Assert.IsNotEmpty(results);

            var first = results[0];
            Assert.AreEqual("Transform", first.GetProperty("className").GetString());
            Assert.AreEqual("Translate", first.GetProperty("methodName").GetString());
            Assert.AreEqual("void", first.GetProperty("returnType").GetString());
        }

        [Test]
        public async Task QueryDocumentation_ShouldReturnParameters()
        {
            // Act
            var json = await tools.QueryDocumentation("Transform.Translate");

            // Assert
            var results = JsonSerializer.Deserialize<JsonElement[]>(json);
            var first = results[0];

            var parameters = first.GetProperty("parameters");
            Assert.AreEqual(2, parameters.GetArrayLength());
            Assert.AreEqual("Vector3 translation", parameters[0].GetString());
            Assert.AreEqual("Space relativeTo", parameters[1].GetString());
        }

        [Test]
        public async Task QueryDocumentation_ShouldReturnDescription()
        {
            // Act
            var json = await tools.QueryDocumentation("Transform.Translate");

            // Assert
            var results = JsonSerializer.Deserialize<JsonElement[]>(json);
            var first = results[0];

            var description = first.GetProperty("description").GetString();
            StringAssert.Contains("Moves the transform", description);
        }

        [Test]
        public async Task QueryDocumentation_ShouldReturnCodeExamples()
        {
            // Act
            var json = await tools.QueryDocumentation("Transform.Translate");

            // Assert
            var results = JsonSerializer.Deserialize<JsonElement[]>(json);
            var first = results[0];

            var codeExamples = first.GetProperty("codeExamples");
            Assert.AreEqual(1, codeExamples.GetArrayLength());
            StringAssert.Contains("transform.Translate", codeExamples[0].GetString());
        }

        [Test]
        public async Task QueryDocumentation_CachedQuery_ShouldCompleteUnder100ms()
        {
            // Arrange - Prime the cache
            await tools.QueryDocumentation("Transform");

            // Act - Measure cached query time
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await tools.QueryDocumentation("Transform");
            stopwatch.Stop();

            // Assert - FR-001 requirement: <100ms for cached queries
            Assert.Less(stopwatch.ElapsedMilliseconds, 100,
                "Cached queries should complete in <100ms (FR-001)");
        }

        [Test]
        public async Task QueryDocumentation_EmptyQuery_ShouldReturnEmptyArray()
        {
            // Act
            var json = await tools.QueryDocumentation("");

            // Assert
            Assert.AreEqual("[]", json);
        }

        [Test]
        public async Task QueryDocumentation_NoResults_ShouldReturnEmptyArray()
        {
            // Act
            var json = await tools.QueryDocumentation("NonExistentApiMethod");

            // Assert
            var results = JsonSerializer.Deserialize<JsonElement[]>(json);
            Assert.IsEmpty(results);
        }

        [Test]
        public async Task QueryDocumentation_MultipleResults_ShouldReturnAll()
        {
            // Act
            var json = await tools.QueryDocumentation("Transform");

            // Assert
            var results = JsonSerializer.Deserialize<JsonElement[]>(json);
            Assert.GreaterOrEqual(results.Length, 2, "Should find multiple Transform methods");
        }

        [Test]
        public async Task QueryDocumentation_ShouldIncludeUnityVersion()
        {
            // Act
            var json = await tools.QueryDocumentation("Transform.Translate");

            // Assert
            var results = JsonSerializer.Deserialize<JsonElement[]>(json);
            var first = results[0];

            var unityVersion = first.GetProperty("unityVersion").GetString();
            Assert.AreEqual("2021.3", unityVersion);
        }

        [Test]
        public async Task QueryDocumentation_ShouldIncludeDocumentationUrl()
        {
            // Act
            var json = await tools.QueryDocumentation("Transform.Translate");

            // Assert
            var results = JsonSerializer.Deserialize<JsonElement[]>(json);
            var first = results[0];

            var url = first.GetProperty("documentationUrl").GetString();
            StringAssert.Contains("docs.unity3d.com", url);
        }
    }
}
