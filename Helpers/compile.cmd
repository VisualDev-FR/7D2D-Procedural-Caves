@echo off

call "%~dp0\_datas.cmd"

dotnet build --no-incremental %*

if ERRORLEVEL 1 exit /b 1

if exist "%MOD_NAME%.zip" DEL "%MOD_NAME%.zip"

if exist ".\%MOD_NAME%" rmdir ".\%MOD_NAME%" /s /q

MKDIR .\%MOD_NAME%

xcopy %MOD_NAME%.dll %MOD_NAME%\ > nul
xcopy %MOD_NAME%.pdb %MOD_NAME%\ > nul
xcopy README.md %MOD_NAME%\ > nul
xcopy ModInfo.xml %MOD_NAME%\ > nul
xcopy Config %MOD_NAME%\Config\ /s > nul
xcopy UIAtlases %MOD_NAME%\UIAtlases\ /s > nul

7z.exe a "%MOD_NAME%.zip" %MOD_NAME% > nul

rmdir ".\%MOD_NAME%" /s /q

DEL *.dll
DEL *.pdb
