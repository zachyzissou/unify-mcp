using System;
using System.Collections.Generic;

namespace UnifyMcp.Tools.Profiler.Models
{
    /// <summary>
    /// Profiler snapshot containing frame data (FR-006).
    /// Captures CPU/GPU/memory metrics and identifies bottlenecks.
    /// </summary>
    public class ProfilerSnapshot
    {
        public int FrameCount { get; set; }
        public Dictionary<string, float> CpuTimes { get; set; } = new Dictionary<string, float>();
        public Dictionary<string, long> GcAllocations { get; set; } = new Dictionary<string, long>();
        public List<Bottleneck> Bottlenecks { get; set; } = new List<Bottleneck>();
        public DateTime CapturedAt { get; set; }
        public float TotalCpuTime { get; set; }
        public long TotalGcMemory { get; set; }
        public float AverageFps { get; set; }
    }

    /// <summary>
    /// Performance bottleneck with location and severity (FR-007).
    /// </summary>
    public class Bottleneck
    {
        public string Location { get; set; }
        public string FileName { get; set; }
        public int LineNumber { get; set; }
        public BottleneckSeverity Severity { get; set; }
        public string Category { get; set; }
        public float CpuTime { get; set; }
        public long MemoryAllocation { get; set; }
        public string Recommendation { get; set; }
    }

    /// <summary>
    /// Detected anti-pattern (FR-009).
    /// </summary>
    public class AntiPattern
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public AntiPatternType Type { get; set; }
        public string Recommendation { get; set; }
    }

    public enum BottleneckSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum AntiPatternType
    {
        FindInUpdate,
        StringConcatenation,
        BoxingAllocation,
        MissingObjectPooling,
        UnoptimizedPhysics,
        RedundantGetComponent
    }
}
