param([string]$Configuration = "Debug")

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = "${env:WINDIR}\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $csc)) {
    throw "csc.exe not found at $csc"
}

$shared = @(
    (Join-Path $root "Shared\Contracts.cs"),
    (Join-Path $root "Shared\Json.cs")
)

$service = $shared + @(
    (Join-Path $root "NowLink.Service\Program.cs"),
    (Join-Path $root "NowLink.Service\ServiceRuntime.cs"),
    (Join-Path $root "NowLink.Service\ServiceHost.cs"),
    (Join-Path $root "NowLink.Service\Storage.cs")
)

$tray = $shared + @(
    (Join-Path $root "NowLink.Tray\Program.cs"),
    (Join-Path $root "NowLink.Tray\Localization.cs"),
    (Join-Path $root "NowLink.Tray\MainForm.cs"),
    (Join-Path $root "NowLink.Tray\PipeClient.cs"),
    (Join-Path $root "NowLink.Tray\PopupForm.cs")
)

$out = Join-Path $root "out\$Configuration"
New-Item -ItemType Directory -Force -Path $out | Out-Null

& $csc /target:winexe /nologo "/out:$(Join-Path $out 'NowLink.Service.exe')" `
  /r:System.dll /r:System.Core.dll /r:System.Web.Extensions.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.ServiceProcess.dll `
  @service

& $csc /target:winexe /nologo "/out:$(Join-Path $out 'NowLink.Tray.exe')" `
  /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll `
  @tray

Write-Host "Built to $out"
