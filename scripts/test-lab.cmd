@echo off
REM Lab 测试入口：不构建已废弃的 IChing.Desktop
dotnet test "%~dp0..\src\IChing.Lab.Tests\IChing.Lab.Tests.csproj" %*
