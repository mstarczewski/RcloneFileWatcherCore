@echo off
REM Publishes the web GUI for Debian/Linux (framework-dependent, folder layout with wwwroot/
REM and locales/). Double-click this file, or run it from a terminal. No Visual Studio needed.
setlocal
set "OUT=%~dp0RcloneFileWatcherCore.Web\bin\Release\net8.0\publish"

dotnet publish "%~dp0RcloneFileWatcherCore.Web\RcloneFileWatcherCore.Web.csproj" -p:PublishProfile=DebianFolder
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
echo Copy the WHOLE folder above (it contains wwwroot\ and locales\) to the Debian server,
echo e.g. /opt/RcloneFileWatcherCoreWeb, and run:  dotnet RcloneFileWatcherCore.Web.dll
echo.
explorer "%OUT%"
pause
endlocal
