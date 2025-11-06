# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

@Context.md

## Project Overview

**unify-mcp** is a Unity Model Context Protocol (MCP) server focused on filling critical gaps in the Unity MCP ecosystem. This project builds upon learnings from existing tools like uLoopMCP, CoderGamester/mcp-unity, and IvanMurzak/Unity-MCP to provide advanced capabilities not currently available.

### Core Mission

Provide AI-assisted Unity development tools that address genuine developer pain points in areas currently underserved:

1. **Advanced Profiling & Performance Analysis** - Programmatic Unity Profiler, Frame Debugger, and Memory Profiler integration
2. **Build Pipeline Automation** - Multi-platform build orchestration, Asset Bundle management, build size analysis
3. **Asset Database Operations** - Batch operations, dependency analysis, unused asset detection, optimization
4. **Scene Analysis & Validation** - Deep scene inspection, validation rules, performance antipattern detection
5. **Enhanced Debugging** - Conditional breakpoint management, call stack analysis, visual debugging tools
6. **Package Management** - Dependency resolution, version management, compatibility checking
7. **Context Window Optimization** - Incremental updates, selective serialization, compressed data formats

## Architecture Philosophy

### Design Principles

1. **Context-Aware Minimalism**: Follow uLoopMCP's approach of minimizing token consumption through intelligent filtering and incremental updates
2. **Modular Tool System**: Plugin architecture where custom tools can be dynamically loaded without core modifications
3. **Event-Driven Updates**: Use Unity's `EditorApplication` callbacks to push updates rather than polling
4. **Type-Safe Schemas**: Generate JSON schemas from C# types automatically for better AI understanding
5. **Async-First**: Leverage Unity's async/await for long-running operations without blocking the editor

### Security & Isolation Model

- **Sandboxed Execution**: Run custom tools in separate AppDomains or processes where appropriate
- **Granular Permissions**: Implement permission levels (read-only vs. write, editor-only vs. runtime)
- **Audit Logging**: Track all operations for debugging and security review
- **Rate Limiting**: Prevent overwhelming the editor with excessive requests
- **Input Validation**: Sanitize all inputs before executing Unity API calls

## Technical Stack

### Unity Integration

- **Editor Scripting**: Primary interface through UnityEditor namespace APIs
- **EditorWindow**: Custom windows for tool interfaces and monitoring
- **AssetDatabase**: Asset management, import settings, dependency tracking
- **Profiler API**: Performance data capture and analysis
- **BuildPipeline API**: Build automation and configuration

### MCP Protocol Implementation

- **Transport**: WebSocket-based communication (following CoderGamester pattern)
- **Serialization**: JSON with optional binary for large data transfers
- **Schema Generation**: Automatic schema generation from C# types
- **Incremental Sync**: Delta compression for reducing data transfer

### Performance Optimizations

- **Object Pooling**: Reuse serialization buffers to reduce GC pressure
- **Lazy Evaluation**: Defer expensive operations until data is requested
- **Caching Layer**: Cache frequently accessed data (scene hierarchies, asset lists) with smart invalidation
- **Parallel Processing**: Use `Task.Run` for CPU-intensive operations
- **Streaming Responses**: For large datasets, stream results incrementally

## Key Tool Categories

### 1. Profiler Integration Tools

**Purpose**: Provide programmatic access to Unity's profiling tools for AI-assisted performance optimization.

**Key Operations**:
- Capture profiler snapshots programmatically
- Parse and analyze profiler data (CPU, GPU, memory, rendering)
- Compare snapshots between builds/commits
- Identify common performance bottlenecks automatically
- Frame Debugger control for rendering inspection
- Memory Profiler integration for leak detection

**Implementation Notes**:
- Use `ProfilerDriver` and `FrameDebuggerUtility` APIs
- Implement custom profiler marker parsing
- Create aggregation and statistical analysis utilities
- Support both Editor and Player profiling modes

### 2. Build Pipeline Automation

