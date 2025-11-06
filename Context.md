# Project Context: unify-mcp

## Project Overview

- **Version**: ContextKit 0.2.0
- **Setup Date**: 2025-11-06
- **Components**: 1 component (main project)
- **Workspace**: None (standalone project)
- **Primary Tech Stack**: C# / Unity Editor / MCP Protocol
- **Development Guidelines**: None (C# guidelines not yet available in ContextKit)

## Component Architecture

**Project Structure**:

```
📁 unify-mcp
└── 🔧 Unity MCP Server (Tool) - Advanced Unity Editor tooling via MCP protocol - C#/Unity/MCP - ./
```

**Component Summary**:
- **1 C# component** - Unity MCP server (in initial development phase)
- **Dependencies**: To be established during development
- **Status**: Brand new project, implementing comprehensive Unity MCP tooling

---

## Component Details

### Unity MCP Server - Tool

**Location**: `./` (project root)

**Purpose**: Unity Model Context Protocol (MCP) server focused on filling critical gaps in the Unity MCP ecosystem. Provides AI-assisted Unity development tools addressing developer pain points in:
- Advanced Profiling & Performance Analysis
- Build Pipeline Automation
- Asset Database Operations
- Scene Analysis & Validation
- Enhanced Debugging
- Package Management
- Context Window Optimization

**Tech Stack**: C#, Unity Editor APIs, MCP Protocol (WebSocket), JSON serialization

**File Structure** (planned):
```
unify-mcp/
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
├── docs/                  # Documentation
├── CLAUDE.md             # AI development guidance
└── Context.md            # This file
```

**Dependencies** (to be established):
- Unity Editor APIs (UnityEditor namespace)
- MCP Protocol implementation libraries
- WebSocket communication layer
- JSON.NET or similar for serialization

**Development Commands**:
```bash
# Note: Build/test commands will be established once Unity project structure is created
# Initial setup requires Unity Editor integration and MCP server implementation

# Planned commands (to be validated):
# - Unity Editor build via Unity command line
# - Test execution via Unity Test Runner
# - MCP server startup and testing
```

**Code Style** (to be established):
- Unity C# coding conventions
- Async/await patterns for long operations
- Type-safe schemas for MCP protocol
- XML documentation comments for public APIs

**Framework Usage** (planned):
- UnityEditor namespace for editor integration
- UnityEditor.Profiling for profiler access
- UnityEditor.Build for build automation
- System.Threading.Tasks for async operations
- WebSocket libraries for MCP transport

---

## Development Environment

**Requirements**:
- Unity 2021.3 LTS or newer
- .NET development environment
- MCP protocol knowledge
- WebSocket communication understanding

**Build Tools** (to be established):
- Unity Editor (command line interface)
- C# compiler (via Unity)
- Unity Test Runner for testing
- Git for version control

**Formatters** (to be configured):
- Standard C# formatting conventions
- Unity-specific code style guidelines
- EditorConfig for consistency

## Development Guidelines

**Applied Guidelines**: None (C# guidelines not available in ContextKit template library)

**Project-Specific Approach**:
- Follow Unity C# coding conventions
- Reference CLAUDE.md for architecture patterns
- Implement MCP protocol following existing Unity MCP servers
- Prioritize context-aware minimalism (minimize token consumption)
- Use modular plugin architecture for extensibility

**Best Practices**:
- Type-safe schemas for all MCP data structures
- Async-first approach for non-blocking operations
- Comprehensive error handling and validation
- Security sandboxing for custom tools
- Performance optimization (object pooling, caching, streaming)

## ContextKit Workflow

**Systematic Feature Development**:
- `/ctxk:plan:1-spec` - Create business requirements specification (prompts interactively)
- `/ctxk:plan:2-research-tech` - Define technical research, architecture and implementation approach
- `/ctxk:plan:3-steps` - Break down into executable implementation tasks

**Development Execution**:
- `/ctxk:impl:start-working` - Continue development within feature branch (requires completed planning phases)
- `/ctxk:impl:commit-changes` - Auto-format code and commit with intelligent messages

**Quality Assurance**: Automated agents validate code quality during development
**Project Management**: All validated build/test commands documented above for immediate use

## Development Automation

**Quality Agents Available**:
- `build-project` - Execute builds with constitutional compliance validation
- `check-accessibility` - Accessibility validation (when UI components are implemented)
- `check-localization` - Localization validation (when applicable)
- `check-error-handling` - Error handling patterns validation
- `check-modern-code` - C# and Unity API modernization checks
- `check-code-debt` - Technical debt cleanup and code quality

**Note**: Quality agents are configured for ContextKit workflow but will be adapted to C#/Unity development patterns.

## Constitutional Principles

**Core Principles**:
- ✅ Code maintainability (readable, testable, documented C# code)
- ✅ Performance by design (optimize for Unity Editor responsiveness)
- ✅ Security-first approach (sandboxed execution, input validation)
- ✅ Modular architecture (plugin system, minimal coupling)
- ✅ Context-aware design (minimize token consumption for AI interactions)

**Unity-Specific Principles**:
- Type-safe MCP protocol schemas
- Non-blocking async operations for Editor integration
- Comprehensive error handling and logging
- Graceful degradation when Unity APIs unavailable
- Memory-efficient caching and object pooling

**Workspace Inheritance**: None - using project-specific principles adapted for Unity MCP development

## Configuration Hierarchy

**Inheritance**: No workspace → **This Project (Standalone)**

**This Project Configuration**:
- **Workspace**: None (standalone project)
- **Project**: Unity MCP server with C#/Unity Editor integration
- **ContextKit**: Workflow commands and quality agents configured

**Override Precedence**: N/A (standalone project, no workspace inheritance)

---
*Generated by ContextKit with comprehensive component analysis. Manual edits preserved during updates.*