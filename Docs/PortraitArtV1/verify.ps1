$ErrorActionPreference='Stop'
Push-Location (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
try {
    $base=Get-Content -Raw -Encoding UTF8 "$PSScriptRoot/baseline.json" | ConvertFrom-Json
    $changed=@($base.files+$base.protected | Where-Object { -not (Test-Path -LiteralPath $_.path -PathType Leaf) -or (Get-FileHash -LiteralPath $_.path).Hash -ne $_.sha256 } | ForEach-Object path)
    [xml]$prior=Get-Content -Raw Docs/PixelTypography/final-editmode.xml
    [xml]$edit=Get-Content -Raw "$PSScriptRoot/final-editmode.xml"
    [xml]$play=Get-Content -Raw "$PSScriptRoot/final-playmode.xml"
    $priorFailures=@($prior.SelectNodes('//test-case[@result="Failed"]') | ForEach-Object { $_.fullname+' | '+$_.SelectSingleNode('failure/message').InnerText })
    $failures=@($edit.SelectNodes('//test-case[@result="Failed"]') | ForEach-Object { $_.fullname+' | '+$_.SelectSingleNode('failure/message').InnerText })
    $difference=@(Compare-Object $priorFailures $failures)
    Add-Type -AssemblyName System.Drawing
    $captures=@(Get-ChildItem "$PSScriptRoot/Captures" -Filter *.png | ForEach-Object {
        $image=[Drawing.Image]::FromFile($_.FullName)
        [ordered]@{file=$_.Name;width=$image.Width;height=$image.Height;sha256=(Get-FileHash $_.FullName).Hash};$image.Dispose()
    })
    $audits=@(Get-ChildItem "$PSScriptRoot/Captures" -Filter *.json | ForEach-Object {
        $a=Get-Content -Raw -Encoding UTF8 $_.FullName | ConvertFrom-Json
        [ordered]@{file=$_.Name;labels=$a.labels.Count;overflow=@($a.labels|Where-Object overflow).Count;missing=@($a.labels|Where-Object missing).Count;buttons=$a.buttons.Count;minimumActiveTouchWidth=($a.buttons|Where-Object interactable|ForEach-Object {$_.screenBounds.width}|Measure-Object -Minimum).Minimum;minimumActiveTouchHeight=($a.buttons|Where-Object interactable|ForEach-Object {$_.screenBounds.height}|Measure-Object -Minimum).Minimum}
    })
    $allTests=@($edit.SelectNodes('//test-case'))+@($play.SelectNodes('//test-case'))
    $result=[ordered]@{
        baselineHead=$base.head;preservedExistingFileCount=$base.files.Count;existingFilesChanged=$changed;protected=$base.protected
        editMode=@{total=[int]$edit.'test-run'.total;passed=[int]$edit.'test-run'.passed;failed=[int]$edit.'test-run'.failed}
        playMode=@{total=[int]$play.'test-run'.total;passed=[int]$play.'test-run'.passed;failed=[int]$play.'test-run'.failed}
        previousFailureDifference=$difference
        newUiTests=@($allTests|Where-Object fullname -match 'PortraitArtV1.Tests'|ForEach-Object {@{name=$_.fullname;result=$_.result}})
        feedbackPolicy=@($allTests|Where-Object fullname -match 'CommercialPolishPolicyTests'|ForEach-Object {@{name=$_.fullname;result=$_.result}})
        sourceZipSha256=(Get-FileHash ANIMOL_Portrait_UI_v1.zip).Hash
        sourceFontUnchanged=((Get-FileHash Assets/ANIMOL/UI/PortraitArtV1/Fonts/pixelroborobo.otf).Hash -eq (Get-FileHash Assets/ANIMOL/Fonts/pixelroborobo.otf).Hash)
        captures=$captures;layoutAudits=$audits
    }
    $result | ConvertTo-Json -Depth 8 | Out-File -Encoding utf8 "$PSScriptRoot/final-verification.json"
    if($changed.Count -or $difference.Count -or [int]$play.'test-run'.failed -or $captures.Count -ne 12 -or @($captures|Where-Object {$_.width -ne 1080 -or $_.height -notin @(1920,2400)}).Count -or @($audits|Where-Object {$_.overflow -or $_.missing}).Count) { throw 'Preservation, tests or capture checks failed.' }
    [pscustomobject]@{Preserved=$base.files.Count;EditPass=$edit.'test-run'.passed;KnownFailures=$failures.Count;PlayPass=$play.'test-run'.passed;Captures=$captures.Count;Overflow=0;Missing=0}|ConvertTo-Json
} finally { Pop-Location }
