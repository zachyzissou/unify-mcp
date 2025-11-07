using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnifyMcp.Core.Protocol;

namespace UnifyMcp.Tools.Unity
{
    /// <summary>
    /// MCP tool for accessing Unity console logs.
    /// Captures Error, Warning, and Log messages with stack traces.
    /// Essential for AI agents to see compilation errors and runtime issues.
    /// </summary>
    public class UnityConsoleTools : IDisposable
    {
        private readonly List<LogEntry> recentLogs = new List<LogEntry>();
        private readonly int maxLogBufferSize;
        private readonly object lockObject = new object();
        private Action<LogEntry> onNewLog;

        public UnityConsoleTools(int maxBufferSize = 100)
        {
            this.maxLogBufferSize = maxBufferSize;
            Application.logMessageReceived += OnLogMessageReceived;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            var entry = new LogEntry
            {
                Message = condition,
                StackTrace = stackTrace,
                Type = type,
                Timestamp = DateTime.UtcNow
            };

            lock (lockObject)
            {
                recentLogs.Add(entry);
                if (recentLogs.Count > maxLogBufferSize)
                {
                    recentLogs.RemoveAt(0);
                }
            }

            // Notify listeners (for potential MCP notifications)
            onNewLog?.Invoke(entry);
        }

        /// <summary>
        /// Gets recent console logs with optional filtering.
        /// </summary>
        [McpTool("get_recent_logs", "Get recent Unity console logs with optional type filtering")]
        public async Task<string> GetRecentLogs(int count = 50, string logType = "all")
        {
            return await Task.Run(() =>
            {
                lock (lockObject)
                {
                    IEnumerable<LogEntry> filtered = recentLogs;

                    // Filter by log type
                    if (logType != "all" && logType != null)
                    {
                        if (Enum.TryParse<LogType>(logType, true, out var typeFilter))
                        {
                            filtered = filtered.Where(l => l.Type == typeFilter);
                        }
                    }

                    // Take last N entries
                    var result = filtered.TakeLast(count).Select(l => new
                    {
                        message = l.Message,
                        stackTrace = l.StackTrace,
                        type = l.Type.ToString(),
                        timestamp = l.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")
                    });

                    return System.Text.Json.JsonSerializer.Serialize(new
                    {
                        totalLogs = recentLogs.Count,
                        filteredCount = filtered.Count(),
                        logs = result
                    }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                }
            });
        }

        /// <summary>
        /// Gets only error logs (compilation errors, exceptions, assertions).
        /// </summary>
        [McpTool("get_errors", "Get only Unity error logs (compilation errors, exceptions, assertions)")]
        public async Task<string> GetErrors(int count = 20)
        {
            return await Task.Run(() =>
            {
                lock (lockObject)
                {
                    var errors = recentLogs
                        .Where(l => l.Type == LogType.Error || l.Type == LogType.Exception || l.Type == LogType.Assert)
                        .TakeLast(count)
                        .Select(l => new
                        {
                            message = l.Message,
                            stackTrace = l.StackTrace,
                            type = l.Type.ToString(),
                            timestamp = l.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")
                        });

                    return System.Text.Json.JsonSerializer.Serialize(new
                    {
                        errorCount = errors.Count(),
                        errors
                    }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                }
            });
        }

        /// <summary>
        /// Gets only warning logs.
        /// </summary>
        [McpTool("get_warnings", "Get only Unity warning logs")]
        public async Task<string> GetWarnings(int count = 20)
        {
            return await Task.Run(() =>
            {
                lock (lockObject)
                {
                    var warnings = recentLogs
                        .Where(l => l.Type == LogType.Warning)
                        .TakeLast(count)
                        .Select(l => new
                        {
                            message = l.Message,
                            stackTrace = l.StackTrace,
                            timestamp = l.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")
                        });

                    return System.Text.Json.JsonSerializer.Serialize(new
                    {
                        warningCount = warnings.Count(),
                        warnings
                    }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                }
            });
        }

        /// <summary>
        /// Clears the console log buffer.
        /// </summary>
        [McpTool("clear_console", "Clear the Unity console log buffer")]
        public async Task<string> ClearConsole()
        {
            return await Task.Run(() =>
            {
                lock (lockObject)
                {
                    var clearedCount = recentLogs.Count;
                    recentLogs.Clear();

                    return System.Text.Json.JsonSerializer.Serialize(new
                    {
                        status = "cleared",
                        clearedLogs = clearedCount
                    }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                }
            });
        }

        /// <summary>
        /// Gets a summary of log counts by type.
        /// </summary>
        [McpTool("get_log_summary", "Get summary of Unity console logs by type")]
        public async Task<string> GetLogSummary()
        {
            return await Task.Run(() =>
            {
                lock (lockObject)
                {
                    var summary = recentLogs
                        .GroupBy(l => l.Type)
                        .ToDictionary(g => g.Key.ToString(), g => g.Count());

                    return System.Text.Json.JsonSerializer.Serialize(new
                    {
                        totalLogs = recentLogs.Count,
                        byType = summary,
                        oldestLog = recentLogs.FirstOrDefault()?.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                        newestLog = recentLogs.LastOrDefault()?.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
                    }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                }
            });
        }

        /// <summary>
        /// Sets a handler for new log notifications.
        /// Can be used to send MCP notifications when errors occur.
        /// </summary>
        public void SetLogNotificationHandler(Action<LogEntry> handler)
        {
            onNewLog = handler;
        }

        public void Dispose()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
        }
    }

    /// <summary>
    /// Represents a single Unity console log entry.
    /// </summary>
    public class LogEntry
    {
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public LogType Type { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
