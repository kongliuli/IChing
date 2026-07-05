@echo off
call "%~dp0ensure-iching-junction.cmd"
if errorlevel 1 exit /b 1
cd /d "%ICHING_ASCII_ROOT%\src\IChing.Tarot.App"
dotnet build -f net10.0-android -c Debug %*
if errorlevel 1 exit /b 1
dotnet build -t:Run -f net10.0-android -c Debug %*
