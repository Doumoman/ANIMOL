$ErrorActionPreference='Stop'
Push-Location (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
try {
    $base=Get-Content "$PSScriptRoot/baseline.json" -Raw -Encoding UTF8 | ConvertFrom-Json
    $changed=@($base.files | Where-Object { -not (Test-Path -LiteralPath $_.path) -or (Get-FileHash -LiteralPath $_.path).Hash -ne $_.sha256 } | ForEach-Object path)
    $owned=@('Assets/ANIMOL/Scenes/Bootstrap.unity','Assets/ANIMOL/Scenes/Lobby.unity','Assets/ANIMOL/UI/PortraitArtV1/PortraitEntryController.cs')
    $unexpected=@(git -c core.safecrlf=false -c core.quotepath=false diff --name-only | Where-Object {$_ -notin $base.files.path -and $_ -notin $owned -and $_ -notlike 'Assets/ANIMOL/UI/FiveThemeMenuMaps/*' -and $_ -notlike 'Docs/MenuHierarchy/*'})
    $tests=@(Get-ChildItem "$PSScriptRoot/*mode-*.json" | ForEach-Object {
        $envelope=Get-Content $_.FullName -Raw | ConvertFrom-Json
        $r=$envelope.data.result
        if(-not $envelope.success -or ($r.status -and $r.status -ne 'completed') -or $r.summary.failed -ne 0 -or $r.summary.total -le 0) {throw "Test not passed: $($_.Name)"}
        @{file=$_.Name;total=$r.summary.total;passed=$r.summary.passed}
    })
    $sceneObjects=@(foreach($scene in @('Bootstrap','Lobby')) {
        $path="Assets/ANIMOL/Scenes/$scene.unity"
        $oldText=git show "$($base.head):$path"
        $newText=Get-Content $path
        $missing=@($oldText | Where-Object {$_ -match '^--- !u!' -and $_ -notin $newText})
        if($missing.Count) {throw "Existing scene objects missing: $scene"}
        @{scene=$scene;allExistingSerializedObjectsPreserved=$true}
    })
    Add-Type -AssemblyName System.Drawing
    Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System.Drawing;
public static class AuthoredMenuPixelAudit {
    public static int Check(string file,int step,int left,int top,int width,int height) {
        int bad=0;
        using(var b=new Bitmap(file)) {
            for(int y=top;y<top+height;y+=step) for(int x=left;x<left+width;x+=step) {
                int color=b.GetPixel(x,y).ToArgb();bool same=true;
                for(int dy=0;dy<step;dy++) for(int dx=0;dx<step;dx++) if(b.GetPixel(x+dx,y+dy).ToArgb()!=color) same=false;
                if(!same) bad++;
            }
        }
        return bad;
    }
}
'@
    $captures=@(Get-ChildItem "$PSScriptRoot/Captures/*.png" | ForEach-Object {
        $bmp=[Drawing.Image]::FromFile($_.FullName)
        $item=@{file=$_.Name;width=$bmp.Width;height=$bmp.Height;sha256=(Get-FileHash $_.FullName).Hash};$bmp.Dispose();$item
    })
    $pixels=@(Get-ChildItem "$PSScriptRoot/Captures/Mode_*.png" | ForEach-Object {
        @{file=$_.Name;backgroundNonUniform4x4Blocks=[AuthoredMenuPixelAudit]::Check($_.FullName,4,0,200,1080,1100);buttonNonUniform2x2Blocks=[AuthoredMenuPixelAudit]::Check($_.FullName,2,32,34,96,96)}
    })
    foreach($size in @('1080x1920','1080x2400')) {
        if(-not (Test-Path "$PSScriptRoot/Captures/$size.complete")) {throw "Capture incomplete: $size"}
    }
    $rows=@(Import-Csv "$PSScriptRoot/Captures/LiveClock_26s.csv")
    $result=@{baselineHead=$base.head;preservedFiles=$base.files.Count;existingFilesChanged=$changed;unexpectedTrackedChanges=$unexpected;tests=$tests;scenes=$sceneObjects;captures=$captures;pixelGrid=$pixels;liveSamples=$rows.Count;liveSeconds=([double]$rows[-1].elapsed-[double]$rows[0].elapsed)}
    $result | ConvertTo-Json -Depth 7 | Out-File -Encoding utf8 "$PSScriptRoot/final-verification.json"
    if($changed.Count -or $unexpected.Count -or $captures.Count -ne 46 -or $result.liveSeconds -lt 25.8 -or @($captures | Where-Object {$_.width -ne 1080 -or $_.height -notin @(1920,2400)}).Count -or @($pixels | Where-Object {$_.backgroundNonUniform4x4Blocks -or $_.buttonNonUniform2x2Blocks}).Count) {throw 'Preservation or capture verification failed.'}
    [pscustomobject]@{Preserved=$base.files.Count;Tests=($tests|ForEach-Object {$_.total}|Measure-Object -Sum).Sum;Captures=$captures.Count;PixelGrid='PASS';LiveSamples=$rows.Count;LiveSeconds=$result.liveSeconds}|ConvertTo-Json
} finally {Pop-Location}
