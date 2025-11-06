# Technical Planning: Unity MCP Documentation-First Platform

**Created**: 2025-11-06
**Status**: Planning Complete
**Prerequisites**: Completed business specification (Context/Features/001-UnifyMcpDocumentationFirst/Spec.md)

---

## Research & Analysis

### Research Scope

This research phase focused on three critical technology domains:

1. **Model Context Protocol (MCP) Implementation in C#/.NET** - Official SDK analysis, transport layer options, JSON schema generation, and Unity Editor integration patterns
2. **Unity Editor APIs** - Profiler, Build Pipeline, Asset Database, Scene Management, and Package Manager APIs across Unity 2021.3 LTS, 2022.3 LTS, and Unity 6
3. **Unity Documentation Access** - Local documentation structure, web documentation patterns, HTML parsing, full-text search indexing, and fuzzy search algorithms

### Key Findings Summary

**Critical Discovery**: The official **ModelContextProtocol C# SDK (v0.4.0-preview.3)** is production-ready with .NET Standard 2.0 compatibility, making it fully compatible with Unity 2021.3 LTS+ (.NET Standard 2.1 API compatibility level). This eliminates the need for custom MCP protocol implementation.

**Documentation-First Feasibility**: Unity documentation is accessible both locally (installed with Unity Editor) and via web (docs.unity3d.com with predictable URL patterns). Combination of local indexing + web scraping fallback is viable with SQLite FTS5 for full-text search and Fastenshtein for fuzzy matching.

**Unity API Maturity**: All required Unity Editor APIs (Profiler, Build, Asset, Scene, Package) are stable and available across target Unity versions (2021.3 LTS through Unity 6) with consistent interfaces. ProfilerRecorder API (introduced 2021.3) provides programmatic profiler access; BuildPipeline and AssetDatabase APIs are mature and well-documented.

**Threading Constraint**: WebSocket server callbacks execute on background threads, but all Unity API calls MUST run on main thread. This requires message queue pattern with EditorApplication.update polling for thread-safe MCP server implementation.

### Codebase Integration Analysis

**Project Status**: This is a **brand new project** with no existing codebase. The following analysis describes the foundational architecture to be created.

**Architecture Foundation to Establish**:
- Unity Editor extension structure using [InitializeOnLoad] for automatic startup
- MCP protocol layer using official ModelContextProtocol SDK
- Modular tool system with plugin architecture for extensibility
- Thread-safe message handling between WebSocket callbacks and Unity main thread
- Event-driven updates using EditorApplication callbacks for context changes

**Project Structure to Create**:
```
unify-mcp/
├── src/
│   ├── Core/                    # NEW: MCP protocol implementation
│   │   ├── McpServer.cs        # MCP server lifecycle management
│   │   ├── TransportLayer/     # stdio and WebSocket transports
│   │   └── SchemaGenerator.cs  # JSON schema generation
│   ├── Tools/                   # NEW: MCP tool implementations
│   │   ├── Documentation/       # Documentation query tools
│   │   ├── Profiler/           # Profiler analysis tools
│   │   ├── Build/              # Build automation tools
│   │   ├── Assets/             # Asset management tools
│   │   ├── Scene/              # Scene validation tools
│   │   └── Packages/           # Package management tools
│   ├── Common/                  # NEW: Shared utilities
│   │   ├── Threading/          # Thread synchronization
│   │   ├── Caching/            # Data caching layer
│   │   └── Serialization/      # JSON/Binary serialization
│   └── Unity/                   # NEW: Unity-specific integration
│       ├── Editor/             # Editor scripts with [InitializeOnLoad]
│       └── Documentation/      # Documentation indexing system
├── tests/                       # NEW: Unit and integration tests
└── docs/                        # NEW: Project documentation
```

**Integration Requirements**:
- **NuGet Package Dependencies**: Install ModelContextProtocol (0.4.0-preview.3), NJsonSchema, Fastenshtein, System.Data.SQLite, AngleSharp, websocket-sharp
- **Unity Package Dependencies**: Unity Editor Coroutines (com.unity.editorcoroutines), Memory Profiler (com.unity.memoryprofiler)
- **Unity API Access**: UnityEditor namespace for all editor APIs, Unity.Profiling for ProfilerRecorder
- **Threading Integration**: ConcurrentQueue<Action> for main thread marshalling with EditorApplication.update polling

