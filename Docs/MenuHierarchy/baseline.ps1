$ErrorActionPreference='Stop'
Push-Location (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
try {
    if(Test-Path "$PSScriptRoot/baseline.json") {throw 'Baseline already exists'}
    $paths=@(git -c core.quotepath=false ls-files --modified --others --exclude-standard) | Where-Object {$_ -notlike 'Docs/MenuHierarchy/*'}
    $protected=@('Assets/ANIMOL/GraphicsQA/MoonGraphicsQA.unity','Assets/ANIMOL/Data/Campaign/Maps/T01-S01.asset','Assets/ANIMOL/Data/Campaign/Stages/T01-S01.asset')
    $files=@($paths+$protected | Sort-Object -Unique | Where-Object {Test-Path -LiteralPath $_ -PathType Leaf} | ForEach-Object { @{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash} })
    @{head=(git rev-parse HEAD);status=@(git status --short);files=$files} | ConvertTo-Json -Depth 5 | Out-File -Encoding utf8 "$PSScriptRoot/baseline.json"
    $files.Count
} finally {Pop-Location}
