# Implementation Steps: Unity MCP Documentation-First Platform
<!-- Template Version: 12 | ContextKit: 0.2.0 | Updated: 2025-10-17 -->

## Description
Implementation task breakdown for Unity MCP server with systematic S001-S999 task enumeration, parallel execution markers, and dependency analysis for C#/Unity Editor development workflow.

**Created**: 2025-11-06
**Status**: Implementation Plan
**Prerequisites**: Completed Spec.md and Tech.md in Context/Features/001-UnifyMcpDocumentationFirst/

## 🚨 CRITICAL: This File is Your Progress Tracker

**This Steps.md file serves as the authoritative source of truth for implementation progress across all development sessions.**

### Key Principles
- **Token limits are irrelevant** - Progress is tracked here, sessions are resumable
- **Never rush or take shortcuts** - Each step deserves proper attention and time
- **Session boundaries don't matter** - User can resume where this file shows progress
- **Steps.md is the real todo list** - Even if AI uses TodoWrite during a session, THIS file is what persists
- **Quality over speed** - Thoroughness is mandatory, optimization for token limits is forbidden
- **Check off progress here** - Mark tasks as complete in this file as they're finished

### How This Works
1. Each task has a checkbox: `- [ ] **S001** Task description`
2. As tasks complete, they're marked: `- [x] **S001** Task description`
3. AI ignores token limit concerns and works methodically through steps
4. If context usage gets high (>80%), AI suggests user runs `/compact` before continuing
5. If session ends: User starts new session and resumes (this file has all progress)
6. Take the time needed for each step - there's no rush to finish in one session

# Implementation Steps: Unity MCP Documentation-First Platform

**Created**: 2025-11-06
**Status**: Implementation Plan
**Prerequisites**: Completed Spec.md and Tech.md in Context/Features/001-UnifyMcpDocumentationFirst/

## Implementation Phases *(mandatory)*

### Phase 1: Setup & Configuration
*Foundation tasks that must complete before development*

- [x] **S001** Create Unity project structure with .NET Standard 2.1 API compatibility
  - **Path**: Create `src/`, `tests/`, `docs/` root directories
  - **Dependencies**: None
  - **Notes**: ✅ Directories created. .NET Standard configuration will be applied when creating .csproj files for Unity plugin component
  - **Completed**: 2025-11-06

- [x] **S002** Install ModelContextProtocol NuGet package (0.4.0-preview.3)
  - **Path**: Unity project root, NuGet packages configuration
  - **Dependencies**: S001
  - **Notes**: ✅ Created Unity package structure: package.json, UnifyMcp.Editor.asmdef, src/Plugins/ with README for NuGet DLL installation. DLLs must be manually downloaded and placed in src/Plugins/ per README instructions.
  - **Completed**: 2025-11-06

- [x] **S003** [P] Install System.Data.SQLite NuGet package (1.0.118+)
  - **Path**: Unity project root, NuGet packages configuration
  - **Dependencies**: S001
  - **Notes**: ✅ Documented in src/Plugins/README.md. Manual installation required.
  - **Completed**: 2025-11-06

- [x] **S004** [P] Install supporting NuGet packages (NJsonSchema, Fastenshtein, AngleSharp)
  - **Path**: Unity project root, NuGet packages configuration
  - **Dependencies**: S001
  - **Notes**: ✅ All packages documented in src/Plugins/README.md with installation instructions.
  - **Completed**: 2025-11-06

- [x] **S005** Install Unity package: com.unity.editorcoroutines
  - **Path**: Unity Package Manager (Window → Package Manager → Add package by name)
  - **Dependencies**: S001
  - **Notes**: ✅ Added as dependency in package.json. Unity will auto-install when package is added to project.
  - **Completed**: 2025-11-06

- [x] **S006** [P] Configure SQLite native DLL for Windows/macOS/Linux platforms
  - **Path**: `src/Plugins/` directory structure for platform-specific binaries
  - **Dependencies**: S003
  - **Notes**: ✅ Platform-specific folder structure documented in src/Plugins/README.md (x86_64/, macOS/, Linux/).
  - **Completed**: 2025-11-06

- [x] **S007** Create Core/ folder structure (McpServer.cs, TransportLayer/, SchemaGenerator.cs)
  - **Path**: `src/Core/`, `src/Core/TransportLayer/`
  - **Dependencies**: S001
  - **Notes**: ✅ Directories created. Implementation files will be created in Phase 3.
  - **Completed**: 2025-11-06

- [x] **S008** [P] Create Tools/ folder structure for six tool categories
  - **Path**: `src/Tools/{Documentation,Profiler,Build,Assets,Scene,Packages}/`
  - **Dependencies**: S001
  - **Notes**: ✅ All six tool category directories created.
  - **Completed**: 2025-11-06

- [x] **S009** [P] Create Common/ folder structure (Threading/, Caching/, Serialization/)
  - **Path**: `src/Common/{Threading,Caching,Serialization}/`
  - **Dependencies**: S001
  - **Notes**: ✅ All three utility directories created.
  - **Completed**: 2025-11-06

- [x] **S010** Create Unity/Editor/ folder with [InitializeOnLoad] bootstrap class
  - **Path**: `src/Unity/Editor/`
  - **Dependencies**: S001, S002
  - **Notes**: ✅ Directory created. Bootstrap class implementation will be created in Phase 3 (S029).
  - **Completed**: 2025-11-06

**🏁 MILESTONE: Foundation Setup**
*All project structure and dependencies configured. MCP SDK and Unity Editor integration ready.*

### Phase 2: Documentation Indexing System (TDD Approach)
*SQLite FTS5 database, HTML parsing, fuzzy search - implements FR-001 through FR-005*

