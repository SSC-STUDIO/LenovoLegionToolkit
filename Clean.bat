@echo off

rem Remove Visual Studio and ReSharper cache
rmdir /s /q .vs
rmdir /s /q _ReSharper.Caches

rem Remove build output directories
rmdir /s /q Build
rmdir /s /q BuildInstaller

rem Remove all project bin and obj directories
for %%p in (
    LenovoLegionToolkit.CLI
    LenovoLegionToolkit.CLI.Lib
    LenovoLegionToolkit.Lib
    LenovoLegionToolkit.Lib.Automation
    LenovoLegionToolkit.Lib.Macro
    LenovoLegionToolkit.WPF
    LenovoLegionToolkit.SpectrumTester
    LenovoLegionToolkit.PerformanceTest
    LenovoLegionToolkit.Tests
) do (
    if exist "%%p\bin" rmdir /s /q "%%p\bin"
    if exist "%%p\obj" rmdir /s /q "%%p\obj"
)

echo Clean completed!
echo Exiting in 5 seconds...
ping -n 6 127.0.0.1 >nul 2>&1
