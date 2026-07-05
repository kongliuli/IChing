@echo off
setlocal
cd /d "%~dp0.."

set VERSION=1.0
set DIST=dist\v%VERSION%
set PROJ=src\IChing.Tarot.App\IChing.Tarot.App.csproj

echo [1/4] Sync tarot images...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0sync-tarot-images-to-maui.ps1"
if errorlevel 1 exit /b 1

echo [2/4] Prepare output folders...
if not exist "%DIST%\windows" mkdir "%DIST%\windows"
if not exist "%DIST%\android" mkdir "%DIST%\android"

echo [3/4] Publish Windows (Release)...
dotnet publish "%PROJ%" -c Release -f net10.0-windows10.0.19041.0 -o "%DIST%\windows"
if errorlevel 1 exit /b 1

echo [4/4] Publish Android APK (Release, ASCII junction)...
call "%~dp0ensure-iching-junction.cmd"
if errorlevel 1 exit /b 1
dotnet publish "%ICHING_ASCII_ROOT%\src\IChing.Tarot.App\IChing.Tarot.App.csproj" -c Release -f net10.0-android -p:AndroidPackageFormat=apk -o "%CD%\%DIST%\android"
if errorlevel 1 exit /b 1

copy /Y "%DIST%\android\com.iching.tarot-Signed.apk" "%DIST%\IChing.Tarot-v%VERSION%-android.apk" >nul
powershell -NoProfile -Command "Compress-Archive -Path '%DIST%\windows\*' -DestinationPath '%DIST%\IChing.Tarot-v%VERSION%-windows-win-x64.zip' -Force"

echo.
echo Done. Artifacts in %DIST%\
echo   IChing.Tarot-v%VERSION%-windows-win-x64.zip
echo   IChing.Tarot-v%VERSION%-android.apk
echo   windows\IChing.Tarot.App.exe
exit /b 0