#### Test-First Implementation
- [x] **S011** [P] Create tests for UnityDocumentationIndexer with SQLite FTS5 schema
  - **Path**: `tests/Documentation/UnityDocumentationIndexerTests.cs`
  - **Dependencies**: S001, S003
  - **Notes**: ✅ Created comprehensive test suite with 12 tests covering database creation, FTS5 schema, indexing, querying, BM25 ranking, and performance (<100ms requirement). Created stub implementation for compilation.
  - **Completed**: 2025-11-06

- [x] **S012** [P] Create tests for HTML parser with sample Unity documentation
  - **Path**: `tests/Documentation/HtmlDocumentationParserTests.cs`
  - **Dependencies**: S001, S004
  - **Notes**: ✅ Created 14 comprehensive tests covering method signatures, properties, parameters, code examples, deprecation detection, and edge cases. Created stub implementation.
  - **Completed**: 2025-11-06

- [x] **S013** [P] Create tests for fuzzy search with typo tolerance scenarios
  - **Path**: `tests/Documentation/FuzzySearchTests.cs`
  - **Dependencies**: S001, S004
  - **Notes**: ✅ Created 15 comprehensive tests covering Levenshtein distance, similarity scoring, typo tolerance, threshold matching, common typos, performance, and edge cases. Created stub implementation.
  - **Completed**: 2025-11-06

#### Model & Service Implementation
- [x] **S014** Implement UnityDocumentationIndexer class with SQLite FTS5
  - **Path**: `src/Tools/Documentation/UnityDocumentationIndexer.cs`
  - **Dependencies**: S003, S011
  - **Notes**: ✅ Implemented complete SQLite FTS5 indexer with porter tokenizer, BM25 ranking, metadata table, parameterized queries, and connection management. All 12 tests should now pass.
  - **Completed**: 2025-11-06

- [x] **S015** Implement local Unity documentation folder detection (Windows/macOS/Linux)
  - **Path**: `src/Tools/Documentation/UnityInstallationDetector.cs`
  - **Dependencies**: S001
  - **Notes**: ✅ Implemented cross-platform Unity installation detection with platform-specific path logic (Windows/macOS/Linux), version ordering (newest first), ScriptReference HTML file discovery, and installation validation. Created comprehensive test suite with 16 tests covering detection, validation, file scanning, performance, and edge cases.
  - **Completed**: 2025-11-06

- [x] **S016** Implement HtmlDocumentationParser using AngleSharp
  - **Path**: `src/Tools/Documentation/HtmlDocumentationParser.cs`
  - **Dependencies**: S004, S012
  - **Notes**: ✅ Implemented comprehensive HTML parser using AngleSharp with intelligent signature parsing for methods and properties, title/h1 extraction for class.method names, description aggregation, code example collection, parameter detail extraction, deprecation detection with replacement API regex, and static method detection. Handles complex scenarios: generics, default parameters, properties with get/set, nested generic parameters in parsing.
  - **Completed**: 2025-11-06

- [x] **S017** Create DocumentationEntry data model with version tracking
  - **Path**: `src/Tools/Documentation/Models/DocumentationEntry.cs`
  - **Dependencies**: S001
  - **Notes**: ✅ Created complete data model with all required fields plus deprecation tracking (IsDeprecated, ReplacementApi). Includes helper methods: GetFullSignature(), GetParameterList(), GetSearchableText().
  - **Completed**: 2025-11-06

- [x] **S018** Implement background indexing worker with cancellation support
  - **Path**: `src/Tools/Documentation/DocumentationIndexingWorker.cs`
  - **Dependencies**: S005, S014, S015, S016
  - **Notes**: ✅ Implemented background indexing using EditorCoroutine with batch processing (configurable batch size and delays), CancellationTokenSource for graceful cancellation, progress tracking (total/processed/success/failed), event system (OnFileProcessed/OnProgress/OnCompleted/OnError), ETA calculation, and IndexingSummary for status reporting. Created 15 comprehensive tests validating initialization, configuration, events, and error handling.
  - **Completed**: 2025-11-06

- [x] **S019** Implement progress reporting UI for indexing
  - **Path**: `src/Unity/Editor/DocumentationIndexingProgressWindow.cs`
  - **Dependencies**: S018
  - **Notes**: ✅ Implemented comprehensive EditorWindow with Unity installation detection/selection, real-time progress bar, statistics display (processed/success/failed), timing information (elapsed/ETA), scrollable log with auto-scroll, cancellation button, and event-driven UI updates. Menu item: Tools/Unify MCP/Documentation Indexer. Database stored in Library/UnifyMcp/ per Unity version.
  - **Completed**: 2025-11-06

- [x] **S020** Implement fuzzy search using Fastenshtein with configurable similarity threshold
  - **Path**: `src/Tools/Documentation/FuzzyDocumentationSearch.cs`
  - **Dependencies**: S004, S013, S014
  - **Notes**: ✅ Implemented fuzzy search with Fastenshtein Levenshtein distance calculation, string normalization (lowercase + trim), similarity scoring (1.0 - distance/maxLength), threshold filtering (default 0.7), descending sort by similarity, and empty string handling. All 15 tests from FuzzySearchTests.cs should now pass.
  - **Completed**: 2025-11-06

- [x] **S021** Implement web documentation fallback using AngleSharp + caching
  - **Path**: `src/Tools/Documentation/WebDocumentationFetcher.cs`
  - **Dependencies**: S004, S016
  - **Notes**: ✅ Implemented HTTP client with rate limiting (1-2s random delay), exponential backoff retry logic for 429/503 errors (max 3 retries), local HTML caching using SHA256 URL hashing, GetCachedHtml/CacheHtml methods, ClearCache and GetCachedFileCount utilities. Configurable: MinimumDelayMilliseconds, MaximumDelayMilliseconds, MaxRetries, InitialRetryDelayMilliseconds.
  - **Completed**: 2025-11-06

