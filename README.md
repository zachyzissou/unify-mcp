# Unity MCP Server

[![CI](https://github.com/zachyzissou/unify-mcp/actions/workflows/ci.yml/badge.svg)](https://github.com/zachyzissou/unify-mcp/actions/workflows/ci.yml)
[![Release](https://github.com/zachyzissou/unify-mcp/releases/latest/badge.svg)](https://github.com/zachyzissou/unify-mcp/releases/latest)

Advanced Unity Editor MCP server providing AI-accessible tools for documentation queries, profiler analysis, build automation, asset management, scene validation, and package management.

## Features

- **Documentation System**: SQLite FTS5 indexing with fuzzy search
- **Context Optimization**: 50-70% token reduction for AI interactions
- **Performance Analysis**: Profiler integration and bottleneck detection
- **Asset Management**: Batch operations, dependency analysis, optimization
- **Scene Validation**: Deep inspection and validation rules
- **Build Automation**: Multi-platform build orchestration

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

- [API Reference](docs/API_REFERENCE.md)
- [Architecture](docs/ARCHITECTURE.md)
- [MCP Examples](docs/MCP_EXAMPLES.md)
- [Contributing](docs/CONTRIBUTING.md)

## Quick Start

Once installed, the MCP server will automatically initialize when Unity Editor starts. You can access it through:

- **Menu**: Tools > UnifyMCP
- **MCP Protocol**: Connect via stdio transport
- **Claude Desktop**: Add to your Claude configuration

## License

MIT License - See LICENSE file for details

## Support

- [GitHub Issues](https://github.com/zachyzissou/unify-mcp/issues)
- [Documentation](docs/README.md)
