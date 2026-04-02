@echo off
setlocal enabledelayedexpansion

REM ============================================================
REM Lenovo Legion Toolkit Plugins Compatibility Wrapper
REM ============================================================
REM This script keeps legacy entry points working while delegating
REM to the standard repository workflow documented in README.
REM ============================================================

SET SCRIPT_DIR=%~dp0
SET CHECKER=%SCRIPT_DIR%scripts\plugin-completion-check.ps1
SET SOLUTION=%SCRIPT_DIR%LenovoLegionToolkit-Plugins.sln

IF "%1"=="-h" GOTO HELP
IF "%1"=="/h" GOTO HELP
IF "%1"=="--help" GOTO HELP
IF "%1"=="help" GOTO HELP
IF "%1"=="clean" GOTO CLEAN
IF "%1"=="check" GOTO CHECK
IF "%1"=="ui" GOTO UI
IF "%1"=="smoke" GOTO SMOKE
IF "%1"=="zip" GOTO ZIP_INFO
IF "%1"=="" GOTO BUILD
IF /I "%1"=="all" GOTO BUILD
IF /I "%1"=="debug" GOTO BUILD_DEBUG

ECHO Error: Legacy per-plugin build entry points have been removed.
ECHO Use dotnet build on the specific project directly, e.g.
ECHO   dotnet build Plugins\CustomMouse\LenovoLegionToolkit.Plugins.CustomMouse.csproj -c Release
EXIT /B 1

:BUILD
ECHO Building solution with the standard entry point...
dotnet build "%SOLUTION%" -c Release
EXIT /B %ERRORLEVEL%

:BUILD_DEBUG
ECHO Building solution with the standard entry point (Debug)...
dotnet build "%SOLUTION%" -c Debug
EXIT /B %ERRORLEVEL%

:CHECK
ECHO Running plugin completion check...
powershell -NoProfile -ExecutionPolicy Bypass -File "%CHECKER%"
EXIT /B %ERRORLEVEL%

:UI
dotnet run --project "%SCRIPT_DIR%Tools\PluginCompletionUiTool\PluginCompletionUiTool.csproj"
EXIT /B %ERRORLEVEL%

:SMOKE
dotnet build "%SCRIPT_DIR%Tools\PluginCompletionUiTool\PluginCompletionUiTool.csproj" -c Release
IF ERRORLEVEL 1 EXIT /B 1
dotnet run --project "%SCRIPT_DIR%Tools\PluginCompletionUiTool.Smoke\PluginCompletionUiTool.Smoke.csproj" -c Release --no-build -- .
EXIT /B %ERRORLEVEL%

:ZIP_INFO
ECHO Packaging is now validated via plugin-completion-check and produced from Build\plugins outputs.
ECHO Use the documented release workflow instead of Make.bat zip.
EXIT /B 1

:CLEAN
dotnet clean "%SOLUTION%"
EXIT /B %ERRORLEVEL%

:HELP
ECHO ============================================================
ECHO Lenovo Legion Toolkit Plugins Compatibility Wrapper
ECHO ============================================================
ECHO.
ECHO Preferred commands:
ECHO   dotnet build LenovoLegionToolkit-Plugins.sln -c Release
ECHO   powershell -ExecutionPolicy Bypass -File .\scripts\plugin-completion-check.ps1
ECHO   dotnet run --project Tools\PluginCompletionUiTool\PluginCompletionUiTool.csproj
ECHO   dotnet build Tools\PluginCompletionUiTool\PluginCompletionUiTool.csproj -c Release
ECHO   dotnet run --project Tools\PluginCompletionUiTool.Smoke\PluginCompletionUiTool.Smoke.csproj -c Release --no-build -- .
ECHO.
ECHO Compatibility commands:
ECHO   make.bat              - dotnet build solution (Release)
ECHO   make.bat debug        - dotnet build solution (Debug)
ECHO   make.bat check        - run plugin completion check
ECHO   make.bat ui           - launch PluginCompletionUiTool
ECHO   make.bat smoke        - run UI smoke flow
ECHO   make.bat clean        - dotnet clean solution
ECHO.
ECHO Notes:
ECHO   - Per-plugin build and zip flows now belong to the standard documented workflow.
ECHO   - Official packaging inputs come from Build\plugins and plugin-completion-check validation.
ECHO ============================================================
EXIT /B 0
