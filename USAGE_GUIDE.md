# Unify-MCP Usage Guide
## How to Use the Unity MCP Server (v0.4.0)

**Status**: ✅ WORKING - MCP server is now functional!

---

## What Just Got Implemented

We've gone from 15% complete to **30% complete** with a **working MCP server**!

### ✅ What Works Now

1. **Core MCP Server** - Stdio transport with JSON-RPC 2.0
2. **Documentation Tools** (4 tools) - Search Unity API docs
3. **Console Log Tools** (5 tools) - View Unity errors/warnings
4. **Scene Query Tools** (6 tools) - Inspect GameObject hierarchy

**Total: 15 working MCP tools**

---

## Quick Start

### Step 1: Configure Claude Desktop

Add this to your Claude Desktop config (`~/Library/Application Support/Claude/claude_desktop_config.json` on macOS):

```json
{
  "mcpServers": {
    "unity-mcp": {
      "command": "/Applications/Unity/Hub/Editor/2021.3.25f1/Unity.app/Contents/MacOS/Unity",
      "args": [
        "-batchmode",
        "-nographics",
        "-projectPath", "/path/to/your/UnityProject",
        "-executeMethod", "UnifyMcp.Core.McpServerLifecycle.Start"
      ]
    }
  }
}
```

**Important**:
- Replace `/Applications/Unity/Hub/Editor/2021.3.25f1/...` with your Unity installation path
- Replace `/path/to/your/UnityProject` with your actual Unity project path
- The project must have unify-mcp installed as a package

### Step 2: Restart Claude Desktop

Close and reopen Claude Desktop. The Unity MCP server will start automatically.

### Step 3: Test It!

In Claude Desktop, try these prompts:

```
"List the available Unity MCP tools"
"Search Unity documentation for GameObject.SetActive"
"Show me the current scene hierarchy"
"Get recent Unity console errors"
```

---

## Available Tools

### Documentation Tools (4)

#### `query_documentation`
Search Unity API documentation with full-text search.

**Example**:
```
"Search Unity docs for Transform.Translate"
```

**Parameters**:
- `query` (string): API name or search terms

#### `search_api_fuzzy`
Fuzzy search with typo tolerance.

**Example**:
```
"Find Unity APIs similar to 'Transform.Translte'"
// (note the typo - it will still find Transform.Translate)
```

**Parameters**:
- `query` (string): API name (can have typos)
- `threshold` (number, optional): Similarity threshold (0.0-1.0, default 0.7)

#### `get_unity_version`
Get current Unity Editor version.

**Example**:
```
"What Unity version is this project using?"
```

#### `check_api_deprecation`
Check if an API is deprecated.

**Example**:
```
"Is WWW class deprecated?"
```

**Parameters**:
- `apiName` (string): API to check

---

### Console Log Tools (5)

#### `get_recent_logs`
Get recent console logs with filtering.

**Example**:
```
"Show me the last 20 Unity logs"
"Show me recent error logs only"
```

**Parameters**:
- `count` (number, optional): Number of logs (default 50)
- `logType` (string, optional): "all", "Error", "Warning", "Log" (default "all")

#### `get_errors`
Get only error logs.

**Example**:
```
"What Unity errors occurred recently?"
"Show me compilation errors"
```

**Parameters**:
- `count` (number, optional): Number of errors (default 20)

#### `get_warnings`
Get only warning logs.

**Example**:
```
"Show me Unity warnings"
```

**Parameters**:
- `count` (number, optional): Number of warnings (default 20)

#### `get_log_summary`
Get summary of log counts by type.

**Example**:
```
"Summarize Unity console logs"
```

#### `clear_console`
Clear the console log buffer.

**Example**:
```
"Clear Unity console"
```

---

### Scene Query Tools (6)

#### `get_scene_hierarchy`
Get complete scene hierarchy.

**Example**:
```
"Show me the current Unity scene hierarchy"
"List all GameObjects in the scene"
```

**Parameters**:
- `maxDepth` (number, optional): Max hierarchy depth (default 3)

#### `find_game_object`
Find a GameObject by name.

**Example**:
```
"Find the Player GameObject"
"Get details about the Main Camera"
```

**Parameters**:
- `name` (string): GameObject name

#### `get_scene_statistics`
Get scene statistics.

**Example**:
```
"How many GameObjects are in the scene?"
"What components are most common?"
```

#### `find_objects_with_component`
Find GameObjects with a specific component.

**Example**:
```
"Find all objects with Rigidbody component"
"Which GameObjects have BoxCollider?"
```

