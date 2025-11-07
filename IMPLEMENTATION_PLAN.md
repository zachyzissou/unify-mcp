# Unify-MCP Implementation Plan
## Transforming Architecture into Production Reality

**Goal**: Ship the best Unity MCP server in the ecosystem within 12 weeks

**Current Status**: 15% complete (Documentation system working, everything else stubbed)

**Target Status**: 100% complete with market-leading features

---

## Overview

### Phases
1. **Phase 1: Foundation** (Weeks 1-2) - Make it actually work
2. **Phase 2: Essential Tools** (Weeks 3-4) - Make it useful
3. **Phase 3: Advanced Features** (Weeks 5-8) - Make it competitive
4. **Phase 4: Market Leadership** (Weeks 9-12) - Make it dominant

### Success Metrics
- **Week 2**: Working MCP server, can query docs from Claude
- **Week 4**: Console logs + tests working, better than basic MCPs
- **Week 8**: Profiler + Assets working, competitive with uLoopMCP
- **Week 12**: Feature-complete, industry leading

---

## Phase 1: Foundation (Weeks 1-2)
**Objective**: Get the MCP protocol actually working

### Task 1.1: MCP Server Implementation (3 days)
**Priority**: P0 - BLOCKER
**File**: `src/Core/McpServerLifecycle.cs`

#### Current State
```csharp
// TODO: Initialize ModelContextProtocol server (Phase 4)
// TODO: Initialize stdio transport (Phase 4)
```

#### Implementation Steps

1. **Create StdioTransport wrapper** (4 hours)
   - File: `src/Core/Transport/StdioTransport.cs`
   - Wrap stdin/stdout for JSON-RPC 2.0
   - Handle async read/write
   - Buffer management for large responses

2. **Initialize MCP Server** (4 hours)
   - Update `McpServerLifecycle.Start()`
   - Create `ModelContextProtocol.McpServer` instance
   - Configure ServerInfo (name, version, capabilities)
   - Connect stdio transport

3. **Tool Registration System** (4 hours)
   - Create `ToolRegistry.cs`
   - Scan assemblies for `[McpServerTool]` attributes
   - Generate schemas from method signatures
   - Register with MCP server

4. **Integration Testing** (4 hours)
   - Test with Claude Desktop
   - Test with MCP Inspector
   - Verify tool discovery
   - Test request/response cycle

#### Code Example
```csharp
// src/Core/McpServerLifecycle.cs
using ModelContextProtocol;
using ModelContextProtocol.Protocol.Server;

private McpServer mcpServer;
private StdioTransport transport;

public void Start()
{
    if (isDisposed) throw new ObjectDisposedException(nameof(McpServerLifecycle));
    if (isRunning) return;

    try
    {
        // Initialize MainThreadDispatcher
        if (MainThreadDispatcher.Instance == null)
        {
            MainThreadDispatcher.InitializeInstance();
        }

        // Create MCP server
        mcpServer = new McpServer(new McpServerOptions
        {
            ServerInfo = new ServerInfo
            {
                Name = "unity-mcp",
                Version = "0.4.0",
                Description = "Unity Editor MCP Server"
            },
            Capabilities = new ServerCapabilities
            {
                Tools = new ToolsCapability { },
                Logging = new LoggingCapability { }
            }
        });

        // Register tools
        RegisterTools();

        // Create and connect transport
        transport = new StdioTransport();
        await mcpServer.ConnectAsync(transport);

        isRunning = true;
        OnStarted?.Invoke();

        Debug.Log("[UnifyMCP] Server started successfully");
    }
    catch (Exception ex)
    {
        OnError?.Invoke(ex);
        Debug.LogError($"[UnifyMCP] Failed to start server: {ex.Message}");
        throw;
    }
}

private void RegisterTools()
{
    // Documentation Tools
    var docTools = new DocumentationTools(GetDatabasePath());
    mcpServer.AddTool("query_documentation",
        "Search Unity API documentation",
        async (args) => await docTools.QueryDocumentation(args["query"].ToString()));

    mcpServer.AddTool("search_api_fuzzy",
        "Fuzzy search for Unity API names",
        async (args) => await docTools.SearchApiFuzzy(
            args["query"].ToString(),
            args.ContainsKey("threshold") ? (double)args["threshold"] : 0.7));

    // ... more tools
}
```

#### Success Criteria
- ✅ Claude Desktop can connect to server
- ✅ `query_documentation` tool appears in Claude's tool list
- ✅ Can successfully query documentation
- ✅ Server logs show connection and requests

---

### Task 1.2: Console Log Streaming (2 days)
**Priority**: P1 - HIGH
**File**: `src/Tools/Unity/UnityConsoleTools.cs`

#### Why This Matters
Without console logs, AI agents are blind to Unity errors, warnings, and compilation issues. This is the #1 requested feature from Unity developers using AI assistants.

#### Implementation Steps

1. **Create Console Log Tool** (4 hours)
   - New file: `src/Tools/Unity/UnityConsoleTools.cs`
   - Hook `Application.logMessageReceived`
   - Buffer recent logs (last 100 entries)
   - Support filtering by log type

2. **MCP Notifications for Real-time Logs** (4 hours)
   - Send MCP notifications on new logs
   - Rate limiting (max 10/second)
   - Batch updates for performance

