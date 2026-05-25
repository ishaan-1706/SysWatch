param(
    [string]$Time = "17:00",
    [string]$TaskName = "SysWatchDailySummary"
)

$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "Error: Requires Administrator privileges"
    exit 1
}

$ErrorActionPreference = "Stop"

try {
    $timeObj = [datetime]::ParseExact($Time, "HH:mm", [cultureinfo]::InvariantCulture)
    $trigger = New-ScheduledTaskTrigger -Daily -At $timeObj
} catch {
    Write-Host "Error: Invalid time format. Use HH:mm"
    exit 1
}

$scriptDir = Split-Path $PSCommandPath -Parent
$scriptPath = Join-Path $scriptDir "view-summary.ps1"

if (-not (Test-Path $scriptPath)) {
    Write-Host "Error: view-summary.ps1 not found"
    exit 1
}

$reportDir = "$env:APPDATA\NotificationAggregator\reports"
if (-not (Test-Path $reportDir)) {
    New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
}

$timestamp = Get-Date -Format "yyyy-MM-dd"
$logFile = "$reportDir\daily-summary-$timestamp.txt"

$action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-NoProfile -ExecutionPolicy Bypass -File $scriptPath >> $logFile"

$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable

$existing = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "Updating task..."
    Set-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings | Out-Null
} else {
    Write-Host "Creating task..."
    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings -Description "Daily SysWatch summary" -Force | Out-Null
}

Write-Host "Done!"
Write-Host ""
Write-Host "Task: $TaskName"
Write-Host "Time: $Time daily"
Write-Host "Reports: $reportDir"
Write-Host ""

$task = Get-ScheduledTask -TaskName $TaskName
$info = Get-ScheduledTaskInfo -InputObject $task
Write-Host "Next run: $($info.NextRunTime)"
Write-Host ""
Write-Host "Commands:"
Write-Host "Get-ScheduledTask -TaskName $TaskName"
Write-Host "Start-ScheduledTask -TaskName $TaskName"
Write-Host "Unregister-ScheduledTask -TaskName $TaskName -Confirm:0"
