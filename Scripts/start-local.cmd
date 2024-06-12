@echo off

set NAME=ProceduralCaves

call "%~dp0\compile.cmd"

if ERRORLEVEL 1 exit /b 1

set MOD_PATH="%PATH_7D2D%\Mods\%NAME%"

if exist %MOD_PATH% RMDIR /s /q %MOD_PATH%

cd %MOD_PATH%\..

7z.exe x "%~dp0..\%NAME%.zip" > nul

taskkill /IM 7DaysToDie.exe /F >nul 2>&1

@REM start steam://rungameid/251570

exit /b 0