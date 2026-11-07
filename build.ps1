param([switch]$Release)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (!(Test-Path $csc)) { throw "找不到 csc.exe: $csc" }

$srcDir = Join-Path $root "src"
$outDir = Join-Path $root "dist"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$sources = @(Get-ChildItem -Path $srcDir -Filter *.cs | Sort-Object Name | ForEach-Object { $_.FullName })
$exe = Join-Path $outDir "SoundWheel.exe"

$cscArgs = @("/nologo", "/target:winexe", "/platform:anycpu", "/optimize+", "/debug-", "/warn:4", "/out:$exe")
$icon = Join-Path $root "assets\app.ico"
if (Test-Path $icon) { $cscArgs += "/win32icon:$icon" }
$cscArgs += $sources

& $csc $cscArgs
if ($LASTEXITCODE -ne 0) { throw "编译失败，退出码 $LASTEXITCODE" }

$mockSrc = Join-Path $root "tools\MockSoundpad.cs"
if (Test-Path $mockSrc) {
    $mockExe = Join-Path $outDir "MockSoundpad.exe"
    & $csc /nologo /target:exe /platform:anycpu "/out:$mockExe" $mockSrc
    if ($LASTEXITCODE -ne 0) { throw "Mock 编译失败，退出码 $LASTEXITCODE" }
    Write-Host "Mock -> $mockExe"
}
Write-Host "OK -> $exe"
