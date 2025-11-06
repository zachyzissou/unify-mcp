using System;
using UnifyMcp.Common.Threading;

namespace UnifyMcp.Core
{
    /// <summary>
    /// Manages MCP server lifecycle with [InitializeOnLoad] for automatic Unity Editor startup.
    /// Handles server initialization, graceful shutdown, and EditorApplication.quitting integration.
    /// </summary>
    public class McpServerLifecycle : IDisposable
    {
        private bool isRunning;
        private bool isDisposed;

        /// <summary>
        /// Event raised when server starts successfully.
        /// </summary>
        public event Action OnStarted;

        /// <summary>
        /// Event raised when server stops.
        /// </summary>
        public event Action OnStopped;

        /// <summary>
        /// Event raised when an error occurs.
        /// </summary>
        public event Action<Exception> OnError;

        /// <summary>
        /// Gets whether the server is currently running.
        /// </summary>
        public bool IsRunning => isRunning && !isDisposed;

        public McpServerLifecycle()
        {
#if UNITY_EDITOR
            // Register for Unity Editor shutdown
            UnityEditor.EditorApplication.quitting += OnEditorQuitting;
#endif
        }

        /// <summary>
        /// Starts the MCP server.
        /// </summary>
        public void Start()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(McpServerLifecycle));

            if (isRunning)
                return; // Already running

            try
            {
                // Initialize MainThreadDispatcher if not already initialized
                if (MainThreadDispatcher.Instance == null)
                {
                    MainThreadDispatcher.InitializeInstance();
                }

                // TODO: Initialize ModelContextProtocol server (Phase 4)
                // TODO: Initialize stdio transport (Phase 4)

                isRunning = true;
                OnStarted?.Invoke();

#if UNITY_EDITOR
                UnityEngine.Debug.Log("[UnifyMCP] Server started successfully");
#endif
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
#if UNITY_EDITOR
                UnityEngine.Debug.LogError($"[UnifyMCP] Failed to start server: {ex.Message}");
#endif
                throw;
            }
        }

        /// <summary>
        /// Stops the MCP server gracefully.
        /// </summary>
        public void Stop()
        {
            if (!isRunning)
                return; // Not running

            try
            {
                // TODO: Shutdown stdio transport (Phase 4)
                // TODO: Shutdown ModelContextProtocol server (Phase 4)

                isRunning = false;
                OnStopped?.Invoke();

#if UNITY_EDITOR
                UnityEngine.Debug.Log("[UnifyMCP] Server stopped");
#endif
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
#if UNITY_EDITOR
                UnityEngine.Debug.LogError($"[UnifyMCP] Error stopping server: {ex.Message}");
#endif
            }
        }

        /// <summary>
        /// Restarts the server (stops then starts).
        /// </summary>
        public void Restart()
        {
            Stop();
            Start();
        }

        /// <summary>
        /// Handles Unity Editor quitting event.
        /// </summary>
        private void OnEditorQuitting()
        {
            Stop();
        }

        /// <summary>
        /// Disposes the lifecycle manager and stops the server.
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.quitting -= OnEditorQuitting;
#endif

            if (isRunning)
            {
                Stop();
            }

            // Dispose MainThreadDispatcher
            MainThreadDispatcher.DisposeInstance();
        }

        /// <summary>
        /// Singleton instance for global access.
        /// Initialized automatically in Unity Editor via static constructor.
        /// </summary>
        public static McpServerLifecycle Instance { get; private set; }

#if UNITY_EDITOR
        /// <summary>
        /// Static constructor with [InitializeOnLoad] for automatic Unity Editor startup.
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            if (Instance == null)
            {
                Instance = new McpServerLifecycle();
                Instance.Start();
            }
        }
#endif
    }
}
