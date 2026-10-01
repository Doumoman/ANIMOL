$ErrorActionPreference='Stop'
Push-Location (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
try {
    $base=Get-Content "$PSScriptRoot/baseline.json" -Raw -Encoding UTF8 | ConvertFrom-Json
    $changed=@($base.files | Where-Object { -not (Test-Path -LiteralPath $_.path) -or (Get-FileHash -LiteralPath $_.path).Hash -ne $_.sha256 } | ForEach-Object path)
    $unexpectedTracked=@(git -c core.safecrlf=false -c core.quotepath=false diff --name-only | Where-Object {$_ -notin $base.files.path -and $_ -notlike 'Assets/ANIMOL/UI/FiveThemeMenuMaps/*' -and $_ -notlike 'Docs/FiveThemeMenu/*' -and $_ -ne 'Docs/ANIMOL_FIVE_THEME_MENU_RESULT.md'})
    $protectedScene='Assets/ANIMOL/GraphicsQA/MoonGraphicsQA.unity'
    $protectedSceneUnchanged=(git hash-object $protectedScene) -eq (git rev-parse "$($base.head):$protectedScene")
    $tests=@(Get-ChildItem "$PSScriptRoot/editmode-*-run.json","$PSScriptRoot/playmode-*.json" | ForEach-Object {
        $r=(Get-Content $_.FullName -Raw | ConvertFrom-Json).data.result
        if(($r.status -and $r.status -ne 'completed') -or $r.summary.failed -ne 0 -or $r.summary.total -le 0) {throw "Test not passed: $($_.Name)"}
        @{file=$_.Name;total=$r.summary.total;passed=$r.summary.passed}
    })
    Add-Type -AssemblyName System.Drawing
    Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System.Drawing;
public static class MenuPixelAudit {
    public static int[] Check(string file,int step,int left,int top,int width,int height) {
        int blocks=0,bad=0;
        using(var b=new Bitmap(file)) {
            for(int y=top;y<top+height;y+=step) for(int x=left;x<left+width;x+=step) {
                int color=b.GetPixel(x,y).ToArgb();bool same=true;
                for(int dy=0;dy<step;dy++) for(int dx=0;dx<step;dx++) if(b.GetPixel(x+dx,y+dy).ToArgb()!=color) same=false;
                blocks++;if(!same) bad++;
            }
        }
        return new[]{blocks,bad};
    }
}
'@
    $captures=@(Get-ChildItem "$PSScriptRoot/Captures" -Filter *.png | ForEach-Object {
        $bmp=[Drawing.Image]::FromFile($_.FullName)
        $item=@{file=$_.Name;width=$bmp.Width;height=$bmp.Height;sha256=(Get-FileHash $_.FullName).Hash};$bmp.Dispose();$item
    })
    $pixel=@(Get-ChildItem "$PSScriptRoot/Captures" -Filter Mode_*.png | ForEach-Object {
        $bg=[MenuPixelAudit]::Check($_.FullName,4,0,200,1080,1100)
        $ui=[MenuPixelAudit]::Check($_.FullName,2,32,34,96,96)
        @{file=$_.Name;backgroundBlocks=$bg[0];backgroundNonUniformBlocks=$bg[1];backButtonBlocks=$ui[0];backButtonNonUniformBlocks=$ui[1]}
    })
    $audits=@(Get-ChildItem "$PSScriptRoot/Captures" -Filter *.json | ForEach-Object {Get-Content $_.FullName -Raw | ConvertFrom-Json})
    $rows=@(Import-Csv "$PSScriptRoot/Captures/LiveClock_26s.csv")
    $transitions=@();$last=$null
    foreach($r in $rows) {if($null -eq $last -or $r.theme -ne $last.theme) {$transitions+=@{elapsed=[double]$r.elapsed;theme=[int]$r.theme}};$last=$r}
    $result=[ordered]@{
        baselineHead=$base.head;existingFileCount=$base.files.Count;existingFilesChanged=$changed;unexpectedTrackedChanges=$unexpectedTracked;moonGraphicsSceneMatchesBaselineCommit=$protectedSceneUnchanged
        zipSha256=(Get-FileHash ANIMOL_FiveTheme_MenuMaps_352_v1.zip).Hash
        tests=$tests;captures=$captures;pixelGridSamples=$pixel
        liveSampleCount=$rows.Count;liveDuration=([double]$rows[-1].elapsed-[double]$rows[0].elapsed);liveTransitions=$transitions
        importedSpriteCount=@(Get-ChildItem Assets/ANIMOL/UI/FiveThemeMenuMaps/Sprites -Filter *.png).Count
    }
    $result | ConvertTo-Json -Depth 7 | Out-File -Encoding utf8 "$PSScriptRoot/final-verification.json"
    if($changed.Count -or $unexpectedTracked.Count -or -not $protectedSceneUnchanged -or $captures.Count -ne 46 -or $result.liveDuration -lt 25.8 -or $result.liveDuration -gt 26.2 -or @($captures|Where-Object {$_.width -ne 1080 -or $_.height -notin @(1920,2400)}).Count -or @($pixel|Where-Object {$_.backgroundNonUniformBlocks -or $_.backButtonNonUniformBlocks}).Count) {throw 'Preservation, capture dimensions, live clock or pixel grid failed.'}
    [pscustomobject]@{Preserved=$base.files.Count;Tests=($tests|ForEach-Object {$_.total}|Measure-Object -Sum).Sum;Captures=$captures.Count;PixelGrid='PASS';LiveSamples=$rows.Count;LiveSeconds=$result.liveDuration}|ConvertTo-Json
} finally {Pop-Location}
