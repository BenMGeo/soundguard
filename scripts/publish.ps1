# Builds the distributable SoundGuard zips into ./dist
#   SoundGuard-self-contained.zip       (no prerequisites, ~44 MB)
#   SoundGuard-framework-dependent.zip  (needs the .NET 10 Desktop Runtime, <1 MB)
# Usage: pwsh scripts/publish.ps1 [-SkipTests]
param([switch]$SkipTests)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$dist = Join-Path $root 'dist'
$project = Join-Path $root 'src/SoundGuard'

function Invoke-Dotnet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($args -join ' ') failed with exit code $LASTEXITCODE" }
}

if (-not $SkipTests) { Invoke-Dotnet test (Join-Path $root 'SoundGuard.sln') -c Release }

if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }

$variants = @(
    @{ Name = 'self-contained';       SelfContained = 'true';  Extra = @('-p:EnableCompressionInSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true') },
    @{ Name = 'framework-dependent';  SelfContained = 'false'; Extra = @() }
)

foreach ($v in $variants) {
    $out = Join-Path $dist $v.Name
    $publishArgs = @('publish', $project, '-c', 'Release', '-r', 'win-x64', '--self-contained', $v.SelfContained,
                     '-p:PublishSingleFile=true', '-o', $out) + $v.Extra
    Invoke-Dotnet @publishArgs
    Get-ChildItem $out -Filter *.pdb | Remove-Item
    Copy-Item (Join-Path $root 'README.md') $out
    Compress-Archive -Path (Join-Path $out '*') -DestinationPath (Join-Path $dist "SoundGuard-$($v.Name).zip") -Force
}

Get-ChildItem $dist -Filter *.zip | Select-Object Name, @{ n = 'MB'; e = { [math]::Round($_.Length / 1MB, 1) } }
