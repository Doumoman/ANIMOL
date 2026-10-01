$ErrorActionPreference = 'Stop'
Push-Location (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
try {
    $baseline = Get-Content -Raw -Encoding UTF8 'Docs/RunPrototype/baseline.json' | ConvertFrom-Json
    $changed = @($baseline.files + $baseline.protected | Where-Object {
        -not (Test-Path -LiteralPath $_.path -PathType Leaf) -or (Get-FileHash -LiteralPath $_.path).Hash -ne $_.sha256
    } | ForEach-Object { $_.path })
    [xml]$before = Get-Content -Raw 'Docs/GraphicsQA/final-editmode.xml'
    [xml]$edit = Get-Content -Raw 'Docs/RunPrototype/final-editmode.xml'
    [xml]$play = Get-Content -Raw 'Docs/RunPrototype/final-playmode.xml'
    $known = @($before.SelectNodes('//test-case[@result="Failed"]') | ForEach-Object { $_.fullname })
    $failed = @($edit.SelectNodes('//test-case[@result="Failed"]') | ForEach-Object { $_.fullname })
    $newFailures = @($failed | Where-Object { $_ -notin $known })
    $changedMessages = @($edit.SelectNodes('//test-case[@result="Failed"]') | ForEach-Object {
        $test = $_
        $old = $before.SelectNodes('//test-case') | Where-Object fullname -eq $test.fullname
        if ($old -and $old.SelectSingleNode('failure/message').InnerText -ne $test.SelectSingleNode('failure/message').InnerText) { $test.fullname }
    })
    $runs = @(Get-ChildItem 'Docs/RunPrototype/Telemetry/*.json' | ForEach-Object { Get-Content -Raw $_.FullName | ConvertFrom-Json })
    if ($runs.Count -ne 5) { throw 'Expected four complete traversals and one exhaustion probe.' }
    foreach ($run in $runs) {
        if (-not $run.PSObject.Properties['unexpectedAirborneSteps']) { throw 'Stale telemetry schema; rerun latest PlayMode tests.' }
        if ($run.strategy -eq 'RunUntilExhausted') {
            if (-not $run.exhausted -or $run.completed) { throw 'Exhaustion probe has unexpected result.' }
        } elseif (-not $run.completed -or -not $run.landingInsideReservation -or $run.unexpectedAirborneSteps -or $run.exhausted) {
            throw ('Route failed: ' + $run.scenario + '/' + $run.strategy)
        }
    }
    $summary = [ordered]@{
        baselineHead=$baseline.head; unity='6000.3.8f1'; chunkTiles=16; requiredRunLegTiles=80
        existingFilesChecked=$baseline.files.Count; existingFilesChanged=$changed
        protectedFiles=$baseline.protected
        editMode=@{total=[int]$edit.'test-run'.total;passed=[int]$edit.'test-run'.passed;failed=[int]$edit.'test-run'.failed}
        playMode=@{total=[int]$play.'test-run'.total;passed=[int]$play.'test-run'.passed;failed=[int]$play.'test-run'.failed}
        existingFailures=$failed; newFailures=$newFailures; changedFailureMessages=$changedMessages
        commercialPolicyTests=@($edit.SelectNodes('//test-case') | Where-Object fullname -match 'CommercialPolishPolicyTests' | ForEach-Object { @{name=$_.fullname;result=$_.result} })
        prototypeTests=@((@($edit.SelectNodes('//test-case')) + @($play.SelectNodes('//test-case'))) | Where-Object fullname -match 'RunPrototype' | ForEach-Object { @{name=$_.fullname;result=$_.result} })
        runs=$runs
    }
    $summary | ConvertTo-Json -Depth 8 | Out-File -Encoding utf8 'Docs/RunPrototype/final-verification.json'
    $summary | ConvertTo-Json -Depth 8
    if ($changed.Count -or $newFailures.Count -or $changedMessages.Count -or [int]$play.'test-run'.failed) { throw 'Preservation or regression verification failed.' }
}
finally { Pop-Location }
