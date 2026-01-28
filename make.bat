@echo off
setlocal enabledelayedexpansion

REM Check if plugin build mode is requested
IF "%1"=="-p" (
    GOTO BUILD_PLUGINS
)

IF "%1"=="" (       
	SET VERSION=3.4.1
) ELSE (
	SET VERSION=%1
)

REM Skip legacy ShellIntegration build - now using plugin architecture
REM pushd ShellIntegration\src
REM if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\VC\Auxiliary\Build\vcvars64.bat" (
REM     call "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\VC\Auxiliary\Build\vcvars64.bat"
REM ) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvars64.bat" (
REM     call "C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvars64.bat"
REM ) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat" (
REM     call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat"
REM ) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\Enterprise\VC\Auxiliary\Build\vcvars64.bat" (
REM     call "C:\Program Files (x86)\Microsoft Visual Studio\2022\Enterprise\VC\Auxiliary\Build\vcvars64.bat"
REM ) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvars64.bat" (
REM     call "C:\Program Files (x86)\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvars64.bat"
REM ) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat" (
REM     call "C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat"
REM )
REM msbuild dll\Shell.vcxproj /p:Configuration=release /p:Platform=x64 /p:SolutionDir=%CD%\ /m || exit /b
REM msbuild setup\ca\ca.vcxproj /p:Configuration=release /p:Platform=x64 /p:SolutionDir=%CD%\ /m || exit /b
REM popd

SET PATH=%PATH%;"C:\Program Files (x86)\Inno Setup 6"
dotnet publish LenovoLegionToolkit.WPF -c release -o build /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION% || exit /b
dotnet publish LenovoLegionToolkit.SpectrumTester -c release -o build /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION% || exit /b
dotnet publish LenovoLegionToolkit.CLI -c release -o build /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION% || exit /b

iscc make_installer.iss /DMyAppVersion=%VERSION% || exit /b
GOTO END

:BUILD_PLUGINS
REM Plugin build mode
REM Usage: make.bat -p [plugin_name] [copy_to_test]
REM   -p: Build plugins mode
REM   plugin_name: Optional, specific plugin to build (default: all plugins)
REM   copy_to_test: Optional, "copy" to copy to local test directory

SET COPY_TO_TEST=0
IF "%3"=="copy" (
    SET COPY_TO_TEST=1
)

IF "%2"=="" (
    ECHO Building all plugins...
    CALL :BUILD_PLUGIN NetworkAcceleration %COPY_TO_TEST%
    CALL :BUILD_PLUGIN ShellIntegration %COPY_TO_TEST%
    CALL :BUILD_PLUGIN ViveTool %COPY_TO_TEST%
    CALL :BUILD_PLUGIN CustomMouse %COPY_TO_TEST%
    ECHO All plugins built successfully!
) ELSE (
    ECHO Building plugin: %2
    CALL :BUILD_PLUGIN %2 %COPY_TO_TEST%
    ECHO Plugin %2 built successfully!
)
GOTO END

:BUILD_PLUGIN
SET PLUGIN_NAME=%1
SET COPY_TO_TEST=%2
SET PLUGIN_DIR=plugins\%PLUGIN_NAME%
SET PLUGIN_PROJECT=%PLUGIN_DIR%\LenovoLegionToolkit.Plugins.%PLUGIN_NAME%.csproj

IF NOT EXIST "%PLUGIN_PROJECT%" (
    ECHO Error: Plugin project not found: %PLUGIN_PROJECT%
    EXIT /B 1
)

ECHO.
ECHO Building %PLUGIN_NAME% plugin...
dotnet build "%PLUGIN_PROJECT%" -c release /p:DebugType=None /m || exit /b

REM Copy to local test directory if requested
IF "%COPY_TO_TEST%"=="1" (
    ECHO Copying %PLUGIN_NAME% plugin to local test directory...
    
    REM Determine output directory
    SET PLUGIN_OUTPUT_DIR=%PLUGIN_DIR%\bin\Release\net8.0-windows\win-x64
    
    REM Create test plugins directory
    SET TEST_PLUGINS_DIR=build\plugins\%PLUGIN_NAME%
    IF NOT EXIST "%TEST_PLUGINS_DIR%" (
        MKDIR "%TEST_PLUGINS_DIR%"
    )
    
    REM Copy plugin DLL and dependencies
    IF EXIST "%PLUGIN_OUTPUT_DIR%\*.dll" (
        COPY /Y "%PLUGIN_OUTPUT_DIR%\*.dll" "%TEST_PLUGINS_DIR%\" >nul 2>&1
    )
    
    REM Copy resource files if they exist (copy entire subdirectories)
    FOR /D /R "%PLUGIN_OUTPUT_DIR%" %%D IN (*) DO (
        SET RES_DIR=%%D
        SET RES_DIR=!RES_DIR:%PLUGIN_OUTPUT_DIR%\=!
        IF NOT "!RES_DIR!"=="" (
            IF NOT EXIST "%TEST_PLUGINS_DIR%\!RES_DIR!" (
                MKDIR "%TEST_PLUGINS_DIR%\!RES_DIR!"
            )
            IF EXIST "%%D\*.resources.dll" (
                COPY /Y "%%D\*.resources.dll" "%TEST_PLUGINS_DIR%\!RES_DIR!\" >nul 2>&1
            )
        )
    )
    
    ECHO %PLUGIN_NAME% plugin copied to %TEST_PLUGINS_DIR%
)

ECHO %PLUGIN_NAME% plugin built successfully!
EXIT /B 0

:END