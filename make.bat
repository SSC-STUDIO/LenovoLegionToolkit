@echo off
setlocal

IF "%1"=="" (
	SET VERSION=1.0
) ELSE (
	SET VERSION=%1
)
pushd ShellIntegration
call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat"
msbuild Shell.sln /p:Configuration=release /p:Platform=x64 /m
popd

SET PATH=%PATH%;"C:\Program Files (x86)\Inno Setup 6"
dotnet build .\ShellIntegration\Shell.Build.csproj -c Release || exit /b
dotnet publish LenovoLegionToolkit.WPF -c release -o build /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION% || exit /b
dotnet publish LenovoLegionToolkit.SpectrumTester -c release -o build /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION% || exit /b
dotnet publish LenovoLegionToolkit.CLI -c release -o build /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION% || exit /b
dotnet build LenovoLegionToolkit.Plugins.NetworkAcceleration -c Release /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION% || exit /b

@REM iscc make_installer.iss /DMyAppVersion=%VERSION% || exit /b