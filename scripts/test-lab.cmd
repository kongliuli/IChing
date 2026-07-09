@echo off
REM Lab 测试入口：跳过 Integration（sidecar 等需本地服务）与已废弃的 IChing.Desktop
dotnet test "%~dp0..\src\IChing.Lab.Tests\IChing.Lab.Tests.csproj" --filter "Category!=Integration" %*
