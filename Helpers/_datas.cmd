@echo off

set MOD_NAME=ProceduralCaves

if not defined PATH_7D2D (
    echo Missing env variable: 'PATH_7D2D'
    exit /b 1
)

set MOD_PATH=%PATH_7D2D%\Mods\%MOD_NAME%