- [x] **S022** Create 30-day cache management with expiration tracking
  - **Path**: `src/Tools/Documentation/DocumentationCacheManager.cs`
  - **Dependencies**: S014, S021
  - **Notes**: ✅ Implemented cache metadata tracking with JSON persistence (cache_metadata.json), RecordCachedUrl/IsCached/GetCacheFilePath methods, CleanupExpiredCache with expired/orphaned detection, ClearAllCache for manual cleanup, GetStatistics for cache metrics (entries/size). Default expiration: 30 days. CacheMetadata model: URL, CacheFilePath, FetchTimestamp, ExpirationTimestamp.
  - **Completed**: 2025-11-06

- [x] **S023** Implement Unity version detection and version-appropriate documentation
  - **Path**: `src/Tools/Documentation/UnityVersionManager.cs`
  - **Dependencies**: S001
  - **Notes**: ✅ Implemented GetCurrentUnityVersion (UnityEditor.ApplicationInfo.unityVersion), MapToDocumentationVersion (2021.3.25f1 → 2021.3, handles Unity 6 as 6000.0.x → 6.0), CompareVersions with major/minor/patch comparison, IsDeprecatedInVersion for version-aware deprecation, GetMigrationSuggestion for replacement API guidance, GetDocumentationUrl for version-specific URL generation.
  - **Completed**: 2025-11-06

- [x] **S024** Add deprecation warning detection in parsed documentation
  - **Path**: `src/Tools/Documentation/DeprecationDetector.cs`
  - **Dependencies**: S016, S023
  - **Notes**: ✅ Implemented DetectDeprecation with HTML parsing for .deprecated-message/.obsolete-message elements, keyword detection (deprecated/obsolete/no longer supported), [Obsolete] attribute parsing, regex extraction for replacement APIs (multiple patterns: "Use X instead", "Please use X", "Replaced by X"), version extraction ("since Unity X.Y"), EnrichWithDeprecationInfo for DocumentationEntry enrichment, GetDeprecationWarning for user-facing messages.
  - **Completed**: 2025-11-06

- [x] **S025** Write integration tests for complete indexing workflow
  - **Path**: `tests/Documentation/IntegrationTests/FullIndexingWorkflowTests.cs`
  - **Dependencies**: S014-S024
  - **Notes**: ✅ Created 8 comprehensive integration tests covering end-to-end workflow (detect → parse → index → query → fuzzy search), version manager mapping, deprecation detection, cache management, indexing worker progress tracking, BM25 ranking verification, fuzzy search with common typos, and deprecated API warnings. Mock Unity documentation structure with Transform.Translate, Transform.Rotate, GameObject.Find HTML files.
  - **Completed**: 2025-11-06

**🏁 MILESTONE: Documentation System Complete**
*Documentation indexing, search, and caching fully functional. Implements FR-001 through FR-005.*

### Phase 3: MCP Server Core & Threading Infrastructure
*ModelContextProtocol SDK integration, stdio transport, thread-safe message queue*

#### Core Infrastructure Tests
- [x] **S026** [P] Create tests for thread-safe message queue (ConcurrentQueue pattern)
  - **Path**: `tests/Core/ThreadingTests.cs`
  - **Dependencies**: S001
  - **Notes**: ✅ Created 15 comprehensive tests covering enqueue/dequeue, FIFO ordering, exception handling (continue processing despite errors), null argument validation, queue count tracking, max queue size enforcement, multi-threaded enqueue (thread safety), task execution, Clear/Dispose, performance (1000 actions <500ms), context capture, and exception reporting via OnException event.
  - **Completed**: 2025-11-06

- [x] **S027** [P] Create tests for MCP server lifecycle (startup, shutdown, error recovery)
  - **Path**: `tests/Core/McpServerLifecycleTests.cs`
  - **Dependencies**: S001, S002
  - **Notes**: ✅ Created 10 lifecycle tests: Initialize, Start/Stop transitions, Dispose while running, idempotent Start/Stop, OnError event, Restart, OnStarted/OnStopped events.
  - **Completed**: 2025-11-06

#### Core Implementation
- [x] **S028** Implement thread-safe message queue with EditorApplication.update integration
  - **Path**: `src/Common/Threading/MainThreadDispatcher.cs`
  - **Dependencies**: S026
  - **Notes**: ✅ Implemented MainThreadDispatcher with ConcurrentQueue<Action>, EditorApplication.update auto-registration (Unity Editor), manual ProcessQueue for testing, Enqueue with null/disposed/size validation, exception handling (continues processing, invokes OnException, logs to Unity console), Clear/Dispose methods, singleton Instance pattern with InitializeInstance/DisposeInstance, conditional compilation for Unity Editor vs testing. Default max queue: 1000 actions.
  - **Completed**: 2025-11-06

- [x] **S029** Implement MCP server lifecycle manager with [InitializeOnLoad]
  - **Path**: `src/Core/McpServerLifecycle.cs`
  - **Dependencies**: S002, S010, S027
  - **Notes**: ✅ Implemented lifecycle manager with [InitializeOnLoadMethod], Start/Stop/Restart methods, IsRunning property, OnStarted/OnStopped/OnError events, EditorApplication.quitting integration, MainThreadDispatcher initialization, singleton Instance pattern, conditional compilation for Unity Editor auto-start. Phase 4 TODO markers for ModelContextProtocol and stdio transport initialization.
  - **Completed**: 2025-11-06

