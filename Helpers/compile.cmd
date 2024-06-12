@echo off

set NAME=ProceduralCaves

dotnet build --no-incremental .\%NAME%.csproj

if ERRORLEVEL 1 exit /b 1

if exist "%NAME%.zip" DEL "%NAME%.zip"

if exist ".\%NAME%" rmdir ".\%NAME%" /s /q

MKDIR .\%NAME%

xcopy *.dll %NAME%\ > nul
xcopy README.md %NAME%\ > nul
xcopy Caves\Stamps %NAME%\Caves\Stamps\ > nul
xcopy Config %NAME%\Config\ > nul
xcopy ModInfo.xml %NAME%\ > nul

7z.exe a "%NAME%.zip" %NAME% > nul

rmdir ".\%NAME%" /s /q

DEL %NAME%.dll
DEL %NAME%.pdb
