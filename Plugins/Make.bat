@echo off
setlocal enabledelayedexpansion

REM ============================================================
REM Lenovo Legion Toolkit Plugins Tooling Wrapper
REM ============================================================
REM This script keeps short commands available while delegating to
REM the standard plugin-tooling CLI and workbench flows.
REM ============================================================

SET SCRIPT_DIR=%~dp0
SET REPO_ROOT=%SCRIPT_DIR:~0,-1%
SET SOLUTION=%REPO_ROOT%\UniversalDeviceToolkit-Plugins.sln
SET TOOLING_SCRIPT=%REPO_ROOT%\Scripts\Invoke-PluginTooling.ps1

IF "%1"=="-h" GOTO HELP
IF "%1"=="/h" GOTO HELP
IF "%1"=="--help" GOTO HELP
IF "%1"=="help" GOTO HELP
IF "%1"=="doctor" GOTO DOCTOR
IF "%1"=="init" GOTO INIT
IF "%1"=="new" GOTO NEW
IF "%1"=="clean" GOTO CLEAN
IF "%1"=="check" GOTO CHECK
IF "%1"=="validate" GOTO VALIDATE
IF "%1"=="ui" GOTO UI
IF "%1"=="workbench" GOTO WORKBENCH
IF "%1"=="workbench-bootstrap" GOTO WORKBENCH_BOOTSTRAP
IF "%1"=="workbench-smoke" GOTO WORKBENCH_SMOKE
IF "%1"=="preview" GOTO PREVIEW
IF "%1"=="dev" GOTO DEV
IF "%1"=="test" GOTO TEST_PLUGIN
IF "%1"=="package" GOTO PACKAGE
IF "%1"=="migrate" GOTO MIGRATE
IF "%1"=="pack" GOTO PACK
IF "%1"=="promote" GOTO PROMOTE
IF "%1"=="smoke" GOTO SMOKE
IF "%1"=="zip" GOTO ZIP_INFO
IF "%1"=="" GOTO BUILD
IF /I "%1"=="all" GOTO BUILD
IF /I "%1"=="debug" GOTO BUILD_DEBUG

ECHO Error: Legacy per-plugin build entry points have been removed.
ECHO Use dotnet build on the specific project directly, e.g.
ECHO   dotnet build Plugins\CustomMouse\UniversalDeviceToolkit.Plugins.CustomMouse.csproj -c Release
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
CALL :RUN_TOOLING validate --profile official-candidate
EXIT /B %ERRORLEVEL%

:VALIDATE
SHIFT
CALL :RUN_TOOLING validate %*
EXIT /B %ERRORLEVEL%

:DOCTOR
CALL :RUN_TOOLING doctor
EXIT /B %ERRORLEVEL%

:NEW
SHIFT
CALL :RUN_TOOLING init %*
EXIT /B %ERRORLEVEL%

:INIT
SHIFT
CALL :RUN_TOOLING init %*
EXIT /B %ERRORLEVEL%

:UI
dotnet run --project "%REPO_ROOT%\Tools\PluginCompletionUiTool\PluginCompletionUiTool.csproj"
EXIT /B %ERRORLEVEL%

:WORKBENCH_BOOTSTRAP
ECHO Ensuring host dependencies for PluginWorkbench...
powershell -NoProfile -ExecutionPolicy Bypass -File "%REPO_ROOT%\Scripts\ensure-host-dependencies.ps1"
EXIT /B %ERRORLEVEL%

:WORKBENCH
CALL :WORKBENCH_BOOTSTRAP
IF ERRORLEVEL 1 EXIT /B 1
dotnet run --project "%REPO_ROOT%\Tools\PluginWorkbench\PluginWorkbench.csproj" -- --repository-root "%REPO_ROOT%"
EXIT /B %ERRORLEVEL%

:PREVIEW
SHIFT
CALL :RUN_TOOLING preview %*
EXIT /B %ERRORLEVEL%

:DEV
SHIFT
CALL :RUN_TOOLING dev %*
EXIT /B %ERRORLEVEL%

:TEST_PLUGIN
SHIFT
CALL :RUN_TOOLING test %*
EXIT /B %ERRORLEVEL%

:WORKBENCH_SMOKE
SHIFT
dotnet build "%SOLUTION%" -c Release
IF ERRORLEVEL 1 EXIT /B 1
dotnet build "%REPO_ROOT%\Tools\PluginWorkbench\PluginWorkbench.csproj" -c Release
IF ERRORLEVEL 1 EXIT /B 1
dotnet build "%REPO_ROOT%\Tools\PluginWorkbench.Smoke\PluginWorkbench.Smoke.csproj" -c Release
IF ERRORLEVEL 1 EXIT /B 1
"%REPO_ROOT%\Tools\PluginWorkbench.Smoke\bin\Release\PluginWorkbench.Smoke.exe" --repository-root "%REPO_ROOT%" %*
EXIT /B %ERRORLEVEL%

