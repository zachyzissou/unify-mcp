# MCP Protocol Examples

This document provides practical examples of using Unity MCP Server with MCP-compatible clients like Claude Desktop and VS Code.

## Table of Contents

1. [Configuration](#configuration)
2. [Documentation Tools](#documentation-tools)
3. [Profiler Tools](#profiler-tools)
4. [Build Tools](#build-tools)
5. [Asset Tools](#asset-tools)
6. [Scene Tools](#scene-tools)
7. [Package Tools](#package-tools)
8. [Context Optimization](#context-optimization)

## Configuration

### Claude Desktop Setup

**Location**: `~/Library/Application Support/Claude/claude_desktop_config.json` (macOS) or `%APPDATA%/Claude/claude_desktop_config.json` (Windows)

```json
{
  "mcpServers": {
    "unity-mcp": {
      "command": "/path/to/unity-mcp-server",
      "args": ["--stdio"],
      "env": {
        "UNITY_PROJECT_PATH": "/path/to/your/unity/project",
        "UNITY_VERSION": "2022.3",
        "MCP_LOG_LEVEL": "info"
      }
    }
  }
}
```

### VS Code MCP Extension Setup

**Location**: `.vscode/mcp.json` in your workspace

```json
{
  "servers": [
    {
      "name": "Unity MCP Server",
      "transport": "stdio",
      "command": "/path/to/unity-mcp-server",
      "args": ["--stdio"],
      "env": {
        "UNITY_PROJECT_PATH": "${workspaceFolder}",
        "UNITY_VERSION": "2022.3"
      }
    }
  ]
}
```

## Documentation Tools

### 1. QueryDocumentation

**Purpose**: Full-text search of Unity API documentation

**MCP Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "QueryDocumentation",
    "arguments": {
      "query": "GameObject.SetActive"
    }
  }
}
```

**Expected Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "[{\"className\":\"GameObject\",\"methodName\":\"SetActive\",\"returnType\":\"void\",\"parameters\":[\"bool value\"],\"description\":\"Activates/Deactivates the GameObject, depending on the given true or false value.\",\"codeExamples\":[\"myGameObject.SetActive(true);\"],\"unityVersion\":\"2022.3\",\"documentationUrl\":\"https://docs.unity3d.com/ScriptReference/GameObject.SetActive.html\",\"isDeprecated\":false}]"
      }
    ]
  }
}
```

**Claude Desktop Example**:
```
You: "How do I activate a GameObject in Unity?"

AI: [Uses QueryDocumentation tool]
"To activate a GameObject, use the SetActive method. Here's an example:

```csharp
myGameObject.SetActive(true);  // Activate
myGameObject.SetActive(false); // Deactivate
```

This is a common pattern for toggling UI elements, spawning objects, etc."
```

### 2. SearchApiFuzzy

**Purpose**: Typo-tolerant fuzzy search

**MCP Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "SearchApiFuzzy",
    "arguments": {
      "query": "GameObject.SetActiv",
      "threshold": 0.7
    }
  }
}
```

**Expected Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "[{\"api\":\"GameObject.SetActive\",\"similarity\":0.95,\"suggestion\":\"Did you mean GameObject.SetActive?\"}]"
      }
    ]
  }
}
```

**Claude Desktop Example**:
```
You: "What's GameObject.SetActiv?"

AI: [Uses SearchApiFuzzy tool]
"Did you mean GameObject.SetActive? It's a method to activate/deactivate GameObjects."
```

### 3. CheckDeprecation

**Purpose**: Check if API is deprecated

**MCP Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
    "name": "CheckDeprecation",
    "arguments": {
      "apiName": "Application.loadedLevelName"
    }
  }
}
```

**Expected Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"apiName\":\"Application.loadedLevelName\",\"isDeprecated\":true,\"replacementApi\":\"SceneManager.GetActiveScene().name\",\"sinceVersion\":\"5.3\",\"message\":\"Use SceneManager API instead\"}"
      }
    ]
  }
}
```

**Claude Desktop Example**:
```
You: "Is Application.loadedLevelName still valid?"

AI: [Uses CheckDeprecation tool]
"No, Application.loadedLevelName has been deprecated since Unity 5.3.
Use SceneManager.GetActiveScene().name instead."
```

### 4. GetCodeExamples

**Purpose**: Get usage examples for Unity APIs

**MCP Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "method": "tools/call",
  "params": {
    "name": "GetCodeExamples",
    "arguments": {
      "apiName": "Instantiate"
    }
  }
}
```

**Expected Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"examples\":[\"GameObject clone = Instantiate(original, position, rotation);\",\"Instantiate(prefab, parent);\"]}"
      }
    ]
  }
}
```

## Profiler Tools

### 5. CaptureProfilerSnapshot

**Purpose**: Capture Unity Profiler data

**MCP Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "method": "tools/call",
  "params": {
    "name": "CaptureProfilerSnapshot",
    "arguments": {
      "frameCount": 300
    }
  }
}
```

**Expected Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"frameCount\":300,\"cpuTimes\":{\"PlayerController.Update\":15.2,\"Rendering\":8.5},\"gcAllocations\":{\"PlayerController.Update\":5120},\"bottlenecks\":[{\"location\":\"PlayerController.Update\",\"severity\":\"High\",\"recommendation\":\"Cache GetComponent<> calls\"}]}"
      }
    ]
  }
}
```

