@echo off
setlocal
set GAME_EXE=%~dp0Builds\Windows\NytSpil.exe

if exist "%GAME_EXE%" (
    start "" "%GAME_EXE%"
    exit /b 0
)

echo.
echo NytSpil er ikke bygget endnu.
echo.
echo Aaben Unity og vaelg:
echo Tools ^> Build ^> Build Windows Demo
echo.
echo Naar buildet er faerdigt, kan du dobbeltklikke paa denne fil igen.
echo.
pause
