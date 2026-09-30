@echo off
setlocal
cd /d "%~dp0"

echo Publishing ScreenSwift...
dotnet publish .\ScreenSwift\ScreenSwift.csproj -c Release -r win-x64 --self-contained true
if errorlevel 1 goto :failed

set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" (
  echo.
  echo Inno Setup 6 was not found. Install it, then run this file again.
  start "" "https://jrsoftware.org/isdl.php"
  exit /b 1
)

echo Creating installer...
"%ISCC%" .\Installer\ScreenSwift.iss
if errorlevel 1 goto :failed

echo.
echo Done: Installer\Output\ScreenSwift-Setup.exe
exit /b 0

:failed
echo.
echo Build failed. Review the messages above.
exit /b 1