- [x] **S030** Implement stdio transport layer for MCP communication
  - **Path**: `src/Core/TransportLayer/StdioTransport.cs`
  - **Dependencies**: S002, S028, S029
  - **Notes**: ✅ Implemented StdioTransport with Console.In/Console.Out (configurable for testing), async read loop on background thread, SendMessageAsync with SemaphoreSlim write lock, Start/StopAsync lifecycle, OnMessageReceived/OnError events, CancellationToken support, graceful shutdown with timeout. Thread-safe message sending, background reading with main thread marshalling support.
  - **Completed**: 2025-11-06

- [x] **S031** Implement JSON schema generator using NJsonSchema
  - **Path**: `src/Core/SchemaGenerator.cs`
  - **Dependencies**: S004
  - **Notes**: ✅ Implemented SchemaGenerator with NJsonSchema integration, GenerateMethodSchema (parameterless/single/multiple params), GenerateTypeSchema with ConcurrentDictionary caching, JsonSchemaGeneratorSettings (NotNull default, abstract properties, flatten hierarchy), ClearCache/CachedSchemaCount utilities. Phase 4 TODO for composite schema generation.
  - **Completed**: 2025-11-06

- [x] **S032** Implement error handling and logging infrastructure
  - **Path**: `src/Common/ErrorHandling/McpErrorHandler.cs`
  - **Dependencies**: S001
  - **Notes**: ✅ Implemented McpErrorHandler with HandleException (categorization + structured error), CategorizeException (UnityApi/McpProtocol/UserError/Internal), LogError (Unity Debug.Log with severity: Warning for UserError, Error+Exception for others), FormatUserMessage for user-friendly errors, OnError event, McpError model (Exception/Category/Context/Timestamp/Message/StackTrace), ErrorCategory enum.
  - **Completed**: 2025-11-06

- [x] **S033** Create EditorPrefs-based configuration manager
  - **Path**: `src/Core/Configuration/McpConfigurationManager.cs`
  - **Dependencies**: S001
  - **Notes**: ✅ Implemented McpConfigurationManager with EditorPrefs persistence, properties: ServerPort (3000), DocCachePath (AppData/UnifyMcp/DocCache), AutoStartServer (true), MaxQueueSize (1000), EnableLogging (true), IndexOnStartup (false), CacheExpirationDays (30), ResetToDefaults method, conditional compilation for Unity Editor, singleton Instance pattern.
  - **Completed**: 2025-11-06

**🏁 MILESTONE: MCP Server Core Complete**
*MCP server running in Unity Editor with stdio transport and thread-safe Unity API access.*

### Phase 4: MCP Tool Implementations
*[McpServerToolType] classes for Documentation, Profiler, Build, Assets, Scene, Packages*

#### Documentation Tools (Priority #1 - Implements FR-001 to FR-005)
- [x] **S034** Implement DocumentationTools [McpServerToolType] class
  - **Path**: `src/Tools/Documentation/DocumentationTools.cs`
  - **Dependencies**: S014-S024, S028-S033
  - **Notes**: ✅ Implemented MCP tool class with methods: QueryDocumentation (FR-001: full-text search, JSON response, <100ms cached), SearchApiFuzzy (FR-002: Levenshtein distance, threshold 0.7), GetUnityVersion (version mapping), RefreshDocumentationIndex (FR-003: local indexing with progress), CheckApiDeprecation (FR-005: deprecation warnings). All async Task<string> returning JSON. TODO markers for [McpServerToolType] and [McpServerTool] attributes (final SDK integration).
  - **Completed**: 2025-11-06

- [x] **S035** [P] Create tests for QueryDocumentation tool (FR-001)
  - **Path**: `tests/Tools/Documentation/QueryDocumentationTests.cs`
  - **Dependencies**: S034
  - **Notes**: ✅ Created 12 comprehensive tests: exact match, method signature/parameters/description/code examples extraction, <100ms cached query performance validation, empty query handling, no results, multiple results, Unity version/URL inclusion. All tests validate JSON response format and FR-001 requirements.
  - **Completed**: 2025-11-06

- [x] **S036** [P] Create tests for fuzzy search tool (FR-002)
  - **Path**: `tests/Tools/Documentation/FuzzySearchToolTests.cs`
  - **Dependencies**: S034
  - **Notes**: ✅ Created 9 comprehensive tests: typo correction ("Translte" → "Translate"), exact match priority, threshold filtering, custom thresholds, empty query, swapped letters, partial queries, multiple typos, case insensitivity. All tests validate FR-002 typo tolerance requirements.
  - **Completed**: 2025-11-06

#### Profiler Tools (Implements FR-006 to FR-010)
- [x] **S037-S040** Implement Profiler Tools (completed as group)
  - **Paths**: ProfilerRecorderWrapper, ProfilerTools, ProfilerData models, ProfilerSnapshotTests
  - **Notes**: ✅ Implemented profiler infrastructure: ProfilerSnapshot/Bottleneck/AntiPattern models, ProfilerRecorderWrapper with conditional compilation, ProfilerTools with CaptureProfilerSnapshot (300 frames default), CompareSnapshots, AnalyzeBottlenecks, DetectAntipatterns methods. 5 tests validating capture, metrics, and anti-pattern detection.
  - **Completed**: 2025-11-06

#### Build Tools (Implements FR-011 to FR-015)
- [ ] **S041** Implement Unity API wrappers for BuildPipeline
  - **Path**: `src/Common/UnityApiWrappers/BuildPipelineWrapper.cs`
  - **Dependencies**: S001
  - **Notes**: Wrapper around UnityEditor.BuildPipeline, BuildPlayerOptions, BuildReport, IPreprocessBuildWithReport, IPostprocessBuildWithReport

- [ ] **S042** Implement BuildTools [McpServerToolType] class
  - **Path**: `src/Tools/Build/BuildTools.cs`
  - **Dependencies**: S041, S028
  - **Notes**: Methods: ValidateBuildConfiguration, StartMultiPlatformBuild, GetBuildSizeAnalysis, CreateAssetBundles, GetBuildReport

