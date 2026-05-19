@echo off
setlocal enabledelayedexpansion

set ERROR_COUNT=0
set BUILD_DIR=Build
set BUILD_ENGLISH_DIR=Build-English
set RELEASE_ASSET_DIR=release-assets

REM Check build mode
IF "%1"=="-d" (
    GOTO BUILD_DEBUG
)

IF "%1"=="" (
    CALL :RESOLVE_VERSION
) ELSE (
    SET VERSION=%1
)

IF "%VERSION%"=="" (
    echo Failed to resolve version.
    exit /b 1
)

SET PATH=%PATH%;"C:\Program Files (x86)\Inno Setup 6"

if exist "%BUILD_DIR%" rmdir /s /q "%BUILD_DIR%"
if exist "%BUILD_ENGLISH_DIR%" rmdir /s /q "%BUILD_ENGLISH_DIR%"
if exist "%RELEASE_ASSET_DIR%" rmdir /s /q "%RELEASE_ASSET_DIR%"

dotnet publish LenovoLegionToolkit.WPF\LenovoLegionToolkit.WPF.csproj -c release -o "%BUILD_DIR%" /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION%
IF %ERRORLEVEL% NEQ 0 set ERROR_COUNT=1

dotnet publish LenovoLegionToolkit.CLI\LenovoLegionToolkit.CLI.csproj -c release -o "%BUILD_DIR%" /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION%
IF %ERRORLEVEL% NEQ 0 set ERROR_COUNT=1

IF %ERROR_COUNT% NEQ 0 GOTO END

CALL :PRUNE_RELEASE_OUTPUT "%BUILD_DIR%"

powershell -NoProfile -ExecutionPolicy Bypass -File "Scripts\Build-LanguageAssets.ps1" -BuildDir "%BUILD_DIR%" -EnglishBuildDir "%BUILD_ENGLISH_DIR%" -ReleaseOutput "%RELEASE_ASSET_DIR%" -Version "%VERSION%"
IF %ERRORLEVEL% NEQ 0 (
    echo Language asset preparation failed.
    set ERROR_COUNT=1
)

iscc MakeInstaller.iss /DMyAppVersion=%VERSION% /DMyAppSourceDir="%BUILD_DIR%" /DMyAppOutputBaseFilename=LenovoLegionToolkitSetup
IF %ERRORLEVEL% NEQ 0 (
    echo Inno Setup failed for full installer.
    set ERROR_COUNT=1
)

if not exist "BuildInstaller\LenovoLegionToolkitSetup.exe" (
    echo Full installer release asset was not produced.
    set ERROR_COUNT=1
) else (
    copy /y "BuildInstaller\LenovoLegionToolkitSetup.exe" "%RELEASE_ASSET_DIR%\LenovoLegionToolkit_v%VERSION%_Setup.exe" >nul
    IF ERRORLEVEL 1 (
        echo Failed to copy full installer release asset.
        set ERROR_COUNT=1
    )
)

iscc MakeInstaller.iss /DMyAppVersion=%VERSION% /DMyAppSourceDir="%BUILD_ENGLISH_DIR%" /DMyAppOutputBaseFilename=LenovoLegionToolkitSetup-English
IF %ERRORLEVEL% NEQ 0 (
    echo Inno Setup failed for English-only installer.
    set ERROR_COUNT=1
)

if not exist "BuildInstaller\LenovoLegionToolkitSetup-English.exe" (
    echo English-only installer release asset was not produced.
    set ERROR_COUNT=1
) else (
    copy /y "BuildInstaller\LenovoLegionToolkitSetup-English.exe" "%RELEASE_ASSET_DIR%\LenovoLegionToolkit_v%VERSION%_English_Setup.exe" >nul
    IF ERRORLEVEL 1 (
        echo Failed to copy English-only installer release asset.
        set ERROR_COUNT=1
    )
)

GOTO END

:BUILD_DEBUG
REM Debug build mode
REM Usage: Make.bat -d [version]

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
echo Building WPF Application (Debug)...
dotnet publish LenovoLegionToolkit.WPF\LenovoLegionToolkit.WPF.csproj -c Debug -o Build\Debug /p:FileVersion=%VERSION% /p:Version=%VERSION%
IF %ERRORLEVEL% NEQ 0 set ERROR_COUNT=1

echo.
echo Building Spectrum Tester (Debug)...
dotnet publish LenovoLegionToolkit.SpectrumTester\LenovoLegionToolkit.SpectrumTester.csproj -c Debug -o Build\Debug /p:FileVersion=%VERSION% /p:Version=%VERSION%
IF %ERRORLEVEL% NEQ 0 set ERROR_COUNT=1

echo.
echo Building CLI (Debug)...
dotnet publish LenovoLegionToolkit.CLI\LenovoLegionToolkit.CLI.csproj -c Debug -o Build\Debug /p:FileVersion=%VERSION% /p:Version=%VERSION%
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

:PRUNE_RELEASE_OUTPUT
set TARGET_DIR=%~1
if "%TARGET_DIR%"=="" exit /b 0

if exist "%TARGET_DIR%\x86" rmdir /s /q "%TARGET_DIR%\x86"
if exist "%TARGET_DIR%\arm64" rmdir /s /q "%TARGET_DIR%\arm64"

if exist "%TARGET_DIR%\SpectrumTester.exe" del /q "%TARGET_DIR%\SpectrumTester.exe"
if exist "%TARGET_DIR%\SpectrumTester.dll" del /q "%TARGET_DIR%\SpectrumTester.dll"
if exist "%TARGET_DIR%\SpectrumTester.deps.json" del /q "%TARGET_DIR%\SpectrumTester.deps.json"
if exist "%TARGET_DIR%\SpectrumTester.runtimeconfig.json" del /q "%TARGET_DIR%\SpectrumTester.runtimeconfig.json"

exit /b 0

:END
echo.
IF %ERROR_COUNT% EQU 0 (
    echo Build completed! Exiting in 5 seconds...
) ELSE (
    echo Build completed with errors! Exiting in 5 seconds...
)
ping -n 6 127.0.0.1 >nul 2>&1
endlocal & exit /b %ERROR_COUNT%

:RESOLVE_VERSION
REM MajorVersion, MinorVersion, PatchVersion are plain numeric text nodes.
REM Reading them directly avoids the '$(…)' MSBuild-expression interpolation
REM trap that causes NuGet to see '..' as a version string on some runners.
for /f "usebackq delims=" %%v in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "$props=[xml](Get-Content -Raw 'Directory.Build.props'); $group=$props.Project.PropertyGroup | Where-Object { $_.MajorVersion -ne $null } | Select-Object -First 1; $maj=[string]$group.MajorVersion; $min=[string]$group.MinorVersion; $pat=[string]$group.PatchVersion; if ([string]::IsNullOrWhiteSpace($maj) -or [string]::IsNullOrWhiteSpace($min) -or [string]::IsNullOrWhiteSpace($pat)) { exit 1 }; '{0}.{1}.{2}' -f $maj,$min,$pat"`) do SET VERSION=%%v
exit /b %ERRORLEVEL%