3. **Control Panel Integration** (2 hours)
   - Display live logs in Control Panel
   - Color coding by severity
   - Export logs to file

4. **Testing** (2 hours)
   - Trigger errors/warnings
   - Verify AI receives logs
   - Test filtering

#### Code Example
```csharp
// src/Tools/Unity/UnityConsoleTools.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnifyMcp.Tools.Unity
{
    public class UnityConsoleTools : IDisposable
    {
        private readonly List<LogEntry> recentLogs = new List<LogEntry>();
        private readonly int maxLogBufferSize = 100;
        private Action<LogEntry> onNewLog;

        public UnityConsoleTools()
        {
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

            lock (recentLogs)
            {
                recentLogs.Add(entry);
                if (recentLogs.Count > maxLogBufferSize)
                {
                    recentLogs.RemoveAt(0);
                }
            }

            // Notify listeners (for MCP notifications)
            onNewLog?.Invoke(entry);
        }

        [McpServerTool]
        public async Task<string> GetRecentLogs(int count = 50, string logType = "all")
        {
            return await Task.Run(() =>
            {
                IEnumerable<LogEntry> filtered = recentLogs;

                if (logType != "all")
                {
                    var typeFilter = Enum.Parse<LogType>(logType, true);
                    filtered = filtered.Where(l => l.Type == typeFilter);
                }

                var result = filtered.TakeLast(count).Select(l => new
                {
                    message = l.Message,
                    stackTrace = l.StackTrace,
                    type = l.Type.ToString(),
                    timestamp = l.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
                });

                return System.Text.Json.JsonSerializer.Serialize(result,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            });
        }

        [McpServerTool]
        public async Task<string> ClearConsole()
        {
            return await Task.Run(() =>
            {
                lock (recentLogs)
                {
                    recentLogs.Clear();
                }
                return "{\"status\": \"cleared\"}";
            });
        }

        public void SetLogNotificationHandler(Action<LogEntry> handler)
        {
            onNewLog = handler;
        }

        public void Dispose()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
        }
    }

    public class LogEntry
    {
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public LogType Type { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
```

#### Success Criteria
- ✅ AI can query recent console logs
- ✅ AI receives real-time notifications of errors
- ✅ Logs include stack traces
- ✅ Can filter by log type (Error/Warning/Info)

---

### Task 1.3: Basic Scene Query Tools (2 days)
**Priority**: P1 - HIGH
**File**: `src/Tools/Unity/SceneQueryTools.cs`

#### Implementation Steps

1. **Scene Hierarchy Query** (3 hours)
   - Get all root GameObjects
   - Recursive children traversal
   - Component listing
   - Support depth limiting

2. **GameObject Inspection** (3 hours)
   - Get GameObject by name/path
   - List all components
   - Read SerializedObject properties
   - Handle null references

3. **Scene Statistics** (2 hours)
   - GameObject count
   - Component breakdown
   - Active/Inactive counts
   - Memory estimates

#### Code Example
```csharp
// src/Tools/Unity/SceneQueryTools.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

namespace UnifyMcp.Tools.Unity
{
    public class SceneQueryTools
    {
        [McpServerTool]
        public async Task<string> GetSceneHierarchy(int maxDepth = 3)
        {
            return await Task.Run(() =>
            {
                var scene = SceneManager.GetActiveScene();
                var rootObjects = scene.GetRootGameObjects();

                var hierarchy = rootObjects.Select(obj =>
                    SerializeGameObject(obj, 0, maxDepth)).ToArray();

                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    sceneName = scene.name,
                    rootCount = rootObjects.Length,
                    hierarchy
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            });
        }

        [McpServerTool]
        public async Task<string> FindGameObject(string name)
        {
            return await Task.Run(() =>
            {
                var obj = GameObject.Find(name);
                if (obj == null)
                {
                    return "{\"found\": false}";
                }

                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    found = true,
                    name = obj.name,
                    tag = obj.tag,
                    layer = LayerMask.LayerToName(obj.layer),
                    active = obj.activeInHierarchy,
                    components = obj.GetComponents<Component>()
                        .Select(c => c.GetType().Name).ToArray(),
                    position = obj.transform.position,
                    rotation = obj.transform.rotation.eulerAngles,
                    scale = obj.transform.localScale
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            });
        }

        private object SerializeGameObject(GameObject obj, int depth, int maxDepth)
        {
            var result = new Dictionary<string, object>
            {
                ["name"] = obj.name,
                ["active"] = obj.activeInHierarchy,
                ["tag"] = obj.tag,
                ["components"] = obj.GetComponents<Component>()
                    .Select(c => c.GetType().Name).ToArray()
            };

            if (depth < maxDepth && obj.transform.childCount > 0)
            {
                var children = new List<object>();
                foreach (Transform child in obj.transform)
                {
                    children.Add(SerializeGameObject(child.gameObject, depth + 1, maxDepth));
                }
                result["children"] = children;
            }
            else if (obj.transform.childCount > 0)
            {
                result["childCount"] = obj.transform.childCount;
            }

            return result;
        }

        [McpServerTool]
        public async Task<string> GetSceneStatistics()
        {
            return await Task.Run(() =>
            {
                var scene = SceneManager.GetActiveScene();
                var allObjects = scene.GetRootGameObjects()
                    .SelectMany(GetAllGameObjects).ToArray();

                var componentCounts = allObjects
                    .SelectMany(obj => obj.GetComponents<Component>())
                    .GroupBy(c => c.GetType().Name)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count());

                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    sceneName = scene.name,
                    totalGameObjects = allObjects.Length,
                    activeGameObjects = allObjects.Count(obj => obj.activeInHierarchy),
                    inactiveGameObjects = allObjects.Count(obj => !obj.activeInHierarchy),
                    topComponents = componentCounts
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            });
        }

        private IEnumerable<GameObject> GetAllGameObjects(GameObject root)
        {
            yield return root;
            foreach (Transform child in root.transform)
            {
                foreach (var obj in GetAllGameObjects(child.gameObject))
                {
                    yield return obj;
                }
            }
        }
    }
}
```

