@echo off
setlocal

IF "%1"=="" (
	SET VERSION=3.1.3
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
msbuild exe\exe.vcxproj /p:Configuration=release /p:Platform=x64 /p:SolutionDir=%CD%\ /m || exit /b
msbuild setup\ca\ca.vcxproj /p:Configuration=release /p:Platform=x64 /p:SolutionDir=%CD%\ /m || exit /b
popd

SET PATH=%PATH%;"C:\Program Files (x86)\Inno Setup 6"
dotnet publish LenovoLegionToolkit.WPF -c release -o build /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION% || exit /b
dotnet publish LenovoLegionToolkit.SpectrumTester -c release -o build /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION% || exit /b
dotnet publish LenovoLegionToolkit.CLI -c release -o build /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION% || exit /b
dotnet build LenovoLegionToolkit.Plugins.NetworkAcceleration -c Release /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION% || exit /b
dotnet build LenovoLegionToolkit.Plugins.ViveTool -c Release /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION% || exit /b

iscc make_installer.iss /DMyAppVersion=%VERSION% || exit /b