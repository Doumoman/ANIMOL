@echo off
setlocal
rem Launch a new editor with D3D11; an already-running editor must be closed first.
pushd "%~dp0.."
unity open "%CD%" --args "-force-d3d11 -executeMethod ANIMOL.Editor.CampaignMapEditorWindow.Open"
set "animolExitCode=%ERRORLEVEL%"
popd
if not "%animolExitCode%"=="0" pause
exit /b %animolExitCode%
