using NUnit.Framework;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using UnifyMcp.Tools.Profiler;
using UnifyMcp.Tools.Profiler.Models;

namespace UnifyMcp.Tests.Tools.Profiler
{
    /// <summary>
    /// Tests for profiler snapshot capture (FR-006).
    /// Tests frame capture (300 frames default), CPU/GPU/memory data, bottleneck identification.
    /// </summary>
    [TestFixture]
    public class ProfilerSnapshotTests
    {
        private ProfilerTools tools;

        [SetUp]
        public void SetUp()
        {
            tools = new ProfilerTools();
        }

        [TearDown]
        public void TearDown()
        {
            tools?.Dispose();
        }

        [Test]
        public async Task CaptureProfilerSnapshot_DefaultFrames_ShouldCapture300()
        {
            // Act
            var json = await tools.CaptureProfilerSnapshot();

            // Assert
            var snapshot = JsonSerializer.Deserialize<ProfilerSnapshot>(json);
            Assert.AreEqual(300, snapshot.FrameCount, "Default should be 300 frames (FR-006)");
        }

        [Test]
        public async Task CaptureProfilerSnapshot_ShouldIncludeCpuMetrics()
        {
            // Act
            var json = await tools.CaptureProfilerSnapshot();

            // Assert
            var snapshot = JsonSerializer.Deserialize<ProfilerSnapshot>(json);
            Assert.IsNotNull(snapshot.CpuTimes);
            Assert.IsNotEmpty(snapshot.CpuTimes);
        }

        [Test]
        public async Task CaptureProfilerSnapshot_ShouldIncludeMemoryMetrics()
        {
            // Act
            var json = await tools.CaptureProfilerSnapshot();

            // Assert
            var snapshot = JsonSerializer.Deserialize<ProfilerSnapshot>(json);
            Assert.IsNotNull(snapshot.GcAllocations);
            Assert.IsNotEmpty(snapshot.GcAllocations);
        }

        [Test]
        public async Task CaptureProfilerSnapshot_ShouldIdentifyBottlenecks()
        {
            // Act
            var json = await tools.CaptureProfilerSnapshot();

            // Assert
            var snapshot = JsonSerializer.Deserialize<ProfilerSnapshot>(json);
            Assert.IsNotNull(snapshot.Bottlenecks);
        }

        [Test]
        public async Task DetectAntipatterns_ShouldReturnKnownPatterns()
        {
            // Act
            var json = await tools.DetectAntipatterns();

            // Assert
            var antiPatterns = JsonSerializer.Deserialize<AntiPattern[]>(json);
            Assert.IsNotEmpty(antiPatterns);
        }
    }
}
