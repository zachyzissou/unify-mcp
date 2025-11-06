using System;
using System.Collections.Generic;

namespace UnifyMcp.Common.ErrorHandling
{
    /// <summary>
    /// Structured error handling for MCP server.
    /// Categorizes errors (Unity API, MCP protocol, user errors) and integrates with Unity Debug.Log.
    /// </summary>
    public class McpErrorHandler
    {
        /// <summary>
        /// Event raised when an error is handled.
        /// </summary>
        public event Action<McpError> OnError;

        /// <summary>
        /// Handles an exception and categorizes it.
        /// </summary>
        /// <param name="exception">Exception to handle</param>
        /// <param name="context">Additional context about where the error occurred</param>
        /// <returns>Structured error object</returns>
        public McpError HandleException(Exception exception, string context = null)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            var category = CategorizeException(exception);
            var error = new McpError
            {
                Exception = exception,
                Category = category,
                Context = context,
                Timestamp = DateTime.UtcNow,
                Message = exception.Message,
                StackTrace = exception.StackTrace
            };

            // Log to Unity console
            LogError(error);

            // Notify listeners
            OnError?.Invoke(error);

            return error;
        }

        /// <summary>
        /// Categorizes an exception based on its type and context.
        /// </summary>
        private ErrorCategory CategorizeException(Exception exception)
        {
            var exceptionType = exception.GetType().FullName;

            // Unity API errors
            if (exceptionType.StartsWith("UnityEngine.") || exceptionType.StartsWith("UnityEditor."))
            {
                return ErrorCategory.UnityApi;
            }

            // JSON-RPC and protocol errors
            if (exceptionType.Contains("JsonRpc") || exceptionType.Contains("Protocol"))
            {
                return ErrorCategory.McpProtocol;
            }

            // Argument exceptions typically indicate user errors
            if (exception is ArgumentException || exception is ArgumentNullException)
            {
                return ErrorCategory.UserError;
            }

            // Invalid operation can be either user error or internal
            if (exception is InvalidOperationException)
            {
                return ErrorCategory.UserError;
            }

            // Default to internal error
            return ErrorCategory.Internal;
        }

        /// <summary>
        /// Logs error to Unity console with appropriate severity.
        /// </summary>
        private void LogError(McpError error)
        {
#if UNITY_EDITOR
            var prefix = $"[UnifyMCP:{error.Category}]";
            var message = $"{prefix} {error.Message}";

            if (!string.IsNullOrEmpty(error.Context))
            {
                message += $"\nContext: {error.Context}";
            }

            switch (error.Category)
            {
                case ErrorCategory.UserError:
                    UnityEngine.Debug.LogWarning(message);
                    break;
                case ErrorCategory.Internal:
                case ErrorCategory.UnityApi:
                case ErrorCategory.McpProtocol:
                    UnityEngine.Debug.LogError(message);
                    if (error.Exception != null)
                    {
                        UnityEngine.Debug.LogException(error.Exception);
                    }
                    break;
            }
#else
            // Outside Unity, write to console
            Console.WriteLine($"[UnifyMCP:{error.Category}] {error.Message}");
            if (error.Exception != null)
            {
                Console.WriteLine(error.Exception.ToString());
            }
#endif
        }

        /// <summary>
        /// Creates a user-friendly error message from an exception.
        /// </summary>
        /// <param name="exception">Exception to format</param>
        /// <returns>User-friendly error message</returns>
        public string FormatUserMessage(Exception exception)
        {
            var category = CategorizeException(exception);

            switch (category)
            {
                case ErrorCategory.UserError:
                    return $"Invalid request: {exception.Message}";

                case ErrorCategory.UnityApi:
                    return $"Unity API error: {exception.Message}";

                case ErrorCategory.McpProtocol:
                    return $"Protocol error: {exception.Message}";

                case ErrorCategory.Internal:
                default:
                    return $"Internal server error: {exception.Message}";
            }
        }
    }

    /// <summary>
    /// Represents a structured error with categorization and context.
    /// </summary>
    public class McpError
    {
        public Exception Exception { get; set; }
        public ErrorCategory Category { get; set; }
        public string Context { get; set; }
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }

        public override string ToString()
        {
            return $"[{Category}] {Message} (at {Timestamp:yyyy-MM-dd HH:mm:ss})";
        }
    }

    /// <summary>
    /// Categories of errors that can occur in the MCP server.
    /// </summary>
    public enum ErrorCategory
    {
        /// <summary>
        /// Error caused by invalid user input or usage
        /// </summary>
        UserError,

        /// <summary>
        /// Error from Unity API calls
        /// </summary>
        UnityApi,

        /// <summary>
        /// Error in MCP protocol communication
        /// </summary>
        McpProtocol,

        /// <summary>
        /// Internal server error
        /// </summary>
        Internal
    }
}
