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

echo [1/3] compiling SoundWheel.exe ...
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

echo [2/3] compiling tools\MockSoundpad.exe ...
if exist "%ROOT%tools\MockSoundpad.cs" (
  "%CSC%" /nologo /target:exe /platform:anycpu /optimize+ /debug- /warn:4 ^
    /out:"%ROOT%dist\MockSoundpad.exe" "%ROOT%tools\MockSoundpad.cs"
  if errorlevel 1 echo [warn] MockSoundpad.exe could not be written ^(it may be running^).
)

echo [3/3] compiling installer (SoundWheelSetup.exe) ...
if exist "%ROOT%installer\Setup.cs" (
  "%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /debug- /warn:4 ^
    /win32icon:"%ROOT%assets\app.ico" ^
    /resource:"%ROOT%dist\SoundWheel.exe",payload.bin ^
    /out:"%ROOT%dist\SoundWheelSetup.exe" "%ROOT%installer\Setup.cs" "%ROOT%installer\SetupUi.cs"
  if errorlevel 1 (
    echo [error] installer build failed.
    exit /b 1
  )
) else (
  echo [warn] installer sources not found, skipping.
)

echo.
echo done: %ROOT%dist\SoundWheel.exe
echo done: %ROOT%dist\SoundWheelSetup.exe
endlocal
