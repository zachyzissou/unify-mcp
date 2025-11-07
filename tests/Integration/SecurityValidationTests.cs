using NUnit.Framework;
using System;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using UnifyMcp.Common.Security;
using UnifyMcp.Tools.Assets;
using UnifyMcp.Tools.Scene;

namespace UnifyMcp.Tests.Integration
{
    /// <summary>
    /// Integration tests for security validation features.
    /// Tests path traversal attack prevention in AssetTools and SceneTools.
    /// </summary>
    [TestFixture]
    public class SecurityValidationTests
    {
        private string testProjectPath;
        private PathValidator pathValidator;
        private AssetTools assetTools;
        private SceneTools sceneTools;

        [SetUp]
        public void SetUp()
        {
            testProjectPath = Path.Combine(Path.GetTempPath(), "TestSecurityProject");
            Directory.CreateDirectory(testProjectPath);

            pathValidator = new PathValidator(testProjectPath);
            assetTools = new AssetTools(pathValidator);
            sceneTools = new SceneTools(pathValidator);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(testProjectPath))
                Directory.Delete(testProjectPath, true);
        }

        [Test]
        public void AssetTools_PathTraversal_ThrowsSecurityException()
        {
            // Arrange
            var maliciousPath = Path.Combine(testProjectPath, "..", "..", "etc", "passwd");

            // Act & Assert
            var ex = Assert.ThrowsAsync<SecurityException>(async () =>
            {
                await assetTools.AnalyzeAssetDependencies(maliciousPath);
            });

            Assert.That(ex.Message, Does.Contain("outside project"));
        }

        [Test]
        public void SceneTools_PathTraversal_ThrowsSecurityException()
        {
            // Arrange
            var maliciousPath = "../../malicious.scene";

            // Act & Assert
            var ex = Assert.ThrowsAsync<SecurityException>(async () =>
            {
                await sceneTools.ValidateScene(maliciousPath);
            });

            Assert.That(ex.Message, Does.Contain("outside project"));
        }

        [Test]
        public void AssetTools_ValidPath_DoesNotThrow()
        {
            // Arrange
            var validPath = Path.Combine(testProjectPath, "Assets", "Prefab.prefab");

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
            {
                await assetTools.AnalyzeAssetDependencies(validPath);
            });
        }

        [Test]
        public void SceneTools_ValidPath_DoesNotThrow()
        {
            // Arrange
            var validPath = Path.Combine(testProjectPath, "Scenes", "Main.unity");

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
            {
                await sceneTools.ValidateScene(validPath);
            });
        }

        [Test]
        public void AssetTools_AbsolutePathOutsideProject_ThrowsSecurityException()
        {
            // Arrange
            var outsidePath = "/usr/bin/malicious.exe";

            // Act & Assert
            var ex = Assert.ThrowsAsync<SecurityException>(async () =>
            {
                await assetTools.AnalyzeAssetDependencies(outsidePath);
            });

            Assert.That(ex.Message, Does.Contain("outside project"));
        }

        [Test]
        public void SceneTools_AbsolutePathOutsideProject_ThrowsSecurityException()
        {
            // Arrange
            var outsidePath = "C:\\Windows\\System32\\malicious.scene";

            // Act & Assert
            var ex = Assert.ThrowsAsync<SecurityException>(async () =>
            {
                await sceneTools.ValidateScene(outsidePath);
            });

            Assert.That(ex.Message, Does.Contain("outside project"));
        }
    }
}
