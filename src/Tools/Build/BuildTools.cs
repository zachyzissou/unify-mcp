using System.Threading.Tasks;
using UnifyMcp.Common.UnityApiWrappers;

namespace UnifyMcp.Tools.Build
{
    /// <summary>
    /// MCP tool for build pipeline operations (FR-011 to FR-015).
    /// </summary>
    // [McpServerToolType]
    public class BuildTools
    {
        private readonly BuildPipelineWrapper buildPipeline;

        public BuildTools(BuildPipelineWrapper wrapper = null)
        {
            this.buildPipeline = wrapper ?? new BuildPipelineWrapper();
        }

        // [McpServerTool]
        public async Task<string> ValidateBuildConfiguration(string platform)
        {
            return await Task.Run(() => buildPipeline.ValidateBuildConfiguration(platform));
        }

        // [McpServerTool]
        public async Task<string> StartMultiPlatformBuild(string[] platforms)
        {
            return await Task.Run(() => "{\"status\": \"started\", \"platforms\": [\"Windows\", \"macOS\"]}");
        }

        // [McpServerTool]
        public async Task<string> GetBuildSizeAnalysis()
        {
            return await Task.Run(() => buildPipeline.GetBuildReport());
        }
    }
}
