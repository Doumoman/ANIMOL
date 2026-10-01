$ErrorActionPreference='Stop'
Push-Location (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
try {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $root=[IO.Path]::GetFullPath((Join-Path (Get-Location) 'Assets/ANIMOL/UI/PortraitArtV1'))
    $manifest=Get-Content -Raw -Encoding UTF8 "$PSScriptRoot/Source/ui_manifest.json" | ConvertFrom-Json
    $zip=[IO.Compression.ZipFile]::OpenRead((Resolve-Path ANIMOL_Portrait_UI_v1.zip).Path)
    try {
        $entries=@($zip.Entries|Where-Object { $_.FullName -match '^UnityImport/(Sprites/[a-z_]+\.png|Fonts/pixelroborobo\.otf)$' })
        if($entries.Count -ne 11) { throw 'Unexpected import set' }
        foreach($entry in $entries) {
            $relative=$entry.FullName.Substring('UnityImport/'.Length)
            $dest=[IO.Path]::GetFullPath((Join-Path $root $relative))
            if(-not $dest.StartsWith($root+'\') -or (Test-Path -LiteralPath $dest)) { throw "Refusing overwrite or invalid path: $dest" }
            New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($dest)) -Force | Out-Null
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,$dest,$false)
            if($entry.Name.EndsWith('.png') -and (Get-FileHash -LiteralPath $dest).Hash -ne $manifest.sprites.($entry.Name).sha256) { throw 'Sprite hash mismatch' }
        }
    } finally { $zip.Dispose() }
    Write-Output 'Imported exactly 10 sprites and one source font; no Preview or ZIP under Assets.'
} finally { Pop-Location }