#### Success Criteria
- ✅ AI can query scene hierarchy
- ✅ AI can find GameObjects by name
- ✅ AI can inspect components
- ✅ Performance acceptable for large scenes (>1000 objects)

---

### Task 1.4: End-to-End Testing & Documentation (1 day)
**Priority**: P1 - HIGH

#### Implementation Steps

1. **Integration Tests** (4 hours)
   - Test MCP server startup
   - Test tool registration
   - Test request/response cycle
   - Test error handling

2. **User Documentation** (2 hours)
   - Quick start guide
   - Installation instructions
   - Example prompts for AI
   - Troubleshooting guide

3. **Demo Video** (2 hours)
   - Show installation
   - Show connecting with Claude
   - Show querying docs, logs, scene
   - Show fixing an error with AI help

#### Example Integration Test
```csharp
// tests/Integration/McpServerIntegrationTests.cs
[TestFixture]
public class McpServerIntegrationTests
{
    [Test]
    public async Task Server_ShouldStartAndAcceptConnections()
    {
        // Arrange
        var lifecycle = new McpServerLifecycle();

        // Act
        lifecycle.Start();
        await Task.Delay(1000); // Wait for startup

        // Assert
        Assert.IsTrue(lifecycle.IsRunning);
    }

    [Test]
    public async Task DocumentationTool_ShouldReturnResults()
    {
        // Arrange
        var lifecycle = new McpServerLifecycle();
        lifecycle.Start();
        await Task.Delay(1000);

        // Simulate MCP request
        var request = new McpRequest
        {
            Method = "tools/call",
            Params = new
            {
                name = "query_documentation",
                arguments = new { query = "GameObject" }
            }
        };

        // Act
        var response = await SendMcpRequest(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsTrue(response.Contains("GameObject"));
    }
}
```

---

## Phase 1 Deliverables

### Week 1 End:
- ✅ MCP server running
- ✅ Can connect from Claude Desktop
- ✅ Documentation queries working
- ✅ Console logs accessible

### Week 2 End:
- ✅ Scene queries working
- ✅ Integration tests passing
- ✅ User documentation complete
- ✅ Demo video published
- ✅ **v0.4.0 release** - "First Working Version"

### Code Quality Checklist
- ✅ All new code has unit tests
- ✅ No commented-out code
- ✅ XML documentation on public APIs
- ✅ Error handling with proper exceptions
- ✅ Async/await used correctly
- ✅ Thread-safe Unity API calls

---

## Phase 2: Essential Developer Tools (Weeks 3-4)
**Objective**: Make it genuinely useful for daily development

### Task 2.1: Test Runner Integration (3 days)
**Priority**: P1 - HIGH
**File**: `src/Tools/Testing/UnityTestTools.cs`

#### Why This Matters
TDD workflows require running tests and seeing results. AI agents need to verify their code changes work.

#### Implementation Steps

1. **Unity Test Framework Integration** (1 day)
   - Reference `UnityEngine.TestRunner`
   - Use `TestRunnerApi` for programmatic test execution
   - Handle Edit Mode and Play Mode tests
   - Capture results with stack traces

2. **Test Discovery** (0.5 day)
   - List all available tests
   - Group by assembly/namespace
   - Filter by category/name

3. **Test Execution** (1 day)
   - Run all tests
   - Run specific test by name
   - Run tests matching pattern
   - Handle async tests
   - Timeout handling

4. **Result Reporting** (0.5 day)
   - Format results for AI consumption
   - Include pass/fail counts
   - Stack traces for failures
   - Performance metrics

