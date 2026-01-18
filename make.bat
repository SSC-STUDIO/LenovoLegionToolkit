@echo off
setlocal

REM Check if plugin build mode is requested
IF "%1"=="-p" (
    GOTO BUILD_PLUGINS
)

IF "%1"=="" (       
	SET VERSION=3.4.0
) ELSE (
	SET VERSION=%1
)

pushd ShellIntegration\src
if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\VC\Auxiliary\Build\vcvars64.bat" (
    call "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\VC\Auxiliary\Build\vcvars64.bat"
) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvars64.bat" (
    call "C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvars64.bat"
) else if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat" (
    call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat"
) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\Enterprise\VC\Auxiliary\Build\vcvars64.bat" (
    call "C:\Program Files (x86)\Microsoft Visual Studio\2022\Enterprise\VC\Auxiliary\Build\vcvars64.bat"
) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvars64.bat" (
    call "C:\Program Files (x86)\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvars64.bat"
) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat" (
    call "C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat"
)
msbuild dll\Shell.vcxproj /p:Configuration=release /p:Platform=x64 /p:SolutionDir=%CD%\ /m || exit /b
msbuild setup\ca\ca.vcxproj /p:Configuration=release /p:Platform=x64 /p:SolutionDir=%CD%\ /m || exit /b
popd

SET PATH=%PATH%;"C:\Program Files (x86)\Inno Setup 6"
dotnet publish LenovoLegionToolkit.WPF -c release -o build /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION% || exit /b
dotnet publish LenovoLegionToolkit.SpectrumTester -c release -o build /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION% || exit /b
dotnet publish LenovoLegionToolkit.CLI -c release -o build /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION% || exit /b

iscc make_installer.iss /DMyAppVersion=%VERSION% || exit /b
GOTO END

:BUILD_PLUGINS
REM Plugin build mode
IF "%2"=="" (
    ECHO Building all plugins...
    CALL :BUILD_PLUGIN NetworkAcceleration
    CALL :BUILD_PLUGIN ViveTool
    CALL :BUILD_PLUGIN Tools
    ECHO All plugins built successfully!
) ELSE (
    ECHO Building plugin: %2
    CALL :BUILD_PLUGIN %2
    ECHO Plugin %2 built successfully!
)
GOTO END

:BUILD_PLUGIN
SET PLUGIN_NAME=%1
SET PLUGIN_DIR=Plugins\%PLUGIN_NAME%
SET PLUGIN_PROJECT=%PLUGIN_DIR%\LenovoLegionToolkit.Plugins.%PLUGIN_NAME%.csproj

IF NOT EXIST "%PLUGIN_PROJECT%" (
    ECHO Error: Plugin project not found: %PLUGIN_PROJECT%
    EXIT /B 1
)

ECHO.
ECHO Building %PLUGIN_NAME% plugin...
dotnet build "%PLUGIN_PROJECT%" -c release /p:DebugType=None /m || exit /b
ECHO %PLUGIN_NAME% plugin built successfully!
EXIT /B 0

:END