param(
    [string]$Python = "python",
    [string]$Config = "",
    [string]$Database = "",
    [ValidateRange(1024,65535)][int]$Port = 8080
)
$ErrorActionPreference = "Stop"
$packageRoot = Split-Path $PSScriptRoot -Parent
if ([string]::IsNullOrWhiteSpace($Config)) { $Config = Join-Path $packageRoot "Server/config.empty.json" }
if ([string]::IsNullOrWhiteSpace($Database)) { $Database = Join-Path $packageRoot "Runtime/net02.sqlite3" }
$server = Join-Path $packageRoot "Server/server.py"
if (!(Test-Path -LiteralPath $server)) { throw "Server source is missing: $server" }
if (!(Test-Path -LiteralPath $Config)) { throw "Config is missing: $Config" }
Write-Host "ANIMOL NET02 DEV lobby HTTP/TCP port $Port. Ctrl+C stops this foreground process."
Write-Host "Phone URL uses this PC private IPv4; do not use 127.0.0.1 or 0.0.0.0 on phone."
Write-Host "Empty config intentionally blocks admission. No firewall/settings are changed."
& $Python $server --config $Config --database $Database --bind "0.0.0.0" --port $Port
if ($LASTEXITCODE -ne 0) { throw "Server stopped with exit code $LASTEXITCODE" }
