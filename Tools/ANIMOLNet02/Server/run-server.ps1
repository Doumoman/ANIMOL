param(
    [string]$BindAddress = "127.0.0.1",
    [ValidateRange(1,65535)][int]$Port = 8080,
    [string]$ConfigPath = (Join-Path $PSScriptRoot "config.empty.json"),
    [string]$DatabasePath = (Join-Path $PSScriptRoot "dev-lobby.sqlite3"),
    [string]$PythonExecutable = "python"
)
$ErrorActionPreference = "Stop"
& $PythonExecutable (Join-Path $PSScriptRoot "server.py") --bind $BindAddress --port $Port --config $ConfigPath --database $DatabasePath
if ($LASTEXITCODE -ne 0) { throw "NET02 DEV lobby exited with code $LASTEXITCODE" }
