@echo off

call "%~dp0\_datas.cmd"

for %%f in (%1) do (
    set ZIP_NAME=%%~nf
)

dotnet build --no-incremental %1

if ERRORLEVEL 1 exit /b 1

if exist "%ZIP_NAME%.zip" DEL "%ZIP_NAME%.zip"

if exist ".\%MOD_NAME%" rmdir ".\%MOD_NAME%" /s /q

MKDIR .\%MOD_NAME%

echo %ZIP_NAME% >> ".\%MOD_NAME%\version.txt"

xcopy ProceduralCaves.dll %MOD_NAME%\ > nul
xcopy ProceduralCaves.pdb %MOD_NAME%\ > nul
xcopy Helpers\installer.cmd %MOD_NAME%\ > nul
xcopy README.md %MOD_NAME%\ > nul
xcopy ModInfo.xml %MOD_NAME%\ > nul

if "%1" == ".\procedural-caves.csproj" (
    xcopy UIAtlases %MOD_NAME%\UIAtlases\ /s > nul
    xcopy Resources %MOD_NAME%\Resources\ /s > nul
    xcopy Config %MOD_NAME%\Config\ /s > nul
)

7z.exe a "%ZIP_NAME%.zip" %MOD_NAME% > nul

rmdir ".\%MOD_NAME%" /s /q

DEL *.dll
DEL *.pdb
