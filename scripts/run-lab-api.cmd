@echo off
cd /d "%~dp0..\src\IChing.Lab.Api"
dotnet run --no-restore --launch-profile http