**Purpose**: Comprehensive build automation beyond basic platform switching.

**Key Operations**:
- Multi-platform build orchestration with BuildPlayer API
- Asset Bundle creation and management
- Build size analysis and breakdown by category
- Custom preprocessor directive management
- Post-build processing hooks
- BuildOptions configuration management

**Implementation Notes**:
- Extend `BuildPlayerWindow.DefaultBuildMethods`
- Implement `IPreprocessBuildWithReport` and `IPostprocessBuildWithReport`
- Parse build reports for size analysis
- Track build metrics over time for trend analysis

### 3. Advanced Asset Database Operations

**Purpose**: Sophisticated asset management beyond basic file operations.

**Key Operations**:
- Batch asset operations with `StartAssetEditing`/`StopAssetEditing`
- Asset dependency graph analysis and visualization
- Unused asset detection with reference scanning
- Bulk import settings modification
- Asset label and bundle assignment automation
- GUID-based reference tracking and repair
- Texture compression optimization across platforms

**Implementation Notes**:
- Use `AssetDatabase.GetDependencies` for graph analysis
- Implement reference search using serialized property traversal
- Cache asset metadata for faster repeated operations
- Support undo/redo for all modifications

### 4. Scene Analysis & Validation

**Purpose**: Deep scene inspection and validation beyond basic hierarchy access.

**Key Operations**:
- Validate scenes against project-specific naming and structure rules
- Detect missing references and broken prefab connections
- Analyze lighting setup (baked, realtime, mixed mode issues)
- Check shader complexity and material usage
- Identify objects outside playable bounds
- Generate scene documentation automatically
- Compare scenes for consistency checking

**Implementation Notes**:
- Traverse scene hierarchy recursively with component inspection
- Use `SerializedObject` to detect missing references
- Integrate with `LightingSettings` API for lighting analysis
- Implement configurable validation rule system

### 5. Context Window Optimization

**Purpose**: Minimize token consumption for AI interactions following uLoopMCP's philosophy.

**Key Strategies**:
- **Incremental Scene Updates**: Provide diff-based updates instead of full hierarchies
- **Selective Component Serialization**: Allow requesting only specific component types
- **Compressed Data Formats**: Use binary serialization for large data
- **Smart Pagination**: Auto-determine optimal page sizes based on complexity
- **Reference IDs**: Return lightweight references expandable on-demand
- **Change Detection**: Track modification timestamps to send only changed data

**Implementation Notes**:
- Implement diffing algorithms for scene hierarchies
- Use `ObjectChangeEvents` API for change detection
- Create lightweight proxy objects with lazy property loading
- Support query parameters for filtering data

## Development Workflow

### Project Structure

```
/                           # Root
├── src/
│   ├── Core/              # Core MCP protocol implementation
│   ├── Tools/             # Individual tool implementations
│   │   ├── Profiler/      # Profiling tools
│   │   ├── Build/         # Build automation
│   │   ├── Assets/        # Asset management
│   │   ├── Scene/         # Scene analysis
│   │   └── Debug/         # Debugging tools
│   ├── Common/            # Shared utilities
│   │   ├── Serialization/ # JSON/Binary serialization
│   │   ├── Caching/       # Data caching layer
│   │   └── Schemas/       # Type schemas
│   └── Unity/             # Unity-specific integration
│       ├── Editor/        # Editor scripts
│       └── Runtime/       # Runtime components
├── tests/                 # Unit and integration tests
└── docs/                  # Documentation
```

### Unity Editor Integration

The MCP server runs within Unity Editor as an EditorWindow or as a background process communicating via WebSocket. Key integration points:

- **Initialization**: Use `[InitializeOnLoad]` attribute for automatic startup
- **Editor Callbacks**: Register for `EditorApplication` events (playModeStateChanged, hierarchyChanged, projectWindowItemOnGUI)
- **Custom Menu Items**: Expose tools via `MenuItem` attributes under "Tools/UnifyMCP/"
- **Settings Provider**: Create `SettingsProvider` for configuration UI