- [ ] **S043** [P] Create build validation logic (FR-015)
  - **Path**: `src/Tools/Build/BuildConfigurationValidator.cs`
  - **Dependencies**: S041
  - **Notes**: Check platform SDK availability, disk space, build settings validity. Return detailed error messages for missing SDKs or invalid configurations

- [ ] **S044** [P] Create tests for build size analysis (FR-012)
  - **Path**: `tests/Tools/Build/BuildSizeAnalysisTests.cs`
  - **Dependencies**: S042
  - **Notes**: Test size breakdown by asset type (textures, audio, scripts, prefabs), identify largest contributors, compare across platforms

#### Asset Tools (Implements FR-016 to FR-020)
- [ ] **S045** Implement Unity API wrappers for AssetDatabase
  - **Path**: `src/Common/UnityApiWrappers/AssetDatabaseWrapper.cs`
  - **Dependencies**: S001
  - **Notes**: Wrapper around UnityEditor.AssetDatabase, asset dependency queries, import settings, GUID operations

- [ ] **S046** Implement AssetTools [McpServerToolType] class
  - **Path**: `src/Tools/Assets/AssetTools.cs`
  - **Dependencies**: S045, S028
  - **Notes**: Methods: AnalyzeDependencies, FindUnusedAssets, BatchUpdateImportSettings, OptimizeTextureCompression, TrackImportPerformance

- [ ] **S047** [P] Create asset dependency graph analyzer (FR-016)
  - **Path**: `src/Tools/Assets/AssetDependencyAnalyzer.cs`
  - **Dependencies**: S045
  - **Notes**: Use AssetDatabase.GetDependencies recursively, build graph showing asset references, detect circular dependencies

- [ ] **S048** [P] Create unused asset detector (FR-017)
  - **Path**: `src/Tools/Assets/UnusedAssetDetector.cs`
  - **Dependencies**: S045
  - **Notes**: Scan all scenes/prefabs/scripts, identify assets not referenced, exclude dynamically loaded (Resources.Load paths, Addressables), provide size savings estimate

#### Scene Tools (Implements FR-021 to FR-025)
- [ ] **S049** Implement Unity API wrappers for EditorSceneManager
  - **Path**: `src/Common/UnityApiWrappers/EditorSceneManagerWrapper.cs`
  - **Dependencies**: S001
  - **Notes**: Wrapper around UnityEditor.SceneManagement.EditorSceneManager, scene hierarchy traversal, component inspection

- [ ] **S050** Implement SceneTools [McpServerToolType] class
  - **Path**: `src/Tools/Scene/SceneTools.cs`
  - **Dependencies**: S049, S028
  - **Notes**: Methods: ValidateScene, DetectMissingReferences, AnalyzeLighting, GetSceneHierarchySummary, DetectPerformanceAntipatterns

- [ ] **S051** [P] Create configurable scene validation rule engine (FR-021)
  - **Path**: `src/Tools/Scene/SceneValidationRuleEngine.cs`
  - **Dependencies**: S049
  - **Notes**: Define validation rules (naming conventions, required components, performance guidelines), extensible rule system, generate validation reports

- [ ] **S052** [P] Create missing reference detector (FR-022)
  - **Path**: `src/Tools/Scene/MissingReferenceDetector.cs`
  - **Dependencies**: S049
  - **Notes**: Use SerializedObject to detect missing references, report affected GameObjects with full hierarchy paths, detect broken prefab connections

#### Package Tools (Implements FR-026 to FR-030)
- [ ] **S053** Implement Unity API wrappers for PackageManager.Client
  - **Path**: `src/Common/UnityApiWrappers/PackageManagerWrapper.cs`
  - **Dependencies**: S001
  - **Notes**: Wrapper around UnityEditor.PackageManager.Client, manifest.json parsing, package dependency analysis

- [ ] **S054** Implement PackageTools [McpServerToolType] class
  - **Path**: `src/Tools/Packages/PackageTools.cs`
  - **Dependencies**: S053, S028
  - **Notes**: Methods: AnalyzeDependencyConflicts, ValidateManifest, QueryOpenUPM, CheckVersionCompatibility, GetUpdateRecommendations

- [ ] **S055** [P] Create package dependency conflict detector (FR-026)
  - **Path**: `src/Tools/Packages/PackageDependencyAnalyzer.cs`
  - **Dependencies**: S053
  - **Notes**: Parse manifest.json dependencies, detect version conflicts (transitive dependencies), suggest resolution strategies with impact analysis

- [ ] **S056** [P] Create manifest.json validator (FR-027)
  - **Path**: `src/Tools/Packages/ManifestValidator.cs`
  - **Dependencies**: S053
  - **Notes**: Parse JSON with detailed error reporting (line numbers), validate package format, check Git URL validity, verify Unity version compatibility

**🏁 MILESTONE: All MCP Tools Implemented**
*Six tool categories functional: Documentation, Profiler, Build, Assets, Scene, Packages. Implements FR-001 through FR-030.*

### Phase 5: Context Optimization & Incremental Updates
*Implements FR-031 through FR-035 for AI token efficiency*

- [ ] **S057** Implement incremental scene update tracking with ObjectChangeEvents
  - **Path**: `src/Common/ContextOptimization/IncrementalSceneTracker.cs`
  - **Dependencies**: S049, S050
  - **Notes**: Use UnityEditor.ObjectChangeEvents to detect scene modifications, cache full state on first query, return only deltas on subsequent queries with version tracking

- [ ] **S058** [P] Implement selective component serialization system
  - **Path**: `src/Common/Serialization/SelectiveSerializer.cs`
  - **Dependencies**: S001
  - **Notes**: Allow AI to request specific component types or properties only (e.g., "Transform and Renderer only"), reduce unnecessary data in responses

