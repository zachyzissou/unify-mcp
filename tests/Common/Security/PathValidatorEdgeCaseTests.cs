using NUnit.Framework;
using System;
using System.IO;
using UnifyMcp.Common.Security;

namespace UnifyMcp.Tests.Common.Security
{
    [TestFixture]
    public class PathValidatorEdgeCaseTests
    {
        private PathValidator validator;
        private string projectPath;

        [SetUp]
        public void SetUp()
        {
            projectPath = Path.Combine(Path.GetTempPath(), "EdgeCaseProject");
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
        public void IsValidPath_NullPath_ReturnsFalse()
        {
            // Act
            var result = validator.IsValidPath(null);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsValidPath_EmptyPath_ReturnsFalse()
        {
            // Act
            var result = validator.IsValidPath("");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsValidPath_WhitespacePath_ReturnsFalse()
        {
            // Act
            var result = validator.IsValidPath("   ");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsValidPath_PathWithMixedSeparators_ReturnsTrue()
        {
            // Arrange - mix forward and back slashes
            var mixedPath = Path.Combine(projectPath, "Assets") + "/Scripts\\Player.cs";

            // Act
            var result = validator.IsValidPath(mixedPath);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsValidPath_PathWithDotSegments_WithinProject_ReturnsTrue()
        {
            // Arrange - path with . and .. that stays within project
            var dottedPath = Path.Combine(projectPath, "Assets", "Scripts", "..", "Prefabs", "Player.prefab");

            // Act
            var result = validator.IsValidPath(dottedPath);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsValidPath_PathWithMultipleTraversals_EscapingProject_ReturnsFalse()
        {
            // Arrange - multiple ../ attempts to escape
            var maliciousPath = Path.Combine(projectPath, "..", "..", "..", "..", "etc", "passwd");

            // Act
            var result = validator.IsValidPath(maliciousPath);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void GetValidatedPath_ValidPath_ReturnsFullPath()
        {
            // Arrange
            var validPath = Path.Combine(projectPath, "Assets", "Prefab.prefab");

            // Act
            var result = validator.GetValidatedPath(validPath);

            // Assert
            Assert.That(result, Does.StartWith(projectPath));
            Assert.That(result, Does.EndWith("Prefab.prefab"));
        }

        [Test]
        public void ValidateOrThrow_ProjectRootPath_DoesNotThrow()
        {
            // Act & Assert - project root itself should be valid
            Assert.DoesNotThrow(() => validator.ValidateOrThrow(projectPath));
        }
    }
}
