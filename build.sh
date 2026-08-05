#!/usr/bin/env bash
set -euo pipefail

# UniversalDeviceToolkit cross-platform build script
# Usage: ./build.sh [configuration] [runtime]
#   configuration: Debug|Release (default: Release)
#   runtime: linux-x64|osx-arm64|osx-x64|win-x64 (default: auto-detect)

CONFIGURATION="${1:-Release}"
RUNTIME="${2:-}"

# Auto-detect runtime
if [ -z "$RUNTIME" ]; then
    case "$(uname -s)" in
        Linux*)  RUNTIME="linux-x64" ;;
        Darwin*)
            if [ "$(uname -m)" = "arm64" ]; then
                RUNTIME="osx-arm64"
            else
                RUNTIME="osx-x64"
            fi
            ;;
        *)       RUNTIME="linux-x64" ;;
    esac
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "=== UniversalDeviceToolkit Build ==="
echo "Configuration: $CONFIGURATION"
echo "Runtime: $RUNTIME"
echo ""

# Build cross-platform libraries
echo "--- Building cross-platform libraries ---"
dotnet build "$SCRIPT_DIR/UniversalDeviceToolkit.Lib.Abstractions/UniversalDeviceToolkit.Lib.Abstractions.csproj" \
    --configuration "$CONFIGURATION" --verbosity minimal

dotnet build "$SCRIPT_DIR/UniversalDeviceToolkit.Lib.Shared/UniversalDeviceToolkit.Lib.Shared.csproj" \
    --configuration "$CONFIGURATION" --verbosity minimal

dotnet build "$SCRIPT_DIR/UniversalDeviceToolkit.ViewModels/UniversalDeviceToolkit.ViewModels.csproj" \
    --configuration "$CONFIGURATION" --verbosity minimal

dotnet build "$SCRIPT_DIR/Plugins/SDK/Abstractions/UniversalDeviceToolkit.Plugins.Abstractions.csproj" \
    --configuration "$CONFIGURATION" --verbosity minimal

# Build Avalonia application
echo ""
echo "--- Building Avalonia application ---"
dotnet build "$SCRIPT_DIR/UniversalDeviceToolkit.Avalonia/UniversalDeviceToolkit.Avalonia.csproj" \
    --configuration "$CONFIGURATION" --verbosity minimal

# Build CrossPlatform CLI
echo ""
echo "--- Building CrossPlatform CLI ---"
dotnet build "$SCRIPT_DIR/UniversalDeviceToolkit.CrossPlatform/UniversalDeviceToolkit.CrossPlatform.csproj" \
    --configuration "$CONFIGURATION" --verbosity minimal

# Run tests
echo ""
echo "--- Running tests ---"
dotnet test "$SCRIPT_DIR/UniversalDeviceToolkit.Tests/UniversalDeviceToolkit.Tests.csproj" \
    --configuration "$CONFIGURATION" --verbosity minimal --no-restore || true

echo ""
echo "=== Build complete ==="