**Claude Desktop Example**:
```
You: "Profile my game's performance"

AI: [Uses CaptureProfilerSnapshot tool]
"I found a performance bottleneck:

**PlayerController.Update()** is taking 15.2ms per frame (should be < 5ms).

Issue: GetComponent<> is called every frame (5KB allocations).

Recommendation: Cache component references in Start():

```csharp
private Rigidbody rb;

void Start() {
    rb = GetComponent<Rigidbody>(); // Cache it
}

void Update() {
    rb.velocity = ...; // Use cached reference
}
```"
```

### 6. AnalyzeBottlenecks

**Purpose**: Identify performance bottlenecks

**MCP Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 6,
  "method": "tools/call",
  "params": {
    "name": "AnalyzeBottlenecks",
    "arguments": {
      "threshold": 5.0
    }
  }
}
```

### 7. DetectAntipatterns

**Purpose**: Find common performance antipatterns

**MCP Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "method": "tools/call",
  "params": {
    "name": "DetectAntipatterns"
  }
}
```

## Build Tools

### 8. ValidateBuildConfiguration

**Purpose**: Validate build settings for target platform

**MCP Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 8,
  "method": "tools/call",
  "params": {
    "name": "ValidateBuildConfiguration",
    "arguments": {
      "platform": "Windows"
    }
  }
}
```

**Expected Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 8,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"platform\":\"Windows\",\"valid\":true,\"warnings\":[],\"errors\":[]}"
      }
    ]
  }
}
```

### 9. StartMultiPlatformBuild

**Purpose**: Build for multiple platforms

**MCP Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 9,
  "method": "tools/call",
  "params": {
    "name": "StartMultiPlatformBuild",
    "arguments": {
      "platforms": ["Windows", "macOS", "Linux"]
    }
  }
}
```

### 10. GetBuildSizeAnalysis

**Purpose**: Analyze build size breakdown

**MCP Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 10,
  "method": "tools/call",
  "params": {
    "name": "GetBuildSizeAnalysis"
  }
}
```

## Asset Tools

### 11. FindUnusedAssets

**Purpose**: Find assets not referenced in scenes

**MCP Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 11,
  "method": "tools/call",
  "params": {
    "name": "FindUnusedAssets"
  }
}
```

**Expected Response**:
```json
{
  "jsonrpc": "2.0",
  "id": 11,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"unusedAssets\":[\"Textures/old_logo.png\",\"Audio/unused_sfx.wav\"],\"totalSizeMB\":15.2}"
      }
    ]
  }
}
```

### 12. AnalyzeAssetDependencies

**Purpose**: Get dependency graph for an asset

**MCP Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 12,
  "method": "tools/call",
  "params": {
    "name": "AnalyzeAssetDependencies",
    "arguments": {
      "assetPath": "Assets/Prefabs/Player.prefab"
    }
  }
}
```

### 13. OptimizeTextureSettings

**Purpose**: Automatically optimize texture import settings

**MCP Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 13,
  "method": "tools/call",
  "params": {
    "name": "OptimizeTextureSettings"
  }
}
```

## Scene Tools

### 14. ValidateScene

**Purpose**: Validate scene for common issues

**MCP Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 14,
  "method": "tools/call",
  "params": {
    "name": "ValidateScene",
    "arguments": {
      "scenePath": "Assets/Scenes/MainMenu.unity"
    }
  }
}
```

