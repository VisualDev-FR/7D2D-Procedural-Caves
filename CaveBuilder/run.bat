@echo off
cd %~dp0

dotnet build --no-incremental > .log

.\bin\Debug\net8.0\standalone.exe %*

