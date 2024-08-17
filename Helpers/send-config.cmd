@echo off

call "%~dp0\_datas.cmd"

set CONFIG_PATH="%MOD_PATH%\Config\"

if exist %CONFIG_PATH% RMDIR /s /q %CONFIG_PATH%

xcopy Config %CONFIG_PATH% /E > nul