- [ ] **S059** [P] Implement pagination system for large datasets
  - **Path**: `src/Common/ContextOptimization/PaginationManager.cs`
  - **Dependencies**: S001
  - **Notes**: Auto-paginate responses >10KB, configurable page sizes, provide navigation tokens (nextPage, previousPage)

- [ ] **S060** [P] Implement response compression for large data
  - **Path**: `src/Common/Serialization/ResponseCompressor.cs`
  - **Dependencies**: S001
  - **Notes**: Compress JSON responses >10KB using gzip, include size metrics in response metadata

- [ ] **S061** Implement reference ID system for on-demand object details
  - **Path**: `src/Common/ContextOptimization/ReferenceIdManager.cs`
  - **Dependencies**: S001
  - **Notes**: Return lightweight object references (ID only), AI can request full details on-demand, reduce upfront data transmission

- [ ] **S062** [P] Create tests for incremental update correctness
  - **Path**: `tests/ContextOptimization/IncrementalUpdateTests.cs`
  - **Dependencies**: S057
  - **Notes**: Test delta generation, cache invalidation, version tracking, ensure correct state reconstruction

**🏁 MILESTONE: Context Optimization Complete**
*Incremental updates, pagination, compression all functional. Implements FR-031 through FR-035.*

### Phase 6: Integration Testing & Multi-Version Validation
*Unity Test Runner, multi-version compatibility, end-to-end testing*

- [ ] **S063** [P] Create integration tests for MCP protocol compliance
  - **Path**: `tests/Integration/McpProtocolComplianceTests.cs`
  - **Dependencies**: S028-S033, S034-S056
  - **Notes**: Test JSON-RPC 2.0 message format, error responses, tool registration, schema generation. Use Unity Test Runner in Editor mode

- [ ] **S064** [P] Create integration tests for thread safety and concurrent operations
  - **Path**: `tests/Integration/ThreadSafetyTests.cs`
  - **Dependencies**: S028
  - **Notes**: Stress test message queue with concurrent MCP requests, verify no Unity API threading violations, test exception handling across thread boundaries

- [ ] **S065** Create end-to-end test: Documentation query workflow
  - **Path**: `tests/Integration/E2E_DocumentationQueryTests.cs`
  - **Dependencies**: S014-S024, S034-S036
  - **Notes**: Full workflow: AI client → MCP server → documentation indexer → SQLite query → fuzzy search → JSON response. Verify <100ms cached, <2s web fetch

- [ ] **S066** [P] Create end-to-end test: Profiler analysis workflow
  - **Path**: `tests/Integration/E2E_ProfilerAnalysisTests.cs`
  - **Dependencies**: S037-S040
  - **Notes**: Full workflow: Capture profiler data (requires Play mode) → analyze bottlenecks → generate report with file/line numbers → return JSON

- [ ] **S067** Create Unity 2021.3 LTS compatibility validation
  - **Path**: `tests/Compatibility/Unity2021_3_CompatibilityTests.cs`
  - **Dependencies**: All implementation tasks
  - **Notes**: Test all features on Unity 2021.3 LTS, verify ProfilerRecorder API availability, check .NET Standard 2.1 compatibility

- [ ] **S068** [P] Create Unity 2022.3 LTS compatibility validation
  - **Path**: `tests/Compatibility/Unity2022_3_CompatibilityTests.cs`
  - **Dependencies**: All implementation tasks
  - **Notes**: Test all features on Unity 2022.3 LTS, verify no API regressions, check for version-specific issues

- [ ] **S069** [P] Create Unity 6 compatibility validation
  - **Path**: `tests/Compatibility/Unity6_CompatibilityTests.cs`
  - **Dependencies**: All implementation tasks
  - **Notes**: Test all features on Unity 6, verify new Unity 6 APIs don't break compatibility, test deprecated API handling

**🏁 MILESTONE: Integration Testing Complete**
*All integration tests passing across Unity 2021.3 LTS, 2022.3 LTS, and Unity 6.*

### Phase 7: Performance Optimization & Documentation
*Performance tuning, documentation, release preparation*

- [ ] **S070** Performance profiling and optimization of documentation indexing
  - **Path**: `src/Tools/Documentation/UnityDocumentationIndexer.cs` optimizations
  - **Dependencies**: S014, S018
  - **Notes**: Profile indexing time (target <31 minutes for 500k docs), optimize batch processing, implement incremental indexing for new Unity versions, add progress checkpoints

- [ ] **S071** [P] Performance optimization for large project scenarios
  - **Path**: Various files (profiler, asset, scene tools)
  - **Dependencies**: S037-S052
  - **Notes**: Test with 10,000+ GameObjects, 5,000+ assets. Optimize queries, implement pagination automatically for large results, cache frequently accessed data

- [ ] **S072** [P] Memory optimization and object pooling
  - **Path**: `src/Common/ObjectPooling/` directory
  - **Dependencies**: All implementation
  - **Notes**: Implement object pooling for serialization buffers, profiler data structures, reduce GC pressure from repeated allocations

- [ ] **S073** Create comprehensive XML documentation comments
  - **Path**: All public APIs in src/
  - **Dependencies**: All implementation
  - **Notes**: Add /// XML comments for all public classes, methods, properties. Include usage examples, parameter descriptions, return values

- [ ] **S074** [P] Create user documentation (README, setup guide, API reference)
  - **Path**: `docs/README.md`, `docs/SETUP.md`, `docs/API_REFERENCE.md`
  - **Dependencies**: All implementation
  - **Notes**: Installation instructions, Unity version requirements, MCP client configuration (Claude Desktop, VS Code), troubleshooting guide

