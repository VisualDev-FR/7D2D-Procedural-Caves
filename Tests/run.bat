@echo off

cd %~dp0

dotnet build --no-incremental > .log

.\bin\Debug\net4.8\cave-viewer.exe %*

