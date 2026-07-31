#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PUBLISH_DIR="$SCRIPT_DIR/../../publish"

echo "=== Publishing Avalonia Application ==="

# Windows
echo "--- Windows x64 ---"
dotnet publish "$SCRIPT_DIR/UniversalDeviceToolkit.Avalonia.csproj" \
    -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true \
    -o "$PUBLISH_DIR/win-x64"

# Linux
echo "--- Linux x64 ---"
dotnet publish "$SCRIPT_DIR/UniversalDeviceToolkit.Avalonia.csproj" \
    -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true \
    -o "$PUBLISH_DIR/linux-x64"

# macOS ARM64
echo "--- macOS ARM64 ---"
dotnet publish "$SCRIPT_DIR/UniversalDeviceToolkit.Avalonia.csproj" \
    -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true \
    -o "$PUBLISH_DIR/osx-arm64"

echo "=== All platforms published to $PUBLISH_DIR ==="
