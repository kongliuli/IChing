@echo off
cd /d "%~dp0.."
dotnet run --project samples\sidecars\IChing.ChartSidecar\IChing.ChartSidecar.csproj -- --preset minimal
