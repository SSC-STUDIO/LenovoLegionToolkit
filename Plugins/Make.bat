@echo off
setlocal enabledelayedexpansion

REM ============================================================
REM Lenovo Legion Toolkit Plugins Build Script
REM ============================================================
REM Usage:
REM   make.bat              - Build all plugins (Release)
REM   make.bat all          - Build all plugins (Release)
REM   make.bat debug        - Build all plugins (Debug)
REM   make.bat <plugin>     - Build specific plugin (Release)
REM   make.bat <plugin> d   - Build specific plugin (Debug)
REM   make.bat zip          - Create ZIP packages for all plugins
REM   make.bat clean        - Clean all build outputs
REM   make.bat help         - Show this help message
REM ============================================================

REM Plugin list
SET PLUGINS=NetworkAcceleration ShellIntegration ViveTool CustomMouse

REM Check parameters
IF "%1"=="-h" GOTO HELP
IF "%1"=="/h" GOTO HELP
IF "%1"=="--help" GOTO HELP
IF "%1"=="help" GOTO HELP
IF "%1"=="" GOTO BUILD_ALL
IF "%1"=="all" GOTO BUILD_ALL
IF "%1"=="debug" GOTO BUILD_ALL_DEBUG
IF "%1"=="clean" GOTO CLEAN
IF "%1"=="zip" GOTO CREATE_ZIPS

REM Build specific plugin
SET PLUGIN_NAME=%1
SET CONFIG=Release
IF "%2"=="d" SET CONFIG=Debug
CALL :BUILD_PLUGIN "%PLUGIN_NAME%" "%CONFIG%"
GOTO END

:BUILD_ALL
REM Build all plugins (Release)
ECHO ============================================================
ECHO Building all plugins (Release)...
ECHO ============================================================
FOR %%P IN (%PLUGINS%) DO (
    CALL :BUILD_PLUGIN "%%P" "Release"
)
ECHO.
ECHO ============================================================
ECHO All plugins built successfully!
ECHO ============================================================
GOTO END

:BUILD_ALL_DEBUG
REM Build all plugins (Debug)
ECHO ============================================================
ECHO Building all plugins (Debug)...
ECHO ============================================================
FOR %%P IN (%PLUGINS%) DO (
    CALL :BUILD_PLUGIN "%%P" "Debug"
)
ECHO.
ECHO ============================================================
ECHO All plugins built successfully!
ECHO ============================================================
GOTO END

:BUILD_PLUGIN
SET PLUGIN_NAME=%1
SET CONFIG=%2
SET PLUGIN_DIR=plugins\%PLUGIN_NAME%
SET PLUGIN_PROJECT=%PLUGIN_DIR%\LenovoLegionToolkit.Plugins.%PLUGIN_NAME%.csproj

IF NOT EXIST "%PLUGIN_PROJECT%" (
    ECHO Error: Plugin project not found: %PLUGIN_PROJECT%
    EXIT /B 1
)

ECHO.
ECHO Building %PLUGIN_NAME% plugin (%CONFIG%)...
dotnet build "%PLUGIN_PROJECT%" -c "%CONFIG%" /p:DebugType=None /m
IF ERRORLEVEL 1 (
    ECHO Error: Failed to build %PLUGIN_NAME%
    EXIT /B 1
)
ECHO %PLUGIN_NAME% plugin built successfully!
EXIT /B 0

:CREATE_ZIPS
REM Create ZIP packages for all plugins
ECHO ============================================================
ECHO Creating ZIP packages for all plugins...
ECHO ============================================================

REM Ensure build directory exists
IF NOT EXIST "build\plugins" (
    MKDIR "build\plugins"
)

FOR %%P IN (%PLUGINS%) DO (
    CALL :CREATE_ZIP "%%P"
)

ECHO.
ECHO ============================================================
ECHO All ZIP packages created successfully!
ECHO Output directory: build\plugins
ECHO ============================================================
GOTO END

