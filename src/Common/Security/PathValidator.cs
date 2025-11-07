using System;
using System.IO;
using System.Security;

namespace UnifyMcp.Common.Security
{
    /// <summary>
    /// Validates file paths to prevent path traversal attacks.
    /// </summary>
    public class PathValidator
    {
        private readonly string projectRootPath;

        public PathValidator(string projectRoot)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new ArgumentException("Project root cannot be null or empty", nameof(projectRoot));

            projectRootPath = Path.GetFullPath(projectRoot);
        }

        /// <summary>
        /// Checks if a path is within the project directory.
        /// </summary>
        public bool IsValidPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                var fullPath = Path.GetFullPath(path);
                return fullPath.StartsWith(projectRootPath, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates a path or throws SecurityException.
        /// </summary>
        public void ValidateOrThrow(string path)
        {
            if (!IsValidPath(path))
            {
                throw new SecurityException(
                    $"Path '{path}' is outside project directory or invalid. " +
                    $"All paths must be within '{projectRootPath}'."
                );
            }
        }

        /// <summary>
        /// Gets the validated full path.
        /// </summary>
        public string GetValidatedPath(string path)
        {
            ValidateOrThrow(path);
            return Path.GetFullPath(path);
        }
    }
}
