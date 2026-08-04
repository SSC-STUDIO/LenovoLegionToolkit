#!/bin/bash
# Run performance tests with automatic runtimeconfig.json creation

set -e

PROJECT_DIR="D:\EliuaK_Csy\Working-Paper\My-Program\UniversalDeviceToolkit-Plugins"
TEST_PROJECT="$PROJECT_DIR\Tests\PerformanceTests"

echo "=== Building PerformanceTests ==="
cd "$PROJECT_DIR"
dotnet build "$TEST_PROJECT\PerformanceTests.csproj" --configuration Release

echo ""
echo "=== Ensuring runtimeconfig.json ==="
RUNTIMECONFIG="$TEST_PROJECT\bin\Release\PerformanceTests.runtimeconfig.json"
if [ ! -f "$RUNTIMECONFIG" ]; then
    echo '{"runtimeOptions":{"tfm":"net10.0-windows","framework":{"name":"Microsoft.WindowsDesktop.App","version":"10.0.0"}}}' > "$RUNTIMECONFIG"
    echo "Created: $RUNTIMECONFIG"
fi

echo ""
echo "=== Running PerformanceTests ==="
cd "$TEST_PROJECT"
dotnet run --configuration Release --no-build

echo ""
echo "=== Done ==="
