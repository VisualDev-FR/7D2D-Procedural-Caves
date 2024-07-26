@echo off

if not defined PATH_7D2D (
    echo Missing env variable: 'PATH_7D2D'
    exit /b 1
)

set MOD_NAME=ProceduralCaves
set MOD_PATH="%PATH_7D2D%\Mods\ProceduralCaves"

if exist %MOD_PATH% RMDIR /s /q %MOD_PATH%

xcopy .\ %MOD_PATH%\ /s

pause