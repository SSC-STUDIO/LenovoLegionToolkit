@echo off
REM Legacy entry point. Full clean logic lives in Make.bat.
call "%~dp0Make.bat" -clean %*
exit /b %ERRORLEVEL%
