@echo off

set NAME=ProceduralCaves

call "%~dp0\start-local.cmd"

if ERRORLEVEL 1 exit /b 1

IF NOT DEFINED PATH_7D2D_DEDI (
    echo env variable 'PATH_7D2D_DEDI' must be defined.
    exit /b 1
)

set MOD_PATH="%PATH_7D2D_DEDI%\Mods\%NAME%"

if exist %MOD_PATH% RMDIR /s /q %MOD_PATH%

cd %MOD_PATH%\..

7z.exe x "%~dp0..\%NAME%.zip" > nul

cd ..

taskkill /IM 7DaysToDieServer.exe /F >nul 2>&1

call "startdedicated.bat"

exit /b 0