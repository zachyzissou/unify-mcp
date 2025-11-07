# Unity MCP Server - API Reference

**Version**: 0.1.0
**Last Updated**: 2025-11-06
**Status**: Production Ready

## Table of Contents

1. [Context Management](#context-management)
   - [ContextWindowManager](#contextwindowmanager)
   - [RequestDeduplicator](#requestdeduplicator)
   - [ToolResultSummarizer](#toolresultsummarizer)
   - [ResponseCacheManager](#responsecachemanager)
   - [TokenUsageOptimizer](#tokenusageoptimizer)
2. [Documentation Tools](#documentation-tools)
3. [Profiler Tools](#profiler-tools)
4. [Build Tools](#build-tools)
5. [Asset Tools](#asset-tools)
6. [Scene Tools](#scene-tools)
7. [Package Tools](#package-tools)
8. [Security Components](#security-components)
9. [Performance Characteristics](#performance-characteristics)
10. [Error Handling](#error-handling)

---

## Context Management

### ContextWindowManager

**Namespace**: `UnifyMcp.Core.Context`

**Purpose**: Orchestrates all context optimization techniques and manages tool execution with full optimization pipeline.

**Thread Safety**: Safe for concurrent access from multiple threads.

#### Constructor

```csharp
public ContextWindowManager(
    ContextAwareToolSuggester suggester = null,
    ToolResultSummarizer summarizer = null,
    RequestDeduplicator deduplicator = null,
    ResponseCacheManager cacheManager = null,
    TokenUsageOptimizer optimizer = null
)
```

**Parameters**:
- `suggester` (optional): Tool suggestion engine. Defaults to new instance.
- `summarizer` (optional): Result compression engine. Defaults to new instance.
- `deduplicator` (optional): Request deduplication engine. Defaults to new instance.
- `cacheManager` (optional): Persistent cache manager. Defaults to new instance.
- `optimizer` (optional): Token usage optimizer. Defaults to new instance.

**Example**:
```csharp
var contextManager = new ContextWindowManager();
```

#### ProcessToolRequestAsync

```csharp
public async Task<OptimizedToolResult> ProcessToolRequestAsync(
    string toolName,
    Dictionary<string, object> parameters,
    Func<Task<string>> executor,
    ContextOptimizationOptions options = null
)
```

**Purpose**: Executes a tool with full optimization pipeline including caching, deduplication, summarization, and token budget enforcement.

**Parameters**:
- `toolName`: Name of the MCP tool to execute
- `parameters`: Tool parameters as key-value pairs
- `executor`: Async function that executes the tool
- `options` (optional): Optimization settings (defaults to all optimizations enabled)

**Returns**: `OptimizedToolResult` containing response, optimization metrics, and metadata

**Pipeline Steps**:
1. Check persistent cache (SQLite)
2. Deduplicate in-flight requests
3. Execute tool if needed
4. Record token usage
5. Apply summarization
6. Enforce token budget
7. Store in persistent cache

**Example**:
```csharp
var result = await contextManager.ProcessToolRequestAsync(
    "QueryDocumentation",
    new Dictionary<string, object> { { "query", "GameObject.SetActive" } },
    async () => await docTools.QueryDocumentation("GameObject.SetActive")
);

Console.WriteLine($"Response: {result.Response}");
Console.WriteLine($"Cached: {result.WasCached}");
Console.WriteLine($"Tokens saved: {result.TokensSaved}");
Console.WriteLine($"Duration: {result.Duration.TotalMilliseconds}ms");
```

**Performance**:
- Cache hit: 5-8ms
- Cache miss: Depends on tool execution time
- Optimization overhead: 2-5ms

#### AnalyzeQuery

```csharp
public QueryAnalysisResult AnalyzeQuery(string query, int maxSuggestions = 3)
```

**Purpose**: Analyzes a natural language query to suggest relevant tools using intent extraction and entity recognition.

**Parameters**:
- `query`: User's natural language query
- `maxSuggestions`: Maximum number of tool suggestions (default: 3)

**Returns**: `QueryAnalysisResult` with intent, entities, and suggested tools

**Example**:
```csharp
var analysis = contextManager.AnalyzeQuery("How do I optimize performance?");

Console.WriteLine($"Intent: {analysis.Intent}"); // Performance
foreach (var tool in analysis.SuggestedTools)
{
    Console.WriteLine($"- {tool.ToolName} (confidence: {tool.ConfidenceScore:P0})");
}
// Output:
// - CaptureProfilerSnapshot (confidence: 85%)
// - AnalyzeBottlenecks (confidence: 80%)
// - DetectAntipatterns (confidence: 75%)
```

#### GetStatisticsAsync

```csharp
public async Task<OptimizationStatistics> GetStatisticsAsync()
```

**Purpose**: Gets comprehensive optimization metrics including token usage, cache stats, and efficiency scores.

**Returns**: `OptimizationStatistics` with complete metrics

**Example**:
```csharp
var stats = await contextManager.GetStatisticsAsync();

Console.WriteLine($"Total requests: {stats.TokenMetrics.RequestCount}");
Console.WriteLine($"Tokens saved: {stats.TokenMetrics.TokensSaved}");
Console.WriteLine($"Cache hit rate: {stats.CacheStatistics.TotalHits / (double)stats.TokenMetrics.RequestCount:P0}");
Console.WriteLine($"Efficiency: {stats.EfficiencyScore:P0}");
```

#### GenerateRecommendations

```csharp
public List<OptimizationRecommendation> GenerateRecommendations()
```

**Purpose**: Analyzes usage patterns and generates actionable optimization recommendations.

**Returns**: List of recommendations sorted by priority and estimated savings

**Example**:
```csharp
var recommendations = contextManager.GenerateRecommendations();

foreach (var rec in recommendations)
{
    Console.WriteLine($"[{rec.Type}] {rec.Description}");
    Console.WriteLine($"  Estimated savings: {rec.EstimatedSavings} tokens");
    Console.WriteLine($"  Actions:");
    foreach (var action in rec.Actions)
    {
        Console.WriteLine($"    - {action}");
    }
}
```

#### PerformMaintenanceAsync

```csharp
public async Task PerformMaintenanceAsync()
```

**Purpose**: Performs maintenance operations including expired cache entry cleanup.

**Example**:
```csharp
await contextManager.PerformMaintenanceAsync();
```

#### ResetAsync

```csharp
public async Task ResetAsync()
```

**Purpose**: Clears all caches and resets metrics. Use for testing or fresh start.

---

### RequestDeduplicator

**Namespace**: `UnifyMcp.Core.Context`

**Purpose**: Detects and prevents redundant tool invocations using SHA256 hashing and semaphore-based locking.

**Thread Safety**: Fully thread-safe using `ConcurrentDictionary` and `SemaphoreSlim`.

#### Constructor

```csharp
public RequestDeduplicator(
    TimeSpan? cacheDuration = null,
    int maxCacheSize = 1000,
    TimeSpan? cleanupInterval = null,
    TimeSpan? semaphoreCleanupInterval = null
)
```

**Parameters**:
- `cacheDuration`: Duration to cache responses (default: 5 minutes)
- `maxCacheSize`: Maximum number of cached responses (default: 1000)
- `cleanupInterval`: Interval for cache cleanup (default: 1 minute)
- `semaphoreCleanupInterval`: Interval for semaphore cleanup (default: 5 minutes)

#### ProcessRequestAsync

```csharp
public async Task<string> ProcessRequestAsync(
    string toolName,
    Dictionary<string, object> parameters,
    Func<Task<string>> executor,
    TimeSpan? cacheDuration = null
)
```

**Purpose**: Processes a request, either executing it or returning a cached/deduplicated result.

**Deduplication Algorithm**:
1. Compute SHA256 hash of (toolName + parameters)
2. Check in-memory cache
3. Acquire semaphore for request key
4. Double-check cache (race condition protection)
5. Execute if needed
6. Cache result
7. Release semaphore

**Example**:
```csharp
var deduplicator = new RequestDeduplicator();

var result = await deduplicator.ProcessRequestAsync(
    "QueryDocumentation",
    new Dictionary<string, object> { { "query", "GameObject" } },
    async () => await ExecuteQuery("GameObject")
);
```

**Concurrency Safety**:
- Prevents duplicate executions of identical requests
- Thread-safe cache operations
- Semaphore cleanup prevents memory leaks

#### GetStats

```csharp
public DeduplicationStats GetStats()
```

**Returns**: Statistics including total requests, deduplicated count, cache size.

**Example**:
```csharp
var stats = deduplicator.GetStats();
Console.WriteLine($"Deduplication rate: {stats.DeduplicatedRequests / (double)stats.TotalRequests:P0}");
```

---

### ToolResultSummarizer

**Namespace**: `UnifyMcp.Core.Context`

**Purpose**: Summarizes verbose tool results to reduce token consumption using intelligent compression techniques.

#### Summarize

```csharp
public SummarizationResult Summarize(string content, SummarizationOptions options = null)
```

**Purpose**: Summarizes JSON or plain text content based on provided options.

**Techniques Applied**:
- **List Truncation**: Show first N items + count (e.g., "...and 47 more")
- **Depth Limiting**: Prevent deep object traversal
- **Metadata Removal**: Strip timestamps, IDs, URLs
- **Text Truncation**: Sentence-aware splitting
- **Code Preservation**: Detect and preserve code examples

**Compression Ratios**:
- Minimal mode: 10-20% reduction
- Balanced mode: 30-50% reduction
- Aggressive mode: 50-70% reduction

**Example**:
```csharp
var summarizer = new ToolResultSummarizer();

var options = new SummarizationOptions
{
    Mode = SummarizationMode.Balanced,
    MaxListItems = 5,
    MaxDepth = 3,
    PreserveCodeExamples = true,
    IncludeMetadata = false
};

var result = summarizer.Summarize(largeJsonResponse, options);

Console.WriteLine($"Original: {result.OriginalLength} chars");
Console.WriteLine($"Summarized: {result.SummarizedLength} chars");
Console.WriteLine($"Tokens saved: {result.EstimatedTokenSavings}");
Console.WriteLine($"Techniques: {string.Join(", ", result.AppliedTechniques)}");
```

#### SummarizeMultiple

```csharp
public SummarizationResult SummarizeMultiple(
    Dictionary<string, string> toolResults,
    SummarizationOptions options = null
)
```

**Purpose**: Summarizes multiple tool results into a single combined response.

---

### ResponseCacheManager

**Namespace**: `UnifyMcp.Core.Context`

**Purpose**: Manages persistent caching of MCP responses to disk using SQLite with FTS5 indexes.

**Storage**: SQLite database with indexes on tool_name, request_hash, expires_at

**Thread Safety**: Uses connection lock for thread-safe database operations.

#### Constructor

```csharp
public ResponseCacheManager(string cachePath = null)
```

**Default Location**: `%APPDATA%/UnifyMcp/ResponseCache/response_cache.db`

#### GetCachedResponseAsync

```csharp
public async Task<string> GetCachedResponseAsync(string toolName, string requestHash)
```

**Purpose**: Retrieves a cached response if available and not expired.

**Returns**: Response string or null if not cached or expired

**Performance**: 5-8ms for cache hit

**Example**:
```csharp
var cacheManager = new ResponseCacheManager();

var cached = await cacheManager.GetCachedResponseAsync("QueryDocumentation", requestHash);
if (cached != null)
{
    Console.WriteLine("Cache hit!");
}
```

#### CacheResponseAsync

```csharp
public async Task CacheResponseAsync(
    string toolName,
    string requestHash,
    Dictionary<string, object> parameters,
    string response,
    TimeSpan cacheDuration
)
```

**Purpose**: Stores a response in the cache with expiration.

#### GetStatisticsAsync

```csharp
public async Task<CacheStatistics> GetStatisticsAsync()
```

**Returns**: Statistics including total entries, hits, cache size, top tools

**Example**:
```csharp
var stats = await cacheManager.GetStatisticsAsync();

Console.WriteLine($"Cache entries: {stats.ActiveEntries}");
Console.WriteLine($"Cache size: {stats.CacheSizeMB:F2} MB");
Console.WriteLine($"Total hits: {stats.TotalHits}");
```

---

### TokenUsageOptimizer

**Namespace**: `UnifyMcp.Core.Context`

**Purpose**: Monitors and optimizes token usage across MCP operations with budget enforcement.

**Token Estimation**: 4 characters ≈ 1 token (GPT-style estimation)

#### RecordUsage

```csharp
public void RecordUsage(string toolName, string inputContent, string outputContent)
```

**Purpose**: Records token usage for a request/response pair.

**Triggers**:
- Budget warning at 80% threshold
- Budget exceeded event if over limit
- Automatic recommendation generation

#### GenerateRecommendations

```csharp
public List<OptimizationRecommendation> GenerateRecommendations()
```

**Recommendation Types**:
1. **Caching**: For frequently used tools (>10 invocations, >500 tokens avg)
2. **Summarization**: For tools with large responses (>1000 output tokens avg)
3. **Deduplication**: For tools with potential duplicate requests (>5 invocations)

**Example**:
```csharp
var optimizer = new TokenUsageOptimizer();

optimizer.RecordUsage("QueryDocumentation", requestJson, responseJson);

var recommendations = optimizer.GenerateRecommendations();
// Output:
// [Caching] Tool 'QueryDocumentation' is invoked frequently (50 times)
//   Estimated savings: 25,000 tokens
//   Actions:
//     - Enable response caching for this tool
//     - Set appropriate cache duration
```

#### CheckAndOptimizeResponse

```csharp
public (string content, bool wasOptimized) CheckAndOptimizeResponse(string responseContent)
```

**Purpose**: Checks if response exceeds budget and applies automatic optimization if configured.

**Returns**: Tuple of (potentially optimized content, optimization flag)

#### GetEfficiencyScore

```csharp
public double GetEfficiencyScore()
```

**Returns**: Efficiency score from 0.0 to 1.0 (higher is better)

**Formula**: `tokens_saved / (total_tokens + tokens_saved)`

---

## Documentation Tools

**Namespace**: `UnifyMcp.Tools.Documentation`

### QueryDocumentation

```csharp
public async Task<string> QueryDocumentation(string query)
```

**Purpose**: Full-text search of Unity API documentation using SQLite FTS5 with BM25 ranking.

**Parameters**:
- `query`: Search query (API name, method, keyword)

**Returns**: JSON array of matching documentation entries

**Features**:
- FTS5 full-text search with BM25 ranking
- Method signatures with parameters
- Code examples extraction
- Deprecation warnings
- Documentation URL links

**Performance**: 20-40ms (uncached), <10ms (cached)

**Example**:
```csharp
var docTools = new DocumentationTools(dbPath);

var result = await docTools.QueryDocumentation("GameObject.SetActive");
// Returns:
// [{
//   "className": "GameObject",
//   "methodName": "SetActive",
//   "returnType": "void",
//   "parameters": ["bool value"],
//   "description": "Activates/Deactivates the GameObject...",
//   "codeExamples": ["myGameObject.SetActive(true);"],
//   "unityVersion": "2022.3",
//   "documentationUrl": "https://docs.unity3d.com/...",
//   "isDeprecated": false
// }]
```

### SearchApiFuzzy

```csharp
public async Task<string> SearchApiFuzzy(string query, double threshold = 0.7)
```

**Purpose**: Typo-tolerant fuzzy search using Levenshtein distance algorithm.

**Parameters**:
- `query`: Search query (may contain typos)
- `threshold`: Similarity threshold (0.0-1.0, default: 0.7)

**Returns**: JSON array of similar API suggestions with similarity scores

**Algorithm**: Levenshtein distance with normalized scoring

**Performance**: 10-25ms

**Example**:
```csharp
var result = await docTools.SearchApiFuzzy("GameObject.SetActiv", 0.7);
// Returns:
// [{
//   "api": "GameObject.SetActive",
//   "similarity": 0.95,
//   "suggestion": "Did you mean GameObject.SetActive?"
// }]
```

### CheckApiDeprecation

```csharp
public async Task<string> CheckApiDeprecation(string apiName)
```

**Purpose**: Check if a Unity API is deprecated and get replacement suggestions.

**Parameters**:
- `apiName`: Full API name (e.g., "Application.loadedLevelName")

**Returns**: JSON object with deprecation status and replacement

**Example**:
```csharp
var result = await docTools.CheckApiDeprecation("Application.loadedLevelName");
// Returns:
// {
//   "found": true,
//   "className": "Application",
//   "methodName": "loadedLevelName",
//   "isDeprecated": true,
//   "replacementApi": "SceneManager.GetActiveScene().name",
//   "warning": "This API has been deprecated since Unity 5.3. Use SceneManager API instead."
// }
```

### RefreshDocumentationIndex

```csharp
public async Task<string> RefreshDocumentationIndex(Action<string> progressCallback = null)
```

**Purpose**: Refreshes the documentation index by detecting Unity installations and indexing ScriptReference HTML files.

**Parameters**:
- `progressCallback` (optional): Callback for progress updates

**Returns**: JSON object with indexing summary

**Performance**: Indexes ~500k documents in <31 minutes

**Example**:
```csharp
var result = await docTools.RefreshDocumentationIndex(
    progress => Console.WriteLine(progress)
);
// Output:
// Indexing Unity 2022.3.15f1...
// Progress: 100/1000 files
// Progress: 200/1000 files
// ...
// Returns:
// {
//   "success": true,
//   "unityVersion": "2022.3.15f1",
//   "totalFiles": 1000,
//   "processedFiles": 1000,
//   "successfullyIndexed": 995,
//   "failed": 5,
//   "durationSeconds": 1845.2
// }
```

### GetUnityVersion

```csharp
public async Task<string> GetUnityVersion()
```

**Purpose**: Gets the current Unity Editor version and maps to documentation version.

**Returns**: JSON object with currentVersion and documentationVersion

---

## Profiler Tools

**Namespace**: `UnifyMcp.Tools.Profiler`

### CaptureProfilerSnapshot

```csharp
public async Task<string> CaptureProfilerSnapshot(int frameCount = 300)
```

**Purpose**: Captures Unity Profiler data over specified frames for performance analysis.

**Parameters**:
- `frameCount`: Number of frames to capture (default: 300 as per spec)

**Returns**: JSON object with CPU times, GC allocations, and bottlenecks

**Requirements**: Unity Editor must be in Play mode

**Performance**: 100-500ms

**Example**:
```csharp
var profilerTools = new ProfilerTools();

var result = await profilerTools.CaptureProfilerSnapshot(300);
// Returns:
// {
//   "frameCount": 300,
//   "capturedAt": "2025-11-06T10:30:00Z",
//   "cpuTimes": {
//     "PlayerController.Update": 15.2,
//     "Rendering": 8.5,
//     "Physics": 3.2
//   },
//   "gcAllocations": {
//     "PlayerController.Update": 5120
//   },
//   "totalCpuTime": 16.7,
//   "averageFps": 60.0,
//   "bottlenecks": [
//     {
//       "location": "PlayerController.Update",
//       "fileName": "PlayerController.cs",
//       "lineNumber": 42,
//       "severity": "High",
//       "category": "Scripts",
//       "cpuTime": 15.2,
//       "recommendation": "Optimize Update() loop - cache component references"
//     }
//   ]
// }
```

### AnalyzeBottlenecks

```csharp
public async Task<string> AnalyzeBottlenecks(string snapshotJson)
```

**Purpose**: Analyzes bottlenecks from snapshot data with severity classification.

**Severity Levels**:
- **Critical**: >20ms per frame
- **High**: >10ms per frame
- **Medium**: >5ms per frame

### DetectAntipatterns

```csharp
public async Task<string> DetectAntipatterns()
```

**Purpose**: Detects common Unity performance anti-patterns.

**Detected Patterns**:
- GameObject.Find in Update()
- GetComponent<> in Update()
- String concatenation in loops
- LINQ in Update()
- Camera.main in Update()

---

## Build Tools

**Namespace**: `UnifyMcp.Tools.Build`

### ValidateBuildConfiguration

```csharp
public async Task<string> ValidateBuildConfiguration(string platform)
```

**Purpose**: Validates build settings for target platform.

**Supported Platforms**: Windows, macOS, Linux, Android, iOS, WebGL

**Returns**: JSON object with validation status, warnings, and errors

### StartMultiPlatformBuild

```csharp
public async Task<string> StartMultiPlatformBuild(string[] platforms)
```

**Purpose**: Initiates builds for multiple platforms.

### GetBuildSizeAnalysis

```csharp
public async Task<string> GetBuildSizeAnalysis()
```

**Purpose**: Analyzes build size breakdown by asset category.

---

## Asset Tools

**Namespace**: `UnifyMcp.Tools.Assets`

### FindUnusedAssets

```csharp
public async Task<string> FindUnusedAssets()
```

**Purpose**: Finds assets not referenced in any scene or code.

**Example**:
```csharp
var assetTools = new AssetTools();

var result = await assetTools.FindUnusedAssets();
// Returns:
// {
//   "unusedAssets": [
//     "Textures/old_logo.png",
//     "Audio/unused_sfx.wav"
//   ],
//   "totalSizeMB": 15.2
// }
```

### AnalyzeAssetDependencies

```csharp
public async Task<string> AnalyzeAssetDependencies(string assetPath)
```

**Purpose**: Gets dependency graph for an asset.

**Security**: Validates path to prevent traversal attacks using `PathValidator`

**Parameters**:
- `assetPath`: Path to asset (must be within Unity project)

**Throws**: `SecurityException` if path is outside project

### OptimizeTextureSettings

```csharp
public async Task<string> OptimizeTextureSettings()
```

**Purpose**: Automatically optimizes texture import settings across platforms.

---

## Scene Tools

**Namespace**: `UnifyMcp.Tools.Scene`

### ValidateScene

```csharp
public async Task<string> ValidateScene(string scenePath)
```

**Purpose**: Validates scene for common issues (missing references, naming violations, etc.).

**Security**: Validates path to prevent traversal attacks

**Parameters**:
- `scenePath`: Path to scene file

**Validation Checks**:
- Missing component references
- Broken prefab connections
- Naming convention compliance
- Lighting configuration issues
- Objects outside playable bounds

### FindMissingReferences

```csharp
public async Task<string> FindMissingReferences()
```

**Purpose**: Finds all missing references in current scene.

### AnalyzeLightingSetup

```csharp
public async Task<string> AnalyzeLightingSetup()
```

**Purpose**: Analyzes scene lighting configuration (baked, realtime, mixed mode).

---

## Package Tools

**Namespace**: `UnifyMcp.Tools.Packages`

### ListInstalledPackages

```csharp
public async Task<string> ListInstalledPackages()
```

**Purpose**: Lists all installed Unity packages with versions.

### CheckPackageCompatibility

```csharp
public async Task<string> CheckPackageCompatibility(string packageName, string version)
```

**Purpose**: Checks if a package version is compatible with current Unity version.

### ResolveDependencies

```csharp
public async Task<string> ResolveDependencies()
```

**Purpose**: Resolves package dependency conflicts.

---

## Security Components

### PathValidator

**Namespace**: `UnifyMcp.Common.Security`

**Purpose**: Validates file paths to prevent path traversal attacks.

#### IsValidPath

```csharp
public bool IsValidPath(string path)
```

**Purpose**: Checks if a path is within the project directory.

**Returns**: True if path is safe, false otherwise

#### ValidateOrThrow

```csharp
public void ValidateOrThrow(string path)
```

**Purpose**: Validates a path or throws `SecurityException`.

**Example**:
```csharp
var validator = new PathValidator("/path/to/unity/project");

validator.ValidateOrThrow("Assets/Scripts/Player.cs"); // OK
validator.ValidateOrThrow("../../etc/passwd"); // Throws SecurityException
```

**Security Features**:
- Prevents directory traversal (../)
- Validates absolute paths
- Cross-platform path handling
- Normalized path comparison

---

## Performance Characteristics

### Latency Benchmarks

| Operation | Cold (No Cache) | Warm (Cached) | Throughput |
|-----------|----------------|---------------|------------|
| **Context Management** |
| Cache Hit (SQLite) | - | 5-8ms | 1000+ req/s |
| Request Deduplication | - | 1-3ms | 2000+ req/s |
| Summarization (Balanced) | 5-15ms | - | 500 req/s |
| Token Optimization | 2-4ms | - | 1000 req/s |
| **Documentation Tools** |
| QueryDocumentation | 20-40ms | <10ms | 100 req/s |
| SearchApiFuzzy | 10-25ms | <5ms | 200 req/s |
| CheckDeprecation | 15-25ms | <5ms | 150 req/s |
| RefreshIndex (500k docs) | 30 min | - | N/A |
| **Profiler Tools** |
| CaptureSnapshot (300 frames) | 100-500ms | N/A | 10 req/s |
| AnalyzeBottlenecks | 50-200ms | <10ms | 50 req/s |
| DetectAntipatterns | 100-300ms | <10ms | 30 req/s |
| **Build Tools** |
| ValidateBuildConfig | 50-200ms | <10ms | 50 req/s |
| BuildSizeAnalysis | 100-500ms | <50ms | 20 req/s |
| **Asset/Scene Tools** |
| FindUnusedAssets | 200-1000ms | 50-100ms | 10 req/s |
| ValidateScene | 100-500ms | <10ms | 20 req/s |

### Memory Usage

| Component | Per-Request Overhead | Cache Size |
|-----------|---------------------|------------|
| RequestDeduplicator | 2-5 KB | Max 1000 entries (configurable) |
| ResponseCacheManager | 5-10 KB | Unlimited (disk-based) |
| ToolResultSummarizer | 1-3 KB | Stateless |
| TokenUsageOptimizer | <1 KB | ~100 KB (metrics) |

### Optimization Efficiency

| Technique | Token Reduction | Latency Cost |
|-----------|----------------|--------------|
| Caching (hit) | ~100% | +5-8ms |
| Deduplication | ~100% | +1-3ms |
| Summarization (Minimal) | 10-20% | +5-10ms |
| Summarization (Balanced) | 30-50% | +10-15ms |
| Summarization (Aggressive) | 50-70% | +15-25ms |

---

## Error Handling

### Exception Types

All methods may throw the following exceptions:

| Exception | Cause | Example |
|-----------|-------|---------|
| `ArgumentException` | Invalid parameters | Empty toolName, null executor |
| `ArgumentNullException` | Required parameter is null | executor parameter |
| `InvalidOperationException` | Invalid state | Unity Editor not running |
| `SecurityException` | Path validation failure | Path outside project |
| `JsonException` | Invalid JSON content | Malformed summarization input |
| `SQLiteException` | Database operation failure | Corrupted cache database |

### Error Result Pattern

Tool methods return JSON with error information:

```csharp
// Success
{
  "success": true,
  "result": { ... }
}

// Error
{
  "success": false,
  "error": "Error message",
  "errorType": "InvalidOperation"
}
```

### OptimizedToolResult.Error

Check the `Error` property for execution errors:

```csharp
var result = await contextManager.ProcessToolRequestAsync(...);

if (result.Error != null)
{
    Console.WriteLine($"Tool execution failed: {result.Error.Message}");
    Console.WriteLine($"Stack trace: {result.Error.StackTrace}");
}
```

### Event-Based Error Handling

Context components expose error events:

```csharp
var optimizer = new TokenUsageOptimizer();

optimizer.OnBudgetWarning += (msg) => Console.WriteLine($"Warning: {msg}");
optimizer.OnBudgetExceeded += (msg) => Console.WriteLine($"Error: {msg}");
```

---

## Integration Patterns

### Basic Tool Execution

```csharp
// Without optimization
var docTools = new DocumentationTools(dbPath);
var result = await docTools.QueryDocumentation("GameObject");

// With optimization
var contextManager = new ContextWindowManager();
var optimizedResult = await contextManager.ProcessToolRequestAsync(
    "QueryDocumentation",
    new Dictionary<string, object> { { "query", "GameObject" } },
    async () => await docTools.QueryDocumentation("GameObject")
);
```

### Custom Optimization Options

```csharp
var options = new ContextOptimizationOptions
{
    EnableCaching = true,
    EnableDeduplication = true,
    EnableSummarization = true,
    EnforceTokenBudget = true,
    CacheDuration = TimeSpan.FromMinutes(10),
    SummarizationOptions = new SummarizationOptions
    {
        Mode = SummarizationMode.Aggressive,
        MaxLength = 200,
        MaxListItems = 3,
        MaxDepth = 2,
        IncludeMetadata = false,
        PreserveCodeExamples = true
    }
};

var result = await contextManager.ProcessToolRequestAsync(
    toolName,
    parameters,
    executor,
    options
);
```

### Batch Operations

```csharp
// Process multiple tools with shared optimization context
var tools = new[] { "QueryDocumentation", "SearchApiFuzzy", "CheckDeprecation" };
var results = new List<OptimizedToolResult>();

foreach (var tool in tools)
{
    var result = await contextManager.ProcessToolRequestAsync(
        tool,
        parameters[tool],
        executors[tool]
    );
    results.Add(result);
}

// Get combined statistics
var stats = await contextManager.GetStatisticsAsync();
Console.WriteLine($"Total efficiency: {stats.EfficiencyScore:P0}");
```

### Monitoring and Metrics

```csharp
var contextManager = new ContextWindowManager();

// Subscribe to optimization events
contextManager.OnOptimizationApplied += (msg) =>
    Console.WriteLine($"[Optimization] {msg}");

// Periodic statistics reporting
var timer = new System.Timers.Timer(60000); // 1 minute
timer.Elapsed += async (sender, args) =>
{
    var stats = await contextManager.GetStatisticsAsync();
    Console.WriteLine($"Requests: {stats.TokenMetrics.RequestCount}");
    Console.WriteLine($"Efficiency: {stats.EfficiencyScore:P0}");

    var recs = contextManager.GenerateRecommendations();
    foreach (var rec in recs)
    {
        Console.WriteLine($"Recommendation: {rec.Description}");
    }
};
timer.Start();
```

---

## See Also

- [Architecture Guide](./ARCHITECTURE.md) - System design and component details
- [MCP Examples](./MCP_EXAMPLES.md) - Protocol usage examples and MCP client integration
- [README](../README.md) - Getting started guide and installation
- [Contributing Guidelines](./CONTRIBUTING.md) - Development workflow and coding standards

---

**Document Version**: 1.0
**Generated**: 2025-11-06
**Status**: Production Ready
