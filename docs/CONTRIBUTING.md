# Contributing to Unity MCP Server

Thank you for your interest in contributing to Unity MCP Server! This guide will help you get started with development, understand our conventions, and submit high-quality contributions.

## Code of Conduct

We are committed to providing a welcoming and inspiring community for all. Be respectful, constructive, and collaborative. We're all here to build great tools for Unity developers.

## Table of Contents

1. [Getting Started](#getting-started)
2. [Development Workflow](#development-workflow)
3. [Code Style Guide](#code-style-guide)
4. [Testing Conventions](#testing-conventions)
5. [Pull Request Process](#pull-request-process)
6. [Adding New Tools](#adding-new-tools)
7. [Security Guidelines](#security-guidelines)
8. [Performance Best Practices](#performance-best-practices)
9. [Documentation Standards](#documentation-standards)
10. [Getting Help](#getting-help)

---

## Getting Started

### Prerequisites

- **Unity 2021.3 LTS or newer** (2022.3 LTS or Unity 6 recommended)
- **.NET Standard 2.1** compatible development environment
- **Git** for version control
- **NUnit** for testing (included with Unity Test Framework)
- **IDE**: Visual Studio 2022, Rider, or VS Code with C# extension

### Development Setup

#### 1. Fork and Clone

```bash
git clone https://github.com/YOUR_USERNAME/unify-mcp.git
cd unify-mcp
```

#### 2. Install Dependencies

Follow instructions in `src/Plugins/README.md` to install required NuGet packages manually. Required dependencies:

- **ModelContextProtocol.dll** (v0.4.0-preview.3)
- **System.Data.SQLite.dll** (v1.0.118.0)
- **NJsonSchema.dll** (v11.0.0)
- **Fastenshtein.dll** (v1.0.0.8)
- **AngleSharp.dll** (v1.1.2)

**Quick install script** (Linux/macOS):

```bash
#!/bin/bash
# install-deps.sh

PLUGINS_DIR="src/Plugins"
mkdir -p "$PLUGINS_DIR"

echo "Downloading NuGet packages..."

# ModelContextProtocol
wget -O "$PLUGINS_DIR/ModelContextProtocol.0.4.0-preview.3.nupkg" \
  "https://www.nuget.org/api/v2/package/ModelContextProtocol/0.4.0-preview.3"

# System.Data.SQLite
wget -O "$PLUGINS_DIR/System.Data.SQLite.Core.1.0.118.0.nupkg" \
  "https://www.nuget.org/api/v2/package/System.Data.SQLite.Core/1.0.118.0"

# NJsonSchema
wget -O "$PLUGINS_DIR/NJsonSchema.11.0.0.nupkg" \
  "https://www.nuget.org/api/v2/package/NJsonSchema/11.0.0"

# Fastenshtein
wget -O "$PLUGINS_DIR/Fastenshtein.1.0.0.8.nupkg" \
  "https://www.nuget.org/api/v2/package/Fastenshtein/1.0.0.8"

# AngleSharp
wget -O "$PLUGINS_DIR/AngleSharp.1.1.2.nupkg" \
  "https://www.nuget.org/api/v2/package/AngleSharp/1.1.2"

echo "Extracting DLLs..."

for nupkg in "$PLUGINS_DIR"/*.nupkg; do
  unzip -q "$nupkg" -d "$PLUGINS_DIR/temp"
  cp "$PLUGINS_DIR/temp/lib/netstandard2.1"/*.dll "$PLUGINS_DIR/" 2>/dev/null || true
  rm -rf "$PLUGINS_DIR/temp"
done

echo "✅ Dependencies installed to $PLUGINS_DIR"
```

**Usage**:
```bash
chmod +x install-deps.sh
./install-deps.sh
```

#### 3. Import into Unity

Copy the source into a Unity project:

```bash
cp -r src/. ~/UnityProjects/TestProject/Packages/com.anthropic.unify-mcp/
```

Or create a symbolic link (recommended for active development):

```bash
ln -s $(pwd)/src ~/UnityProjects/TestProject/Packages/com.anthropic.unify-mcp
```

#### 4. Run Tests

**Via command line** (.NET tests):
```bash
dotnet test
```

**Via Unity Test Runner** (Unity API tests):
1. Open Unity Editor
2. Window > General > Test Runner
3. Select PlayMode or EditMode
4. Click "Run All"

#### 5. Verify Setup

Run a quick smoke test:

```bash
dotnet test --filter "FullyQualifiedName~ContextWindowManager" --verbosity normal
```

Expected output: All tests pass

---

## Development Workflow

### Branching Strategy

We use a feature branch workflow:

- **`main`**: Production-ready code, always stable
- **`feature/XXX-feature-name`**: New features
- **`fix/XXX-bug-description`**: Bug fixes
- **`docs/XXX-documentation`**: Documentation updates
- **`refactor/XXX-improvement`**: Code refactoring
- **`perf/XXX-optimization`**: Performance improvements

**Branch naming convention**: `type/issue-number-description`

**Examples**:
```bash
feature/042-gpu-profiler-integration
fix/038-semaphore-memory-leak
docs/045-api-reference-update
perf/051-cache-optimization
```

### Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/) specification:

```
<type>(<scope>): <subject>

<body>

<footer>
```

#### Types

- **`feat`**: New feature
- **`fix`**: Bug fix
- **`docs`**: Documentation only
- **`test`**: Adding or updating tests
- **`refactor`**: Code restructuring (no functional changes)
- **`perf`**: Performance improvement
- **`security`**: Security fix or improvement
- **`chore`**: Build/tooling changes, dependencies

#### Examples

```bash
# Feature addition
git commit -m "feat(profiler): add GPU metrics capture

- Integrate Unity.Profiling.ProfilerRecorder for GPU
- Add GPU time to ProfilerSnapshot model
- Update tests to verify GPU metrics
- Document GPU profiling in API reference"

# Bug fix
git commit -m "fix(cache): prevent semaphore leak in RequestDeduplicator

- Add timer-based cleanup for old semaphores
- Track last access time per semaphore
- Dispose semaphores after 5 minutes of inactivity
- Add regression test"

# Documentation
git commit -m "docs(contributing): add section on Unity API testing

- Explain wrapper pattern for testability
- Provide examples of interface-based abstractions
- Document test organization conventions"
```

### Development Process (TDD)

We follow **Test-Driven Development** for all new features and bug fixes:

#### 1. Create Feature Branch

```bash
git checkout -b feature/042-your-feature-name
```

#### 2. Write Tests First

```csharp
[TestFixture]
public class NewFeatureTests
{
    [Test]
    public void NewFeature_ValidInput_ReturnsExpectedResult()
    {
        // Arrange
        var input = "test";
        var expectedOutput = "expected";

        // Act
        var result = newFeature.Process(input);

        // Assert
        Assert.AreEqual(expectedOutput, result);
    }
}
```

#### 3. Run Tests to See Them Fail

```bash
dotnet test --filter "FullyQualifiedName~NewFeature"
```

Expected: **FAIL** (method not implemented)

#### 4. Implement Minimal Code

```csharp
public class NewFeature
{
    public string Process(string input)
    {
        // Minimal implementation to pass test
        return "expected";
    }
}
```

#### 5. Run Tests to See Them Pass

```bash
dotnet test --filter "FullyQualifiedName~NewFeature"
```

Expected: **PASS**

#### 6. Refactor if Needed

Improve code quality while keeping tests green.

#### 7. Commit Frequently

```bash
git add .
git commit -m "feat: add new feature with tests"
```

---

## Code Style Guide

### C# Conventions

#### Naming Conventions

- **Classes, Methods, Properties**: `PascalCase`
  ```csharp
  public class ProfilerSnapshot
  {
      public string MethodName { get; set; }
      public void CaptureSnapshot() { }
  }
  ```

- **Local variables, parameters**: `camelCase`
  ```csharp
  public void Process(string inputData)
  {
      var processedResult = Transform(inputData);
  }
  ```

- **Constants**: `UPPER_CASE` or `PascalCase`
  ```csharp
  private const int MAX_CACHE_SIZE = 1000;
  private const string DefaultCachePath = "cache.db";
  ```

- **Interfaces**: Prefix with `I`
  ```csharp
  public interface IProfilerRecorder
  {
      ProfilerData Capture();
  }
  ```

- **Private fields**: Prefix with underscore (optional but preferred)
  ```csharp
  private readonly IRepository _repository;
  ```

#### Formatting

- **Indentation**: 4 spaces (no tabs)
- **Line length**: Maximum 120 characters
- **Braces**: Opening brace on same line
  ```csharp
  public void Method() {
      if (condition) {
          DoSomething();
      }
  }
  ```

- **Using directives**: Order alphabetically, System namespaces first
  ```csharp
  using System;
  using System.Collections.Generic;
  using System.Threading.Tasks;
  using UnifyMcp.Core;
  using UnityEngine;
  ```

#### Type Usage

- Use `var` when type is obvious:
  ```csharp
  var list = new List<string>();  // ✅ Type is clear
  var result = GetResult();       // ❌ Type is unclear
  string result = GetResult();    // ✅ Explicit type
  ```

- Use explicit types for clarity in complex scenarios

#### Null Handling

- Use null-conditional operators:
  ```csharp
  var count = items?.Count ?? 0;
  ```

- Use null-coalescing assignment:
  ```csharp
  _cache ??= new ConcurrentDictionary<string, object>();
  ```

- Validate parameters:
  ```csharp
  public void Process(string input)
  {
      if (string.IsNullOrWhiteSpace(input))
          throw new ArgumentException("Input cannot be null or empty", nameof(input));
  }
  ```

### Async/Await Patterns

- **Suffix async methods** with `Async`:
  ```csharp
  public async Task<string> QueryDocumentationAsync(string query)
  {
      return await Task.Run(() => ExecuteQuery(query));
  }
  ```

- **Use `async`/`await`** for I/O-bound operations
- **Use `Task.Run`** for CPU-bound work:
  ```csharp
  public async Task<ProfilerSnapshot> CaptureAsync()
  {
      return await Task.Run(() => {
          // CPU-intensive profiler data processing
          return ProcessProfilerData();
      });
  }
  ```

- **Always await** async calls (don't use `.Result` or `.Wait()`)
  ```csharp
  // ❌ BAD - Can cause deadlocks
  var result = QueryAsync(query).Result;

  // ✅ GOOD
  var result = await QueryAsync(query);
  ```

### Complete Example

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnifyMcp.Common.Security;

namespace UnifyMcp.Tools.Documentation
{
    /// <summary>
    /// Provides documentation search and retrieval functionality.
    /// </summary>
    public class DocumentationTools : IDisposable
    {
        private const int MaxResultCount = 100;
        private readonly IDocumentationRepository _repository;
        private readonly PathValidator _pathValidator;

        /// <summary>
        /// Initializes a new instance of DocumentationTools.
        /// </summary>
        /// <param name="repository">Documentation repository</param>
        /// <param name="pathValidator">Path validation service</param>
        public DocumentationTools(
            IDocumentationRepository repository,
            PathValidator pathValidator = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _pathValidator = pathValidator ?? CreateDefaultValidator();
        }

        /// <summary>
        /// Queries documentation using full-text search.
        /// </summary>
        /// <param name="query">Search query</param>
        /// <returns>JSON array of matching documentation entries</returns>
        public async Task<string> QueryDocumentationAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be null or empty", nameof(query));

            return await Task.Run(() => {
                var results = _repository.Search(query, MaxResultCount);
                return SerializeResults(results);
            });
        }

        private PathValidator CreateDefaultValidator()
        {
            var projectPath = Environment.GetEnvironmentVariable("UNITY_PROJECT_PATH")
                ?? Directory.GetCurrentDirectory();
            return new PathValidator(projectPath);
        }

        private string SerializeResults(List<DocumentationEntry> results)
        {
            return System.Text.Json.JsonSerializer.Serialize(results);
        }

        public void Dispose()
        {
            _repository?.Dispose();
        }
    }
}
```

---

## Testing Conventions

### Test Organization

- **Test naming**: `MethodName_Scenario_ExpectedBehavior`
  ```csharp
  [Test]
  public void QueryDocumentation_ValidQuery_ReturnsResults()

  [Test]
  public void QueryDocumentation_NullQuery_ThrowsArgumentException()
  ```

- **Test structure**: Arrange-Act-Assert (AAA)
  ```csharp
  [Test]
  public void Process_ValidInput_ReturnsExpectedOutput()
  {
      // Arrange
      var input = "test";
      var expected = "result";
      var processor = new Processor();

      // Act
      var actual = processor.Process(input);

      // Assert
      Assert.AreEqual(expected, actual);
  }
  ```

### Test Categories

Use `[Category]` attribute to organize tests:

```csharp
[Test]
[Category("Unit")]
public void UnitTest_FastIsolated_Passes() { }

[Test]
[Category("Integration")]
public void IntegrationTest_WithDatabase_Passes() { }

[Test]
[Category("Performance")]
public void PerformanceTest_UnderLoadTarget_Passes() { }
```

**Run specific categories**:
```bash
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
dotnet test --filter "Category=Performance"
```

### Test Coverage Requirements

- **New features**: Minimum 80% code coverage
- **Bug fixes**: Must include regression test
- **Refactoring**: Maintain or improve existing coverage
- **Critical paths**: 100% coverage (security, data integrity)

### Testing Unity-Specific Code

Use wrapper interfaces for testability:

```csharp
// ❌ BAD - Direct Unity API call (not testable)
public class PlayerController
{
    public GameObject FindPlayer()
    {
        return GameObject.Find("Player");
    }
}

// ✅ GOOD - Wrapped Unity API (testable)
public interface IGameObjectFinder
{
    GameObject Find(string name);
}

public class UnityGameObjectFinder : IGameObjectFinder
{
    public GameObject Find(string name)
    {
        return GameObject.Find(name);
    }
}

public class PlayerController
{
    private readonly IGameObjectFinder _finder;

    public PlayerController(IGameObjectFinder finder)
    {
        _finder = finder;
    }

    public GameObject FindPlayer()
    {
        return _finder.Find("Player");
    }
}

// Test with mock
[Test]
public void FindPlayer_PlayerExists_ReturnsPlayer()
{
    // Arrange
    var mockFinder = new Mock<IGameObjectFinder>();
    var expectedPlayer = new GameObject("Player");
    mockFinder.Setup(f => f.Find("Player")).Returns(expectedPlayer);
    var controller = new PlayerController(mockFinder.Object);

    // Act
    var result = controller.FindPlayer();

    // Assert
    Assert.AreEqual(expectedPlayer, result);
}
```

### Complete Test Example

```csharp
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using UnifyMcp.Tools.Documentation;

namespace UnifyMcp.Tests.Tools.Documentation
{
    [TestFixture]
    [Category("Unit")]
    public class DocumentationToolsTests
    {
        private DocumentationTools _tools;
        private Mock<IDocumentationRepository> _mockRepository;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new Mock<IDocumentationRepository>();
            _tools = new DocumentationTools(_mockRepository.Object);
        }

        [Test]
        public async Task QueryDocumentationAsync_ValidQuery_ReturnsJsonResults()
        {
            // Arrange
            var query = "GameObject.SetActive";
            var expectedEntries = new List<DocumentationEntry>
            {
                new DocumentationEntry
                {
                    ClassName = "GameObject",
                    MethodName = "SetActive",
                    Description = "Activates/Deactivates the GameObject"
                }
            };
            _mockRepository
                .Setup(r => r.Search(query, It.IsAny<int>()))
                .Returns(expectedEntries);

            // Act
            var result = await _tools.QueryDocumentationAsync(query);

            // Assert
            Assert.That(result, Does.Contain("GameObject"));
            Assert.That(result, Does.Contain("SetActive"));
            _mockRepository.Verify(r => r.Search(query, 100), Times.Once);
        }

        [Test]
        public void QueryDocumentationAsync_NullQuery_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(
                async () => await _tools.QueryDocumentationAsync(null)
            );
            Assert.That(ex.ParamName, Is.EqualTo("query"));
        }

        [Test]
        [Category("Integration")]
        public async Task QueryDocumentationAsync_WithRealDatabase_ReturnsResults()
        {
            // Integration test with actual SQLite database
            using var realRepo = new SqliteDocumentationRepository("test.db");
            using var tools = new DocumentationTools(realRepo);

            var result = await tools.QueryDocumentationAsync("Transform");

            Assert.That(result, Is.Not.Empty);
        }

        [TearDown]
        public void TearDown()
        {
            _tools?.Dispose();
        }
    }
}
```

---

## Pull Request Process

### Before Submitting

Complete this checklist before opening a PR:

- [ ] **Tests pass**: `dotnet test` runs without failures
- [ ] **Code style**: Follows conventions in this guide
- [ ] **Documentation**: Updated docs for new features/changes
- [ ] **Commit messages**: Follow conventional commits format
- [ ] **No merge conflicts**: Rebased on latest `main`
- [ ] **Self-review**: Reviewed your own code for issues
- [ ] **Comments**: Added for complex or non-obvious logic
- [ ] **No warnings**: No new compiler warnings introduced

### Pull Request Template

When creating a PR, use this template:

```markdown
## Description

Brief description of the changes in this PR.

## Related Issues

Fixes #42
Closes #38

## Type of Change

- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update
- [ ] Performance improvement
- [ ] Security fix

## Implementation Details

Explain the approach taken and any important design decisions.

## Testing

- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Performance tests added (if applicable)
- [ ] Manual testing completed

### Test Evidence

```bash
# Show test output
dotnet test --filter "FullyQualifiedName~YourFeature"
```

## Performance Impact

- [ ] No performance impact
- [ ] Performance improved (provide benchmarks)
- [ ] Performance regression (justify)

## Breaking Changes

List any breaking changes and migration steps.

## Checklist

- [ ] Code follows style guidelines
- [ ] Self-review completed
- [ ] Comments added for complex logic
- [ ] Documentation updated
- [ ] No new warnings generated
- [ ] Tests provide adequate coverage
- [ ] Commit messages follow conventions

## Screenshots (if applicable)

Add screenshots for UI changes or visual features.

## Additional Context

Any additional information reviewers should know.
```

### Review Process

1. **Automated checks**: CI/CD pipeline must pass
   - Unit tests
   - Integration tests
   - Code coverage check
   - Build verification

2. **Code review**: At least one approval from maintainer required
   - Code quality and style
   - Test adequacy
   - Documentation completeness
   - Security considerations

3. **Testing verification**: Reviewer validates:
   - Tests actually verify the fix/feature
   - Edge cases are covered
   - No unnecessary tests added

4. **Documentation review**: Reviewer checks:
   - API documentation updated
   - Examples are correct
   - README/guides updated if needed

### Addressing Feedback

- **Be responsive**: Reply to comments within 48 hours
- **Ask questions**: If feedback is unclear, ask for clarification
- **Make changes**: Push new commits (don't force-push during review)
- **Explain decisions**: If you disagree, explain your reasoning respectfully
- **Request re-review**: Once changes are complete

### Merging

After approval:

1. **Squash and merge** (preferred for features)
2. **Rebase and merge** (for clean commit history)
3. **Create merge commit** (only for large features)

Maintainer will merge after final approval.

---

## Adding New Tools

Follow this workflow to add new MCP tools to the server.

### Step 1: Plan the Tool

**Ask these questions**:

1. What specific developer pain point does this address?
2. How can the data be represented efficiently (token-aware)?
3. What Unity APIs are required?
4. Are there security implications (file access, code execution)?
5. What are realistic performance targets?

**Example**: GPU Profiler Integration

- **Pain point**: No programmatic access to GPU metrics
- **Representation**: JSON with frame times, draw calls, vertices
- **Unity APIs**: `UnityEngine.Profiling.ProfilerRecorder`
- **Security**: Read-only access to profiler data
- **Performance**: < 100ms capture time

### Step 2: Create Tool Class

Create tool file in appropriate category:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnifyMcp.Tools.Profiler
{
    /// <summary>
    /// Provides GPU profiling capabilities.
    /// </summary>
    // [McpServerToolType] // Uncomment when MCP SDK integrated
    public class GpuProfilerTools : IDisposable
    {
        private readonly IProfilerRecorder _recorder;

        public GpuProfilerTools(IProfilerRecorder recorder = null)
        {
            _recorder = recorder ?? new UnityProfilerRecorder();
        }

        /// <summary>
        /// Captures GPU performance metrics.
        /// </summary>
        /// <param name="frameCount">Number of frames to capture</param>
        /// <returns>JSON object with GPU metrics</returns>
        // [McpServerTool] // Uncomment when MCP SDK integrated
        public async Task<string> CaptureGpuMetrics(int frameCount = 300)
        {
            if (frameCount <= 0)
                throw new ArgumentException("Frame count must be positive", nameof(frameCount));

            return await Task.Run(() => {
                var metrics = _recorder.CaptureGpuData(frameCount);
                return SerializeMetrics(metrics);
            });
        }

        private string SerializeMetrics(GpuMetrics metrics)
        {
            return System.Text.Json.JsonSerializer.Serialize(metrics);
        }

        public void Dispose()
        {
            _recorder?.Dispose();
        }
    }
}
```

### Step 3: Write Tests

Create test file mirroring source structure:

```csharp
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using UnifyMcp.Tools.Profiler;

namespace UnifyMcp.Tests.Tools.Profiler
{
    [TestFixture]
    [Category("Unit")]
    public class GpuProfilerToolsTests
    {
        private GpuProfilerTools _tools;
        private Mock<IProfilerRecorder> _mockRecorder;

        [SetUp]
        public void SetUp()
        {
            _mockRecorder = new Mock<IProfilerRecorder>();
            _tools = new GpuProfilerTools(_mockRecorder.Object);
        }

        [Test]
        public async Task CaptureGpuMetrics_ValidFrameCount_ReturnsJsonMetrics()
        {
            // Arrange
            var expectedMetrics = new GpuMetrics
            {
                FrameCount = 300,
                AverageFrameTime = 16.67,
                DrawCalls = 120
            };
            _mockRecorder
                .Setup(r => r.CaptureGpuData(300))
                .Returns(expectedMetrics);

            // Act
            var result = await _tools.CaptureGpuMetrics(300);

            // Assert
            Assert.That(result, Does.Contain("\"frameCount\":300"));
            Assert.That(result, Does.Contain("\"drawCalls\":120"));
        }

        [Test]
        public void CaptureGpuMetrics_NegativeFrameCount_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(
                async () => await _tools.CaptureGpuMetrics(-1)
            );
        }

        [TearDown]
        public void TearDown()
        {
            _tools?.Dispose();
        }
    }
}
```

### Step 4: Add Documentation

Update `docs/MCP_EXAMPLES.md`:

```markdown
### CaptureGpuMetrics

**Purpose**: Programmatic GPU performance metrics capture.

**Request**:
```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "CaptureGpuMetrics",
    "arguments": {
      "frameCount": 300
    }
  },
  "id": 1
}
```

**Response**:
```json
{
  "jsonrpc": "2.0",
  "result": {
    "frameCount": 300,
    "averageFrameTime": 16.67,
    "drawCalls": 120,
    "vertices": 45000,
    "triangles": 15000
  },
  "id": 1
}
```

**Usage Example**:
```javascript
const gpuMetrics = await mcp.call("CaptureGpuMetrics", { frameCount: 300 });
console.log(`Average GPU time: ${gpuMetrics.averageFrameTime}ms`);
```
```

### Step 5: Register in Context Manager (Optional)

If the tool benefits from context optimization:

```csharp
// In ContextWindowManager or tool registry
public void RegisterGpuProfilerTools()
{
    var gpuTools = new GpuProfilerTools();
    _toolRegistry.Register("CaptureGpuMetrics", async (params) => {
        var frameCount = params.GetValueOrDefault("frameCount", 300);
        return await gpuTools.CaptureGpuMetrics(frameCount);
    });
}
```

### Step 6: Integration Test

Add end-to-end test:

```csharp
[Test]
[Category("Integration")]
public async Task GpuProfiler_EndToEnd_WorksWithContextManager()
{
    // Arrange
    var contextManager = new ContextWindowManager();
    var gpuTools = new GpuProfilerTools();

    // Act
    var result = await contextManager.ProcessToolRequestAsync(
        "CaptureGpuMetrics",
        new Dictionary<string, object> { { "frameCount", 300 } },
        async () => await gpuTools.CaptureGpuMetrics(300)
    );

    // Assert
    Assert.That(result.Response, Is.Not.Empty);
    Assert.That(result.ToolName, Is.EqualTo("CaptureGpuMetrics"));
}
```

---

## Security Guidelines

Security is critical when providing AI access to Unity projects.

### Path Validation

**Always validate file paths** to prevent path traversal attacks:

```csharp
using UnifyMcp.Common.Security;

public class AssetTools
{
    private readonly PathValidator _pathValidator;

    public AssetTools(PathValidator validator)
    {
        _pathValidator = validator;
    }

    public async Task<string> LoadAsset(string assetPath)
    {
        // Validate before accessing file system
        _pathValidator.ValidateOrThrow(assetPath);

        return await File.ReadAllTextAsync(assetPath);
    }
}
```

### Input Sanitization

**Validate all inputs** before using in Unity API calls:

```csharp
public async Task<string> QueryDocumentation(string query)
{
    // Sanitize query to prevent injection
    if (string.IsNullOrWhiteSpace(query))
        throw new ArgumentException("Query cannot be empty");

    if (query.Length > 500)
        throw new ArgumentException("Query too long (max 500 chars)");

    var sanitized = SanitizeQuery(query);
    return await ExecuteQuery(sanitized);
}

private string SanitizeQuery(string query)
{
    // Remove SQL injection attempts
    return query.Replace("--", "").Replace(";", "").Replace("'", "");
}
```

### Permission Levels

Implement granular permissions:

```csharp
public enum ToolPermission
{
    ReadOnly,      // Can only read data
    ReadWrite,     // Can modify assets/settings
    EditorOnly,    // Only works in Editor
    RuntimeCapable // Can run during Play mode
}

[Permission(ToolPermission.ReadOnly)]
public class DocumentationTools { }

[Permission(ToolPermission.ReadWrite)]
public class AssetTools { }
```

### Audit Logging

Log all critical operations:

```csharp
public async Task<string> DeleteAsset(string assetPath)
{
    _logger.LogWarning($"Asset deletion requested: {assetPath}");
    _pathValidator.ValidateOrThrow(assetPath);

    try
    {
        await File.DeleteAsync(assetPath);
        _logger.LogInformation($"Asset deleted: {assetPath}");
        return "{\"success\": true}";
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Failed to delete asset: {assetPath}");
        throw;
    }
}
```

### Secrets Management

**Never commit secrets**:

- Don't hardcode API keys
- Use environment variables
- Add sensitive files to `.gitignore`

```csharp
// ❌ BAD
private const string ApiKey = "sk-1234567890abcdef";

// ✅ GOOD
private readonly string _apiKey = Environment.GetEnvironmentVariable("MCP_API_KEY")
    ?? throw new InvalidOperationException("MCP_API_KEY not set");
```

---

## Performance Best Practices

### Async Operations

**Use async for I/O and long operations**:

```csharp
// ✅ GOOD - Non-blocking
public async Task<string> QueryDocumentationAsync(string query)
{
    return await Task.Run(() => _repository.Search(query));
}

// ❌ BAD - Blocks thread
public string QueryDocumentation(string query)
{
    return _repository.Search(query); // Blocks Unity Editor!
}
```

### Caching

**Cache expensive operations**:

```csharp
public class AssetTools
{
    private readonly ConcurrentDictionary<string, string> _assetCache;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

    public async Task<string> GetAssetInfo(string assetPath)
    {
        if (_assetCache.TryGetValue(assetPath, out var cached))
            return cached;

        var info = await LoadAssetInfo(assetPath);
        _assetCache[assetPath] = info;
        return info;
    }
}
```

### Object Pooling

**Reuse objects to reduce GC pressure**:

```csharp
public class StringBuilderPool
{
    private static readonly ObjectPool<StringBuilder> Pool =
        new ObjectPool<StringBuilder>(
            () => new StringBuilder(256),
            sb => sb.Clear()
        );

    public string BuildString(Action<StringBuilder> buildAction)
    {
        var sb = Pool.Get();
        try
        {
            buildAction(sb);
            return sb.ToString();
        }
        finally
        {
            Pool.Return(sb);
        }
    }
}
```

### Performance Targets

All tools should meet these targets:

| Operation Type | Target Latency | Target Throughput |
|---------------|----------------|-------------------|
| Cache Hit | < 10ms | > 1000 req/s |
| Documentation Query | < 50ms | > 100 req/s |
| Asset Operation | < 100ms | > 50 req/s |
| Profiler Snapshot | < 500ms | > 10 req/s |

**Verify with performance tests**:

```csharp
[Test]
[Category("Performance")]
public async Task QueryDocumentation_UnderLoad_MeetsLatencyTarget()
{
    // Arrange
    var tools = new DocumentationTools();
    var stopwatch = Stopwatch.StartNew();

    // Act - Run 100 queries
    for (int i = 0; i < 100; i++)
    {
        await tools.QueryDocumentationAsync("GameObject");
    }
    stopwatch.Stop();

    // Assert - Average latency < 50ms
    var avgLatency = stopwatch.ElapsedMilliseconds / 100.0;
    Assert.Less(avgLatency, 50, $"Average latency: {avgLatency}ms");
}
```

---

## Documentation Standards

### Code Documentation

**Use XML documentation comments** for all public APIs:

```csharp
/// <summary>
/// Queries Unity documentation using full-text search with BM25 ranking.
/// </summary>
/// <param name="query">Search query (API name, method, or keyword)</param>
/// <returns>JSON array of matching documentation entries</returns>
/// <exception cref="ArgumentException">Thrown when query is null or empty</exception>
/// <example>
/// <code>
/// var result = await docTools.QueryDocumentationAsync("GameObject.SetActive");
/// // Returns: [{"className":"GameObject","methodName":"SetActive",...}]
/// </code>
/// </example>
public async Task<string> QueryDocumentationAsync(string query)
{
    // Implementation
}
```

### README Updates

When adding features, update `docs/README.md`:

- Add feature to "Key Features" section
- Update installation steps if dependencies added
- Add usage examples
- Update compatibility matrix if needed

### API Reference

Update `docs/API_REFERENCE.md` with new tools:

```markdown
### CaptureGpuMetrics

**Namespace**: `UnifyMcp.Tools.Profiler`

**Purpose**: Captures GPU performance metrics programmatically.

**Signature**:
```csharp
public async Task<string> CaptureGpuMetrics(int frameCount = 300)
```

**Parameters**:
- `frameCount` (optional): Number of frames to capture (default: 300)

**Returns**: JSON object with GPU performance data

**Example**:
```csharp
var metrics = await profilerTools.CaptureGpuMetrics(300);
// Returns: {"frameCount":300,"averageFrameTime":16.67,...}
```
```

### MCP Examples

Add to `docs/MCP_EXAMPLES.md` showing MCP protocol usage:

- Request format
- Response format
- Error handling
- Usage examples

---

## Project Structure

Understanding the codebase organization:

```
unify-mcp/
├── src/
│   ├── Core/                    # MCP protocol & context management
│   │   ├── Context/            # ContextWindowManager, optimization
│   │   └── Protocol/           # MCP server implementation
│   ├── Tools/                   # MCP tool implementations
│   │   ├── Documentation/      # Documentation search & indexing
│   │   ├── Profiler/           # Performance profiling tools
│   │   ├── Build/              # Build automation
│   │   ├── Assets/             # Asset management
│   │   ├── Scene/              # Scene analysis & validation
│   │   └── Packages/           # Package management
│   ├── Common/                  # Shared utilities
│   │   ├── Security/           # Path validation, permissions
│   │   ├── Threading/          # Thread safety, dispatchers
│   │   └── Serialization/      # JSON/binary serialization
│   └── Unity/                   # Unity Editor integration
│       ├── Editor/             # Editor scripts
│       └── Runtime/            # Runtime components
├── tests/
│   ├── Integration/            # End-to-end tests
│   ├── Performance/            # Benchmarks, load tests
│   └── {mirrors src/}          # Unit tests mirror src structure
├── docs/                        # Documentation
│   ├── README.md              # Getting started guide
│   ├── ARCHITECTURE.md        # System design
│   ├── API_REFERENCE.md       # API documentation
│   ├── MCP_EXAMPLES.md        # Protocol usage examples
│   └── CONTRIBUTING.md        # This file
└── Context/                     # Feature planning (ContextKit)
    ├── Features/               # Feature specs & implementation plans
    ├── Backlog/                # Ideas & bug tracking
    └── Scripts/                # Automation scripts
```

---

## Getting Help

### Documentation

- **[README](./README.md)**: Getting started guide
- **[Architecture](./ARCHITECTURE.md)**: System design and patterns
- **[API Reference](./API_REFERENCE.md)**: Complete API documentation
- **[MCP Examples](./MCP_EXAMPLES.md)**: Protocol usage examples

### Community

- **Issues**: [Search existing issues](https://github.com/zachyzissou/unify-mcp/issues)
- **Discussions**: [Join discussions](https://github.com/zachyzissou/unify-mcp/discussions)
- **Questions**: Open a discussion (not an issue) for general questions

### Issue Etiquette

- **Search first**: Check if issue already exists
- **Use templates**: Fill out issue templates completely
- **Be specific**: Provide repro steps, Unity version, error messages
- **Be patient**: Maintainers are volunteers

---

## Recognition

Contributors are recognized in:

- **CHANGELOG.md**: Listed for each release contribution
- **README.md**: Contributors section with GitHub avatars
- **Release Notes**: Highlighted in release announcements

---

## License

By contributing, you agree that your contributions will be licensed under the same license as the project (check LICENSE file in repository root).

---

Thank you for contributing to Unity MCP Server! Your efforts help make Unity development more accessible and efficient for everyone.
