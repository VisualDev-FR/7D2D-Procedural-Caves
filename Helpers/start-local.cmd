@echo off

call "%~dp0\compile.cmd" %1

if ERRORLEVEL 1 exit /b 1

set MOD_PATH="%PATH_7D2D%\Mods\%MOD_NAME%"

if exist %MOD_PATH% RMDIR /s /q %MOD_PATH%

cd %MOD_PATH%\..

7z.exe x "%~dp0..\%MOD_NAME%.zip" > nul

taskkill /IM 7DaysToDie.exe /F >nul 2>&1

cd "%PATH_7D2D%"

start "" "%PATH_7D2D%\7DaysToDie" -noeac

call "%~dp0\remove-world.cmd" "Old Honihebu County" @REM default 2048
call "%~dp0\remove-world.cmd" "Old Wosayuwe Valley" @REM default 4096

exit /b 0
