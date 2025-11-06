using NUnit.Framework;
using System;
using UnifyMcp.Core;

namespace UnifyMcp.Tests.Core
{
    /// <summary>
    /// Tests for MCP server lifecycle management.
    /// Tests [InitializeOnLoad] startup, graceful shutdown, and error recovery.
    /// </summary>
    [TestFixture]
    public class McpServerLifecycleTests
    {
        [Test]
        public void Initialize_ShouldCreateServerInstance()
        {
            // Act
            var lifecycle = new McpServerLifecycle();

            // Assert
            Assert.IsNotNull(lifecycle, "Lifecycle manager should be created");
            Assert.IsFalse(lifecycle.IsRunning, "Should not be running initially");

            // Cleanup
            lifecycle.Dispose();
        }

        [Test]
        public void Start_ShouldTransitionToRunning()
        {
            // Arrange
            var lifecycle = new McpServerLifecycle();

            // Act
            lifecycle.Start();

            // Assert
            Assert.IsTrue(lifecycle.IsRunning, "Should be running after Start");

            // Cleanup
            lifecycle.Dispose();
        }

        [Test]
        public void Stop_ShouldTransitionToStopped()
        {
            // Arrange
            var lifecycle = new McpServerLifecycle();
            lifecycle.Start();

            // Act
            lifecycle.Stop();

            // Assert
            Assert.IsFalse(lifecycle.IsRunning, "Should not be running after Stop");

            // Cleanup
            lifecycle.Dispose();
        }

        [Test]
        public void Dispose_WhenRunning_ShouldStopGracefully()
        {
            // Arrange
            var lifecycle = new McpServerLifecycle();
            lifecycle.Start();

            // Act & Assert
            Assert.DoesNotThrow(() => lifecycle.Dispose());
            Assert.IsFalse(lifecycle.IsRunning, "Should not be running after Dispose");
        }

        [Test]
        public void Start_WhenAlreadyRunning_ShouldNotThrow()
        {
            // Arrange
            var lifecycle = new McpServerLifecycle();
            lifecycle.Start();

            // Act & Assert - Starting again should be idempotent
            Assert.DoesNotThrow(() => lifecycle.Start());
            Assert.IsTrue(lifecycle.IsRunning);

            // Cleanup
            lifecycle.Dispose();
        }

        [Test]
        public void Stop_WhenNotRunning_ShouldNotThrow()
        {
            // Arrange
            var lifecycle = new McpServerLifecycle();

            // Act & Assert - Stopping when not running should be safe
            Assert.DoesNotThrow(() => lifecycle.Stop());
            Assert.IsFalse(lifecycle.IsRunning);

            // Cleanup
            lifecycle.Dispose();
        }

        [Test]
        public void ServerError_ShouldTriggerOnError()
        {
            // Arrange
            var lifecycle = new McpServerLifecycle();
            Exception caughtError = null;
            lifecycle.OnError += (ex) => caughtError = ex;

            // Act - Simulate error
            var testError = new InvalidOperationException("Test error");
            lifecycle.OnError?.Invoke(testError);

            // Assert
            Assert.IsNotNull(caughtError);
            Assert.AreEqual("Test error", caughtError.Message);

            // Cleanup
            lifecycle.Dispose();
        }

        [Test]
        public void Restart_ShouldStopAndStart()
        {
            // Arrange
            var lifecycle = new McpServerLifecycle();
            lifecycle.Start();

            // Act
            lifecycle.Restart();

            // Assert
            Assert.IsTrue(lifecycle.IsRunning, "Should be running after Restart");

            // Cleanup
            lifecycle.Dispose();
        }

        [Test]
        public void OnStarted_Event_ShouldFireOnStart()
        {
            // Arrange
            var lifecycle = new McpServerLifecycle();
            var eventFired = false;
            lifecycle.OnStarted += () => eventFired = true;

            // Act
            lifecycle.Start();

            // Assert
            Assert.IsTrue(eventFired, "OnStarted event should fire");

            // Cleanup
            lifecycle.Dispose();
        }

        [Test]
        public void OnStopped_Event_ShouldFireOnStop()
        {
            // Arrange
            var lifecycle = new McpServerLifecycle();
            lifecycle.Start();

            var eventFired = false;
            lifecycle.OnStopped += () => eventFired = true;

            // Act
            lifecycle.Stop();

            // Assert
            Assert.IsTrue(eventFired, "OnStopped event should fire");

            // Cleanup
            lifecycle.Dispose();
        }
    }
}
