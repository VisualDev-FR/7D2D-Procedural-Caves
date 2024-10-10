@echo off

cd %~dp0

dotnet build --no-incremental

.\bin\Debug\net4.8\cave-viewer.exe %*

