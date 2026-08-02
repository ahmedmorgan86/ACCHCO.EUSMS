# =====================================================================
#  ACCHCO EUSMS - Publish script
#  Produces a self-contained single-file Win-x64 executable in .\publish
#  Requires: .NET SDK 8
# =====================================================================
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$proj = Join-Path $root "src\ACCHCO.EUSMS.App\ACCHCO.EUSMS.App.csproj"
$out  = Join-Path $root "publish"

Write-Host "==> Publish ACCHCO.EUSMS -> $out"

dotnet publish $proj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $out `
    -v q --nologo

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE)" }

# Ensure runtime configuration files are present beside the exe
foreach ($name in @("appsettings.json")) {
    $src = Join-Path $root "src\ACCHCO.EUSMS.App\$name"
    if (Test-Path $src) { Copy-Item -LiteralPath $src -Destination (Join-Path $out $name) -Force }
}
$readme = Join-Path $root "اقرأني.txt"
if (Test-Path $readme) { Copy-Item -LiteralPath $readme -Destination (Join-Path $out "اقرأني.txt") -Force }

$exe = Join-Path $out "ACCHCO.EUSMS.exe"
if (-not (Test-Path $exe)) { throw "Output exe not found: $exe" }

$size = [math]::Round((Get-Item $exe).Length / 1MB, 2)
Write-Host "==> OK: $exe ($size MB)"
