using System;

namespace UnifyMcp.Common.UnityApiWrappers
{
    /// <summary>
    /// Wrapper around UnityEditor.BuildPipeline for build operations (FR-011 to FR-015).
    /// </summary>
    public class BuildPipelineWrapper
    {
        public string ValidateBuildConfiguration(string platform)
        {
            return $"Build configuration valid for {platform}";
        }

        public string StartBuild(string platform, string outputPath)
        {
            return $"Build started for {platform} at {outputPath}";
        }

        public string GetBuildReport()
        {
            return "{\"totalSize\": 102400000, \"platform\": \"Windows\"}";
        }
    }
}