#### Code Example
```csharp
// src/Tools/Testing/UnityTestTools.cs
using UnityEditor.TestTools.TestRunner.Api;
using System.Collections.Generic;
using System.Linq;

namespace UnifyMcp.Tools.Testing
{
    public class UnityTestTools : IDisposable
    {
        private readonly TestRunnerApi testRunner;
        private readonly List<TestResult> latestResults = new List<TestResult>();
        private bool isRunning = false;

        public UnityTestTools()
        {
            testRunner = ScriptableObject.CreateInstance<TestRunnerApi>();
            testRunner.RegisterCallbacks(new TestCallbacks(this));
        }

        [McpServerTool]
        public async Task<string> RunAllTests()
        {
            return await RunTests(null);
        }

        [McpServerTool]
        public async Task<string> RunTestByName(string testName)
        {
            var filter = new Filter
            {
                testNames = new[] { testName }
            };
            return await RunTests(filter);
        }

        [McpServerTool]
        public async Task<string> RunTestsMatchingPattern(string pattern)
        {
            var filter = new Filter
            {
                testMode = TestMode.EditMode | TestMode.PlayMode
            };
            return await RunTests(filter);
        }

        private async Task<string> RunTests(Filter filter)
        {
            if (isRunning)
            {
                return "{\"error\": \"Tests already running\"}";
            }

            isRunning = true;
            latestResults.Clear();

            // Run on main thread
            await MainThreadDispatcher.EnqueueAsync(() =>
            {
                testRunner.Execute(new ExecutionSettings(filter));
            });

            // Wait for completion
            while (isRunning)
            {
                await Task.Delay(100);
            }

            return FormatResults();
        }

        private string FormatResults()
        {
            var passed = latestResults.Count(r => r.TestStatus == TestStatus.Passed);
            var failed = latestResults.Count(r => r.TestStatus == TestStatus.Failed);
            var skipped = latestResults.Count(r => r.TestStatus == TestStatus.Skipped);

            var failures = latestResults
                .Where(r => r.TestStatus == TestStatus.Failed)
                .Select(r => new
                {
                    test = r.FullName,
                    message = r.Message,
                    stackTrace = r.StackTrace
                });

            return System.Text.Json.JsonSerializer.Serialize(new
            {
                summary = new
                {
                    total = latestResults.Count,
                    passed,
                    failed,
                    skipped,
                    duration = latestResults.Sum(r => r.Duration)
                },
                failures
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }

        private class TestCallbacks : ICallbacks
        {
            private readonly UnityTestTools parent;

            public TestCallbacks(UnityTestTools parent)
            {
                this.parent = parent;
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                parent.CollectResults(result);
                parent.isRunning = false;
            }

            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
        }

        private void CollectResults(ITestResultAdaptor result)
        {
            if (result.HasChildren)
            {
                foreach (var child in result.Children)
                {
                    CollectResults(child);
                }
            }
            else
            {
                latestResults.Add(new TestResult
                {
                    FullName = result.FullName,
                    TestStatus = result.TestStatus,
                    Message = result.Message,
                    StackTrace = result.StackTrace,
                    Duration = result.Duration
                });
            }
        }

        public void Dispose()
        {
            if (testRunner != null)
            {
                ScriptableObject.DestroyImmediate(testRunner);
            }
        }
    }

    public class TestResult
    {
        public string FullName { get; set; }
        public TestStatus TestStatus { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public double Duration { get; set; }
    }
}
```

#### Success Criteria
- ✅ AI can discover all tests
- ✅ AI can run specific tests
- ✅ Results include stack traces
- ✅ Edit Mode and Play Mode tests both work

---

### Task 2.2: Compiler Error Integration (2 days)
**Priority**: P1 - HIGH
**File**: `src/Tools/Unity/CompilerTools.cs`

#### Why This Matters
AI agents need to see compilation errors to fix them. This is essential for code generation workflows.

#### Implementation Steps

1. **Compilation Pipeline Hook** (1 day)
   - Hook `CompilationPipeline.assemblyCompilationFinished`
   - Capture compiler messages
   - Parse error/warning locations
   - Map to file paths

2. **Error Formatting** (0.5 day)
   - Format for AI readability
   - Include file, line, column
   - Include error message
   - Group by severity

3. **Real-time Notifications** (0.5 day)
   - Send MCP notifications on compile
   - Batch errors by compilation unit
   - Rate limiting

#### Code Example
```csharp
// src/Tools/Unity/CompilerTools.cs
using UnityEditor.Compilation;
using System.Collections.Generic;
using System.Linq;

namespace UnifyMcp.Tools.Unity
{
    public class CompilerTools
    {
        private readonly List<CompilerMessage> recentMessages = new List<CompilerMessage>();
        private Action<CompilerMessage[]> onCompilationFinished;

        public CompilerTools()
        {
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
        }

        private void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            lock (recentMessages)
            {
                recentMessages.Clear();
                recentMessages.AddRange(messages);
            }

            onCompilationFinished?.Invoke(messages);
        }

        [McpServerTool]
        public async Task<string> GetCompilerErrors()
        {
            return await Task.Run(() =>
            {
                lock (recentMessages)
                {
                    var errors = recentMessages.Where(m => m.type == CompilerMessageType.Error);
                    var warnings = recentMessages.Where(m => m.type == CompilerMessageType.Warning);

                    return System.Text.Json.JsonSerializer.Serialize(new
                    {
                        hasErrors = errors.Any(),
                        errorCount = errors.Count(),
                        warningCount = warnings.Count(),
                        errors = errors.Select(FormatMessage),
                        warnings = warnings.Select(FormatMessage)
                    }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                }
            });
        }

        [McpServerTool]
        public async Task<string> TriggerRecompilation()
        {
            return await Task.Run(() =>
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    CompilationPipeline.RequestScriptCompilation();
                });

                return "{\"status\": \"recompilation_requested\"}";
            });
        }

        private object FormatMessage(CompilerMessage msg)
        {
            return new
            {
                type = msg.type.ToString(),
                message = msg.message,
                file = msg.file,
                line = msg.line,
                column = msg.column
            };
        }

        public void SetCompilationFinishedHandler(Action<CompilerMessage[]> handler)
        {
            onCompilationFinished = handler;
        }

        public void Dispose()
        {
            CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished;
        }
    }
}
```

