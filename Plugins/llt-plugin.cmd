@echo off
setlocal

set SCRIPT_DIR=%~dp0
set REPO_ROOT=%SCRIPT_DIR:~0,-1%
set TOOLING=%REPO_ROOT%\Tools\PluginTooling.Cli\PluginTooling.Cli.csproj

dotnet run --project "%TOOLING%" -- %* --repository-root "%REPO_ROOT%"
exit /b %ERRORLEVEL%
