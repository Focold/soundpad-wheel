@echo off
rem ============================================================
rem  SoundWheel build script (no .NET SDK needed)
rem  Uses the C# 5 compiler shipped with .NET Framework 4.x
rem ============================================================
setlocal enabledelayedexpansion
set "ROOT=%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo [error] csc.exe of .NET Framework 4.x not found.
  exit /b 1
)
if not exist "%ROOT%dist" mkdir "%ROOT%dist"

set "SRC="
for %%f in ("%ROOT%src\*.cs") do set "SRC=!SRC! "%%f""

echo [1/2] compiling SoundWheel.exe ...
if exist "%ROOT%assets\app.ico" (
  "%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /debug- /warn:4 ^
    /win32icon:"%ROOT%assets\app.ico" /out:"%ROOT%dist\SoundWheel.exe" !SRC!
) else (
  "%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /debug- /warn:4 ^
    /out:"%ROOT%dist\SoundWheel.exe" !SRC!
)
if errorlevel 1 (
  echo [error] build failed.
  exit /b 1
)

echo [2/2] compiling tools\MockSoundpad.exe ...
if exist "%ROOT%tools\MockSoundpad.cs" (
  "%CSC%" /nologo /target:exe /platform:anycpu /optimize+ /debug- /warn:4 ^
    /out:"%ROOT%dist\MockSoundpad.exe" "%ROOT%tools\MockSoundpad.cs"
  if errorlevel 1 echo [warn] MockSoundpad.exe could not be written ^(it may be running^).
)

echo.
echo done: %ROOT%dist\SoundWheel.exe
endlocal
