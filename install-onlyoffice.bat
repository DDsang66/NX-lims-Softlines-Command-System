@echo off
echo ========================================
echo  Reinstalling OnlyOffice Document Server
echo ========================================
echo.
echo Please run this as Administrator!
echo.
pause

echo Step 1: Stopping services...
net stop DsDocService
net stop DsConverter
net stop DsProxy
net stop DsExample
timeout /t 3 /nobreak

echo.
echo Step 2: Opening download page...
echo Please download DocumentServer.exe from the browser
start https://www.onlyoffice.com/download-docs.aspx

echo.
echo After download, run the installer manually.
echo.
pause
