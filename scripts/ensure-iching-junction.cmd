@echo off
setlocal
set "REPO=%~dp0.."
for %%I in ("%REPO%") do set "REPO=%%~fI"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0ensure-iching-junction.ps1" -Repo "%REPO%"
if errorlevel 1 exit /b 1
endlocal & set "ICHING_ASCII_ROOT=C:\IChingDev"