**Data Flow Architecture**:
1. **MCP Client → MCP Server**: JSON-RPC 2.0 messages over stdio or WebSocket
2. **MCP Server → Unity Main Thread**: Message queue pattern via ConcurrentQueue
3. **Unity APIs → Data Serialization**: Unity Editor APIs called on main thread, results serialized to JSON
4. **Data Serialization → MCP Client**: JSON responses with context optimization (pagination, incremental updates)
5. **Change Detection → MCP Notifications**: EditorApplication callbacks trigger server-initiated notifications

**Implementation Considerations**:
- **Consistency Requirements**: Follow Unity C# coding conventions, use async/await patterns where supported, XML documentation for public APIs
- **No Existing Conflicts**: Clean slate project allows optimal architecture without legacy constraints
- **Foundation First**: Phase 1 establishes core infrastructure (MCP server, documentation tools) that all other tools will build upon

### Technology Research

#### ModelContextProtocol C# SDK
**Version**: 0.4.0-preview.3 (October 20, 2025)
**Documentation**: https://modelcontextprotocol.io/specification/2025-06-18, https://github.com/modelcontextprotocol/csharp-sdk
**Research Date**: 2025-11-06
**NuGet Package**: `ModelContextProtocol` (MIT License, Anthropic + Microsoft collaboration)

**Key Capabilities**:
- Official C# SDK with attribute-based tool registration ([McpServerTool], [Description])
- Full Microsoft.Extensions.DependencyInjection integration for service injection
- Native support for stdio and SSE transports (WebSocket via community patterns)
- Automatic JSON schema generation from C# method parameters
- .NET Standard 2.0+ compatibility (Unity 2021.3 LTS compatible)
- Task<T>/ValueTask<T> async patterns throughout

**Limitations**:
- Preview status (breaking changes possible, but stable API)
- WebSocket transport not officially in SDK (requires custom implementation)
- Requires .NET Standard 2.0+ (Unity must use .NET Standard 2.1 API compatibility level)

**Best Practices**:
- Return JSON-serialized strings from tools for LLM processing flexibility
- Use [Description] attributes extensively for LLM context
- Leverage DI for HttpClient, logging, and custom services
- Implement CancellationToken support for long-running operations

**Decision Rationale**: Official SDK eliminates custom protocol implementation, provides battle-tested JSON-RPC 2.0 handling, and offers excellent Unity compatibility through .NET Standard 2.0 target.

#### Unity ProfilerRecorder API
**Version**: Available Unity 2021.3 LTS+
**Documentation**: https://docs.unity3d.com/ScriptReference/Unity.Profiling.ProfilerRecorder.html
**Research Date**: 2025-11-06

**Key Capabilities**:
- Programmatic access to Unity Profiler counters (CPU, GPU, Memory, Rendering)
- Low-overhead continuous monitoring with ProfilerRecorder.StartNew()
- Access to RawFrameDataView and HierarchyFrameDataView for frame-level analysis
- Memory snapshot capture via MemoryProfiler.TakeSnapshot()
- Frame Debugger control through FrameDebuggerUtility

**Limitations**:
- Most functionality requires Development Build enabled
- RawFrameDataView/HierarchyFrameDataView are Editor-only (ProfilerRecorder works in both)
- ProfilerDriver class is deprecated (use ProfilerRecorder instead)
- Memory snapshots are expensive operations (use sparingly)

**Best Practices**:
- Use ProfilerRecorder for continuous monitoring (minimal allocation)
- Capture snapshots only on-demand to avoid performance impact
- Process profiler data on background threads when possible
- Cache aggregated results; invalidate on frame boundaries

**Decision Rationale**: Mature API with consistent interface across Unity versions, provides all necessary data for performance analysis requirements (FR-006 through FR-010).

#### Unity Build Pipeline API
**Version**: Stable across Unity 2021.3 LTS, 2022.3 LTS, Unity 6
**Documentation**: https://docs.unity3d.com/ScriptReference/BuildPipeline.html
**Research Date**: 2025-11-06

