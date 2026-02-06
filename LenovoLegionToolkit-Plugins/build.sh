#!/bin/bash

# ============================================================
# Lenovo Legion Toolkit Plugins Build Script (Linux/macOS)
# ============================================================
# Usage:
#   ./build.sh              - Build all plugins (Release)
#   ./build.sh all          - Build all plugins (Release)
#   ./build.sh debug        - Build all plugins (Debug)
#   ./build.sh <plugin>     - Build specific plugin (Release)
#   ./build.sh <plugin> d   - Build specific plugin (Debug)
#   ./build.sh zip          - Create ZIP packages for all plugins
#   ./build.sh clean        - Clean all build outputs
#   ./build.sh help         - Show this help message
# ============================================================

set -e

# Plugin list
PLUGINS=("NetworkAcceleration" "ShellIntegration" "ViveTool" "CustomMouse")

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

print_status() {
    echo -e "${GREEN}[BUILD]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

build_plugin() {
    local PLUGIN_NAME=$1
    local CONFIG=$2
    local PLUGIN_DIR="plugins/$PLUGIN_NAME"
    local PLUGIN_PROJECT="$PLUGIN_DIR/LenovoLegionToolkit.Plugins.$PLUGIN_NAME.csproj"
    
    if [ ! -f "$PLUGIN_PROJECT" ]; then
        print_error "Plugin project not found: $PLUGIN_PROJECT"
        return 1
    fi
    
    print_status "Building $PLUGIN_NAME plugin ($CONFIG)..."
    dotnet build "$PLUGIN_PROJECT" -c "$CONFIG" --nologo -v q
}

create_zip() {
    local PLUGIN_NAME=$1
    local PLUGIN_DIR="plugins/$PLUGIN_NAME"
    local OUTPUT_DIR="build/plugins/$PLUGIN_NAME"
    local ZIP_NAME="$PLUGIN_NAME.zip"
    
    print_status "Creating $ZIP_NAME..."
    
    # Clean previous output
    rm -rf "$OUTPUT_DIR"
    rm -f "$ZIP_NAME"
    
    # Create output directory
    mkdir -p "$OUTPUT_DIR"
    
    # Copy plugin files
    cp -r "$PLUGIN_DIR/bin/Release/net8.0-windows/win-x64/"* "$OUTPUT_DIR/" 2>/dev/null || true
    
    # Remove SDK DLL to avoid conflicts
    rm -f "$OUTPUT_DIR/LenovoLegionToolkit.Plugins.SDK.dll"
    
    # Create ZIP package
    if command -v zip &> /dev/null; then
        cd "$OUTPUT_DIR"
        zip -r "../$ZIP_NAME" . -x "*.pdb" -x "*.xml"
        cd - > /dev/null
    else
        print_warning "zip command not found, using PowerShell..."
        pwsh -Command "Compress-Archive -Path '$OUTPUT_DIR/*' -DestinationPath '$ZIP_NAME' -Force"
    fi
    
    print_status "Created $ZIP_NAME"
}

show_help() {
    echo "============================================================"
    echo "Lenovo Legion Toolkit Plugins Build Script"
    echo "============================================================"
    echo ""
    echo "Usage: $0 [command] [options]"
    echo ""
    echo "Commands:"
    echo "  (none)      Build all plugins (Release)"
    echo "  all         Build all plugins (Release)"
    echo "  debug       Build all plugins (Debug)"
    echo "  clean       Clean all build outputs"
    echo "  zip         Create ZIP packages for all plugins"
    echo "  help        Show this help message"
    echo ""
    echo "Options for specific plugin:"
    echo "  <plugin>    Build specific plugin (Release)"
    echo "  <plugin> d  Build specific plugin (Debug)"
    echo ""
    echo "Examples:"
    echo "  $0                  # Build all plugins"
    echo "  $0 debug            # Build all plugins (Debug)"
    echo "  $0 ViveTool         # Build ViveTool plugin"
    echo "  $0 ViveTool d       # Build ViveTool plugin (Debug)"
    echo "  $0 zip              # Create ZIP packages"
    echo "  $0 clean            # Clean all outputs"
    echo ""
    echo "Available plugins:"
    for PLUGIN in "${PLUGINS[@]}"; do
        echo "  - $PLUGIN"
    done
    echo ""
    echo "============================================================"
}

# Main script
case "$1" in
    ""|"all")
        print_status "Building all plugins (Release)..."
        for PLUGIN in "${PLUGINS[@]}"; do
            build_plugin "$PLUGIN" "Release"
        done
        print_status "All plugins built successfully!"
        ;;
    "debug")
        print_status "Building all plugins (Debug)..."
        for PLUGIN in "${PLUGINS[@]}"; do
            build_plugin "$PLUGIN" "Debug"
        done
        print_status "All plugins built successfully!"
        ;;
    "clean")
        print_status "Cleaning all build outputs..."
        for PLUGIN in "${PLUGINS[@]}"; do
            rm -rf "plugins/$PLUGIN/bin"
            rm -rf "plugins/$PLUGIN/obj"
            rm -rf "build/plugins/$PLUGIN"
            rm -f "$PLUGIN.zip"
        done
        rm -rf "build"
        print_status "All build outputs cleaned!"
        ;;
    "zip")
        print_status "Creating ZIP packages for all plugins..."
        mkdir -p "build/plugins"
        for PLUGIN in "${PLUGINS[@]}"; do
            create_zip "$PLUGIN"
        done
        print_status "All ZIP packages created successfully!"
        ;;
    "help"|"-h"|"--help")
        show_help
        ;;
    *)
        PLUGIN_NAME="$1"
        CONFIG="Release"
        if [ "$2" == "d" ]; then
            CONFIG="Debug"
        fi
        
        # Check if it's a valid plugin
        if [[ " ${PLUGINS[@]} " =~ " ${PLUGIN_NAME} " ]]; then
            build_plugin "$PLUGIN_NAME" "$CONFIG"
            print_status "$PLUGIN_NAME plugin built successfully!"
        else
            print_error "Unknown plugin: $PLUGIN_NAME"
            echo "Available plugins: ${PLUGINS[*]}"
            exit 1
        fi
        ;;
esac
