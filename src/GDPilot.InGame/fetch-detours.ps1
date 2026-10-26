# Fetches Microsoft Detours at one pinned commit and refuses anything whose
# fingerprint differs, so a build never compiles code nobody has looked at.
$ErrorActionPreference = 'Stop'

$commit = 'e4bfd6b03e50de46b47abfbd1e46b384f0c5f833'
$sha256 = 'fef0b17c5c3c9356fb8ba7366a3b196149daadd131fb6a92abeda1cb4d3b9b85'
$external = Join-Path $PSScriptRoot 'external'
$source = Join-Path $external "Detours-$commit"
$library = Join-Path $source 'lib.X64\detours.lib'

if (Test-Path $library) {
    exit 0
}

New-Item -ItemType Directory -Force $external | Out-Null
$archive = Join-Path $external 'detours.zip'
Invoke-WebRequest -UseBasicParsing -Uri "https://github.com/microsoft/Detours/archive/$commit.zip" -OutFile $archive
$actual = (Get-FileHash -Algorithm SHA256 $archive).Hash.ToLowerInvariant()

if ($actual -ne $sha256) {
    Remove-Item $archive
    throw "Detours archive fingerprint $actual does not match the pinned $sha256."
}

Expand-Archive -Force $archive $external
Remove-Item $archive
Push-Location (Join-Path $source 'src')

try {
    $env:DETOURS_TARGET_PROCESSOR = 'X64'
    nmake /nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Building Detours failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}
