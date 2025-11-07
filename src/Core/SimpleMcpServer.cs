using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using UnityMcp.Core.Protocol;
using UnityMcp.Core.Transport;

namespace UnifyMcp.Core
{
    /// <summary>
    /// Simple MCP server implementation using JSON-RPC 2.0.
    /// Handles initialize, tools/list, and tools/call requests.
    /// </summary>
    public class SimpleMcpServer : IDisposable
    {
        private readonly StdioTransport transport;
        private readonly ServerInfo serverInfo;
        private readonly Dictionary<string, ToolRegistration> tools;
        private bool isInitialized;
        private bool isDisposed;

        public SimpleMcpServer(ServerInfo serverInfo, StdioTransport transport = null)
        {
            this.serverInfo = serverInfo ?? throw new ArgumentNullException(nameof(serverInfo));
            this.transport = transport ?? new StdioTransport();
            this.tools = new Dictionary<string, ToolRegistration>();

            this.transport.OnMessageReceived += HandleMessage;
            this.transport.OnError += HandleError;
        }

        /// <summary>
        /// Registers a tool instance and scans for [McpTool] attributed methods.
        /// </summary>
        public void RegisterTool(object toolInstance)
        {
            if (toolInstance == null)
                throw new ArgumentNullException(nameof(toolInstance));

            var type = toolInstance.GetType();
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<McpToolAttribute>();
                if (attr == null)
                    continue;

                var toolName = attr.Name ?? $"{type.Name}.{method.Name}";
                var description = attr.Description ?? $"{type.Name}.{method.Name}";

                var registration = new ToolRegistration
                {
                    Name = toolName,
                    Description = description,
                    Method = method,
                    Instance = toolInstance
                };

                tools[toolName] = registration;
                Log($"[SimpleMcpServer] Registered tool: {toolName}");
            }
        }

        /// <summary>
        /// Registers a tool with manual configuration.
        /// </summary>
        public void RegisterTool(string name, string description, Func<Dictionary<string, object>, Task<string>> handler)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Tool name cannot be empty", nameof(name));

            var registration = new ToolRegistration
            {
                Name = name,
                Description = description,
                Handler = handler
            };

