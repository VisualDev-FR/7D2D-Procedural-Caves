@echo off

if [%1] == [] exit /b 1

set WORLD_NAME=%~1

RMDIR "%APPDATA%\7DaysToDie\GeneratedWorlds\%WORLD_NAME%" /S /Q >nul 2>&1
RMDIR "%APPDATA%\7DaysToDie\Saves\%WORLD_NAME%\Caves\Region" /S /Q >nul 2>&1
RMDIR "%APPDATA%\7DaysToDie\Saves\%WORLD_NAME%\Caves\DynamicMeshes" /S /Q >nul 2>&1

del /Q "%APPDATA%\7DaysToDie\Saves\%WORLD_NAME%\Caves\decoration.7dt" >nul 2>&1