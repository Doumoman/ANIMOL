param([string]$Name = 'baseline.json')
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $root
try {
    $destination = Join-Path 'Docs/RunPrototype' $Name
    if (Test-Path $destination) { throw 'Baseline already exists; do not overwrite.' }
    $paths = @(git -c core.quotepath=false ls-files --modified --others --exclude-standard) | Where-Object { $_ -notlike 'Docs/RunPrototype/*' }
    $protected = @('Assets/ANIMOL/Data/Campaign/Maps/T01-S01.asset','Assets/ANIMOL/GraphicsQA/MoonGraphicsQA.unity')
    $record = [ordered]@{
        head = (git rev-parse HEAD)
        status = @(git -c core.quotepath=false status --short --untracked-files=all)
        files = @($paths | ForEach-Object { if (Test-Path -LiteralPath $_ -PathType Leaf) { @{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash} } })
        protected = @($protected | ForEach-Object { @{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash} })
    }
    $record | ConvertTo-Json -Depth 6 | Out-File -Encoding utf8 $destination
    Write-Output ('Recorded HEAD ' + $record.head + ', existing files ' + $record.files.Count)
}
finally { Pop-Location }
