$ErrorActionPreference='Stop'
Push-Location (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
try {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    if(-not (Test-Path "$PSScriptRoot/baseline.json")) {
        $paths=@(git -c core.quotepath=false ls-files --modified --others --exclude-standard) | Where-Object {$_ -notlike 'Docs/FiveThemeMenu/*'}
        $protected=@('Assets/ANIMOL/GraphicsQA/MoonGraphicsQA.unity','Assets/ANIMOL/Data/Campaign/Maps/T01-S01.asset','Assets/ANIMOL/Data/Campaign/Stages/T01-S01.asset')
        $files=@($paths+$protected | Sort-Object -Unique | Where-Object {Test-Path -LiteralPath $_ -PathType Leaf} | ForEach-Object { @{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash} })
        @{head=(git rev-parse HEAD);status=@(git status --short);files=$files} | ConvertTo-Json -Depth 5 | Out-File -Encoding utf8 "$PSScriptRoot/baseline.json"
    }
    $zip=[IO.Compression.ZipFile]::OpenRead((Resolve-Path 'ANIMOL_FiveTheme_MenuMaps_352_v1.zip'))
    try {
        foreach($entry in $zip.Entries) {
            $relative=$entry.FullName
            if($relative -match '^UnityImport/(Sprites/[A-Za-z0-9_.-]+\.png|Fonts/[A-Za-z0-9_.-]+\.otf)$') {
                $dest=Join-Path 'Assets/ANIMOL/UI/FiveThemeMenuMaps' $Matches[1]
            } elseif($relative -match '^(README_KO\.md|CLI_APPLY_PROMPT_KO\.md|manifest\.json|Previews/[A-Za-z0-9_.-]+\.(png|mp4))$') {
                $dest=Join-Path "$PSScriptRoot/Source" $relative
            } else {continue}
            if(Test-Path -LiteralPath $dest) {continue}
            New-Item -ItemType Directory -Force -Path (Split-Path $dest) | Out-Null
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,[IO.Path]::GetFullPath($dest),$false)
        }
    } finally {$zip.Dispose()}
    Get-ChildItem "$PSScriptRoot/Source/Previews" | Select-Object Name,Length
} finally {Pop-Location}
