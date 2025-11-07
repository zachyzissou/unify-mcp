using NUnit.Framework;
using System;
using System.IO;
using UnifyMcp.Common.Security;

namespace UnifyMcp.Tests.Common.Security
{
    [TestFixture]
    public class PathValidatorTests
    {
        private PathValidator validator;
        private string projectPath;

        [SetUp]
        public void SetUp()
        {
            projectPath = Path.Combine(Path.GetTempPath(), "TestProject");
            Directory.CreateDirectory(projectPath);
            validator = new PathValidator(projectPath);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(projectPath))
                Directory.Delete(projectPath, true);
        }

        [Test]
        public void IsValidPath_PathWithinProject_ReturnsTrue()
        {
            // Arrange
            var validPath = Path.Combine(projectPath, "Assets", "Scripts", "Player.cs");

            // Act
            var result = validator.IsValidPath(validPath);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsValidPath_PathTraversal_ReturnsFalse()
        {
            // Arrange
            var maliciousPath = Path.Combine(projectPath, "..", "..", "etc", "passwd");

            // Act
            var result = validator.IsValidPath(maliciousPath);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsValidPath_AbsolutePathOutsideProject_ReturnsFalse()
        {
            // Arrange
            var outsidePath = "/usr/bin/malicious.exe";

            // Act
            var result = validator.IsValidPath(outsidePath);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ValidateOrThrow_ValidPath_DoesNotThrow()
        {
            // Arrange
            var validPath = Path.Combine(projectPath, "Assets", "Prefabs", "Player.prefab");

            // Act & Assert
            Assert.DoesNotThrow(() => validator.ValidateOrThrow(validPath));
        }

        [Test]
        public void ValidateOrThrow_InvalidPath_ThrowsSecurityException()
        {
            // Arrange
            var maliciousPath = Path.Combine(projectPath, "..", "outside.txt");

            // Act & Assert
            var ex = Assert.Throws<System.Security.SecurityException>(
                () => validator.ValidateOrThrow(maliciousPath)
            );
            Assert.That(ex.Message, Does.Contain("outside project"));
        }
    }
}
