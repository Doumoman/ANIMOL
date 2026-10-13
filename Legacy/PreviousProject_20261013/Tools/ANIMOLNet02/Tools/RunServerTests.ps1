param([string]$Python = "python")
$ErrorActionPreference = "Stop"
$packageRoot = Split-Path $PSScriptRoot -Parent
Push-Location $packageRoot
try {
    & $Python -m unittest discover -s ServerTests -v
    if ($LASTEXITCODE -ne 0) { throw "NET02 server tests failed: $LASTEXITCODE" }
} finally { Pop-Location }
