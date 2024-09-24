@echo off
setlocal

rem Set the output file name
set LOGFILE=bluetooth_log_%date:~-4,4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%%time:~6,2%.txt
set LOGFILE=%LOGFILE: =0%

rem Start the ADB logcat process and filter for Bluetooth
echo Logging Bluetooth data to %LOGFILE%
echo Press Ctrl+C to stop logging

adb logcat | findstr /i "bluetooth" > %LOGFILE%

echo Logging complete. Output saved to %LOGFILE%
pause