@echo off

call "%~dp0\compile.cmd" %1

if ERRORLEVEL 1 exit /b 1

set MOD_PATH="%PATH_7D2D%\Mods\%MOD_NAME%"

if exist %MOD_PATH% RMDIR /s /q %MOD_PATH%

cd %MOD_PATH%\..

7z.exe x "%~dp0..\%ZIP_NAME%.zip" > nul

taskkill /IM 7DaysToDie.exe /F >nul 2>&1

cd "%PATH_7D2D%"

start "" "%PATH_7D2D%\7DaysToDie" -noeac


RMDIR "%APPDATA%\7DaysToDie\GeneratedWorlds\Old Honihebu County" >nul 2>&1

del /Q "%APPDATA%\7DaysToDie\Saves\Old Honihebu County\Caves\Region" >nul 2>&1
del /Q "%APPDATA%\7DaysToDie\Saves\Old Honihebu County\Caves\DynamicMeshes" >nul 2>&1
del /Q "%APPDATA%\7DaysToDie\Saves\Old Honihebu County\Caves\decoration.7dt" >nul 2>&1

exit /b 0
