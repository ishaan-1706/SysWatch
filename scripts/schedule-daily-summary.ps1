#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Schedule the SysWatch summary report to run daily.

.DESCRIPTION
    Creates a Windows scheduled task that runs view-summary.ps1 daily at a specified time.
    Results are saved to a daily report file.

.PARAMETER Time
    Time to run the task (default: 17:00 / 5 PM)
    Format: HH:mm (24-hour)

.PARAMETER TaskName
    Name of the scheduled task (default: SysWatchDailySummary)

.EXAMPLE
    .\scripts\schedule-daily-summary.ps1 -Time "17:00"
    
.EXAMPLE
    .\scripts\schedule-daily-summary.ps1 -Time "20:00" -TaskName "MyCustomTask"

.NOTES
    Requires Administrator privileges.
    Reports are saved to: %APPDATA%\NotificationAggregator\reports\
#>

param(
    [string]$Time = "17:00",
    [string]$TaskName = "SysWatchDailySummary"
)

# Verify we're running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "Error: This script requires Administrator privileges." -ForegroundColor Red
    Write-Host "Please run PowerShell as Administrator." -ForegroundColor Red
    exit 1
}

$ErrorActionPreference = "Stop"

# Parse time
try {
    $timeObj = [datetime]::ParseExact($Time, "HH:mm", [cultureinfo]::InvariantCulture)
    $trigger = New-ScheduledTaskTrigger -Daily -At $timeObj
} catch {
    Write-Host "Error: Invalid time format. Use HH:mm (e.g., 17:00)" -ForegroundColor Red
    exit 1
}

# Script to run
$scriptPath = Join-Path (Split-Path $PSCommandPath -Parent) "view-summary.ps1"
if (-not (Test-Path $scriptPath)) {
    Write-Host "Error: view-summary.ps1 not found at $scriptPath" -ForegroundColor Red
    exit 1
}

# Create report directory
$reportDir = "$env:APPDATA\NotificationAggregator\reports"
if (-not (Test-Path $reportDir)) {
    New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
}

# Action: Run PowerShell with the script
$action = New-ScheduledTaskAction `
    -Execute "powershell.exe" `
    -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$scriptPath`" >> `"$reportDir\daily-summary-$(Get-Date -Format 'yyyy-MM-dd').txt`""

# Settings
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable

# Check if task already exists
$existingTask = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if ($existingTask) {
    Write-Host "Task '$TaskName' already exists. Updating..." -ForegroundColor Yellow
    Set-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings | Out-Null
} else {
    Write-Host "Creating new scheduled task '$TaskName'..." -ForegroundColor Cyan
    Register-ScheduledTask `
        -TaskName $TaskName `
        -Action $action `
        -Trigger $trigger `
        -Settings $settings `
        -Description "Daily SysWatch summary report" `
        -Force | Out-Null
}

Write-Host "✓ Scheduled task created successfully!" -ForegroundColor Green
Write-Host "`nTask Details:" -ForegroundColor Cyan
Write-Host "  Name:        $TaskName"
Write-Host "  Time:        $Time (daily)"
Write-Host "  Script:      $scriptPath"
Write-Host "  Reports:     $reportDir"
Write-Host "`nNext Run:" -ForegroundColor Cyan

$task = Get-ScheduledTask -TaskName $TaskName
$taskInfo = Get-ScheduledTaskInfo -InputObject $task
Write-Host "  $($taskInfo.NextRunTime)"

Write-Host "`nManage Task:" -ForegroundColor Cyan
Write-Host "  View task:    Get-ScheduledTask -TaskName '$TaskName'"
Write-Host "  Run now:      Start-ScheduledTask -TaskName '$TaskName'"
Write-Host "  Remove task:  Unregister-ScheduledTask -TaskName '$TaskName' -Confirm:`$false"