            tools[name] = registration;
            Log($"[SimpleMcpServer] Registered tool: {name}");
        }

        /// <summary>
        /// Starts the MCP server.
        /// </summary>
        public async Task StartAsync()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(SimpleMcpServer));

            Log($"[SimpleMcpServer] Starting server: {serverInfo.name} v{serverInfo.version}");
            await transport.StartAsync();
        }

        /// <summary>
        /// Stops the MCP server.
        /// </summary>
        public void Stop()
        {
            if (!isDisposed)
            {
                Log("[SimpleMcpServer] Stopping server");
                transport.Stop();
            }
        }

        private void HandleMessage(string message)
        {
            try
            {
                var request = JsonSerializer.Deserialize<McpRequest>(message);
                if (request == null)
                {
                    Log("[SimpleMcpServer] Failed to parse request");
                    return;
                }

                Log($"[SimpleMcpServer] Handling method: {request.method}");

                Task.Run(async () =>
                {
                    try
                    {
                        var response = await ProcessRequest(request);
                        if (response != null)
                        {
                            var json = JsonSerializer.Serialize(response);
                            await transport.SendAsync(json);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"[SimpleMcpServer] Error processing request: {ex.Message}");
                        var errorResponse = new McpResponse
                        {
                            id = request.id,
                            error = new McpError
                            {
                                code = -32603,
                                message = ex.Message
                            }
                        };
                        var json = JsonSerializer.Serialize(errorResponse);
                        await transport.SendAsync(json);
                    }
                });
            }
            catch (Exception ex)
            {
                Log($"[SimpleMcpServer] Error handling message: {ex.Message}");
            }
        }

        private async Task<McpResponse> ProcessRequest(McpRequest request)
        {
            switch (request.method)
            {
                case "initialize":
                    return HandleInitialize(request);

                case "initialized":
                    // Notification, no response needed
                    isInitialized = true;
                    Log("[SimpleMcpServer] Client initialized");
                    return null;

                case "tools/list":
                    return HandleToolsList(request);

                case "tools/call":
                    return await HandleToolsCall(request);

                default:
                    return new McpResponse
                    {
                        id = request.id,
                        error = new McpError
                        {
                            code = -32601,
                            message = $"Method not found: {request.method}"
                        }
                    };
            }
        }

        private McpResponse HandleInitialize(McpRequest request)
        {
            Log("[SimpleMcpServer] Handling initialize");

            return new McpResponse
            {
                id = request.id,
                result = new
                {
                    protocolVersion = "2024-11-05",
                    capabilities = new
                    {
                        tools = new { }
                    },
                    serverInfo = new
                    {
                        name = serverInfo.name,
                        version = serverInfo.version
                    }
                }
            };
        }

        private McpResponse HandleToolsList(McpRequest request)
        {
            Log($"[SimpleMcpServer] Listing {tools.Count} tools");

            var toolsList = tools.Values.Select(t => new
            {
                name = t.Name,
                description = t.Description,
                inputSchema = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>()
                }
            }).ToArray();

            return new McpResponse
            {
                id = request.id,
                result = new
                {
                    tools = toolsList
                }
            };
        }

        private async Task<McpResponse> HandleToolsCall(McpRequest request)
        {
            if (request.@params == null || !request.@params.ContainsKey("name"))
            {
                return new McpResponse
                {
                    id = request.id,
                    error = new McpError
                    {
                        code = -32602,
                        message = "Missing 'name' parameter"
                    }
                };
            }

            var toolName = request.@params["name"].ToString();
            Log($"[SimpleMcpServer] Calling tool: {toolName}");

            if (!tools.TryGetValue(toolName, out var tool))
            {
                return new McpResponse
                {
                    id = request.id,
                    error = new McpError
                    {
                        code = -32602,
                        message = $"Tool not found: {toolName}"
                    }
                };
            }

            try
            {
                var args = request.@params.ContainsKey("arguments")
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(
                        JsonSerializer.Serialize(request.@params["arguments"]))
                    : new Dictionary<string, object>();

                string result;
                if (tool.Handler != null)
                {
                    result = await tool.Handler(args);
                }
                else
                {
                    // Invoke via reflection
                    var methodParams = tool.Method.GetParameters();
                    var invokeArgs = new List<object>();

                    foreach (var param in methodParams)
                    {
                        if (args.ContainsKey(param.Name))
                        {
                            invokeArgs.Add(args[param.Name]);
                        }
                        else if (param.HasDefaultValue)
                        {
                            invokeArgs.Add(param.DefaultValue);
                        }
                        else
                        {
                            invokeArgs.Add(null);
                        }
                    }

                    var invokeResult = tool.Method.Invoke(tool.Instance, invokeArgs.ToArray());
                    if (invokeResult is Task<string> taskResult)
                    {
                        result = await taskResult;
                    }
                    else
                    {
                        result = invokeResult?.ToString() ?? "null";
                    }
                }

                return new McpResponse
                {
                    id = request.id,
                    result = new
                    {
                        content = new[]
                        {
                            new
                            {
                                type = "text",
                                text = result
                            }
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                Log($"[SimpleMcpServer] Error invoking tool: {ex.Message}");
                return new McpResponse
                {
                    id = request.id,
                    error = new McpError
                    {
                        code = -32603,
                        message = $"Tool execution error: {ex.Message}",
                        data = ex.StackTrace
                    }
                };
            }
        }

        private void HandleError(Exception ex)
        {
            Log($"[SimpleMcpServer] Transport error: {ex.Message}");
        }

        private void Log(string message)
        {
            Console.Error.WriteLine(message);
            Console.Error.Flush();
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            transport?.Dispose();
        }

        private class ToolRegistration
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public MethodInfo Method { get; set; }
            public object Instance { get; set; }
            public Func<Dictionary<string, object>, Task<string>> Handler { get; set; }
        }
    }
}