**Key Capabilities**:
- BuildPipeline.BuildPlayer() with BuildPlayerOptions for platform configuration
- BuildReport with detailed size breakdown, timing, and file-level analysis
- IPreprocessBuildWithReport / IPostprocessBuildWithReport for build hooks
- AssetBundle creation and management via BuildAssetBundles()
- BuildReport.GetLatestReport() for post-build analysis

**Limitations**:
- Build operations are synchronous (blocking on main thread)
- BuildOptions.DetailedBuildReport adds 3-5% build time overhead
- IPreprocessBuildWithReport / IPostprocessBuildWithReport NOT invoked for AssetBundle builds
- Platform SDK availability must be checked before builds

**Best Practices**:
- Validate BuildPlayerOptions before starting builds (detect missing SDKs, disk space)
- Use BuildOptions.DetailedBuildReport for analysis (acceptable overhead)
- Wrap build operations in async Task for non-blocking patterns
- Cache BuildReport data for trending analysis

**Decision Rationale**: Comprehensive API covers all build automation requirements (FR-011 through FR-015), stable across Unity versions, well-documented with extensive examples.

#### SQLite FTS5 for Documentation Search
**Version**: System.Data.SQLite v1.0.118+
**Documentation**: https://www.sqlite.org/fts5.html
**Research Date**: 2025-11-06
**NuGet Package**: `System.Data.SQLite` (Public Domain)

**Key Capabilities**:
- Embedded full-text search engine (no separate server process)
- FTS5 extension supports phrase queries, prefix queries, boolean operators
- Built-in relevance ranking with BM25 algorithm
- SQL-based queries (familiar syntax)
- Automatic index updates on data changes
- ~0.03 seconds query time for 500k documents

**Limitations**:
- Index creation is CPU-intensive (~31 minutes for 500k documents)
- Storage overhead ~140% of original text size
- Native DLL required in Unity project (platform-specific)
- FTS5 requires SQLite 3.9.0+ (System.Data.SQLite includes this)

**Best Practices**:
- Use porter tokenizer for stemming ("running" matches "run")
- Create indexes during idle time or background threads
- Cache query results for frequently accessed documentation
- Implement incremental indexing for new Unity versions

**Decision Rationale**: Tight integration with embedded database, excellent query performance, familiar SQL syntax, and proven Unity Editor compatibility make this ideal for local documentation indexing (FR-001, FR-002, FR-004).

#### Fastenshtein for Fuzzy Search
**Version**: v1.0.11
**Documentation**: https://github.com/DanHarltey/Fastenshtein
**Research Date**: 2025-11-06
**NuGet Package**: `Fastenshtein` (MIT License)

**Key Capabilities**:
- Fastest .NET Levenshtein distance implementation (benchmarked)
- Simple API: `new Levenshtein(source).DistanceFrom(target)`
- No dependencies, fully unit tested
- Optimized for speed and memory usage
- Returns edit distance as integer

**Limitations**:
- Only Levenshtein algorithm (not Damerau-Levenshtein)
- No built-in similarity scoring (must calculate from distance)
- Case-sensitive (requires preprocessing for case-insensitive matching)

**Best Practices**:
- Normalize strings (lowercase, trim) before distance calculation
- Convert distance to similarity score: `1.0 - (distance / maxLength)`
- Set threshold for acceptable matches (e.g., similarity > 0.7)
- Cache Levenshtein instances for repeated queries against same source

**Decision Rationale**: Performance-optimized for typo tolerance requirement (FR-002), minimal dependencies, simple integration, and well-tested implementation.

#### AngleSharp for HTML Parsing
**Version**: Active development (check NuGet for latest)
**Documentation**: https://anglesharp.github.io/
**Research Date**: 2025-11-06
**NuGet Package**: `AngleSharp` (MIT License)

**Key Capabilities**:
- HTML5-compliant parser following W3C specifications
- Native CSS selector support (jQuery-like syntax)
- LINQ-friendly API for document traversal
- Complete DOM implementation
- Async parsing via `ParseDocumentAsync()`

**Limitations**:
- Higher memory footprint than HtmlAgilityPack
- Slightly slower for simple parsing tasks
- Requires familiarity with CSS selectors

