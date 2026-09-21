#requires -Version 7.0
param([string]$Dotnet = 'dotnet', [switch]$GpuTests)
$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
Push-Location $PSScriptRoot
try {
    & $Dotnet restore src/EdgeGlow.csproj --locked-mode
    if ($LASTEXITCODE) { throw 'Dependency restore failed.' }
    & $Dotnet build src/EdgeGlow.csproj -c Release --no-restore
    if ($LASTEXITCODE) { throw 'Build failed.' }
    if ($GpuTests) { & $Dotnet run --project tests/EdgeGlow.Tests.csproj -c Release -- --gpu }
    else { & $Dotnet run --project tests/EdgeGlow.Tests.csproj -c Release }
    if ($LASTEXITCODE) { throw 'Tests failed.' }
    & $Dotnet publish src/EdgeGlow.csproj -c Release -r win-x64 --self-contained true -p:RestoreLockedMode=true -p:PublishSingleFile=false -p:PublishTrimmed=false -o portable
    if ($LASTEXITCODE) { throw 'Publish failed.' }
    Copy-Item README.md,LICENSE,THIRD-PARTY-NOTICES.md,CHANGELOG.md -Destination portable
    Copy-Item licenses,docs,assets -Destination portable -Recurse -Force
    Write-Host 'Ready: portable/EdgeGlow.exe. Keep the entire portable folder together.'
} finally { Pop-Location }
