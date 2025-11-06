using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnifyMcp.Common.Threading;

namespace UnifyMcp.Tests.Core
{
    /// <summary>
    /// Tests for thread-safe message queue using ConcurrentQueue pattern.
    /// Tests message enqueue/dequeue, exception handling across thread boundaries, and queue size limits.
    /// </summary>
    [TestFixture]
    public class ThreadingTests
    {
        private MainThreadDispatcher dispatcher;

        [SetUp]
        public void SetUp()
        {
            dispatcher = new MainThreadDispatcher(maxQueueSize: 100);
        }

        [TearDown]
        public void TearDown()
        {
            dispatcher?.Dispose();
        }

        [Test]
        public void Enqueue_SimpleAction_ShouldSucceed()
        {
            // Arrange
            var actionExecuted = false;
            Action testAction = () => actionExecuted = true;

            // Act
            dispatcher.Enqueue(testAction);
            dispatcher.ProcessQueue(); // Manually process for testing

            // Assert
            Assert.IsTrue(actionExecuted, "Action should be executed");
        }

        [Test]
        public void Enqueue_NullAction_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => dispatcher.Enqueue(null));
        }

        [Test]
        public void ProcessQueue_MultipleActions_ShouldExecuteInOrder()
        {
            // Arrange
            var executionOrder = new List<int>();
            dispatcher.Enqueue(() => executionOrder.Add(1));
            dispatcher.Enqueue(() => executionOrder.Add(2));
            dispatcher.Enqueue(() => executionOrder.Add(3));

            // Act
            dispatcher.ProcessQueue();

            // Assert
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, executionOrder,
                "Actions should execute in FIFO order");
        }

        [Test]
        public void ProcessQueue_WithException_ShouldContinueProcessing()
        {
            // Arrange
            var executedActions = new List<int>();
            dispatcher.Enqueue(() => executedActions.Add(1));
            dispatcher.Enqueue(() => throw new Exception("Test exception"));
            dispatcher.Enqueue(() => executedActions.Add(3));

            // Act - Should not throw
            Assert.DoesNotThrow(() => dispatcher.ProcessQueue());

            // Assert - Should have executed actions 1 and 3
            Assert.Contains(1, executedActions, "First action should execute");
            Assert.Contains(3, executedActions, "Third action should execute despite exception in second");
        }

        [Test]
        public void ProcessQueue_EmptyQueue_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => dispatcher.ProcessQueue());
        }

        [Test]
        public void QueueCount_ShouldTrackQueuedActions()
        {
            // Arrange
            Assert.AreEqual(0, dispatcher.QueueCount);

            // Act
            dispatcher.Enqueue(() => { });
            Assert.AreEqual(1, dispatcher.QueueCount);

            dispatcher.Enqueue(() => { });
            Assert.AreEqual(2, dispatcher.QueueCount);

            dispatcher.ProcessQueue();

            // Assert
            Assert.AreEqual(0, dispatcher.QueueCount, "Queue should be empty after processing");
        }

        [Test]
        public void Enqueue_ExceedMaxQueueSize_ShouldThrowInvalidOperationException()
        {
            // Arrange - Create dispatcher with small queue size
            var smallDispatcher = new MainThreadDispatcher(maxQueueSize: 3);

            // Act - Fill queue
            smallDispatcher.Enqueue(() => { });
            smallDispatcher.Enqueue(() => { });
            smallDispatcher.Enqueue(() => { });

            // Assert - Should throw on next enqueue
            Assert.Throws<InvalidOperationException>(() => smallDispatcher.Enqueue(() => { }));

            smallDispatcher.Dispose();
        }

        [Test]
        public void EnqueueFromMultipleThreads_ShouldBeThreadSafe()
        {
            // Arrange
            var executedCount = 0;
            var threads = new List<Thread>();
            var actionsPerThread = 50;
            var threadCount = 4;

            // Act - Enqueue from multiple threads
            for (int i = 0; i < threadCount; i++)
            {
                var thread = new Thread(() =>
                {
                    for (int j = 0; j < actionsPerThread; j++)
                    {
                        dispatcher.Enqueue(() => Interlocked.Increment(ref executedCount));
                    }
                });
                threads.Add(thread);
                thread.Start();
            }

            // Wait for all threads
            foreach (var thread in threads)
            {
                thread.Join();
            }

            // Process all queued actions
            dispatcher.ProcessQueue();

            // Assert
            Assert.AreEqual(threadCount * actionsPerThread, executedCount,
                "All actions from all threads should be executed");
        }

        [Test]
        public void ProcessQueue_WithTaskReturningAction_ShouldExecute()
        {
            // Arrange
            var taskCompleted = false;
            var completionSource = new TaskCompletionSource<bool>();

            dispatcher.Enqueue(() =>
            {
                Task.Run(async () =>
                {
                    await Task.Delay(10);
                    taskCompleted = true;
                    completionSource.SetResult(true);
                });
            });

            // Act
            dispatcher.ProcessQueue();
            var completed = completionSource.Task.Wait(1000);

            // Assert
            Assert.IsTrue(completed, "Task should complete");
            Assert.IsTrue(taskCompleted, "Task should execute");
        }

        [Test]
        public void Clear_ShouldRemoveAllQueuedActions()
        {
            // Arrange
            dispatcher.Enqueue(() => { });
            dispatcher.Enqueue(() => { });
            dispatcher.Enqueue(() => { });
            Assert.AreEqual(3, dispatcher.QueueCount);

            // Act
            dispatcher.Clear();

            // Assert
            Assert.AreEqual(0, dispatcher.QueueCount, "Queue should be empty after Clear");
        }

        [Test]
        public void Dispose_ShouldClearQueue()
        {
            // Arrange
            dispatcher.Enqueue(() => { });
            dispatcher.Enqueue(() => { });

            // Act
            dispatcher.Dispose();

            // Assert
            Assert.AreEqual(0, dispatcher.QueueCount, "Queue should be cleared on Dispose");
        }

        [Test]
        public void ProcessQueue_PerformanceTest_ShouldHandleMany()
        {
            // Arrange
            var executedCount = 0;
            var actionCount = 1000;

            for (int i = 0; i < actionCount; i++)
            {
                dispatcher.Enqueue(() => executedCount++);
            }

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            dispatcher.ProcessQueue();
            stopwatch.Stop();

            // Assert
            Assert.AreEqual(actionCount, executedCount, "All actions should execute");
            Assert.Less(stopwatch.ElapsedMilliseconds, 500,
                "Processing 1000 actions should be fast (<500ms)");
        }

        [Test]
        public void Enqueue_WithCapture_ShouldPreserveContext()
        {
            // Arrange
            var capturedValue = 42;
            var receivedValue = 0;

            // Act
            dispatcher.Enqueue(() => receivedValue = capturedValue);
            dispatcher.ProcessQueue();

            // Assert
            Assert.AreEqual(42, receivedValue, "Captured value should be preserved");
        }

        [Test]
        public void ExceptionDuringProcessing_ShouldBeReported()
        {
            // Arrange
            Exception caughtException = null;
            dispatcher.OnException += (ex) => caughtException = ex;

            var testException = new InvalidOperationException("Test error");
            dispatcher.Enqueue(() => throw testException);

            // Act
            dispatcher.ProcessQueue();

            // Assert
            Assert.IsNotNull(caughtException, "Exception should be caught");
            Assert.AreEqual("Test error", caughtException.Message);
        }
    }
}
