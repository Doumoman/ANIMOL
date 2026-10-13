$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $repoRoot
try {
    $baseline = Get-Content -Raw -Encoding UTF8 'Docs/GraphicsQA/baseline-worktree.json' | ConvertFrom-Json
    $changed = @($baseline.files | Where-Object {
        -not (Test-Path -LiteralPath $_.path -PathType Leaf) -or
        (Get-FileHash -LiteralPath $_.path -Algorithm SHA256).Hash -ne $_.sha256
    } | ForEach-Object { $_.path })
    [xml]$before = Get-Content -Raw 'Docs/GraphicsQA/baseline-editmode.xml'
    [xml]$edit = Get-Content -Raw 'Docs/GraphicsQA/final-editmode.xml'
    [xml]$play = Get-Content -Raw 'Docs/GraphicsQA/final-playmode.xml'
    $known = @($before.SelectNodes('//test-case[@result="Failed"]') | ForEach-Object { $_.fullname })
    $failed = @($edit.SelectNodes('//test-case[@result="Failed"]') | ForEach-Object { $_.fullname })
    $newFailures = @($failed | Where-Object { $_ -notin $known })
    $messageChanges = @($edit.SelectNodes('//test-case[@result="Failed"]') | ForEach-Object {
        $test = $_
        $old = $before.SelectNodes('//test-case') | Where-Object fullname -eq $test.fullname
        if ($old -and $old.SelectSingleNode('failure/message').InnerText -ne $test.SelectSingleNode('failure/message').InnerText) { $test.fullname }
    })
    $report = [ordered]@{
        unity = '6000.3.8f1'
        baselineHead = $baseline.head
        existingFilesChecked = $baseline.files.Count
        existingFilesChanged = $changed
        protectedMapSha256 = (Get-FileHash 'Assets/ANIMOL/Data/Campaign/Maps/T01-S01.asset').Hash
        mapMatchesBaseline = (Get-FileHash 'Assets/ANIMOL/Data/Campaign/Maps/T01-S01.asset').Hash -eq $baseline.protectedMap
        editMode = @{total=[int]$edit.'test-run'.total;passed=[int]$edit.'test-run'.passed;failed=[int]$edit.'test-run'.failed}
        playMode = @{total=[int]$play.'test-run'.total;passed=[int]$play.'test-run'.passed;failed=[int]$play.'test-run'.failed}
        existingFailedTests = $failed
        newFailedTests = $newFailures
        changedFailureMessages = $messageChanges
        qaTests = @((@($edit.SelectNodes('//test-case')) + @($play.SelectNodes('//test-case'))) | Where-Object fullname -match 'MoonGraphicsQa' | ForEach-Object { @{name=$_.fullname;result=$_.result} })
        commercialPolishTests = @($edit.SelectNodes('//test-case') | Where-Object fullname -match 'CommercialPolishPolicyTests' | ForEach-Object { @{name=$_.fullname;result=$_.result} })
    }
    $report | ConvertTo-Json -Depth 8 | Out-File -Encoding utf8 'Docs/GraphicsQA/final-verification.json'
    $report | ConvertTo-Json -Depth 8
    if ($changed.Count -or $newFailures.Count -or $messageChanges.Count -or [int]$play.'test-run'.failed -or -not $report.mapMatchesBaseline) {
        throw 'Verification has a preservation change or a new/unclassified test failure.'
    }
}
finally { Pop-Location }
