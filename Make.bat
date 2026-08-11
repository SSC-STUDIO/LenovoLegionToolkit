@echo off
setlocal enabledelayedexpansion

REM Usage:
REM   Make.bat [version]       Release publish + installers (full clean first)
REM   Make.bat -clean          Clean workspace only (same as legacy Clean.bat)
REM   Make.bat -c              Alias for -clean
REM   Make.bat -d [version]    Debug publish to Build\Debug (no full clean)

set ERROR_COUNT=0
set BUILD_DIR=Build
set BUILD_ONLINE_DIR=Build-English
set RELEASE_ASSET_DIR=release-assets
set PAGES_ASSET_DIR=%RELEASE_ASSET_DIR%\pages

IF /I "%1"=="-clean" GOTO CLEAN_ONLY
IF /I "%1"=="-c" GOTO CLEAN_ONLY
IF "%1"=="-d" GOTO BUILD_DEBUG

IF "%1"=="" (
    CALL :RESOLVE_VERSION
) ELSE (
    SET VERSION=%1
)

IF "%VERSION%"=="" (
    echo Failed to resolve version.
    exit /b 1
)

CALL :RESOLVE_CROSS_PLATFORM_CLI_POLICY
IF %ERROR_COUNT% NEQ 0 GOTO END

CALL :CLEAN_WORKSPACE
IF %ERROR_COUNT% NEQ 0 GOTO END

echo --- Building main solution ---
dotnet build UniversalDeviceToolkit.sln --configuration Release --disable-build-servers -m:1
IF %ERRORLEVEL% NEQ 0 set ERROR_COUNT=1

dotnet publish UniversalDeviceToolkit.CLI\UniversalDeviceToolkit.CLI.csproj -c release -o "%BUILD_DIR%" /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION%
IF %ERRORLEVEL% NEQ 0 set ERROR_COUNT=1

dotnet publish UniversalDeviceToolkit.NetworkProxy\UniversalDeviceToolkit.NetworkProxy.csproj -c release -o "%BUILD_DIR%" /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION%
IF %ERRORLEVEL% NEQ 0 set ERROR_COUNT=1

echo --- Building Platform.Windows ---
dotnet build UniversalDeviceToolkit.Platform.Windows\UniversalDeviceToolkit.Platform.Windows.csproj --configuration Release --verbosity minimal
IF %ERRORLEVEL% NEQ 0 set ERROR_COUNT=1

REM Stage plugin runtime DLLs (SDK/Shared) from the in-tree Plugins solution before the payload assert.
powershell -NoProfile -ExecutionPolicy Bypass -File "Scripts\Build-PluginRuntimeAssets.ps1" -DestinationPath "%BUILD_DIR%" -Configuration Release
IF %ERRORLEVEL% NEQ 0 set ERROR_COUNT=1

IF %ERROR_COUNT% NEQ 0 GOTO END

echo --- Building in-tree plugin solution ---
dotnet build Plugins\UniversalDeviceToolkit.Plugins.sln --configuration Release --disable-build-servers -m:1
IF %ERRORLEVEL% NEQ 0 set ERROR_COUNT=1

IF %ERROR_COUNT% NEQ 0 GOTO END

CALL :PRUNE_RELEASE_OUTPUT "%BUILD_DIR%"
IF %ERROR_COUNT% NEQ 0 GOTO END

powershell -NoProfile -ExecutionPolicy Bypass -File "Scripts\Build-LanguageAssets.ps1" -BuildDir "%BUILD_DIR%" -OnlineBuildDir "%BUILD_ONLINE_DIR%" -ReleaseOutput "%RELEASE_ASSET_DIR%" -PagesOutput "%PAGES_ASSET_DIR%" -Version "%VERSION%"
IF %ERRORLEVEL% NEQ 0 (
    echo Release asset preparation failed.
    set ERROR_COUNT=1
)

REM Self-built WPF installer (Tools\Installer) — Inno Setup is retired.
powershell -NoProfile -ExecutionPolicy Bypass -File "Scripts\Build-InstallerAssets.ps1" -Version "%VERSION%" -ReleaseOutput "%RELEASE_ASSET_DIR%" -InstallerOutput "BuildInstaller"
IF %ERRORLEVEL% NEQ 0 (
    echo Installer build failed.
    set ERROR_COUNT=1
)

