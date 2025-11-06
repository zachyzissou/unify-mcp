using System;
using System.Collections.Concurrent;

namespace UnifyMcp.Common.Threading
{
    /// <summary>
    /// Thread-safe message queue for dispatching actions to Unity's main thread.
    /// Uses ConcurrentQueue for cross-thread communication and EditorApplication.update for polling.
    /// Prevents memory exhaustion with configurable queue size limits.
    /// </summary>
    public class MainThreadDispatcher : IDisposable
    {
        private readonly ConcurrentQueue<Action> actionQueue;
        private readonly int maxQueueSize;
        private bool isDisposed;

        /// <summary>
        /// Event raised when an exception occurs during action processing.
        /// </summary>
        public event Action<Exception> OnException;

        /// <summary>
        /// Gets the current number of queued actions.
        /// </summary>
        public int QueueCount => actionQueue.Count;

        /// <summary>
        /// Creates a new MainThreadDispatcher with specified queue size limit.
        /// </summary>
        /// <param name="maxQueueSize">Maximum number of actions that can be queued (default 1000)</param>
        public MainThreadDispatcher(int maxQueueSize = 1000)
        {
            if (maxQueueSize <= 0)
                throw new ArgumentException("Max queue size must be positive", nameof(maxQueueSize));

            this.maxQueueSize = maxQueueSize;
            this.actionQueue = new ConcurrentQueue<Action>();

#if UNITY_EDITOR
            // Register with Unity's EditorApplication.update for automatic processing
            UnityEditor.EditorApplication.update += ProcessQueue;
#endif
        }

        /// <summary>
        /// Enqueues an action to be executed on the main thread.
        /// </summary>
        /// <param name="action">Action to execute on main thread</param>
        /// <exception cref="ArgumentNullException">If action is null</exception>
        /// <exception cref="InvalidOperationException">If queue is full</exception>
        public void Enqueue(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (isDisposed)
                throw new ObjectDisposedException(nameof(MainThreadDispatcher));

            // Check queue size limit to prevent memory exhaustion
            if (actionQueue.Count >= maxQueueSize)
            {
                throw new InvalidOperationException(
                    $"Queue is full (max size: {maxQueueSize}). Cannot enqueue more actions.");
            }

            actionQueue.Enqueue(action);
        }

        /// <summary>
        /// Processes all queued actions on the current thread.
        /// Called automatically by EditorApplication.update in Unity Editor.
        /// Can also be called manually for testing.
        /// </summary>
        public void ProcessQueue()
        {
            if (isDisposed)
                return;

            // Process all currently queued actions
            // Note: We snapshot the count to avoid infinite loop if actions enqueue more actions
            int actionsToProcess = actionQueue.Count;

            for (int i = 0; i < actionsToProcess; i++)
            {
                if (actionQueue.TryDequeue(out var action))
                {
                    try
                    {
                        action.Invoke();
                    }
                    catch (Exception ex)
                    {
                        // Don't let one exception stop processing of remaining actions
                        HandleException(ex);
                    }
                }
            }
        }

        /// <summary>
        /// Clears all queued actions.
        /// </summary>
        public void Clear()
        {
            while (actionQueue.TryDequeue(out _))
            {
                // Keep dequeuing until empty
            }
        }

        /// <summary>
        /// Handles exceptions that occur during action processing.
        /// </summary>
        private void HandleException(Exception ex)
        {
            // Notify listeners
            OnException?.Invoke(ex);

            // Also log to Unity console if available
#if UNITY_EDITOR
            UnityEngine.Debug.LogException(ex);
#else
            // Outside Unity, write to console
            Console.WriteLine($"MainThreadDispatcher exception: {ex}");
#endif
        }

        /// <summary>
        /// Disposes the dispatcher and clears the queue.
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

#if UNITY_EDITOR
            // Unregister from Unity's update loop
            UnityEditor.EditorApplication.update -= ProcessQueue;
#endif

            // Clear any remaining actions
            Clear();
        }

        /// <summary>
        /// Creates a singleton instance for global use.
        /// In Unity Editor, this is automatically initialized.
        /// </summary>
        public static MainThreadDispatcher Instance { get; private set; }

        /// <summary>
        /// Initializes the singleton instance.
        /// Called automatically in Unity Editor via [InitializeOnLoad].
        /// </summary>
        public static void InitializeInstance(int maxQueueSize = 1000)
        {
            if (Instance != null)
            {
                Instance.Dispose();
            }

            Instance = new MainThreadDispatcher(maxQueueSize);
        }

        /// <summary>
        /// Disposes the singleton instance.
        /// </summary>
        public static void DisposeInstance()
        {
            if (Instance != null)
            {
                Instance.Dispose();
                Instance = null;
            }
        }
    }
}
