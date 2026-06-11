> **License Notice**
> This repository is source-visible only. Copying is only permitted as technically necessary for review/evaluation. Modification, redistribution, deployment, or commercial use is not permitted without prior written permission from Zach Gonser. See `LICENSE` for details.

# Unity MCP Server

[![CI](https://github.com/zachyzissou/unify-mcp/actions/workflows/ci.yml/badge.svg)](https://github.com/zachyzissou/unify-mcp/actions/workflows/ci.yml)
[![Release](https://github.com/zachyzissou/unify-mcp/releases/latest/badge.svg)](https://github.com/zachyzissou/unify-mcp/releases/latest)

**🚧 Work in Progress** — See [ROADMAP.md](ROADMAP.md) for honest project status

Unity Editor MCP server for AI-accessible tools. **Documentation system is functional**; core MCP protocol and most tools are in development.

## Current Truth

- Canonical GitHub Project: [#40 unify-mcp](https://github.com/users/zachyzissou/projects/40)
- Last refreshed: 2026-05-31T06:40Z.
- Current active issues: `#3`, `#4`, `#5`, `#6`, `#7`, `#8`, `#9`.
- Current active PRs: `#10`, `#11`.
- Active gate: keep status/docs honest while MCP protocol, tool implementations, auth, and CI integration are still incomplete.

## Implementation Status

### ✅ What Works Today
- **Documentation System**: SQLite FTS5 indexing, fuzzy search, Unity API docs
- **Context Optimization**: Token reduction (50-70%), request deduplication, caching
- **Security**: Path validation, error handling framework

### 🚧 In Development (Stubbed)
- **MCP Protocol**: Server lifecycle has TODOs; stdio transport not wired ([Issue #4](https://github.com/zachyzissou/unify-mcp/issues/4))
- **Build Tools**: Returns stub JSON ([Issue #5](https://github.com/zachyzissou/unify-mcp/issues/5))
- **Asset Tools**: Returns stub JSON ([Issue #5](https://github.com/zachyzissou/unify-mcp/issues/5))
- **Scene Tools**: Returns stub JSON ([Issue #5](https://github.com/zachyzissou/unify-mcp/issues/5))
- **Profiler Tools**: Implementation status unclear

### 📋 Planned
- Tool authorization/permissions ([Issue #6](https://github.com/zachyzissou/unify-mcp/issues/6))
- Unity Test Runner in CI ([Issue #9](https://github.com/zachyzissou/unify-mcp/issues/9))

**See [ROADMAP.md](ROADMAP.md) for detailed development plan (4-5 month timeline)**

## Installation

### Option 1: Unity Package Manager (Git URL)

1. Open Unity Editor
2. Go to **Window > Package Manager**
3. Click the **+** button in the top-left
4. Select **Add package from git URL...**
5. Enter: `https://github.com/zachyzissou/unify-mcp.git`
6. Click **Add**

### Option 2: Unity Package Manager (Manual)

1. Open your Unity project
2. Open `Packages/manifest.json` in a text editor
3. Add this line to the `dependencies` section:
```json
{
  "dependencies": {
    "com.zachyzissou.unify-mcp": "https://github.com/zachyzissou/unify-mcp.git#v0.2.1"
  }
}
```
4. Save and return to Unity (it will install automatically)

### Option 3: Download Release

1. Download the latest release from [Releases](https://github.com/zachyzissou/unify-mcp/releases)
2. Extract `unify-mcp-v0.1.0.tar.gz`
3. Copy contents to `Assets/Plugins/UnifyMcp/` in your Unity project

## Post-Installation

After installation, you need to install NuGet dependencies:

```bash
cd Packages/com.zachyzissou.unify-mcp  # Or Assets/Plugins/UnifyMcp
./scripts/install-dependencies.sh
```

This installs:
- ModelContextProtocol.dll (v0.4.0-preview.3)
- System.Data.SQLite.dll (v1.0.118.0)
- NJsonSchema.dll (v11.0.0)
- Fastenshtein.dll (v1.0.0.8)
- AngleSharp.dll (v1.1.2)

## Requirements

- Unity 2021.3 LTS or newer
- .NET Standard 2.1

## Documentation

- [API Reference](Documentation~/API_REFERENCE.md)
- [Architecture](Documentation~/ARCHITECTURE.md)
- [MCP Examples](Documentation~/MCP_EXAMPLES.md)
- [Contributing](Documentation~/CONTRIBUTING.md)

## Quick Start

Once installed, the MCP server will automatically initialize when Unity Editor starts. You can access it through:

- **Menu**: Tools > UnifyMCP
- **MCP Protocol**: Connect via stdio transport
- **Claude Desktop**: Add to your Claude configuration

## License

Source-Visible License - See LICENSE file for details

## Support

- [GitHub Issues](https://github.com/zachyzissou/unify-mcp/issues)
- [Documentation](docs/README.md)