**Best Practices**:
- Use CSS selectors for clean, readable element queries
- Dispose document objects after use to free memory
- Parse HTML on background threads when possible
- Cache parsed documents for repeated access

**Decision Rationale**: Standards-compliant parsing ensures Unity documentation HTML is correctly interpreted, CSS selectors provide clean code, and async support enables non-blocking documentation fetching.

#### websocket-sharp for WebSocket Server
**Version**: Stable (GitHub: sta/websocket-sharp)
**Documentation**: https://sta.github.io/websocket-sharp/
**Research Date**: 2025-11-06
**License**: MIT

**Key Capabilities**:
- Full WebSocket client and server implementation in C#
- Server capabilities via WebSocketSharp.Server.HttpServer
- Custom WebSocketBehavior classes for message handling
- Unity-compatible with editor integration
- Supports secure WebSocket (wss://)

**Limitations**:
- Less actively maintained (stable but fewer updates)
- Known issue: Can freeze Unity Editor on Send() operations
- WebSocket callbacks run on background threads (Unity main thread marshalling required)
- Not available for WebGL (browser limitation, not relevant for Editor server)

**Best Practices**:
- Bind server to localhost only for security
- Implement message queue pattern for thread-safe Unity API access
- Use ConcurrentQueue + EditorApplication.update for main thread synchronization
- Handle connection lifecycle properly (OnOpen, OnClose, OnError)

**Decision Rationale**: Only mature WebSocket server solution for Unity Editor, despite threading limitations. Message queue pattern addresses threading constraints effectively.

### API & Service Research

**Note**: This project does not integrate with external third-party APIs or services. All functionality is provided through:
1. Unity Editor internal APIs (documented in Technology Research above)
2. Local Unity documentation files (no external service dependency)
3. Unity web documentation (docs.unity3d.com) as fallback only

**Unity Documentation Web Access**:
- **URL**: https://docs.unity3d.com (no API key required)
- **Rate Limits**: No official rate limits documented; implement conservative rate limiting (1-2 second delays between requests)
- **Caching Strategy**: Cache downloaded HTML locally for 30 days to minimize network requests (FR-004)
- **Error Handling**: Implement exponential backoff for 429/503 responses, graceful fallback to cached data

### Architecture Pattern Research

#### Thread-Safe Unity Editor Integration Pattern
**Research Sources**: Unity documentation (EditorApplication callbacks), websocket-sharp threading documentation, C# ConcurrentQueue patterns
**Research Date**: 2025-11-06

**Approach**:
- WebSocket server callbacks run on background threads (websocket-sharp behavior)
- All Unity API calls MUST execute on main thread (Unity Editor constraint)
- Implement message queue pattern: ConcurrentQueue<Action> for cross-thread communication
- EditorApplication.update polls queue and executes actions on main thread

**Benefits**:
- Non-blocking WebSocket message handling (responsive server)
- Safe Unity API access (no threading violations)
- Clean separation between network layer and Unity integration
- Standard C# pattern (well-documented, proven in Unity Editor extensions)

**Drawbacks**:
- Adds latency (wait for next EditorApplication.update frame)
- Requires careful error handling across thread boundary
- Queue management overhead (allocation for each queued action)

**Implementation Considerations**:
- Use `ConcurrentQueue<Action>` for thread-safe enqueue/dequeue
- Limit queue size to prevent memory exhaustion under load
- Handle exceptions in queued actions (don't crash main thread)
- Implement timeout mechanism for long-running Unity API calls

**Decision Rationale**: Only viable pattern for WebSocket server in Unity Editor given threading constraints. Pattern is proven in existing Unity Editor extensions and well-documented.

#### Modular Tool Registration Pattern (MCP SDK)
**Research Sources**: ModelContextProtocol SDK documentation, Microsoft DI patterns
**Research Date**: 2025-11-06

**Approach**:
- Use [McpServerToolType] attribute to mark classes containing tools
- Use [McpServerTool] attribute on individual tool methods
- ModelContextProtocol SDK automatically discovers and registers tools
- Dependency injection provides services (HttpClient, logging, custom services)

**Benefits**:
- Clean separation of concerns (each tool is independent)
- Easy to add new tools without modifying core MCP server
- Automatic JSON schema generation from method parameters
- Built-in parameter validation and error handling

**Drawbacks**:
- Attribute-based magic (less explicit than manual registration)
- Requires understanding of MCP SDK conventions
- Limited control over tool discovery process

**Implementation Considerations**:
- Organize tools into separate classes by category (Documentation, Profiler, Build, etc.)
- Use [Description] attributes extensively for LLM context
- Return JSON-serialized strings (not complex objects) for flexibility
- Implement cancellation token support for long-running operations

**Decision Rationale**: Official MCP SDK pattern provides clean architecture, eliminates boilerplate code, and ensures protocol compliance. Attribute-based approach is idiomatic for C# and well-documented.

#### Incremental Data Update Pattern (Context Optimization)
**Research Sources**: uLoopMCP incremental update philosophy, Unity ObjectChangeEvents API
**Research Date**: 2025-11-06

**Approach**:
- Use EditorApplication callbacks (hierarchyChanged, projectChanged) to detect changes
- Use ObjectChangeEvents API for fine-grained change detection
- Cache full state on first query, return only deltas on subsequent queries
- Client tracks state version; server returns changes since version N

**Benefits**:
- Dramatically reduces token consumption for AI context (FR-031)
- Faster responses (less data to serialize/transmit)
- More responsive AI interactions (smaller context windows)
- Follows uLoopMCP's proven approach

**Drawbacks**:
- More complex state management (server must track client state)
- Cache invalidation complexity (ensure correctness)
- Client must handle delta application (reconstruct full state)

**Implementation Considerations**:
- Use versioning scheme (incrementing integer or timestamp)
- Implement cache eviction policy (limit memory usage)
- Provide "full refresh" option when delta too large
- Handle concurrent clients with separate cache states

**Decision Rationale**: Context window optimization is critical for AI-assisted development (specified in project CLAUDE.md). Incremental updates directly address FR-031 requirement and align with uLoopMCP's successful approach.

### Research-Informed Recommendations

**Primary Technology Choices**:
1. **ModelContextProtocol SDK (0.4.0-preview.3)** - Official MCP implementation eliminates custom protocol handling
2. **SQLite FTS5 (System.Data.SQLite)** - Embedded full-text search for local documentation indexing
3. **Unity ProfilerRecorder API** - Official profiler access with low overhead
4. **NJsonSchema** - Automatic JSON schema generation from C# types (Unity compatible)
5. **Fastenshtein** - High-performance fuzzy string matching for typo tolerance
6. **AngleSharp** - Standards-compliant HTML parsing for Unity documentation
7. **websocket-sharp** - Mature WebSocket server for Unity Editor (with thread-safe message queue pattern)

**Architecture Approach**:
- **Phase 1: Foundation** - Establish MCP server infrastructure with stdio transport, documentation indexing system, and basic Unity API wrappers
- **Phase 2: Core Tools** - Implement Profiler, Asset, and Scene tools using proven Unity APIs
- **Phase 3: Advanced Features** - Add Build automation, Package management, and WebSocket transport
- **Phase 4: Optimization** - Implement incremental updates, caching, and performance tuning

**Key Constraints Identified**:
1. **Unity Threading Model**: All Unity API calls require main thread execution (use message queue pattern)
2. **Documentation Indexing Time**: ~31 minutes for 500k documents (index during idle time, show progress)
3. **WebSocket Stability**: websocket-sharp has known editor freeze issues (implement timeouts and error recovery)
4. **.NET Compatibility**: Requires .NET Standard 2.1 API compatibility level in Unity Project Settings
5. **Memory Profiler Detail**: Requires Development Build for full functionality (document in user-facing tool descriptions)

---

## Technical Architecture

> **Note**: This section references the detailed research findings above to avoid duplication.

### System Overview

**High-Level Architecture**: Unity Editor extension implementing MCP server protocol (using ModelContextProtocol SDK) with modular tool architecture. Server runs inside Unity Editor process, communicates via stdio transport initially (WebSocket transport in Phase 3), and provides AI-accessible tools for documentation queries, profiler analysis, build automation, asset management, scene validation, and package management.

**Core Components**:
- **MCP Server Core**: ModelContextProtocol SDK-based server with [InitializeOnLoad] startup, attribute-based tool registration, and thread-safe message handling via ConcurrentQueue + EditorApplication.update pattern
- **Documentation Indexing System**: SQLite FTS5 database indexing local Unity documentation, AngleSharp HTML parsing for web fallback, Fastenshtein fuzzy search for typo tolerance
- **Unity API Wrapper Layer**: Type-safe C# classes wrapping Unity Editor APIs (ProfilerRecorder, BuildPipeline, AssetDatabase, EditorSceneManager, PackageManager.Client) with error handling and async patterns
- **Tool Implementations**: Six tool categories (Documentation, Profiler, Build, Assets, Scene, Packages) implemented as [McpServerToolType] classes with individual [McpServerTool] methods
- **Context Optimization Layer**: Incremental update tracking using ObjectChangeEvents, caching with smart invalidation, pagination for large datasets, selective serialization

**Data Flow**:
1. AI Client → MCP Server (JSON-RPC 2.0 over stdio)
2. MCP Server → Message Queue (ConcurrentQueue<Action>)
3. EditorApplication.update → Dequeue & Execute on Main Thread
4. Unity API Call → Data Retrieval (profiler data, asset lists, scene hierarchy, etc.)
5. Data Serialization → JSON with Context Optimization (incremental updates, pagination)
6. MCP Server → AI Client (JSON-RPC 2.0 response)

### C# / Unity Editor Implementation Details

#### MCP Server Structure

**Component Hierarchy**:
```
UnifyMcpServer (EditorWindow or [InitializeOnLoad] static class)
├── Core/
│   ├── McpServerLifecycle - Server initialization, startup, shutdown
│   ├── TransportManager - stdio transport (Phase 1), WebSocket (Phase 3)
│   └── MessageQueue - Thread-safe ConcurrentQueue<Action> with main thread dispatcher
├── Tools/
│   ├── DocumentationTools - [McpServerToolType] class with query methods
│   ├── ProfilerTools - [McpServerToolType] class with analysis methods
│   ├── BuildTools - [McpServerToolType] class with automation methods
│   ├── AssetTools - [McpServerToolType] class with management methods
│   ├── SceneTools - [McpServerToolType] class with validation methods
│   └── PackageTools - [McpServerToolType] class with package operations
└── Common/
    ├── UnityApiWrappers - Type-safe wrappers for Unity Editor APIs
    ├── ContextOptimizer - Incremental updates, caching, pagination
    └── JsonSchemaGenerator - NJsonSchema-based schema generation
```

**Threading Model**:
- **Main Thread**: All Unity API calls, EditorApplication.update polling, message queue processing
- **Background Threads**: WebSocket callbacks (Phase 3), documentation indexing, HTML parsing
- **Synchronization**: ConcurrentQueue<Action> + EditorApplication.update pattern for cross-thread communication

**Architectural Decision Rationale**:
- **Why [InitializeOnLoad]**: Automatic server startup when Unity Editor launches (no manual activation required)
- **Why Message Queue Pattern**: Only viable approach for WebSocket→Unity API communication given threading constraints
- **Trade-offs**: Small latency (wait for next update frame) vs. thread safety and Unity API access

#### Data Layer Design

**Storage Strategy**: SQLite FTS5 for documentation index (file-based embedded database)

**Model Architecture**:
```csharp
// Primary data structures (conceptual - detailed implementation in Steps phase)
public class UnityApiMethod
{
    public string ClassName { get; set; }
    public string MethodName { get; set; }
    public string ReturnType { get; set; }
    public List<Parameter> Parameters { get; set; }
    public string Description { get; set; }
    public List<string> CodeExamples { get; set; }
    public string UnityVersion { get; set; }
    public string DocumentationUrl { get; set; }
}

public class ProfilerSnapshot
{
    public int FrameCount { get; set; }
    public Dictionary<string, float> CpuTimes { get; set; }
    public long GcAllocations { get; set; }
    public List<Bottleneck> Bottlenecks { get; set; }
}

public class AssetDependencyGraph
{
    public Dictionary<string, List<string>> Dependencies { get; set; }
    public List<string> UnusedAssets { get; set; }
}
```

**Data Access Pattern**: Direct Unity Editor API access on main thread, results cached with ObjectChangeEvents-based invalidation

**Caching Strategy**:
- Documentation queries: 30-day cache with SQLite persistence (FR-004)
- Profiler snapshots: In-memory cache for current session only
- Asset dependency graphs: Cache with invalidation on AssetDatabase.projectChanged

**Decision Rationale**:
- **Why SQLite FTS5**: Embedded database with excellent full-text search, proven Unity compatibility, no external service dependency
- **Performance characteristics**: <100ms cached queries (FR-001), ~30 seconds asset analysis (FR-017)
- **Scalability considerations**: Handles 10,000+ GameObjects, 5,000+ assets with pagination (edge case requirement)

#### MCP Tool Architecture

**Tool Organization** (using ModelContextProtocol SDK patterns):
```csharp
[McpServerToolType]
public class DocumentationTools
{
    [McpServerTool]
    [Description("Query Unity API documentation for method signatures, parameters, and usage examples")]
    public async Task<string> QueryDocumentation(
        [Description("Unity API name (e.g., Transform.Translate)")] string apiName,
        CancellationToken cancellationToken)
    {
        // Implementation uses SQLite FTS5 + Fastenshtein fuzzy search
        // Returns JSON-serialized documentation with examples
    }
}

[McpServerToolType]
public class ProfilerTools
{
    [McpServerTool]
    [Description("Capture Unity Profiler data and identify performance bottlenecks")]
    public async Task<string> CaptureProfilerSnapshot(
        [Description("Number of frames to capture (default 300)")] int frameCount = 300,
        CancellationToken cancellationToken)
    {
        // Implementation uses ProfilerRecorder API
        // Returns JSON-serialized bottleneck analysis
    }
}
```

**Integration Strategy**:
- **Unity APIs**: Direct calls via wrapper classes (ProfilerRecorderWrapper, BuildPipelineWrapper, etc.)
- **Error Handling**: Try-catch with detailed error messages, graceful degradation for unavailable features
- **Async Patterns**: Use async/await with CancellationToken support, marshal to main thread via message queue when needed

**Dependency Management**:
- **NuGet Dependencies**: ModelContextProtocol (0.4.0-preview.3), NJsonSchema, System.Data.SQLite, Fastenshtein, AngleSharp, websocket-sharp
- **Unity Package Dependencies**: com.unity.editorcoroutines, com.unity.memoryprofiler
- **Version Constraints**: .NET Standard 2.1 API compatibility level required

#### Platform-Specific Considerations

**Unity Editor Platforms**:
- **Windows**: Primary development/testing platform (Unity 2021.3 LTS+)
- **macOS**: Full support (Unity Editor available)
- **Linux**: Full support (Unity Editor available)

**Unity Version Requirements**:
- **Minimum Version**: Unity 2021.3 LTS (ProfilerRecorder API availability)
- **Tested Versions**: 2021.3 LTS, 2022.3 LTS, Unity 6
- **API Compatibility**: .NET Standard 2.1 API compatibility level (Edit > Project Settings > Player > Other Settings)

**Performance Targets**:
- Documentation query: <100ms cached, <2s web fetch (FR-001)
- Profiler analysis: <10s for 300 frames (FR-006)
- Asset dependency scan: <30s for 1,000+ assets (FR-017)
- Build validation: <5s pre-build checks (FR-015)
- Editor responsiveness: No frame drops during MCP operations (non-blocking async patterns)

**Unity Editor Compliance**:
- **Threading Compliance**: All Unity API calls on main thread (EditorApplication.update)
- **EditorPrefs**: Store user settings (server port, documentation cache location)
- **No Play Mode Dependency**: Tools work in Edit mode (except profiler which requires Play mode for some features)
- **Undo/Redo Support**: Asset modifications use AssetDatabase APIs with proper undo integration where applicable

### Implementation Complexity Assessment

**Complexity Level**: **Complex**

**Implementation Challenges**:
- **Setup and Infrastructure**:
  - NuGet package integration in Unity (requires custom .csproj modifications or Unity Package Manager integration)
  - SQLite native DLL management across platforms (Windows, macOS, Linux)
  - MCP SDK preview status requires monitoring for breaking changes
  - Thread-safe message queue pattern adds complexity to all Unity API interactions
- **Core Implementation**:
  - Documentation indexing (~31 minutes initial setup) requires background processing with progress reporting
  - WebSocket server threading constraints require careful error handling across thread boundaries
  - Profiler API integration needs Development Build for full functionality (user workflow impact)
  - Incremental update tracking requires complex state management and cache invalidation logic
- **Integration Points**:
  - Clean slate project simplifies integration but requires establishing all foundational patterns
  - Unity Editor lifecycle management ([InitializeOnLoad] timing, domain reloads, assembly recompilation)
  - EditorApplication.update integration must handle exceptions gracefully to avoid editor crashes
- **Testing Requirements**:
  - Unity Test Runner for Editor tests (integration tests require active Unity Editor instance)
  - Mock Unity APIs for unit testing (complex due to static method prevalence)
  - Multi-version testing across Unity 2021.3 LTS, 2022.3 LTS, and Unity 6

**Risk Assessment**:
- **High Risk Areas**:
  1. **websocket-sharp stability**: Known editor freeze issues require robust timeout and error recovery mechanisms
  2. **Threading bugs**: Unity main thread violations cause cryptic errors and editor crashes
  3. **Documentation indexing performance**: 31-minute initial index could frustrate users (needs clear communication and progress UI)
  4. **NuGet dependency conflicts**: Unity's package system and .NET ecosystem may clash
  5. **MCP SDK preview status**: Breaking changes in ModelContextProtocol package could require rework
- **Mitigation Strategies**:
  1. Start with stdio transport (simpler, no threading issues) before adding WebSocket support
  2. Comprehensive thread safety testing with message queue pattern validation
  3. Background indexing with cancellation support and user-facing progress indicators
  4. Vendor critical dependencies if necessary (include source code in project)
  5. Pin ModelContextProtocol to specific version, monitor GitHub releases
- **Unknowns**:
  - Real-world performance of SQLite FTS5 with Unity's varied project sizes
  - Memory overhead of concurrent MCP operations in large Unity projects
  - Unity Editor performance impact of EditorApplication.update polling frequency

**Dependency Analysis**:
- **External Dependencies** (NuGet):
  - ModelContextProtocol 0.4.0-preview.3 (MIT) - Preview status risk
  - System.Data.SQLite 1.0.118+ (Public Domain) - Native DLL platform dependencies
  - NJsonSchema (MIT) - Stable, widely used
  - Fastenshtein 1.0.11 (MIT) - Stable, minimal
  - AngleSharp (MIT) - Active development
  - websocket-sharp (MIT) - Less actively maintained (risk)
- **Unity Package Dependencies**:
  - com.unity.editorcoroutines - Official Unity package, stable
  - com.unity.memoryprofiler - Optional, for advanced profiler features
- **Breaking Changes**: None (new project, no existing API contracts to maintain)

**Testing Strategy**:
- **Unit Tests**:
  - Core logic (documentation parsing, fuzzy search, data serialization) with mocked Unity APIs
  - Use NSubstitute or Moq for dependency injection testing
  - Target 80%+ code coverage for non-Unity API code
- **Integration Tests**:
  - Unity Editor Test Runner for testing actual Unity API integration
  - Create test scenes/projects for validation scenarios
  - Test across Unity 2021.3 LTS, 2022.3 LTS, Unity 6
- **Manual Testing**:
  - MCP protocol compliance testing with Claude Desktop/VS Code MCP client
  - Performance testing with large projects (10,000+ GameObjects, 5,000+ assets)
  - Threading stress testing (concurrent MCP requests)

### Technical Clarifications

**No Outstanding Clarifications**: All technical uncertainties addressed through comprehensive research. Architecture decisions are well-founded on proven technologies (ModelContextProtocol SDK, Unity Editor APIs) and established patterns (message queue for threading, FTS5 for search).

**Implementation is ready to proceed to task breakdown phase.**

---

**Next Phase**: After this technical planning is approved, proceed to `/ctxk:plan:3-steps` for implementation task breakdown.
