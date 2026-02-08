@echo off
setlocal enabledelayedexpansion

REM Check build mode
IF "%1"=="-d" (
    GOTO BUILD_DEBUG
)

IF "%1"=="" (       
	SET VERSION=0.0.0
) ELSE (
	SET VERSION=%1
)

SET PATH=%PATH%;"C:\Program Files (x86)\Inno Setup 6"
dotnet publish LenovoLegionToolkit.WPF -c release -o build /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION% || exit /b
dotnet publish LenovoLegionToolkit.SpectrumTester -c release -o build /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION% || exit /b
dotnet publish LenovoLegionToolkit.CLI -c release -o build /p:DebugType=None /p:FileVersion=%VERSION% /p:Version=%VERSION% || exit /b

iscc MakeInstaller.iss /DMyAppVersion=%VERSION% || exit /b
GOTO END

:BUILD_DEBUG
REM Debug build mode
REM Usage: Make.bat -d [version]

echo Building DEBUG version...

IF "%2"=="" (
	SET VERSION=0.0.0
) ELSE (
	SET VERSION=%2
)

echo.
echo Building WPF Application (Debug)...
dotnet publish LenovoLegionToolkit.WPF -c Debug -o Build\Bebug /p:FileVersion=%VERSION% /p:Version=%VERSION% || exit /b

echo.
echo Building Spectrum Tester (Debug)...
dotnet publish LenovoLegionToolkit.SpectrumTester -c Debug -o Build\Bebug /p:FileVersion=%VERSION% /p:Version=%VERSION% || exit /b

echo.
echo Building CLI (Debug)...
dotnet publish LenovoLegionToolkit.CLI -c Debug -o Build\Bebug /p:FileVersion=%VERSION% /p:Version=%VERSION% || exit /b

echo.
echo Debug build completed successfully!
echo Output directory: Build\Bebug
echo.
echo To debug: Open solution in VS 2022 and attach to process
echo.
GOTO END

:END
endlocal
exit /b 0