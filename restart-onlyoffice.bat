@echo off
echo ========================================
echo  Restarting OnlyOffice Services
echo ========================================
echo.
echo Stopping services...
net stop DsDocServiceSvc
net stop DsConverterSvc
net stop DsProxySvc
timeout /t 3 /nobreak

echo.
echo Starting services...
net start DsProxySvc
net start DsConverterSvc
net start DsDocServiceSvc

echo.
echo ========================================
echo  Done!
echo ========================================
pause
