@echo off
setlocal

set SCRIPT_DIR=%~dp0
set REPO_ROOT=%SCRIPT_DIR:~0,-1%
set TOOLING_SCRIPT=%REPO_ROOT%\Scripts\Invoke-PluginTooling.ps1

powershell -NoProfile -ExecutionPolicy Bypass -File "%TOOLING_SCRIPT%" %*
exit /b %ERRORLEVEL%
