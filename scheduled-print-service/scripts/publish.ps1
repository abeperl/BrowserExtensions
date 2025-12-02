param(
  [string]$Configuration = "Release",
  [string]$Runtime = "win-x64",
  [switch]$SelfContained
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $PSScriptRoot
$project = Join-Path $scriptDir 'ScheduledPrintService\ScheduledPrintService.csproj'
$outDir = Join-Path $scriptDir "publish"

$sc = $false
if ($SelfContained) { $sc = $true }

Write-Host "Publishing ScheduledPrintService ($Configuration, $Runtime, SelfContained=$sc)" -ForegroundColor Cyan

if ($sc) { $scArg = '-p:SelfContained=true' } else { $scArg = '-p:SelfContained=false' }

& dotnet publish $project -c $Configuration -r $Runtime $scArg -p:PublishSingleFile=false -p:IncludeNativeLibrariesForSelfExtract=true -o $outDir

Write-Host "Output: $outDir" -ForegroundColor Green
Write-Host "Files:" -ForegroundColor DarkCyan
Get-ChildItem -Recurse $outDir | Select-Object FullName, Length | Format-Table -AutoSize
