@echo off
setlocal enabledelayedexpansion

set ERROR_COUNT=0

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

dotnet publish LenovoLegionToolkit.WPF -c release -o Build /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION%
IF %ERRORLEVEL% NEQ 0 set ERROR_COUNT=1

dotnet publish LenovoLegionToolkit.SpectrumTester -c release -o Build /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION%
IF %ERRORLEVEL% NEQ 0 set ERROR_COUNT=1

dotnet publish LenovoLegionToolkit.CLI -c release -o Build /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION%
IF %ERRORLEVEL% NEQ 0 set ERROR_COUNT=1

iscc MakeInstaller.iss /DMyAppVersion=%VERSION%
IF %ERRORLEVEL% NEQ 0 (
    echo Inno Setup failed.
    set ERROR_COUNT=1
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
dotnet publish LenovoLegionToolkit.WPF -c Debug -o Build\Debug /p:FileVersion=%VERSION% /p:Version=%VERSION%
IF %ERRORLEVEL% NEQ 0 set ERROR_COUNT=1

echo.
echo Building Spectrum Tester (Debug)...
dotnet publish LenovoLegionToolkit.SpectrumTester -c Debug -o Build\Debug /p:FileVersion=%VERSION% /p:Version=%VERSION%
IF %ERRORLEVEL% NEQ 0 set ERROR_COUNT=1

echo.
echo Building CLI (Debug)...
dotnet publish LenovoLegionToolkit.CLI -c Debug -o Build\Debug /p:FileVersion=%VERSION% /p:Version=%VERSION%
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
