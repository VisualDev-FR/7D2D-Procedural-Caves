@echo off

set NAME=ProceduralCaves

g++ -shared -o lib.dll cpp\lib.cpp -m64

dotnet build --no-incremental .\%NAME%.csproj

if ERRORLEVEL 1 exit /b 1

if exist "%NAME%.zip" DEL "%NAME%.zip"

if exist ".\%NAME%" rmdir ".\%NAME%" /s /q

MKDIR .\%NAME%

xcopy lib.dll %NAME%\Plugins\x86_64\ > nul
xcopy ProceduralCaves.dll %NAME%\ > nul
xcopy README.md %NAME%\ > nul
xcopy Caves %NAME%\Caves\ /s > nul
xcopy Config %NAME%\Config\ /s > nul
xcopy Prefabs %NAME%\Prefabs\ /s > nul
xcopy ModInfo.xml %NAME%\ > nul

7z.exe a "%NAME%.zip" %NAME% > nul

rmdir ".\%NAME%" /s /q

DEL *.dll
DEL *.pdb
