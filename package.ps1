#requires -Version 7.0
param([string]$Dotnet = 'dotnet', [switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    if (-not $SkipBuild) { & ./build.ps1 -Dotnet $Dotnet }
    if (-not (Test-Path -LiteralPath portable/EdgeGlow.exe)) { throw 'Build the portable release first.' }
    New-Item -ItemType Directory -Force artifacts | Out-Null
    $destination=Join-Path $PSScriptRoot 'artifacts/EdgeGlow-portable-win-x64.zip'
    Add-Type -AssemblyName System.IO.Compression
    $stream=[IO.File]::Create($destination)
    $archive=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create)
    try {
        $root=(Resolve-Path portable).Path
        Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object {
            $_.Extension -notin '.pdb','.log' -and $_.Name -notmatch '^(settings|benchmark|capture-smoke|synthetic-smoke).*' -and $_.Name -notlike '*-error.txt'
        } | ForEach-Object {
            $relative=[IO.Path]::GetRelativePath($root,$_.FullName).Replace('\','/')
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$_.FullName,$relative,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    } finally { $archive.Dispose();$stream.Dispose() }
    $hash=(Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  EdgeGlow-portable-win-x64.zip" | Set-Content artifacts/SHA256SUMS.txt -Encoding ascii
    Write-Host "Release files: $destination and artifacts/SHA256SUMS.txt"
} finally { Pop-Location }
