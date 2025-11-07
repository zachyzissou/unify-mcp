using System;
using System.IO;
using System.Threading.Tasks;
using UnifyMcp.Common.Threading;
using UnifyMcp.Core.Protocol;
using UnifyMcp.Tools.Documentation;

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
        private SimpleMcpServer mcpServer;
        private Task serverTask;

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
        /// In batch mode, starts stdio transport. In Editor mode, just initializes.
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

                // Check if running in batch mode (stdio server mode)
                bool isBatchMode = UnityEngine.Application.isBatchMode;

                if (isBatchMode)
                {
                    // Batch mode: Start stdio MCP server
                    var serverInfo = new ServerInfo
                    {
                        name = "unity-mcp",
                        version = "0.4.0"
                    };

                    mcpServer = new SimpleMcpServer(serverInfo);
                    RegisterTools();

                    // Start server async (non-blocking)
                    serverTask = Task.Run(async () =>
                    {
                        try
                        {
                            await mcpServer.StartAsync();
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"[UnifyMCP] Server error: {ex.Message}");
                            OnError?.Invoke(ex);
                        }
                    });

                    Console.Error.WriteLine("[UnifyMCP] Stdio MCP server started in batch mode");
                }
                else
                {
                    // Editor mode: Just initialize (no stdio server needed)
#if UNITY_EDITOR
                    UnityEngine.Debug.Log("[UnifyMCP] Editor mode initialized (stdio server not started)");
#endif
                }

                isRunning = true;
                OnStarted?.Invoke();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
#if UNITY_EDITOR
                UnityEngine.Debug.LogError($"[UnifyMCP] Failed to start server: {ex.Message}");
#endif
                Console.Error.WriteLine($"[UnifyMCP] Failed to start server: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Registers all available MCP tools.
        /// </summary>
        private void RegisterTools()
        {
            try
            {
                // Get Unity project path for database
                var projectPath = UnityEngine.Application.dataPath;
                var databasePath = Path.Combine(Path.GetDirectoryName(projectPath), "Library", "UnifyMcp", "documentation.db");

                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(databasePath));

                // Register Documentation Tools
                var docTools = new DocumentationTools(databasePath);

                mcpServer.RegisterTool("query_documentation",
                    "Search Unity API documentation with full-text search",
                    async (args) =>
                    {
                        var query = args.ContainsKey("query") ? args["query"].ToString() : "";
                        return await docTools.QueryDocumentation(query);
                    });

                mcpServer.RegisterTool("search_api_fuzzy",
                    "Fuzzy search for Unity API names with typo tolerance",
                    async (args) =>
                    {
                        var query = args.ContainsKey("query") ? args["query"].ToString() : "";
                        var threshold = args.ContainsKey("threshold") ? Convert.ToDouble(args["threshold"]) : 0.7;
                        return await docTools.SearchApiFuzzy(query, threshold);
                    });

                mcpServer.RegisterTool("get_unity_version",
                    "Get current Unity Editor version and documentation version",
                    async (args) => await docTools.GetUnityVersion());

                mcpServer.RegisterTool("check_api_deprecation",
                    "Check if an API is deprecated and get migration suggestions",
                    async (args) =>
                    {
                        var apiName = args.ContainsKey("apiName") ? args["apiName"].ToString() : "";
                        return await docTools.CheckApiDeprecation(apiName);
                    });

                Console.Error.WriteLine($"[UnifyMCP] Registered {4} documentation tools");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[UnifyMCP] Error registering tools: {ex.Message}");
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
                // Stop MCP server if running
                if (mcpServer != null)
                {
                    mcpServer.Stop();
                    mcpServer.Dispose();
                    mcpServer = null;
                    Console.Error.WriteLine("[UnifyMCP] MCP server stopped");
                }

                // Wait for server task to complete (with timeout)
                if (serverTask != null && !serverTask.IsCompleted)
                {
                    serverTask.Wait(TimeSpan.FromSeconds(5));
                    serverTask = null;
                }

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
                Console.Error.WriteLine($"[UnifyMCP] Error stopping server: {ex.Message}");
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