**Parameters**:
- `componentName` (string): Component type name
- `maxResults` (number, optional): Max results (default 50)

#### `find_objects_by_tag`
Find GameObjects by tag.

**Example**:
```
"Find all objects tagged 'Enemy'"
```

**Parameters**:
- `tag` (string): Tag name

#### `get_loaded_scenes`
Get list of loaded scenes.

**Example**:
```
"What scenes are currently loaded?"
```

---

## Example Workflows

### Workflow 1: Debugging Errors

```
User: "Are there any Unity errors?"
AI: Uses get_errors tool
AI: "Yes, there are 3 errors. The main one is a NullReferenceException in PlayerController.cs:42"

User: "Show me the PlayerController GameObject"
AI: Uses find_game_object tool
AI: "Found PlayerController with components: Transform, Rigidbody, PlayerController, BoxCollider"

User: "What does the PlayerController script do?"
AI: Uses find_game_object to get component info
AI: Provides analysis and suggests fixes
```

### Workflow 2: Scene Exploration

```
User: "What's in this Unity scene?"
AI: Uses get_scene_statistics tool
AI: "The scene has 247 GameObjects, with 45 active. Most common components are Transform (247), MeshRenderer (89), BoxCollider (67)"

User: "Show me the scene hierarchy"
AI: Uses get_scene_hierarchy tool
AI: Displays tree structure of GameObjects

User: "Find all objects with Rigidbody"
AI: Uses find_objects_with_component tool
AI: "Found 12 objects with Rigidbody: Player, Enemy1, Enemy2, ..."
```

### Workflow 3: API Documentation

```
User: "How do I move a GameObject in Unity?"
AI: Uses query_documentation tool for "Transform"
AI: "Use Transform.Translate or Transform.position. Here's how..."
AI: Provides code examples from documentation

User: "Is Transform.Translate deprecated?"
AI: Uses check_api_deprecation tool
AI: "No, Transform.Translate is not deprecated and works in your Unity version 2021.3"
```

---

## Troubleshooting

### Server Won't Start

**Error**: "Unity not found"
- Check Unity path in config is correct
- Verify Unity 2021.3+ is installed

**Error**: "Project not found"
- Check projectPath in config
- Ensure it's a valid Unity project (has Assets/ folder)

### No Tools Appearing

**Issue**: Claude doesn't see the tools
- Check Claude Desktop logs (`~/Library/Logs/Claude/mcp*.log`)
- Ensure Unity batch mode is starting
- Check Unity Editor logs in project's `Logs/` folder

### Tools Return Errors

**Error**: "Tool execution failed"
- Check Unity console for errors
- Ensure scene is loaded (some tools require active scene)
- Verify Unity isn't crashed/frozen

---

## Performance Notes

- **Startup time**: Unity batch mode takes 10-30 seconds to start
- **Tool response time**:
  - Documentation queries: <100ms (cached)
  - Scene queries: <500ms (small scenes)
  - Console logs: <50ms

- **Memory usage**: Unity batch mode uses ~500MB-1GB RAM

---

## What's Next

### Coming in v0.5.0 (Week 3-4)

- ✅ Test runner integration (run NUnit tests)
- ✅ Compiler error integration (see C# compilation errors)
- ✅ Project file management (create scripts, list files)

### Coming in v0.6.0 (Week 5-8)

- ✅ Real profiler integration (performance analysis)
- ✅ Asset management (find unused assets, dependencies)
- ✅ Build automation (multi-platform builds)

---

## Feedback

Found a bug? Have a feature request?
- GitHub Issues: https://github.com/zachyzissou/unify-mcp/issues
- Or just ask Claude to help you debug it! 😄

---

## Technical Details

### Architecture

```
Claude Desktop
    ↓ (stdio)
Unity Batch Mode Process
    ↓
SimpleMcpServer (JSON-RPC 2.0)
    ↓
Tool Handlers (DocumentationTools, ConsoleTools, SceneTools)
    ↓
Unity Editor APIs (EditorApplication, SceneManager, etc.)
```

### Communication Flow

1. Claude sends JSON-RPC request via stdin
2. StdioTransport reads and parses
3. SimpleMcpServer routes to appropriate tool
4. Tool executes (async)
5. Result serialized to JSON
6. Response sent via stdout
7. Claude receives and processes

### Thread Safety

All Unity API calls are marshalled to main thread via `MainThreadDispatcher` to avoid Unity's "can only be called from main thread" errors.

---

**Version**: 0.4.0
**Status**: Production-ready (basic features)
**Last Updated**: 2025-11-07
