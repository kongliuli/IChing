@echo off
cd /d "%~dp0.."
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0sync-tarot-images-to-maui.ps1" >nul 2>&1
cd /d "%~dp0..\src\IChing.Tarot.App"
dotnet run -f net10.0-windows10.0.19041.0
