#!/bin/bash
# install-dependencies.sh
# Automated NuGet dependency installer for Unity MCP Server

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
