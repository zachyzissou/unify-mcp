using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using UnifyMcp.Tools.Documentation;

namespace UnifyMcp.Tests.Documentation
{
    /// <summary>
    /// Tests for Unity installation detection across Windows, macOS, and Linux platforms.
    /// Validates path detection, version parsing, and documentation folder validation.
    /// </summary>
    [TestFixture]
    public class UnityInstallationDetectorTests
    {
        private UnityInstallationDetector detector;
        private string tempTestDir;

        [SetUp]
        public void SetUp()
        {
            detector = new UnityInstallationDetector();
            tempTestDir = Path.Combine(Path.GetTempPath(), $"UnifyMcpTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempTestDir);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up temporary test directories
            if (Directory.Exists(tempTestDir))
            {
                Directory.Delete(tempTestDir, recursive: true);
            }
        }

        [Test]
        public void DetectUnityInstallations_OnCurrentPlatform_ShouldRunWithoutError()
        {
            // Act
            var installations = detector.DetectUnityInstallations();

            // Assert
            Assert.IsNotNull(installations, "Should return a list (even if empty)");
            // Note: May be empty if Unity not installed, but should not throw
        }

        [Test]
        public void DetectUnityInstallations_WithMultipleVersions_ShouldOrderByVersionDescending()
        {
            // Note: This test validates the ordering logic
            // In a real scenario with multiple Unity installations, newest should be first

            // Act
            var installations = detector.DetectUnityInstallations();

            // Assert
            if (installations.Count > 1)
            {
                for (int i = 0; i < installations.Count - 1; i++)
                {
                    var current = installations[i].Version;
                    var next = installations[i + 1].Version;

                    // Verify descending order (current >= next)
                    Assert.That(string.Compare(current, next, StringComparison.Ordinal) >= 0,
                        $"Versions should be ordered descending: {current} should come before {next}");
                }
            }
        }

        [Test]
        public void GetScriptReferenceFiles_WithValidPath_ShouldReturnHtmlFiles()
        {
            // Arrange - Create mock Unity documentation structure
            var mockDocPath = Path.Combine(tempTestDir, "Documentation");
            var mockScriptRefPath = Path.Combine(mockDocPath, "ScriptReference");
            Directory.CreateDirectory(mockScriptRefPath);

            // Create some mock HTML files
            File.WriteAllText(Path.Combine(mockScriptRefPath, "Transform.html"), "<html>Transform</html>");
            File.WriteAllText(Path.Combine(mockScriptRefPath, "GameObject.html"), "<html>GameObject</html>");

            // Create nested directory structure
            var nestedPath = Path.Combine(mockScriptRefPath, "UnityEngine");
            Directory.CreateDirectory(nestedPath);
            File.WriteAllText(Path.Combine(nestedPath, "Vector3.html"), "<html>Vector3</html>");

            // Act
            var htmlFiles = detector.GetScriptReferenceFiles(mockDocPath);

            // Assert
            Assert.AreEqual(3, htmlFiles.Count, "Should find all HTML files recursively");
            Assert.IsTrue(htmlFiles.Any(f => f.Contains("Transform.html")), "Should include Transform.html");
            Assert.IsTrue(htmlFiles.Any(f => f.Contains("GameObject.html")), "Should include GameObject.html");
            Assert.IsTrue(htmlFiles.Any(f => f.Contains("Vector3.html")), "Should include nested Vector3.html");
        }

        [Test]
        public void GetScriptReferenceFiles_WithNonExistentPath_ShouldReturnEmptyList()
        {
            // Arrange
            var nonExistentPath = Path.Combine(tempTestDir, "NonExistent");

            // Act
            var htmlFiles = detector.GetScriptReferenceFiles(nonExistentPath);

            // Assert
            Assert.IsEmpty(htmlFiles, "Should return empty list for non-existent path");
        }

        [Test]
        public void GetScriptReferenceFiles_WithNullPath_ShouldReturnEmptyList()
        {
            // Act
            var htmlFiles = detector.GetScriptReferenceFiles(null);

            // Assert
            Assert.IsEmpty(htmlFiles, "Should return empty list for null path");
        }

        [Test]
        public void GetScriptReferenceFiles_WithEmptyPath_ShouldReturnEmptyList()
        {
            // Act
            var htmlFiles = detector.GetScriptReferenceFiles(string.Empty);

            // Assert
            Assert.IsEmpty(htmlFiles, "Should return empty list for empty path");
        }

        [Test]
        public void GetScriptReferenceFiles_WithoutScriptReferenceFolder_ShouldReturnEmptyList()
        {
            // Arrange - Create documentation folder but no ScriptReference subfolder
            var mockDocPath = Path.Combine(tempTestDir, "Documentation");
            Directory.CreateDirectory(mockDocPath);

            // Act
            var htmlFiles = detector.GetScriptReferenceFiles(mockDocPath);

            // Assert
            Assert.IsEmpty(htmlFiles, "Should return empty list when ScriptReference folder missing");
        }

        [Test]
        public void ValidateInstallation_WithValidInstallation_ShouldReturnTrue()
        {
            // Arrange - Create mock Unity installation structure
            var mockDocPath = Path.Combine(tempTestDir, "Documentation");
            var mockScriptRefPath = Path.Combine(mockDocPath, "ScriptReference");
            Directory.CreateDirectory(mockScriptRefPath);
            File.WriteAllText(Path.Combine(mockScriptRefPath, "Transform.html"), "<html>Transform</html>");

            var installation = new UnityInstallation
            {
                Version = "2021.3.25f1",
                EditorPath = tempTestDir,
                DocumentationPath = mockDocPath,
                Platform = "Windows"
            };

            // Act
            var isValid = detector.ValidateInstallation(installation);

            // Assert
            Assert.IsTrue(isValid, "Should validate installation with documentation and HTML files");
        }

        [Test]
        public void ValidateInstallation_WithMissingDocumentationPath_ShouldReturnFalse()
        {
            // Arrange
            var installation = new UnityInstallation
            {
                Version = "2021.3.25f1",
                EditorPath = tempTestDir,
                DocumentationPath = Path.Combine(tempTestDir, "NonExistent"),
                Platform = "Windows"
            };

            // Act
            var isValid = detector.ValidateInstallation(installation);

            // Assert
            Assert.IsFalse(isValid, "Should not validate installation with missing documentation path");
        }

        [Test]
        public void ValidateInstallation_WithMissingScriptReferenceFolder_ShouldReturnFalse()
        {
            // Arrange - Create documentation folder but no ScriptReference
            var mockDocPath = Path.Combine(tempTestDir, "Documentation");
            Directory.CreateDirectory(mockDocPath);

            var installation = new UnityInstallation
            {
                Version = "2021.3.25f1",
                EditorPath = tempTestDir,
                DocumentationPath = mockDocPath,
                Platform = "Windows"
            };

            // Act
            var isValid = detector.ValidateInstallation(installation);

            // Assert
            Assert.IsFalse(isValid, "Should not validate installation without ScriptReference folder");
        }

        [Test]
        public void ValidateInstallation_WithEmptyScriptReferenceFolder_ShouldReturnFalse()
        {
            // Arrange - Create folders but no HTML files
            var mockDocPath = Path.Combine(tempTestDir, "Documentation");
            var mockScriptRefPath = Path.Combine(mockDocPath, "ScriptReference");
            Directory.CreateDirectory(mockScriptRefPath);

            var installation = new UnityInstallation
            {
                Version = "2021.3.25f1",
                EditorPath = tempTestDir,
                DocumentationPath = mockDocPath,
                Platform = "Windows"
            };

            // Act
            var isValid = detector.ValidateInstallation(installation);

            // Assert
            Assert.IsFalse(isValid, "Should not validate installation with empty ScriptReference folder");
        }

        [Test]
        public void ValidateInstallation_WithNullInstallation_ShouldReturnFalse()
        {
            // Act
            var isValid = detector.ValidateInstallation(null);

            // Assert
            Assert.IsFalse(isValid, "Should not validate null installation");
        }

        [Test]
        public void UnityInstallation_ToString_ShouldFormatCorrectly()
        {
            // Arrange
            var installation = new UnityInstallation
            {
                Version = "2021.3.25f1",
                EditorPath = "/path/to/editor",
                DocumentationPath = "/path/to/docs",
                Platform = "macOS"
            };

            // Act
            var result = installation.ToString();

            // Assert
            Assert.IsTrue(result.Contains("2021.3.25f1"), "Should include version");
            Assert.IsTrue(result.Contains("macOS"), "Should include platform");
            Assert.IsTrue(result.Contains("/path/to/docs"), "Should include documentation path");
        }

        [Test]
        public void GetScriptReferenceFiles_PerformanceTest_ShouldHandleLargeDirectories()
        {
            // Arrange - Create many HTML files to test performance
            var mockDocPath = Path.Combine(tempTestDir, "Documentation");
            var mockScriptRefPath = Path.Combine(mockDocPath, "ScriptReference");
            Directory.CreateDirectory(mockScriptRefPath);

            // Create 100 HTML files
            for (int i = 0; i < 100; i++)
            {
                File.WriteAllText(
                    Path.Combine(mockScriptRefPath, $"Class{i}.html"),
                    $"<html>Class{i}</html>"
                );
            }

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var htmlFiles = detector.GetScriptReferenceFiles(mockDocPath);
            stopwatch.Stop();

            // Assert
            Assert.AreEqual(100, htmlFiles.Count, "Should find all 100 HTML files");
            Assert.Less(stopwatch.ElapsedMilliseconds, 500,
                "Should scan 100 files quickly (<500ms)");
        }

        [Test]
        public void GetScriptReferenceFiles_WithNestedDirectories_ShouldScanRecursively()
        {
            // Arrange - Create nested directory structure
            var mockDocPath = Path.Combine(tempTestDir, "Documentation");
            var mockScriptRefPath = Path.Combine(mockDocPath, "ScriptReference");

            // Create multiple levels of nesting
            var level1 = Path.Combine(mockScriptRefPath, "UnityEngine");
            var level2 = Path.Combine(level1, "UI");
            var level3 = Path.Combine(level2, "Components");

            Directory.CreateDirectory(level3);

            // Create files at different levels
            File.WriteAllText(Path.Combine(mockScriptRefPath, "Root.html"), "<html>Root</html>");
            File.WriteAllText(Path.Combine(level1, "Level1.html"), "<html>Level1</html>");
            File.WriteAllText(Path.Combine(level2, "Level2.html"), "<html>Level2</html>");
            File.WriteAllText(Path.Combine(level3, "Level3.html"), "<html>Level3</html>");

            // Act
            var htmlFiles = detector.GetScriptReferenceFiles(mockDocPath);

            // Assert
            Assert.AreEqual(4, htmlFiles.Count, "Should find files at all directory levels");
        }

        [Test]
        public void GetScriptReferenceFiles_WithMixedFileTypes_ShouldOnlyReturnHtml()
        {
            // Arrange
            var mockDocPath = Path.Combine(tempTestDir, "Documentation");
            var mockScriptRefPath = Path.Combine(mockDocPath, "ScriptReference");
            Directory.CreateDirectory(mockScriptRefPath);

            // Create HTML and non-HTML files
            File.WriteAllText(Path.Combine(mockScriptRefPath, "Valid.html"), "<html>Valid</html>");
            File.WriteAllText(Path.Combine(mockScriptRefPath, "Image.png"), "PNG data");
            File.WriteAllText(Path.Combine(mockScriptRefPath, "Style.css"), "CSS data");
            File.WriteAllText(Path.Combine(mockScriptRefPath, "Script.js"), "JS data");
            File.WriteAllText(Path.Combine(mockScriptRefPath, "ReadMe.txt"), "Text data");

            // Act
            var htmlFiles = detector.GetScriptReferenceFiles(mockDocPath);

            // Assert
            Assert.AreEqual(1, htmlFiles.Count, "Should only return .html files");
            Assert.IsTrue(htmlFiles[0].EndsWith("Valid.html"), "Should return the HTML file");
        }
    }
}
