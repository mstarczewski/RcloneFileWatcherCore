@echo off
REM Publishes the web GUI for WINDOWS (framework-dependent, folder layout with wwwroot\ and
REM locales\ next to RcloneFileWatcherCore.Web.exe). Use publish-web.cmd for Debian/Linux instead.
REM Needs the ASP.NET Core 8 Runtime on the target. Double-click or run from a terminal.
setlocal
set "OUT=%~dp0RcloneFileWatcherCore.Web\bin\Release\net8.0\win-x64\publish"

REM Clean the target first so the output is always fresh (no stale leftover files).
if exist "%OUT%" rmdir /s /q "%OUT%"

dotnet publish "%~dp0RcloneFileWatcherCore.Web\RcloneFileWatcherCore.Web.csproj" -c Release -r win-x64 --self-contained false -p:DebugType=none -p:DebugSymbols=false -p:AllowedReferenceRelatedFileExtensions=none
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
echo Copy the WHOLE folder above (it contains wwwroot\ and locales\) to the target, then run:
echo   RcloneFileWatcherCore.Web.exe        (or:  dotnet RcloneFileWatcherCore.Web.dll)
echo.
explorer "%OUT%"
pause
endlocal
