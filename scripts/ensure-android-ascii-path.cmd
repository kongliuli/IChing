@echo off
call "%~dp0ensure-iching-junction.cmd"
if errorlevel 1 exit /b 1
set "SLN=%ICHING_ASCII_ROOT%\src\IChing.Lab.sln"
echo.
echo 请用 Visual Studio 打开（Android 调试必须走此 ASCII 路径，否则 APT2265）：
echo   %SLN%
echo.
echo 启动项目: IChing.Tarot.App ^| 框架: net10.0-android
echo.
start "" "%SLN%"
