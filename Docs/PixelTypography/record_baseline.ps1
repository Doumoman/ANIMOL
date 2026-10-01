$ErrorActionPreference = 'Stop'
$destination = Join-Path $PSScriptRoot 'baseline.json'
if (Test-Path $destination) { throw 'Do not overwrite baseline.' }
$paths = @(git -c core.quotepath=false ls-files --modified --others --exclude-standard) | Where-Object { $_ -notlike 'Docs/PixelTypography/*' }
$record = [ordered]@{
    head=(git rev-parse HEAD)
    status=@(git -c core.quotepath=false status --short --untracked-files=all)
    files=@($paths | ForEach-Object { if (Test-Path -LiteralPath $_ -PathType Leaf) { @{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash} } })
    protected=@('Assets/ANIMOL/Data/Campaign/Maps/T01-S01.asset','Assets/ANIMOL/GraphicsQA/MoonGraphicsQA.unity') | ForEach-Object { @{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash} }
}
$record | ConvertTo-Json -Depth 6 | Out-File -Encoding utf8 $destination
Write-Output ('Recorded ' + $record.files.Count + ' existing dirty files at ' + $record.head)
