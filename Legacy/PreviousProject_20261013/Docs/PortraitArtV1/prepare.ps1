$ErrorActionPreference='Stop'
Push-Location (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
try {
    if (Test-Path "$PSScriptRoot/baseline.json") { throw 'Baseline already exists.' }
    $paths=@(git -c core.quotepath=false ls-files --modified --others --exclude-standard) | Where-Object { $_ -notlike 'Docs/PortraitArtV1/*' }
    [ordered]@{
        head=(git rev-parse HEAD)
        status=@(git -c core.quotepath=false status --short --untracked-files=all)
        files=@($paths | ForEach-Object { if(Test-Path -LiteralPath $_ -PathType Leaf) { @{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash} } })
        protected=@('Assets/ANIMOL/Data/Campaign/Maps/T01-S01.asset','Assets/ANIMOL/GraphicsQA/MoonGraphicsQA.unity') | ForEach-Object { @{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash} }
    } | ConvertTo-Json -Depth 6 | Out-File -Encoding utf8 "$PSScriptRoot/baseline.json"
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip=[IO.Compression.ZipFile]::OpenRead((Resolve-Path ANIMOL_Portrait_UI_v1.zip).Path)
    try {
        foreach($entry in $zip.Entries | Where-Object { $_.FullName -in @('README_KO.md','ui_manifest.json') -or $_.FullName -match '^Previews/[a-zA-Z0-9_]+\.png$' }) {
            $dest=[IO.Path]::GetFullPath((Join-Path "$PSScriptRoot/Source" $entry.FullName))
            if(-not $dest.StartsWith([IO.Path]::GetFullPath("$PSScriptRoot/Source")+'\')) { throw 'Invalid ZIP path' }
            New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($dest)) -Force | Out-Null
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,$dest,$false)
        }
    } finally { $zip.Dispose() }
    Write-Output "Recorded baseline and extracted source specification/previews outside Assets."
} finally { Pop-Location }