### 15. FindMissingReferences

**Purpose**: Find broken prefab/component references

**MCP Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 15,
  "method": "tools/call",
  "params": {
    "name": "FindMissingReferences"
  }
}
```

### 16. AnalyzeLightingSetup

**Purpose**: Analyze scene lighting configuration

**MCP Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 16,
  "method": "tools/call",
  "params": {
    "name": "AnalyzeLightingSetup"
  }
}
```

## Package Tools

### 17. ListInstalledPackages

**Purpose**: List all installed Unity packages

**MCP Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 17,
  "method": "tools/call",
  "params": {
    "name": "ListInstalledPackages"
  }
}
```

### 18. CheckPackageCompatibility

**Purpose**: Check if package version is compatible

**MCP Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 18,
  "method": "tools/call",
  "params": {
    "name": "CheckPackageCompatibility",
    "arguments": {
      "packageName": "com.unity.render-pipelines.universal",
      "version": "14.0.0"
    }
  }
}
```

### 19. ResolveDependencies

**Purpose**: Resolve package dependency conflicts

**MCP Request**:
```json
{
  "jsonrpc": "2.0",
  "id": 19,
  "method": "tools/call",
  "params": {
    "name": "ResolveDependencies"
  }
}
```

## Context Optimization

### Query Analysis

**Purpose**: Get tool suggestions for a natural language query

**Example**:
```
User Query: "How do I optimize my game's performance?"

AI Analysis:
- Intent: Performance
- Suggested Tools:
  1. CaptureProfilerSnapshot (confidence: 0.85)
  2. AnalyzeBottlenecks (confidence: 0.80)
  3. DetectAntipatterns (confidence: 0.75)
```

### Token Usage Statistics

**Get Optimization Metrics**:
```json
{
  "totalTokens": 125000,
  "tokensSaved": 62500,
  "cacheHitRate": 0.65,
  "averageTokensPerRequest": 250,
  "efficiencyScore": 0.50
}
```

### Optimization Recommendations

**Example Output**:
```
Recommendations:
1. [Caching] Tool 'QueryDocumentation' invoked 50 times
   → Enable caching (estimated savings: 12,500 tokens)

2. [Summarization] Tool 'CaptureProfilerSnapshot' generates large responses
   → Enable summarization (estimated savings: 8,000 tokens)

3. [Deduplication] 20% of requests are duplicates
   → Already enabled, saving 15,000 tokens
```

## Testing and Debugging

### Manual Testing Checklist

- [ ] **Documentation Search**: Query for "GameObject"
- [ ] **Fuzzy Search**: Try typo "GameObjec"
- [ ] **Deprecation Check**: Check old API
- [ ] **Code Examples**: Get Instantiate examples
- [ ] **Profiler**: Capture snapshot (requires Play mode)
- [ ] **Build Validation**: Validate Windows build
- [ ] **Asset Analysis**: Find unused assets
- [ ] **Scene Validation**: Validate current scene
- [ ] **Package List**: List installed packages
- [ ] **Cache Hit**: Repeat query (should be < 10ms)

### Troubleshooting

**Issue**: Tool returns empty result

**Debug Steps**:
1. Check Unity Editor is running
2. Verify documentation is indexed (Tools → Unify MCP → Documentation Indexer)
3. Check Unity Console for errors
4. Enable MCP logging: `"MCP_LOG_LEVEL": "debug"`

**Issue**: Slow response times

**Solutions**:
- Enable caching and deduplication
- Reduce summarization aggressiveness
- Check database indexes are created
- Monitor memory usage

## Performance Expectations

| Tool | Cold (No Cache) | Warm (Cached) |
|------|----------------|---------------|
| QueryDocumentation | 20-50ms | < 10ms |
| SearchApiFuzzy | 10-30ms | < 5ms |
| CheckDeprecation | 15-25ms | < 5ms |
| CaptureProfilerSnapshot | 100-500ms | N/A (not cached) |
| ValidateBuildConfiguration | 50-200ms | < 10ms |
| FindUnusedAssets | 200-1000ms | 50-100ms |

---

**Last Updated**: Implementation Complete
**MCP Protocol Version**: 2024-11-05
**Unity MCP Server Version**: 0.1.0