#### Success Criteria
- ✅ AI can query compilation errors
- ✅ Errors include file/line/column
- ✅ AI receives notifications on compile
- ✅ Can trigger recompilation

---

### Task 2.3: Project File Management (2 days)
**Priority**: P2 - MEDIUM
**File**: `src/Tools/Unity/ProjectTools.cs`

#### Implementation Steps

1. **File Operations** (1 day)
   - List files in project
   - Read file contents (with security validation)
   - Search files by pattern
   - Get file metadata

2. **Script Operations** (1 day)
   - Create new C# script
   - Create new test script
   - Add to assembly definition
   - Template system

#### Code Example
```csharp
// src/Tools/Unity/ProjectTools.cs
using System.IO;
using UnityEditor;
using UnifyMcp.Common.Security;

namespace UnifyMcp.Tools.Unity
{
    public class ProjectTools
    {
        private readonly PathValidator pathValidator;

        public ProjectTools()
        {
            pathValidator = new PathValidator(Application.dataPath);
        }

        [McpServerTool]
        public async Task<string> ListScripts(string directory = "Assets")
        {
            return await Task.Run(() =>
            {
                var path = Path.Combine(Application.dataPath,
                    directory.Replace("Assets/", ""));
                pathValidator.ValidateOrThrow(path);

                var files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
                    .Select(f => f.Replace(Application.dataPath, "Assets"))
                    .Select(f => f.Replace("\\", "/"));

                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    directory,
                    count = files.Count(),
                    files
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            });
        }

        [McpServerTool]
        public async Task<string> CreateScript(string name, string directory = "Assets/Scripts")
        {
            return await Task.Run(async () =>
            {
                var fullPath = Path.Combine(Application.dataPath,
                    directory.Replace("Assets/", ""), $"{name}.cs");
                pathValidator.ValidateOrThrow(fullPath);

                if (File.Exists(fullPath))
                {
                    return "{\"error\": \"File already exists\"}";
                }

                var template = GetScriptTemplate(name);

                await MainThreadDispatcher.EnqueueAsync(() =>
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                    File.WriteAllText(fullPath, template);
                    AssetDatabase.Refresh();
                });

                return System.Text.Json.JsonSerializer.Serialize(new
                {
                    created = true,
                    path = directory + "/" + name + ".cs"
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            });
        }

        private string GetScriptTemplate(string className)
        {
            return $@"using UnityEngine;

namespace YourNamespace
{{
    public class {className} : MonoBehaviour
    {{
        void Start()
        {{

        }}

        void Update()
        {{

        }}
    }}
}}";
        }
    }
}
```

#### Success Criteria
- ✅ AI can list project scripts
- ✅ AI can create new scripts
- ✅ Security validation prevents path traversal
- ✅ AssetDatabase refreshes automatically

---

## Phase 2 Deliverables

### Week 3 End:
- ✅ Test runner working
- ✅ Compiler errors accessible
- ✅ AI can do TDD workflow

### Week 4 End:
- ✅ Project file management
- ✅ Script creation
- ✅ **v0.5.0 release** - "Developer Essentials"
- ✅ **Better than basic Unity MCPs**

---

## Phase 3: Advanced Features (Weeks 5-8)
**Objective**: Competitive with uLoopMCP + unique advantages

### Task 3.1: Real Profiler Integration (1 week)
**Priority**: P1 - HIGH
**File**: `src/Tools/Profiler/ProfilerTools.cs`

#### Current State: STUBBED
```csharp
TotalCpuTime = 16.7f, // Hardcoded!
AverageFps = 60.0f // Hardcoded!
```

#### Implementation Steps

1. **ProfilerRecorder Integration** (2 days)
   - Use `Unity.Profiling.ProfilerRecorder`
   - Capture CPU markers
   - Capture memory allocations
   - Capture render statistics

2. **Frame Debugger Control** (1 day)
   - `UnityEditor.FrameDebuggerUtility`
   - Capture frame events
   - Analyze draw calls
   - Shader analysis

3. **Memory Profiler** (2 days)
   - `UnityEditor.Profiling.Memory.Experimental`
   - Capture memory snapshots
   - Analyze allocations
   - Detect leaks

4. **Performance Analysis** (2 days)
   - Bottleneck detection
   - Anti-pattern detection
   - Optimization recommendations