:CREATE_ZIP
SET PLUGIN_NAME=%1
SET PLUGIN_DIR=plugins\%PLUGIN_NAME%
SET OUTPUT_DIR=build\plugins\%PLUGIN_NAME%
SET ZIP_NAME=%PLUGIN_NAME%.zip

ECHO Creating %ZIP_NAME%...

REM Clean previous output
IF EXIST "%OUTPUT_DIR%" RMDIR /S /Q "%OUTPUT_DIR%"
IF EXIST "%ZIP_NAME%" DEL /Q "%ZIP_NAME%"

REM Create output directory
MKDIR "%OUTPUT_DIR%"

REM Copy plugin files
XCOPY /E /I /Y "%PLUGIN_DIR%\bin\Release\net8.0-windows\win-x64\*" "%OUTPUT_DIR%\" >nul 2>&1

REM Remove SDK DLL to avoid conflicts
IF EXIST "%OUTPUT_DIR%\LenovoLegionToolkit.Plugins.SDK.dll" (
    DEL /Q "%OUTPUT_DIR%\LenovoLegionToolkit.Plugins.SDK.dll"
)

REM Create ZIP package
powershell -Command "Compress-Archive -Path '%OUTPUT_DIR%\*' -DestinationPath '%ZIP_NAME%' -Force"
IF ERRORLEVEL 1 (
    ECHO Error: Failed to create %ZIP_NAME%
    EXIT /B 1
)

ECHO Created %ZIP_NAME% (%~z1 bytes)
EXIT /B 0

:CLEAN
REM Clean all build outputs
ECHO ============================================================
ECHO Cleaning all build outputs...
ECHO ============================================================

FOR %%P IN (%PLUGINS%) DO (
    SET PLUGIN_DIR=plugins\%%P
    IF EXIST "%PLUGIN_DIR%\bin" RMDIR /S /Q "%PLUGIN_DIR%\bin"
    IF EXIST "%PLUGIN_DIR%\obj" RMDIR /S /Q "%PLUGIN_DIR%\obj"
    IF EXIST "build\plugins\%%P" RMDIR /S /Q "build\plugins\%%P"
    IF EXIST "%%P.zip" DEL /Q "%%P.zip"
)

IF EXIST "build" RMDIR /S /Q "build"
IF EXIST "*.zip" DEL /Q "*.zip"

ECHO.
ECHO ============================================================
ECHO All build outputs cleaned!
ECHO ============================================================
GOTO END

:HELP
ECHO ============================================================
ECHO Lenovo Legion Toolkit Plugins Build Script
ECHO ============================================================
ECHO.
ECHO Usage: make.bat [command] [options]
ECHO.
ECHO Commands:
ECHO   (none)      Build all plugins (Release)
ECHO   all         Build all plugins (Release)
ECHO   debug       Build all plugins (Debug)
ECHO   clean       Clean all build outputs
ECHO   zip         Create ZIP packages for all plugins
ECHO   help        Show this help message
ECHO.
ECHO Options for specific plugin:
ECHO   <plugin>    Build specific plugin (Release)
ECHO   <plugin> d  Build specific plugin (Debug)
ECHO.
ECHO Examples:
ECHO   make.bat                  - Build all plugins
ECHO   make.bat debug            - Build all plugins (Debug)
ECHO   make.bat ViveTool         - Build ViveTool plugin
ECHO   make.bat ViveTool d       - Build ViveTool plugin (Debug)
ECHO   make.bat zip              - Create ZIP packages
ECHO   make.bat clean            - Clean all outputs
ECHO.
ECHO Available plugins:
ECHO   - NetworkAcceleration
ECHO   - ShellIntegration
ECHO   - ViveTool
ECHO   - CustomMouse
ECHO.
ECHO ============================================================
GOTO END

:END
endlocal
exit /b 0
