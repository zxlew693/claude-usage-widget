# Builds the MSIX package for the Microsoft Store.
#
#   powershell -File tools/build-msix.ps1 -Version 0.2.0
#
# The Store signs the package itself, so the .msix this produces is unsigned. To try it
# locally, enable Developer Mode and run with -LayoutOnly, then:
#   Add-AppxPackage -Register artifacts\msix-layout\AppxManifest.xml
#
# Before the first Store upload, pass the identity values from Partner Center
# (Product management → Product identity) instead of the dev defaults.
param(
    [string]$Version = "0.0.1",
    [string]$IdentityName = "UsageMeterForClaude.Dev",
    [string]$Publisher = "CN=UsageMeterForClaudeDev",
    [string]$PublisherDisplayName = "zxlew693",
    [string]$OutDir = (Join-Path $PSScriptRoot "..\artifacts"),
    [switch]$LayoutOnly
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
New-Item -ItemType Directory -Force $OutDir | Out-Null
$OutDir = (Resolve-Path $OutDir).Path

# MSIX versions are four-part, and the Store requires the last part to be 0.
$v = [version]$Version
$msixVersion = '{0}.{1}.{2}.0' -f $v.Major, $v.Minor, [math]::Max($v.Build, 0)

# 1. Publish a normal (not single-file) self-contained build into the package layout.
#    Not single-file because MSIX already compresses, and unpacked files start faster.
$layout = Join-Path $OutDir 'msix-layout'
if (Test-Path $layout) { Remove-Item -Recurse -Force $layout }
dotnet publish (Join-Path $root 'ClaudeUsageWidget') -c Release -r win-x64 --self-contained `
    -p:PublishSingleFile=false -p:DebugType=none -p:Version=$Version -o $layout -nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)" }

# 2. Tile/Start menu images and the manifest.
Copy-Item (Join-Path $root 'packaging\Assets') (Join-Path $layout 'Assets') -Recurse -Force

$esc = { param($s) [System.Security.SecurityElement]::Escape($s) }
$manifest = [System.IO.File]::ReadAllText((Join-Path $root 'packaging\AppxManifest.xml'))
$manifest = $manifest.Replace('$IDENTITY_NAME$', (& $esc $IdentityName))
$manifest = $manifest.Replace('$PUBLISHER_DISPLAY_NAME$', (& $esc $PublisherDisplayName))
$manifest = $manifest.Replace('$PUBLISHER$', (& $esc $Publisher))
$manifest = $manifest.Replace('$VERSION$', $msixVersion)
[System.IO.File]::WriteAllText((Join-Path $layout 'AppxManifest.xml'), $manifest, (New-Object System.Text.UTF8Encoding $false))

if ($LayoutOnly) { Write-Output $layout; return }

# 3. Pack with the newest makeappx from the Windows SDK.
$makeappx = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\makeappx.exe" -ErrorAction SilentlyContinue |
    Where-Object { $_.Directory.Parent.Name -match '^\d+\.\d+\.\d+\.\d+$' } |
    Sort-Object { [version]$_.Directory.Parent.Name } -Descending |
    Select-Object -First 1
if (-not $makeappx) { throw "makeappx.exe not found. Install the Windows 10/11 SDK." }

$msix = Join-Path $OutDir "UsageMeterForClaude_${msixVersion}_x64.msix"
& $makeappx.FullName pack /d $layout /p $msix /o | Out-Null
if ($LASTEXITCODE -ne 0) { throw "makeappx failed ($LASTEXITCODE)" }
Write-Output $msix