if not exist "BuildInstaller\UniversalDeviceToolkitSetup-Full.exe" (
    echo Expected full installer was not created.
    set ERROR_COUNT=1
)
if not exist "BuildInstaller\UniversalDeviceToolkitSetup-Online.exe" (
    echo Expected online installer was not created.
    set ERROR_COUNT=1
)

IF "%ENABLE_CROSS_PLATFORM_CLI%"=="1" (
    powershell -NoProfile -ExecutionPolicy Bypass -File "Scripts\Build-CrossPlatformCliAsset.ps1" -Version "%VERSION%" -ReleaseOutput "%RELEASE_ASSET_DIR%" -SkipHashUpdate
    IF !ERRORLEVEL! NEQ 0 (
        echo Cross-platform CLI asset build failed.
        set ERROR_COUNT=1
    )
) ELSE (
    echo Cross-platform CLI asset skipped for release builds before 5.x.
)

SET CROSS_PLATFORM_CLI_FINALIZE_ARG=
IF "%ENABLE_CROSS_PLATFORM_CLI%"=="1" SET CROSS_PLATFORM_CLI_FINALIZE_ARG=-IncludeCrossPlatformCli

powershell -NoProfile -ExecutionPolicy Bypass -File "Scripts\Build-LanguageAssets.ps1" -FinalizeOnly -ReleaseOutput "%RELEASE_ASSET_DIR%" -PagesOutput "%PAGES_ASSET_DIR%" -Version "%VERSION%" -FullInstallerPath "BuildInstaller\UniversalDeviceToolkitSetup-Full.exe" -OnlineInstallerPath "BuildInstaller\UniversalDeviceToolkitSetup-Online.exe" %CROSS_PLATFORM_CLI_FINALIZE_ARG%
IF %ERRORLEVEL% NEQ 0 (
    echo Release asset finalization failed.
    set ERROR_COUNT=1
)

GOTO END

:CLEAN_ONLY
CALL :CLEAN_WORKSPACE
GOTO END

:BUILD_DEBUG
echo Building DEBUG version...

IF "%2"=="" (
    CALL :RESOLVE_VERSION
) ELSE (
    SET VERSION=%2
)

IF "%VERSION%"=="" (
    echo Failed to resolve version.
    exit /b 1
)

echo.

echo.
echo Test and validation tools are separate from the main debug payload.
echo Build SpectrumTester explicitly when needed:
echo   dotnet publish UniversalDeviceToolkit.SpectrumTester\UniversalDeviceToolkit.SpectrumTester.csproj -c Debug -o Build\Tools\SpectrumTester
echo.
echo Building CLI (Debug)...
dotnet publish UniversalDeviceToolkit.CLI\UniversalDeviceToolkit.CLI.csproj -c Debug -o Build\Debug /p:FileVersion=%VERSION% /p:Version=%VERSION%

echo Building NetworkProxy (Debug)...
dotnet publish UniversalDeviceToolkit.NetworkProxy\UniversalDeviceToolkit.NetworkProxy.csproj -c Debug -o Build\Debug /p:FileVersion=%VERSION% /p:Version=%VERSION%
IF %ERRORLEVEL% NEQ 0 set ERROR_COUNT=1

echo.
echo Building Platform.Windows (Debug)...
dotnet build UniversalDeviceToolkit.Platform.Windows\UniversalDeviceToolkit.Platform.Windows.csproj --configuration Debug --verbosity minimal
IF %ERRORLEVEL% NEQ 0 set ERROR_COUNT=1

echo.
IF %ERROR_COUNT% EQU 0 (
    echo Debug build completed successfully!
) ELSE (
    echo Debug build completed with errors!
)
echo Output directory: Build\Debug
echo.
echo To debug: Open solution in VS 2022 and attach to process
echo.

GOTO END

:CLEAN_WORKSPACE
echo Cleaning workspace...

if exist ".vs" rmdir /s /q ".vs"
if exist "_ReSharper.Caches" rmdir /s /q "_ReSharper.Caches"
if exist "%BUILD_DIR%" rmdir /s /q "%BUILD_DIR%"
if exist "%BUILD_ONLINE_DIR%" rmdir /s /q "%BUILD_ONLINE_DIR%"
if exist "BuildInstaller" rmdir /s /q "BuildInstaller"
if exist "%RELEASE_ASSET_DIR%" rmdir /s /q "%RELEASE_ASSET_DIR%"
if exist "%PAGES_ASSET_DIR%" rmdir /s /q "%PAGES_ASSET_DIR%"

