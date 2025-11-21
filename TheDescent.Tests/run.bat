@echo off

cd %~dp0

dotnet build --no-incremental

if ERRORLEVEL 1 exit /b 1

@REM cls

.\bin\Debug\net4.8\cave-viewer.exe %*

