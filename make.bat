@echo off
setlocal

IF "%1"=="" (
	SET VERSION=1.0
) ELSE (
	SET VERSION=%1
)
pushd ShellIntegration\src
call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat"
msbuild dll\Shell.vcxproj /p:Configuration=release /p:Platform=x64 /m || exit /b
msbuild exe\exe.vcxproj /p:Configuration=release /p:Platform=x64 /m || exit /b
msbuild setup\ca\ca.vcxproj /p:Configuration=release /p:Platform=x64 /m || exit /b
popd

SET PATH=%PATH%;"C:\Program Files (x86)\Inno Setup 6"
dotnet publish LenovoLegionToolkit.WPF -c release -o build /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION% || exit /b
dotnet publish LenovoLegionToolkit.SpectrumTester -c release -o build /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION% || exit /b
dotnet publish LenovoLegionToolkit.CLI -c release -o build /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION% || exit /b
dotnet build LenovoLegionToolkit.Plugins.NetworkAcceleration -c Release /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION% || exit /b

iscc make_installer.iss /DMyAppVersion=%VERSION% || exit /b