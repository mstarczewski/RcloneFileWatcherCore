@echo off
REM Publishes the Windows system-tray companion (framework-dependent, single .exe, win-x64).
REM Needs the .NET 8 Desktop Runtime on the target machine. Double-click or run from a terminal.
setlocal
set "OUT=%~dp0RcloneFileWatcherCore.Tray\bin\Release\net8.0-windows\win-x64\publish"

dotnet publish "%~dp0RcloneFileWatcherCore.Tray\RcloneFileWatcherCore.Tray.csproj" -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
if errorlevel 1 (
  echo.
  echo *** Publish FAILED ***
  pause
  exit /b 1
)

echo.
echo Published to:
echo   %OUT%
echo.
echo Usage:
echo   - Drop RcloneFileWatcherCore.Tray.exe into the published web folder to auto-launch the web app, or
echo   - run it standalone pointing at a URL:  RcloneFileWatcherCore.Tray.exe http://localhost:5005
echo.
explorer "%OUT%"
pause
endlocal