### Testing Strategy

Building on uLoopMCP's testing approach:

- **Unit Tests**: Test individual tool logic in isolation
- **Integration Tests**: Test Unity API interactions in Editor test mode
- **Performance Tests**: Measure operation costs and identify optimization opportunities
- **Regression Tests**: Compare outputs across Unity versions
- **Coverage Analysis**: Track test coverage and identify gaps

### Common Patterns

#### Async Editor Operations

```csharp
// Use EditorCoroutine or async/await for long operations
public static async Task<ProfilerData> CaptureProfilerSnapshotAsync()
{
    await Task.Run(() => {
        // Heavy computation on background thread
    });

    // Return to main thread for Unity API calls
    await EditorAwait.NextUpdate();

    return data;
}
```

#### Safe Asset Modification

```csharp
// Always wrap batch operations
AssetDatabase.StartAssetEditing();
try
{
    // Perform multiple asset operations
}
finally
{
    AssetDatabase.StopAssetEditing();
}
```

#### Context-Aware Serialization

```csharp
// Support incremental data transfer
public class SceneData
{
    public SerializationMode Mode { get; set; } // Full, Incremental, Summary
    public List<GameObject> NewObjects { get; set; }
    public List<string> DeletedObjectIds { get; set; }
    public Dictionary<string, object> ModifiedProperties { get; set; }
}
```

## Key Differentiators from Existing Tools

### vs. uLoopMCP
- **Extends**: Build on autonomous development cycle concepts
- **Adds**: Profiling integration, advanced asset operations, scene validation
- **Improves**: More granular context optimization, streaming responses

### vs. CoderGamester/mcp-unity
- **Extends**: WebSocket communication pattern
- **Adds**: Build automation, package management, debugging tools
- **Improves**: Batch operations, async handling

### vs. IvanMurzak/Unity-MCP
- **Extends**: Reflection-powered discovery
- **Adds**: Performance analysis, validation rules, optimization recommendations
- **Improves**: Type-safe schemas, incremental updates

## Future Expansion Areas

### Phase 1: Core Tools (Current Focus)
- Profiler integration
- Asset batch operations
- Build pipeline automation
- Scene validation

### Phase 2: Advanced Intelligence
- AI-powered code refactoring suggestions
- Project health monitoring dashboard
- Technical debt tracking
- Intelligent asset workflow recommendations

### Phase 3: Runtime Integration
- In-game MCP server for runtime debugging
- Live performance monitoring during play mode
- Dynamic asset loading optimization
- Runtime scene manipulation for testing

## Research References

This project is informed by comprehensive research into Unity MCP ecosystem gaps, developer pain points, and existing tool limitations. Key insights:

- Unity developers' primary pain points: performance optimization, build complexity, asset management at scale
- MCP protocol flexibility enables innovation beyond current tool offerings
- Context window optimization is critical for AI effectiveness
- Security and sandboxing essential for tool trust
- Modular architecture enables community contributions

## Contributing Guidelines

When implementing new tools:

1. **Start with User Pain Point**: Identify specific manual/repetitive/error-prone workflow
2. **Design for Minimal Context**: How can this data be represented most efficiently?
3. **Implement Incrementally**: Support full, summary, and incremental modes
4. **Add Validation**: Validate all inputs before Unity API calls
5. **Enable Async**: Long operations must be non-blocking
6. **Document Schemas**: Generate JSON schemas for all data structures
7. **Add Tests**: Unit tests for logic, integration tests for Unity API usage
8. **Optimize Performance**: Profile and optimize before merging

## Notes on Unity Version Compatibility

- **Target**: Unity 2021.3 LTS and newer
- **API Changes**: Use `#if` directives for version-specific APIs
- **Deprecations**: Avoid deprecated APIs, implement migration helpers
- **Testing**: Test against multiple Unity LTS versions
- **Documentation**: Note version-specific features clearly