:SMOKE
dotnet build "%REPO_ROOT%\Tools\PluginCompletionUiTool\PluginCompletionUiTool.csproj" -c Release
IF ERRORLEVEL 1 EXIT /B 1
dotnet build "%REPO_ROOT%\Tools\PluginCompletionUiTool.Smoke\PluginCompletionUiTool.Smoke.csproj" -c Release
IF ERRORLEVEL 1 EXIT /B 1
"%REPO_ROOT%\Tools\PluginCompletionUiTool.Smoke\bin\Release\PluginCompletionUiTool.Smoke.exe" "%REPO_ROOT%"
EXIT /B %ERRORLEVEL%

:ZIP_INFO
ECHO Packaging is now validated via the native completion checker and produced from Build\plugins outputs.
ECHO Use the documented release workflow instead of Make.bat zip.
EXIT /B 1

:PACK
SHIFT
CALL :RUN_TOOLING package %*
EXIT /B %ERRORLEVEL%

:PACKAGE
SHIFT
CALL :RUN_TOOLING package %*
EXIT /B %ERRORLEVEL%

:MIGRATE
SHIFT
CALL :RUN_TOOLING migrate %*
EXIT /B %ERRORLEVEL%

:PROMOTE
SHIFT
CALL :RUN_TOOLING promote %*
EXIT /B %ERRORLEVEL%

:CLEAN
dotnet clean "%SOLUTION%"
EXIT /B %ERRORLEVEL%

:RUN_TOOLING
powershell -NoProfile -ExecutionPolicy Bypass -File "%TOOLING_SCRIPT%" %*
EXIT /B %ERRORLEVEL%

:HELP
ECHO ============================================================
ECHO Lenovo Legion Toolkit Plugins Tooling Wrapper
ECHO ============================================================
ECHO.
ECHO Preferred commands:
ECHO   llt-plugin.cmd doctor
ECHO   llt-plugin.cmd init --template feature-settings --folder MyPlugin --id my-plugin --name "My Plugin"
ECHO   llt-plugin.cmd dev --plugin my-plugin --theme system
ECHO   llt-plugin.cmd validate --profile contributor --plugin my-plugin
ECHO   llt-plugin.cmd preview --plugin my-plugin --theme system
ECHO   llt-plugin.cmd package --plugin my-plugin --build-first
ECHO   dotnet build UniversalDeviceToolkit-Plugins.sln -c Release
ECHO   dotnet build Tools\PluginCompletionUiTool\PluginCompletionUiTool.csproj -c Release
ECHO   dotnet run --project Tools\PluginCompletionUiTool\PluginCompletionUiTool.csproj
ECHO   powershell -NoProfile -ExecutionPolicy Bypass -File Scripts\ensure-host-dependencies.ps1
ECHO   dotnet run --project Tools\PluginWorkbench\PluginWorkbench.csproj
ECHO   dotnet build Tools\PluginWorkbench.Smoke\PluginWorkbench.Smoke.csproj -c Release
ECHO   Tools\PluginWorkbench.Smoke\bin\Release\PluginWorkbench.Smoke.exe --repository-root . --plugin-id custom-mouse --theme Dark
ECHO   dotnet build Tools\PluginCompletionUiTool.Smoke\PluginCompletionUiTool.Smoke.csproj -c Release
ECHO   Tools\PluginCompletionUiTool.Smoke\bin\Release\PluginCompletionUiTool.Smoke.exe .
ECHO.
ECHO Compatibility commands:
ECHO   make.bat              - dotnet build solution (Release)
ECHO   make.bat debug        - dotnet build solution (Debug)
ECHO   make.bat doctor       - validate local environment and host dependencies
ECHO   make.bat init         - scaffold a new plugin via the CLI
ECHO   make.bat new          - compatibility alias for init
ECHO   make.bat check        - run official-candidate validation
ECHO   make.bat validate     - run plugin-tooling validation with custom args
ECHO   make.bat ui           - launch PluginCompletionUiTool
ECHO   make.bat workbench-bootstrap - ensure standalone host dependencies
ECHO   make.bat workbench    - launch PluginWorkbench
ECHO   make.bat preview      - open PluginWorkbench for a specific plugin
ECHO   make.bat dev          - build then open PluginWorkbench for a plugin
ECHO   make.bat package      - create a release ZIP from Build\plugins output
ECHO   make.bat pack         - compatibility alias for package
ECHO   make.bat migrate      - generate plugin.manifest.json and compatibility manifests
ECHO   make.bat promote      - sync official store metadata compatibility files
ECHO   make.bat workbench-smoke --plugin-id custom-mouse --theme Dark - run PluginWorkbench UI smoke
ECHO   make.bat smoke        - run UI smoke flow
ECHO   make.bat clean        - dotnet clean solution
ECHO.
ECHO Notes:
ECHO   - llt-plugin.cmd publishes and reuses a stable CLI executable under Build\tooling.
ECHO   - plugin.manifest.json is the authoring source of truth; plugin.json is emitted for host compatibility.
ECHO   - store.json should be treated as generated release output; official metadata lives in plugin.manifest.json store metadata.
ECHO   - PluginWorkbench loads plugin build outputs or local ZIPs without needing the main repo checkout.
ECHO   - PluginWorkbench.Smoke validates theme and Preview -> Real Runtime behavior against built plugin outputs.
ECHO ============================================================
EXIT /B 0
