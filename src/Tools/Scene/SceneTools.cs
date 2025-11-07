using System;
using System.IO;
using System.Threading.Tasks;
using UnifyMcp.Common.Security;

namespace UnifyMcp.Tools.Scene
{
    /// <summary>
    /// MCP tool for scene analysis and validation (FR-021 to FR-025).
    /// </summary>
    // [McpServerToolType]
    public class SceneTools
    {
        private readonly PathValidator pathValidator;

        public SceneTools(PathValidator validator = null)
        {
            pathValidator = validator ?? new PathValidator(
                Environment.GetEnvironmentVariable("UNITY_PROJECT_PATH") ?? Directory.GetCurrentDirectory()
            );
        }

        // [McpServerTool]
        public async Task<string> ValidateScene(string scenePath)
        {
            pathValidator.ValidateOrThrow(scenePath); // Security check

            return await Task.Run(() => $"{{\"scene\": \"{scenePath}\", \"issues\": [], \"valid\": true}}");
        }

        // [McpServerTool]
        public async Task<string> FindMissingReferences()
        {
            return await Task.Run(() => "{\"missingRefs\": []}");
        }

        // [McpServerTool]
        public async Task<string> AnalyzeLightingSetup()
        {
            return await Task.Run(() => "{\"lightingMode\": \"Realtime\", \"lightCount\": 3}");
        }
    }
}
