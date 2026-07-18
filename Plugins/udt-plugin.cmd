@echo off
setlocal

REM Canonical plugin tooling entry for Universal Device Toolkit Plugins.
REM llt-plugin.cmd is kept as a compatibility alias.

set SCRIPT_DIR=%~dp0
set REPO_ROOT=%SCRIPT_DIR:~0,-1%
set TOOLING_SCRIPT=%REPO_ROOT%\Scripts\Invoke-PluginTooling.ps1

powershell -NoProfile -ExecutionPolicy Bypass -File "%TOOLING_SCRIPT%" %*
exit /b %ERRORLEVEL%
