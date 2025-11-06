using System;

namespace UnifyMcp.Core.Configuration
{
    /// <summary>
    /// Manages MCP server configuration using EditorPrefs for persistence.
    /// Stores server settings, documentation cache location, and indexing preferences.
    /// </summary>
    public class McpConfigurationManager
    {
        // EditorPrefs keys
        private const string KeyServerPort = "UnifyMcp.ServerPort";
        private const string KeyDocCachePath = "UnifyMcp.DocCachePath";
        private const string KeyAutoStartServer = "UnifyMcp.AutoStartServer";
        private const string KeyMaxQueueSize = "UnifyMcp.MaxQueueSize";
        private const string KeyEnableLogging = "UnifyMcp.EnableLogging";
        private const string KeyIndexOnStartup = "UnifyMcp.IndexOnStartup";
        private const string KeyCacheExpirationDays = "UnifyMcp.CacheExpirationDays";

        // Default values
        private const int DefaultServerPort = 3000;
        private const int DefaultMaxQueueSize = 1000;
        private const int DefaultCacheExpirationDays = 30;

        /// <summary>
        /// Gets or sets the server port.
        /// </summary>
        public int ServerPort
        {
            get => GetInt(KeyServerPort, DefaultServerPort);
            set => SetInt(KeyServerPort, value);
        }

        /// <summary>
        /// Gets or sets the documentation cache path.
        /// </summary>
        public string DocCachePath
        {
            get
            {
                var defaultPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "UnifyMcp",
                    "DocCache"
                );
                return GetString(KeyDocCachePath, defaultPath);
            }
            set => SetString(KeyDocCachePath, value);
        }

        /// <summary>
        /// Gets or sets whether to auto-start the server on Unity Editor launch.
        /// </summary>
        public bool AutoStartServer
        {
            get => GetBool(KeyAutoStartServer, true);
            set => SetBool(KeyAutoStartServer, value);
        }

        /// <summary>
        /// Gets or sets the maximum message queue size.
        /// </summary>
        public int MaxQueueSize
        {
            get => GetInt(KeyMaxQueueSize, DefaultMaxQueueSize);
            set => SetInt(KeyMaxQueueSize, value);
        }

        /// <summary>
        /// Gets or sets whether logging is enabled.
        /// </summary>
        public bool EnableLogging
        {
            get => GetBool(KeyEnableLogging, true);
            set => SetBool(KeyEnableLogging, value);
        }

        /// <summary>
        /// Gets or sets whether to index documentation on startup.
        /// </summary>
        public bool IndexOnStartup
        {
            get => GetBool(KeyIndexOnStartup, false);
            set => SetBool(KeyIndexOnStartup, value);
        }

        /// <summary>
        /// Gets or sets the cache expiration in days.
        /// </summary>
        public int CacheExpirationDays
        {
            get => GetInt(KeyCacheExpirationDays, DefaultCacheExpirationDays);
            set => SetInt(KeyCacheExpirationDays, value);
        }

        /// <summary>
        /// Resets all configuration to defaults.
        /// </summary>
        public void ResetToDefaults()
        {
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.DeleteKey(KeyServerPort);
            UnityEditor.EditorPrefs.DeleteKey(KeyDocCachePath);
            UnityEditor.EditorPrefs.DeleteKey(KeyAutoStartServer);
            UnityEditor.EditorPrefs.DeleteKey(KeyMaxQueueSize);
            UnityEditor.EditorPrefs.DeleteKey(KeyEnableLogging);
            UnityEditor.EditorPrefs.DeleteKey(KeyIndexOnStartup);
            UnityEditor.EditorPrefs.DeleteKey(KeyCacheExpirationDays);
#endif
        }

        // Helper methods for EditorPrefs access
        private int GetInt(string key, int defaultValue)
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetInt(key, defaultValue);
#else
            return defaultValue;
#endif
        }

        private void SetInt(string key, int value)
        {
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetInt(key, value);
#endif
        }

        private string GetString(string key, string defaultValue)
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetString(key, defaultValue);
#else
            return defaultValue;
#endif
        }

        private void SetString(string key, string value)
        {
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetString(key, value);
#endif
        }

        private bool GetBool(string key, bool defaultValue)
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(key, defaultValue);
#else
            return defaultValue;
#endif
        }

        private void SetBool(string key, bool value)
        {
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetBool(key, value);
#endif
        }

        /// <summary>
        /// Singleton instance for global configuration access.
        /// </summary>
        public static McpConfigurationManager Instance { get; } = new McpConfigurationManager();
    }
}