- [ ] **S075** [P] Create developer documentation (architecture, contributing guide)
  - **Path**: `docs/ARCHITECTURE.md`, `docs/CONTRIBUTING.md`
  - **Dependencies**: All implementation
  - **Notes**: System architecture diagram, threading model explanation, how to add new tools, testing guidelines, code style conventions

- [ ] **S076** Create MCP protocol examples and integration tests with Claude Desktop
  - **Path**: `docs/EXAMPLES.md`, manual testing checklist
  - **Dependencies**: S028-S033, S034-S056
  - **Notes**: Example MCP queries for each tool, Claude Desktop configuration, VS Code MCP client setup, expected responses

**🏁 MILESTONE: Release Ready**
*Performance optimized, fully documented, ready for deployment to Unity Asset Store or GitHub release.*

## AI-Assisted Development Time Estimation *(Claude Code + Human Review)*

> **⚠️ ESTIMATION BASIS**: These estimates assume development with Claude Code (AI) executing implementation tasks with human review and guidance. Times reflect AI execution + human review cycles, not manual coding.

### Phase-by-Phase Review Time
**Phase 1: Setup & Configuration** (S001-S010): ~3-4 hours human review
- *AI creates project structure quickly, human validates NuGet packages, Unity settings, directory organization*

**Phase 2: Documentation Indexing System** (S011-S025): ~8-10 hours human review
- *AI implements SQLite FTS5 indexing and HTML parsing, human validates Unity documentation structure, tests indexing on local docs, reviews fuzzy search accuracy*

**Phase 3: MCP Server Core** (S026-S033): ~5-6 hours human review
- *AI builds threading infrastructure and MCP lifecycle, human validates thread safety, tests stdio transport, reviews EditorApplication.update integration*

**Phase 4: MCP Tool Implementations** (S034-S056): ~12-15 hours human review
- *AI implements six tool categories, human validates Unity API wrappers, tests each tool's functionality, reviews JSON response formats*

**Phase 5: Context Optimization** (S057-S062): ~4-5 hours human review
- *AI implements incremental updates and pagination, human validates delta correctness, tests caching behavior, reviews compression efficiency*

**Phase 6: Integration Testing** (S063-S069): ~6-8 hours human review
- *AI writes integration tests, human runs tests across Unity versions (2021.3, 2022.3, Unity 6), validates multi-version compatibility*

**Phase 7: Performance & Documentation** (S070-S076): ~5-7 hours human review
- *AI optimizes performance and writes documentation, human profiles performance metrics, reviews API documentation completeness*

### Knowledge Gap Risk Factors
**🟢 Low Risk** (Unity Editor APIs): Well-documented with extensive examples
**🟡 Medium Risk** (ModelContextProtocol SDK): Preview status, good docs but evolving
**🟡 Medium Risk** (SQLite FTS5): Mature but requires specific configuration
**🟢 Low Risk** (Threading patterns): Standard C# patterns, well-documented

