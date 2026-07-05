@echo off
setlocal
set "LINK=C:\IChingDev"
set "REPO=%~dp0.."
if not exist "%LINK%" (
  echo 创建 junction: %LINK%
  mklink /J "%LINK%" "%REPO%"
  if errorlevel 1 (
    echo 需要管理员权限。请右键「以管理员身份运行」。
    exit /b 1
  )
)
endlocal & set "ICHING_ASCII_ROOT=C:\IChingDev"