#### Code Example
```csharp
// src/Tools/Profiler/ProfilerTools.cs (Real Implementation)
using Unity.Profiling;
using UnityEditor.Profiling;
using System.Collections.Generic;

namespace UnifyMcp.Tools.Profiler
{
    public class ProfilerTools : IDisposable
    {
        private ProfilerRecorder mainThreadTimeRecorder;
        private ProfilerRecorder gcAllocRecorder;
        private ProfilerRecorder drawCallsRecorder;
        private ProfilerRecorder trianglesRecorder;

        public ProfilerTools()
        {
            mainThreadTimeRecorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Internal, "Main Thread", 15);
            gcAllocRecorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Memory, "GC.Alloc", 15);
            drawCallsRecorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Render, "Draw Calls Count", 15);
            trianglesRecorder = ProfilerRecorder.StartNew(
                ProfilerCategory.Render, "Triangles Count", 15);
        }

        [McpServerTool]
        public async Task<string> CaptureProfilerSnapshot(int frameCount = 300)
        {
            return await Task.Run(() =>
            {
                var snapshot = new ProfilerSnapshot
                {
                    FrameCount = frameCount,
                    CapturedAt = DateTime.UtcNow,

                    // Real data from Unity Profiler
                    TotalCpuTime = GetAverageValue(mainThreadTimeRecorder),
                    AverageFps = 1000.0f / GetAverageValue(mainThreadTimeRecorder),
                    GcAllocations = GetTotalValue(gcAllocRecorder),
                    DrawCalls = (int)GetAverageValue(drawCallsRecorder),
                    Triangles = (int)GetAverageValue(trianglesRecorder),

                    CpuTimes = GetCpuMarkers(),
                    Bottlenecks = AnalyzeBottlenecks()
                };

                return System.Text.Json.JsonSerializer.Serialize(snapshot,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            });
        }

        private float GetAverageValue(ProfilerRecorder recorder)
        {
            if (!recorder.Valid || recorder.Count == 0)
                return 0;

            double sum = 0;
            var samples = new List<ProfilerRecorderSample>(recorder.Capacity);
            recorder.CopyTo(samples);

            foreach (var sample in samples)
                sum += sample.Value;

            return (float)(sum / recorder.Count);
        }

        private long GetTotalValue(ProfilerRecorder recorder)
        {
            if (!recorder.Valid)
                return 0;

            return recorder.LastValue;
        }

        private Dictionary<string, float> GetCpuMarkers()
        {
            var markers = new Dictionary<string, float>();

            // Get all available CPU markers
            var allMarkers = new[]
            {
                "PlayerLoop",
                "Update.ScriptRunBehaviourUpdate",
                "Render.Mesh",
                "Physics.Processing",
                "Scripts.Update"
            };

            foreach (var markerName in allMarkers)
            {
                var recorder = ProfilerRecorder.StartNew(
                    ProfilerCategory.Scripts, markerName, 1);
                if (recorder.Valid)
                {
                    markers[markerName] = GetAverageValue(recorder);
                }
                recorder.Dispose();
            }

            return markers;
        }

        private List<Bottleneck> AnalyzeBottlenecks()
        {
            var bottlenecks = new List<Bottleneck>();

            // Analyze CPU time
            var mainThreadTime = GetAverageValue(mainThreadTimeRecorder);
            if (mainThreadTime > 16.7f) // 60 FPS threshold
            {
                bottlenecks.Add(new Bottleneck
                {
                    Location = "Main Thread",
                    Severity = mainThreadTime > 33.3f ?
                        BottleneckSeverity.Critical : BottleneckSeverity.High,
                    Category = "CPU",
                    CpuTime = mainThreadTime,
                    Recommendation = $"Main thread time {mainThreadTime:F1}ms exceeds 16.7ms target. " +
                        "Consider optimizing Update() loops or moving work to background threads."
                });
            }

            // Analyze GC allocations
            var gcAllocs = GetTotalValue(gcAllocRecorder);
            if (gcAllocs > 1024 * 100) // 100KB per frame
            {
                bottlenecks.Add(new Bottleneck
                {
                    Location = "Garbage Collection",
                    Severity = BottleneckSeverity.High,
                    Category = "Memory",
                    Recommendation = $"High GC allocations ({gcAllocs / 1024}KB/frame). " +
                        "Use object pooling and avoid LINQ in Update() loops."
                });
            }

            return bottlenecks;
        }

        [McpServerTool]
        public async Task<string> DetectAntipatterns()
        {
            return await Task.Run(() =>
            {
                var antiPatterns = new List<AntiPattern>();

                // Check for Find in Update
                var scriptsWithFindInUpdate = FindScriptsWithPattern(
                    "GameObject.Find", "void Update");

                foreach (var script in scriptsWithFindInUpdate)
                {
                    antiPatterns.Add(new AntiPattern
                    {
                        Name = "GameObject.Find in Update",
                        Description = "GameObject.Find called every frame",
                        Location = script,
                        Type = AntiPatternType.FindInUpdate,
                        Recommendation = "Cache reference in Start() or Awake()"
                    });
                }

                // Check for GetComponent in Update
                var scriptsWithGetComponentInUpdate = FindScriptsWithPattern(
                    "GetComponent", "void Update");

                foreach (var script in scriptsWithGetComponentInUpdate)
                {
                    antiPatterns.Add(new AntiPattern
                    {
                        Name = "GetComponent in Update",
                        Description = "GetComponent called every frame",
                        Location = script,
                        Type = AntiPatternType.GetComponentInUpdate,
                        Recommendation = "Cache component reference in Awake()"
                    });
                }

                return System.Text.Json.JsonSerializer.Serialize(antiPatterns,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            });
        }

        private List<string> FindScriptsWithPattern(string pattern1, string pattern2)
        {
            var results = new List<string>();
            var scripts = AssetDatabase.FindAssets("t:Script");

            foreach (var guid in scripts)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var content = File.ReadAllText(path);

                if (content.Contains(pattern1) && content.Contains(pattern2))
                {
                    // Simple heuristic - check if pattern1 appears after pattern2
                    var updateIndex = content.IndexOf(pattern2);
                    if (updateIndex >= 0)
                    {
                        var findIndex = content.IndexOf(pattern1, updateIndex);
                        if (findIndex > updateIndex)
                        {
                            results.Add(path);
                        }
                    }
                }
            }

            return results;
        }

        public void Dispose()
        {
            mainThreadTimeRecorder?.Dispose();
            gcAllocRecorder?.Dispose();
            drawCallsRecorder?.Dispose();
            trianglesRecorder?.Dispose();
        }
    }
}
```