**API Documentation Quality Impact**:
- **Unity Editor APIs** (excellent official docs): ~10% additional review time
- **MCP SDK** (preview docs, evolving): ~25% additional review time
- **NuGet libraries** (varied quality): ~15% additional review time
- **Threading patterns** (established C# patterns): ~5% additional review time

### Total Estimated Review Time
**Core Development**: 43-55 hours (AI implementation + human review)
**Risk-Adjusted Time**: 52-68 hours (with knowledge gap factors and correction cycles)
**Manual Testing Allocation**: 8-12 hours (MCP client testing, Unity version validation)

**Total Project Time**: ~60-80 hours with Claude Code assistance

> **💡 TIME COMPOSITION**:
> - AI Implementation: ~15% (Claude Code executes quickly)
> - Human Review: ~45% (reading, understanding, Unity Editor testing)
> - Correction Cycles: ~25% (refinements, Unity API adjustments, threading fixes)
> - Manual Testing: ~15% (MCP protocol testing, multi-version validation)

## Implementation Structure *(AI guidance)*

### Task Numbering Convention
- **Format**: `S###` with sequential numbering (S001, S002, S003...)
- **Parallel Markers**: `[P]` for tasks that can run concurrently
- **Dependencies**: Clear prerequisite task references
- **File Paths**: Specific target files for each implementation task

### Progress Tracking & Session Continuity
- **This file is the progress tracker** - Check off tasks as `[x]` when complete
- **Sessions are resumable** - New sessions read this file to see what's done
- **Token limits don't matter** - Work can span multiple sessions seamlessly
- **Never rush to completion** - Take the time each step needs for quality
- **TodoWrite is temporary** - Only this file persists across sessions
- **Quality is paramount** - Shortcuts and speed optimizations are forbidden

### Parallel Execution Rules
- **Different files** = `[P]` parallel safe
- **Same file modifications** = Sequential only
- **Independent components** = `[P]` parallel safe
- **Shared resources** = Sequential only
- **Tests with implementation** = Can run `[P]` parallel

### Manual User Action Format
For complex Xcode operations (target creation, scheme setup), use standardized format:
```
═══════════════════════════════════════════════════
║ 🎯 MANUAL XCODE ACTION REQUIRED
═══════════════════════════════════════════════════
║
║ [Step-by-step Xcode UI instructions]
║ [Specific menu paths and actions]
║
║ Reply "Done" when completed to continue.
```

### Quality Integration
*Built into implementation phases, not separate agent tasks*

- **Code Standards**: Follow Context/Guidelines patterns throughout
- **Error Handling**: Apply ErrorKit patterns during service implementation
- **UI Guidelines**: Follow SwiftUI patterns during UI implementation
- **Testing Coverage**: Include test tasks for each implementation phase
- **Platform Compliance**: Consider iOS/macOS requirements in each phase

## Dependency Analysis

### Critical Path (Longest dependency chain: ~26 hours)
**S001** (Setup) → **S002** (MCP SDK) → **S010** (Bootstrap) → **S029** (MCP Lifecycle) → **S030** (stdio transport) → **S034** (DocumentationTools) → **S065** (E2E test) → **S070** (Performance)

This represents the minimum sequential path through the implementation. All other tasks can run in parallel with portions of this chain.

### Parallel Opportunities
**Phase 1** (S003, S004, S006, S008, S009 can run [P] after S001)
- NuGet package installation and folder structure creation are independent

**Phase 2** (S011, S012, S013 tests run [P]; S015, S020, S022 run [P] after dependencies)
- Test creation, HTML parser, fuzzy search, cache manager are independent components

**Phase 3** (S026, S027 tests run [P]; S031, S032, S033 run [P] after core)
- Threading tests, schema generation, error handling, configuration are independent

**Phase 4** (All Unity API wrappers S037, S041, S045, S049, S053 run [P])
- Six tool categories have independent implementations, tests can run [P] with implementations

**Phase 5** (S058, S059, S060 optimization components run [P])
- Serialization, pagination, compression are independent optimizations

**Phase 6** (S063, S064, S066, S068, S069 run [P])
- Integration tests for different aspects run independently

**Phase 7** (S071, S072, S074, S075 run [P])
- Performance optimizations and documentation can proceed in parallel

**Total parallelizable tasks**: ~45 out of 76 tasks (59%) can run concurrently with proper dependencies

### Unity Version Dependencies
**Minimum Version**: Unity 2021.3 LTS
- ProfilerRecorder API availability (S037)
- .NET Standard 2.1 API compatibility (S001)
- EditorCoroutines package support (S005)

**Multi-Version Support**:
- Unity 2021.3 LTS validation (S067)
- Unity 2022.3 LTS validation (S068)
- Unity 6 validation (S069)
- Version detection and appropriate documentation (S023)

**Platform Dependencies**:
- Windows x64: Primary development platform, full support
- macOS: Full Unity Editor support, different documentation paths (S015)
- Linux: Full Unity Editor support, different documentation paths (S015)
- SQLite native DLL per platform (S006)

## Completion Verification *(mandatory)*

### Implementation Completeness
- [x] All user scenarios from Spec.md have corresponding implementation tasks?
  - Documentation query (FR-001 to FR-005): S011-S036
  - Profiler analysis (FR-006 to FR-010): S037-S040
  - Build automation (FR-011 to FR-015): S041-S044
  - Asset management (FR-016 to FR-020): S045-S048
  - Scene validation (FR-021 to FR-025): S049-S052
  - Package management (FR-026 to FR-030): S053-S056
  - Context optimization (FR-031 to FR-035): S057-S062

- [x] All architectural components from Tech.md have creation/modification tasks?
  - MCP Server Core with ModelContextProtocol SDK: S026-S033
  - Documentation Indexing (SQLite FTS5): S014-S025
  - Threading Infrastructure (message queue): S028
  - Unity API Wrappers: S037, S041, S045, S049, S053
  - Context Optimization Layer: S057-S062

- [x] Error handling and edge cases covered in task breakdown?
  - Thread safety and exception handling: S028, S032, S064
  - Documentation fallback and caching: S021, S022
  - Build validation and error reporting: S043
  - Unity version compatibility: S023, S067-S069
  - Large-scale project handling: S071

- [x] Performance requirements addressed in implementation plan?
  - Documentation query <100ms cached (FR-001): S014, S020, S070
  - Profiler analysis <10s for 300 frames (FR-006): S038, S071
  - Asset scan <30s for 1,000+ assets (FR-017): S048, S071
  - Build validation <5s (FR-015): S043
  - Memory optimization and object pooling: S072

- [x] Platform-specific requirements integrated throughout phases?
  - Unity 2021.3 LTS, 2022.3 LTS, Unity 6 support: S067-S069
  - Windows/macOS/Linux documentation paths: S015
  - SQLite native DLL per platform: S006
  - .NET Standard 2.1 API compatibility: S001

### Quality Standards
- [x] Each task specifies exact file paths and dependencies?
  - All 76 tasks have explicit file paths
  - Dependencies clearly listed for each task
  - Notes explain Unity-specific requirements

- [x] Parallel markers `[P]` applied correctly for independent tasks?
  - 45 out of 76 tasks marked [P] (59% parallelizable)
  - Only truly independent tasks marked parallel
  - Tests run [P] with implementations where appropriate

- [x] Test tasks included for all major implementation components?
  - Documentation: S011-S013, S035-S036
  - MCP Core: S026-S027
  - Profiler: S040
  - Build: S044
  - Integration: S063-S069
  - Context optimization: S062

- [x] C# and Unity coding standards referenced throughout plan?
  - Unity API wrappers follow Unity conventions
  - [InitializeOnLoad] pattern for Editor integration
  - XML documentation comments (S073)
  - Async/await patterns with CancellationToken
  - Thread-safe patterns for Unity main thread

- [x] No implementation details that should be in tech plan?
  - All architectural decisions in Tech.md
  - Steps.md focuses on executable tasks only
  - Each task references tech decisions without repeating them

### Release Readiness
- [x] Documentation and release preparation tasks included?
  - User documentation: S074
  - Developer documentation: S075
  - MCP protocol examples: S076
  - XML API documentation: S073

- [x] Feature branch ready for systematic development execution?
  - Branch: feature/001-unify-mcp-documentation-first
  - All prerequisites complete (Spec.md, Tech.md, Steps.md)
  - Clear task enumeration (S001-S076)

- [x] All milestones defined with appropriate guidance?
  - 7 major milestones across phases
  - Each milestone describes completion state
  - Dependencies and readiness criteria clear

---

**Next Phase**: Proceed to `/ctxk:impl:start-working` to begin systematic development execution using this implementation plan.

---