@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0update-dev.ps1" %*
exit /b %ERRORLEVEL%