#### Success Criteria
- ✅ Real profiler data from Unity
- ✅ Accurate bottleneck detection
- ✅ Anti-pattern detection works
- ✅ Performance overhead <5%

---

### Task 3.2: Real Asset Management (1 week)
**Priority**: P1 - HIGH
**File**: `src/Tools/Assets/AssetTools.cs`

#### Implementation Steps

1. **Dependency Analysis** (2 days)
   - Use `AssetDatabase.GetDependencies`
   - Build dependency graph
   - Find circular dependencies
   - Visualize in Control Panel

2. **Unused Asset Detection** (2 days)
   - Scan all scenes
   - Check Resources folders
   - Check ScriptableObjects
   - Mark unreferenced assets

3. **Batch Operations** (2 days)
   - Import settings modification
   - Texture compression
   - Audio format conversion
   - Model import optimization

4. **Asset Validation** (1 day)
   - Missing references
   - Naming conventions
   - Size limits
   - Format requirements

#### Code Example (abbreviated)
```csharp
[McpServerTool]
public async Task<string> FindUnusedAssets()
{
    return await Task.Run(() =>
    {
        var allAssets = AssetDatabase.GetAllAssetPaths()
            .Where(p => p.StartsWith("Assets/"))
            .Where(p => !AssetDatabase.IsValidFolder(p))
            .ToHashSet();

        var referencedAssets = new HashSet<string>();

        // Scan all scenes
        var scenePaths = AssetDatabase.FindAssets("t:Scene")
            .Select(AssetDatabase.GUIDToAssetPath);

        foreach (var scenePath in scenePaths)
        {
            var dependencies = AssetDatabase.GetDependencies(scenePath, true);
            foreach (var dep in dependencies)
            {
                referencedAssets.Add(dep);
            }
        }

        // Scan all prefabs
        var prefabPaths = AssetDatabase.FindAssets("t:Prefab")
            .Select(AssetDatabase.GUIDToAssetPath);

        foreach (var prefabPath in prefabPaths)
        {
            var dependencies = AssetDatabase.GetDependencies(prefabPath, true);
            foreach (var dep in dependencies)
            {
                referencedAssets.Add(dep);
            }
        }

        var unusedAssets = allAssets.Except(referencedAssets).ToArray();

        return System.Text.Json.JsonSerializer.Serialize(new
        {
            totalAssets = allAssets.Count,
            referencedAssets = referencedAssets.Count,
            unusedAssets = unusedAssets.Length,
            assets = unusedAssets.Take(100) // Limit results
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    });
}
```

#### Success Criteria
- ✅ Accurate unused asset detection
- ✅ Dependency graph analysis
- ✅ Batch operations work
- ✅ Performance acceptable for large projects

---

### Task 3.3: Build Automation (1 week)
**Priority**: P2 - MEDIUM
**File**: `src/Tools/Build/BuildTools.cs`

#### Implementation Steps

1. **BuildPlayer Integration** (2 days)
2. **BuildReport Analysis** (1 day)
3. **Asset Bundle Management** (2 days)
4. **Platform Switching** (1 day)
5. **Build Scripts** (1 day)

---

## Phase 3 Deliverables

### Week 6 End:
- ✅ Real profiler integration
- ✅ Performance bottleneck detection

### Week 8 End:
- ✅ Asset management working
- ✅ Build automation working
- ✅ **v0.6.0 release** - "Advanced Features"
- ✅ **Feature-complete, competitive with uLoopMCP**

---

## Phase 4: Market Leadership (Weeks 9-12)
**Objective**: Industry-leading Unity MCP server

### Task 4.1: Context Window Integration (1 week)
**Connect all the context optimization code that's sitting unused**

1. Wire up RequestDeduplicator
2. Wire up ResponseCacheManager
3. Wire up ToolResultSummarizer
4. Wire up TokenBudgetOptimizer
5. Measure 50-70% token reduction

### Task 4.2: Advanced Scene Tools (1 week)
1. Component modification
2. GameObject creation/deletion
3. Prefab operations
4. Scene validation rules

### Task 4.3: Visual Debugging (1 week)
1. Gizmo drawing from AI prompts
2. Debug.DrawLine from commands
3. Screenshot capture
4. Scene view control

### Task 4.4: Polish & Scale (1 week)
1. Performance optimization
2. Large project testing (10GB+)
3. Error handling improvements
4. Documentation polish
5. Marketing materials

---

## Implementation Guidelines

### Code Quality Standards

**Every new feature must have:**
1. ✅ Unit tests (min 80% coverage)
2. ✅ Integration test
3. ✅ XML documentation
4. ✅ Error handling
5. ✅ Security validation
6. ✅ Performance test

### Review Checklist
```
□ Passes all existing tests
□ New tests added
□ No compiler warnings
□ Thread-safe Unity API calls
□ Security validated (PathValidator)
□ Async/await used correctly
□ No hardcoded paths
□ Proper error messages
□ XML docs on public APIs
□ Example added to docs
```

