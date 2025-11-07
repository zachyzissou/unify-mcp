using System;
using System.Collections.Generic;

namespace UnifyMcp.Core.Protocol
{
    /// <summary>
    /// MCP JSON-RPC 2.0 request message.
    /// </summary>
    public class McpRequest
    {
        public string jsonrpc { get; set; } = "2.0";
        public object id { get; set; }
        public string method { get; set; }
        public Dictionary<string, object> @params { get; set; }
    }

    /// <summary>
    /// MCP JSON-RPC 2.0 response message.
    /// </summary>
    public class McpResponse
    {
        public string jsonrpc { get; set; } = "2.0";
        public object id { get; set; }
        public object result { get; set; }
        public McpError error { get; set; }
    }

    /// <summary>
    /// MCP JSON-RPC 2.0 error object.
    /// </summary>
    public class McpError
    {
        public int code { get; set; }
        public string message { get; set; }
        public object data { get; set; }
    }

    /// <summary>
    /// MCP notification message (no response expected).
    /// </summary>
    public class McpNotification
    {
        public string jsonrpc { get; set; } = "2.0";
        public string method { get; set; }
        public Dictionary<string, object> @params { get; set; }
    }

    /// <summary>
    /// MCP server information.
    /// </summary>
    public class ServerInfo
    {
        public string name { get; set; }
        public string version { get; set; }
    }

    /// <summary>
    /// MCP tool definition.
    /// </summary>
    public class ToolDefinition
    {
        public string name { get; set; }
        public string description { get; set; }
        public Dictionary<string, object> inputSchema { get; set; }
    }

    /// <summary>
    /// MCP tool call result.
    /// </summary>
    public class ToolResult
    {
        public string content { get; set; }
        public bool isError { get; set; }
    }

    /// <summary>
    /// Attribute to mark methods as MCP tools.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class McpToolAttribute : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public McpToolAttribute(string name = null, string description = null)
        {
            Name = name;
            Description = description;
        }
    }
}
