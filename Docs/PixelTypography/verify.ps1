$ErrorActionPreference = 'Stop'
Push-Location (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
try {
    $baseline=Get-Content -Raw -Encoding UTF8 Docs/PixelTypography/baseline.json | ConvertFrom-Json
    $changed=@($baseline.files + $baseline.protected | Where-Object { -not (Test-Path -LiteralPath $_.path -PathType Leaf) -or (Get-FileHash -LiteralPath $_.path).Hash -ne $_.sha256 } | ForEach-Object { $_.path })
    [xml]$old=Get-Content -Raw Docs/RunPrototype/final-editmode.xml
    [xml]$edit=Get-Content -Raw Docs/PixelTypography/final-editmode.xml
    [xml]$play=Get-Content -Raw Docs/PixelTypography/final-playmode.xml
    $oldFailed=@($old.SelectNodes('//test-case[@result="Failed"]'))
    $failed=@($edit.SelectNodes('//test-case[@result="Failed"]'))
    $newFailed=@($failed | Where-Object { $_.fullname -notin $oldFailed.fullname } | ForEach-Object { $_.fullname })
    $different=@($failed | ForEach-Object { $test=$_; $prior=$oldFailed | Where-Object fullname -eq $test.fullname; if($prior -and $prior.SelectSingleNode('failure/message').InnerText -ne $test.SelectSingleNode('failure/message').InnerText) { $test.fullname } })
    $audits=@(Get-ChildItem Docs/PixelTypography/Captures -Filter *.json | ForEach-Object {
        $audit=Get-Content -Raw -Encoding UTF8 $_.FullName | ConvertFrom-Json
        [ordered]@{file=$_.Name;width=$audit.width;height=$audit.height;labels=$audit.labels.Count;overflow=@($audit.labels|Where-Object overflow).Count;missing=@($audit.labels|Where-Object missing).Count;sizes=@($audit.labels.pointPixels|Sort-Object -Unique)}
    })
    Add-Type -AssemblyName System.Drawing
    $captures=@(Get-ChildItem Docs/PixelTypography/Captures -Filter *.png | ForEach-Object {
        $im=[System.Drawing.Image]::FromFile($_.FullName)
        [ordered]@{file=$_.Name;width=$im.Width;height=$im.Height;sha256=(Get-FileHash $_.FullName).Hash};$im.Dispose()
    })
    $invalid=@($captures | Where-Object { $_.width -ne 1080 -or $_.height -notin @(1920,2400) })
    $summary=[ordered]@{
        baselineHead=$baseline.head;existingFiles=$baseline.files.Count;existingFilesChanged=$changed;protected=$baseline.protected
        editMode=@{total=[int]$edit.'test-run'.total;passed=[int]$edit.'test-run'.passed;failed=[int]$edit.'test-run'.failed}
        playMode=@{total=[int]$play.'test-run'.total;passed=[int]$play.'test-run'.passed;failed=[int]$play.'test-run'.failed}
        newEditFailures=$newFailed;changedFailureMessages=$different
        typographyTests=@((@($edit.SelectNodes('//test-case'))+@($play.SelectNodes('//test-case'))) | Where-Object fullname -match 'Typography.Tests' | ForEach-Object { @{name=$_.fullname;result=$_.result} })
        commercialPolicy=@($edit.SelectNodes('//test-case')|Where-Object fullname -match 'CommercialPolishPolicyTests' | ForEach-Object { @{name=$_.fullname;result=$_.result} })
        captures=$captures;layoutAudits=$audits
    }
    $summary | ConvertTo-Json -Depth 8 | Out-File -Encoding utf8 Docs/PixelTypography/final-verification.json
    if($changed.Count -or $newFailed.Count -or $different.Count -or [int]$play.'test-run'.failed -or $invalid.Count -or @($audits|Where-Object { $_.overflow -or $_.missing }).Count) { throw 'Preservation, tests or capture audit failed; inspect final-verification.json.' }
    [pscustomobject]@{ExistingPreserved=$baseline.files.Count;EditPassed=$edit.'test-run'.passed;ExistingEditFailures=$failed.Count;PlayPassed=$play.'test-run'.passed;CaptureCount=$captures.Count;Overflow=0;Missing=0} | ConvertTo-Json
}
finally { Pop-Location }
