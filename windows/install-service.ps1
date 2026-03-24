$ErrorActionPreference = "Stop"
$exe = Join-Path $PSScriptRoot "out\Debug\NowLink.Service.exe"
if (-not (Test-Path $exe)) {
    throw "NowLink.Service.exe not found. Build the project first."
}

Start-Process -FilePath $exe -ArgumentList "--install" -Verb RunAs -Wait
Start-Process -FilePath "sc.exe" -ArgumentList "start NowLinkService" -Verb RunAs -Wait
Write-Host "NowLinkService installed and started."
