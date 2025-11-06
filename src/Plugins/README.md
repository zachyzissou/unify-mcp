# NuGet Dependencies for Unity

This directory contains precompiled DLLs from NuGet packages required by Unify MCP.

## Required Packages

### Core MCP Protocol
- **ModelContextProtocol** (v0.4.0-preview.3)
  - Download from: https://www.nuget.org/packages/ModelContextProtocol/0.4.0-preview.3
  - Required: `ModelContextProtocol.dll`

### Documentation Indexing
- **System.Data.SQLite** (v1.0.118+)
  - Download from: https://www.nuget.org/packages/System.Data.SQLite.Core/
  - Required: `System.Data.SQLite.dll`
  - Platform-specific native libraries (see Platform-Specific section below)

- **Fastenshtein** (v1.0.11)
  - Download from: https://www.nuget.org/packages/Fastenshtein/
  - Required: `Fastenshtein.dll`

- **AngleSharp** (latest stable)
  - Download from: https://www.nuget.org/packages/AngleSharp/
  - Required: `AngleSharp.dll`

### JSON Schema Generation
- **NJsonSchema** (latest stable)
  - Download from: https://www.nuget.org/packages/NJsonSchema/
  - Required: `NJsonSchema.dll`

## Installation Instructions

### Option 1: Manual Download (Recommended for Unity)

1. Download each NuGet package (.nupkg file)
2. Rename .nupkg to .zip and extract
3. Copy the DLL files from `lib/netstandard2.0/` or `lib/netstandard2.1/` to this directory
4. For SQLite, also copy native binaries to platform-specific folders (see below)

### Option 2: Using NuGet CLI

```bash
# Install NuGet CLI if not already installed
# Then run from project root:

nuget install ModelContextProtocol -Version 0.4.0-preview.3 -OutputDirectory packages
nuget install System.Data.SQLite.Core -Version 1.0.118 -OutputDirectory packages
nuget install Fastenshtein -Version 1.0.11 -OutputDirectory packages
nuget install AngleSharp -OutputDirectory packages
nuget install NJsonSchema -OutputDirectory packages

# Copy DLLs from packages/*/lib/netstandard2.*/  to src/Plugins/
```

## Platform-Specific SQLite Native Libraries

SQLite requires platform-specific native libraries. Create the following folder structure:

```
src/Plugins/
├── x86_64/           # Windows 64-bit
│   └── SQLite.Interop.dll
├── macOS/            # macOS
│   └── libSQLite.Interop.dylib
└── Linux/            # Linux
    └── libSQLite.Interop.so
```

These files are included in the System.Data.SQLite NuGet package under:
- `runtimes/win-x64/native/` for Windows
- `runtimes/osx-x64/native/` for macOS
- `runtimes/linux-x64/native/` for Linux

## Unity Meta Files

After placing DLLs, Unity will auto-generate .meta files. Ensure:

1. **Platform targeting** is set correctly in Inspector:
   - Main DLLs: Editor only
   - Native libraries: Appropriate platform (Windows x64, macOS, Linux x64)

2. **API Compatibility Level**:
   - Set Unity Project Settings → Player → Other Settings → Api Compatibility Level = .NET Standard 2.1

## Verification

After installation, verify in Unity Editor:
1. Open Unity project with this package
2. Check Console for any missing assembly errors
3. Assets → Refresh (F5) if needed
4. Verify assembly references in UnifyMcp.Editor.asmdef are resolved

## Troubleshooting

**"Assembly not found" errors:**
- Ensure DLLs are in correct folder
- Check .meta file platform settings
- Verify API Compatibility Level setting

**SQLite native library errors:**
- Verify platform-specific folders exist with correct structure
- Check native library .meta files have correct platform targeting
- Ensure file names match exactly (case-sensitive on Linux/macOS)

**Version conflicts:**
- Stick to specified versions for compatibility
- ModelContextProtocol 0.4.0-preview.3 is pinned (breaking changes possible in newer versions)
