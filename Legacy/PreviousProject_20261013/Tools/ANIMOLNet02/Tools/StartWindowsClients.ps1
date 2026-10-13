param(
    [Parameter(Mandatory=$true)][string]$Executable,
    [ValidateRange(1,4)][int]$Count = 3
)
$ErrorActionPreference = "Stop"
$resolvedExe = (Resolve-Path -LiteralPath $Executable).Path
if (!(Test-Path -LiteralPath $resolvedExe -PathType Leaf)) { throw "DEV client executable is missing" }
for ($index = 1; $index -le $Count; $index++) {
    $slot = "PC{0:D2}" -f $index
    $process = Start-Process -FilePath $resolvedExe -ArgumentList @("-animolNet02Slot", $slot) -PassThru
    Write-Host "Started $slot (PID $($process.Id)). Choose a different configured DEV account in each window."
}
Write-Host "The existing UI bootstrap must bind NET02 service; each slot isolates its request journal."
Write-Host "Close these client windows normally. No unrelated process will be stopped."
