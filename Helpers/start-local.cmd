@echo off

set NAME=ProceduralCaves

call "%~dp0\compile.cmd"

if ERRORLEVEL 1 exit /b 1

set MOD_PATH="%PATH_7D2D%\Mods\%NAME%"

if exist %MOD_PATH% RMDIR /s /q %MOD_PATH%

DEL "%PATH_7D2D%\Data\Worlds\Navezgane\cavePrefabs.xml" >nul 2>&1
xcopy Config\cavePrefabs.xml "%PATH_7D2D%\Data\Worlds\Navezgane\" >nul 2>&1

cd %MOD_PATH%\..

7z.exe x "%~dp0..\%NAME%.zip" > nul

taskkill /IM 7DaysToDie.exe /F >nul 2>&1

cd "%PATH_7D2D%"

start "" "%PATH_7D2D%\7DaysToDie" -noeac

del /Q "%APPDATA%\7DaysToDie\Saves\Navezgane\Caves\Region"
del /Q "%APPDATA%\7DaysToDie\Saves\Old Honihebu County\Caves\Region"
del /Q "%APPDATA%\7DaysToDie\Saves\Old Honihebu County\Entrances\Region"

@REM RMDIR "%APPDATA%\7DaysToDie\GeneratedWorlds\Old Honihebu County" /s /q

exit /b 0