### Git Workflow
```bash
# Feature branch naming
git checkout -b feature/mcp-server-implementation
git checkout -b feature/console-log-streaming
git checkout -b feature/test-runner-integration

# Commit message format
git commit -m "feat: implement MCP server initialization"
git commit -m "fix: handle null scene objects"
git commit -m "test: add profiler integration tests"
git commit -m "docs: update API reference"

# Release tagging
git tag -a v0.4.0 -m "First working MCP server"
git push origin v0.4.0
```

---

## Testing Strategy

### Unit Tests
- Test individual methods in isolation
- Mock Unity APIs where needed
- Fast execution (<1s total)

### Integration Tests
- Test full MCP request/response cycle
- Test tool registration
- Test Unity API integration
- Slower but comprehensive

### Performance Tests
- Measure tool execution time
- Test with large scenes (1000+ objects)
- Test with large projects (10GB+)
- Profiler overhead measurement

### Manual Testing Checklist
```
Phase 1:
□ Connect from Claude Desktop
□ Query documentation
□ View console logs
□ Query scene hierarchy
□ Handle errors gracefully

Phase 2:
□ Run tests via AI
□ Fix compilation errors with AI
□ Create new scripts via AI
□ TDD workflow end-to-end

Phase 3:
□ Capture real profiler data
□ Detect performance bottlenecks
□ Find unused assets
□ Build project from AI commands

Phase 4:
□ Context optimization working
□ Token usage reduced 50%+
□ Large project performance
□ All features polished
```

---

## Documentation Requirements

### User Documentation
1. **Quick Start Guide** - 5 minutes to first success
2. **Installation Guide** - All platforms
3. **Tool Reference** - All MCP tools documented
4. **Example Prompts** - Common AI workflows
5. **Troubleshooting** - Common issues

### Developer Documentation
1. **Architecture Guide** - System design
2. **API Reference** - All public APIs
3. **Contributing Guide** - How to add tools
4. **Testing Guide** - How to write tests
5. **Release Process** - How to cut releases

### Video Content
1. **Installation & Setup** (5 min)
2. **Basic Usage** (10 min)
3. **Advanced Features** (15 min)
4. **Case Studies** (various)

---

## Release Schedule

### v0.4.0 - "First Working Version" (Week 2)
- MCP server working
- Documentation queries
- Console logs
- Basic scene queries

### v0.5.0 - "Developer Essentials" (Week 4)
- Test runner
- Compiler errors
- Project file management
- **Better than basic MCPs**

### v0.6.0 - "Advanced Features" (Week 8)
- Real profiler integration
- Asset management
- Build automation
- **Competitive with uLoopMCP**

### v1.0.0 - "Production Ready" (Week 12)
- Context optimization live
- Visual debugging
- All features polished
- **Industry leading**

---

## Success Metrics

### Technical Metrics
- **Test Coverage**: >80%
- **Performance**: <100ms for common queries
- **Token Reduction**: 50-70% vs naive implementation
- **Uptime**: >99% (server doesn't crash)

### User Metrics
- **Week 2**: 10 users
- **Week 4**: 50 users
- **Week 8**: 200 users
- **Week 12**: 1000 users

### Quality Metrics
- **GitHub Stars**: 100+ by week 12
- **Issues Opened**: <20 open at any time
- **PR Review Time**: <24 hours
- **Release Cadence**: Every 2 weeks

---

## Risk Mitigation

### Technical Risks

**Risk**: MCP SDK breaking changes
- **Mitigation**: Pin to specific version, monitor releases
- **Contingency**: Fork SDK if needed

**Risk**: Unity version compatibility
- **Mitigation**: Test on 2021.3, 2022.3, 6000.0
- **Contingency**: Version-specific builds

**Risk**: Performance in large projects
- **Mitigation**: Pagination, streaming, caching
- **Contingency**: Performance mode toggle

### Resource Risks

**Risk**: Running out of time
- **Mitigation**: MVP first, iterate based on feedback
- **Contingency**: Drop Phase 4 features

**Risk**: Complexity creep
- **Mitigation**: Strict scope control, say no
- **Contingency**: Defer to v2.0

---

## Next Steps

### Immediate Actions (This Week)

1. **Day 1-2**: Implement MCP server
   - StdioTransport
   - Tool registration
   - Test with Claude Desktop

2. **Day 3-4**: Console log streaming
   - Application.logMessageReceived hook
   - MCP notifications
   - Testing

3. **Day 5**: Scene query tools
   - Hierarchy traversal
   - GameObject inspection

### Week 2 Actions

1. **Day 1-2**: Integration testing
2. **Day 3-4**: Documentation
3. **Day 5**: v0.4.0 release

### Week 3-4: Phase 2
1. Test runner
2. Compiler integration
3. Project tools
4. v0.5.0 release

---

## Conclusion

This plan transforms unify-mcp from a well-designed blueprint into the best Unity MCP server available. The architecture is already excellent - we just need to execute the implementation.

**Key Success Factors**:
1. ✅ Ship working MCP server ASAP (Week 2)
2. ✅ Focus on developer essentials first
3. ✅ Test with real users early
4. ✅ Iterate based on feedback
5. ✅ Maintain high code quality

**Expected Outcome**: Industry-leading Unity MCP server used by 1000+ developers within 12 weeks.

Let's build this! 🚀
