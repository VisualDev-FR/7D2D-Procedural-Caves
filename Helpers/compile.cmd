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

xcopy TheDescent.dll %MOD_NAME%\ > nul
xcopy TheDescent.pdb %MOD_NAME%\ > nul
xcopy README.md %MOD_NAME%\ > nul
xcopy ModInfo.xml %MOD_NAME%\ > nul
xcopy Config %MOD_NAME%\Config\ /s > nul
xcopy UIAtlases %MOD_NAME%\UIAtlases\ /s > nul

7z.exe a "%ZIP_NAME%.zip" %MOD_NAME% > nul

rmdir ".\%MOD_NAME%" /s /q

DEL *.dll
DEL *.pdb