for %%p in (
    UniversalDeviceToolkit.CLI
    UniversalDeviceToolkit.CLI.Lib
    UniversalDeviceToolkit.Lib
    UniversalDeviceToolkit.Lib.Automation
    UniversalDeviceToolkit.Lib.Macro
    UniversalDeviceToolkit.Lib.Plugins
    UniversalDeviceToolkit.SpectrumTester
    UniversalDeviceToolkit.PerformanceTest
    UniversalDeviceToolkit.Tests
    UniversalDeviceToolkit.CrossPlatform
    UniversalDeviceToolkit.Platform.Windows
    UniversalDeviceToolkit.ViewModels
    UniversalDeviceToolkit.Lib.Abstractions
    UniversalDeviceToolkit.Lib.Shared
    Plugins\SDK\Abstractions
) do (
    if exist "%%p\bin" rmdir /s /q "%%p\bin"
    if exist "%%p\obj" rmdir /s /q "%%p\obj"
)

if exist "UniversalDeviceToolkit.sln" (
    dotnet clean UniversalDeviceToolkit.sln -v q
    IF %ERRORLEVEL% NEQ 0 set ERROR_COUNT=1
)

exit /b 0

:PRUNE_RELEASE_OUTPUT
set TARGET_DIR=%~1
if "%TARGET_DIR%"=="" exit /b 0

powershell -NoProfile -ExecutionPolicy Bypass -File "Scripts\Prune-ShippingFootprint.ps1" -PayloadPath "%TARGET_DIR%" -AllowedCultures "en;zh-hans;zh-hant"
IF %ERRORLEVEL% NEQ 0 (
    echo Shipping footprint prune failed.
    set ERROR_COUNT=1
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "Scripts\Assert-ShippingPayload.ps1" -PayloadPath "%TARGET_DIR%"
IF %ERRORLEVEL% NEQ 0 set ERROR_COUNT=1
exit /b %ERROR_COUNT%

:END
echo.
IF "%1"=="-clean" (
    IF %ERROR_COUNT% EQU 0 (
        echo Clean completed!
    ) ELSE (
        echo Clean completed with errors!
    )
) ELSE IF /I "%1"=="-c" (
    IF %ERROR_COUNT% EQU 0 (
        echo Clean completed!
    ) ELSE (
        echo Clean completed with errors!
    )
) ELSE IF "%1"=="-d" (
    IF %ERROR_COUNT% EQU 0 (
        echo Debug build completed! Exiting in 5 seconds...
    ) ELSE (
        echo Debug build completed with errors! Exiting in 5 seconds...
    )
) ELSE (
    IF %ERROR_COUNT% EQU 0 (
        echo Build completed! Exiting in 5 seconds...
    ) ELSE (
        echo Build completed with errors! Exiting in 5 seconds...
    )
)
ping -n 6 127.0.0.1 >nul 2>&1
endlocal & exit /b %ERROR_COUNT%

:RESOLVE_VERSION
REM MajorVersion, MinorVersion, PatchVersion are plain numeric text nodes.
REM Reading them directly avoids the '$(�?' MSBuild-expression interpolation
REM trap that causes NuGet to see '..' as a version string on some runners.
for /f "usebackq delims=" %%v in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "$props=[xml](Get-Content -Raw 'Directory.Build.props'); $group=$props.Project.PropertyGroup | Where-Object { $_.MajorVersion -ne $null } | Select-Object -First 1; $maj=[string]$group.MajorVersion; $min=[string]$group.MinorVersion; $pat=[string]$group.PatchVersion; if ([string]::IsNullOrWhiteSpace($maj) -or [string]::IsNullOrWhiteSpace($min) -or [string]::IsNullOrWhiteSpace($pat)) { exit 1 }; '{0}.{1}.{2}' -f $maj,$min,$pat"`) do SET VERSION=%%v
exit /b %ERRORLEVEL%

:RESOLVE_CROSS_PLATFORM_CLI_POLICY
SET ENABLE_CROSS_PLATFORM_CLI=0
SET VERSION_MAJOR=
for /f "tokens=1 delims=.-" %%v in ("%VERSION%") do SET VERSION_MAJOR=%%v
IF NOT "!VERSION_MAJOR!"=="" (
    IF !VERSION_MAJOR! GEQ 5 SET ENABLE_CROSS_PLATFORM_CLI=1
)
exit /b 0
