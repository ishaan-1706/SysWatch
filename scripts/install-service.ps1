# Installation Script for Windows Service

# Must run as Administrator
#Requires -RunAsAdministrator

param(
    [Parameter(Mandatory=$false)]
    [string]$ServiceName = "NotificationAggregator",
    
    [Parameter(Mandatory=$false)]
    [string]$PublishPath = ".\publish"
)

Write-Host "Installing Notification Aggregator as Windows Service..." -ForegroundColor Cyan

# Check if service already exists
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "Service already exists. Stopping and removing..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    Remove-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

# Publish application
Write-Host "Building release version..." -ForegroundColor Cyan
$PublishPathFull = (Resolve-Path $PublishPath).Path
dotnet publish src/NotificationAggregator.Service/NotificationAggregator.Service.csproj -c Release -o $PublishPathFull

$exePath = Join-Path $PublishPathFull "NotificationAggregator.Service.exe"

if (!(Test-Path $exePath)) {
    Write-Host "ERROR: Executable not found at $exePath" -ForegroundColor Red
    exit 1
}

Write-Host "Executable path: $exePath" -ForegroundColor Green

# Create Windows Service with ABSOLUTE path
Write-Host "Creating Windows Service..." -ForegroundColor Cyan
New-Service -Name $ServiceName `
    -BinaryPathName "`"$exePath`"" `
    -DisplayName "Notification Aggregator" `
    -Description "Consolidates system and app notifications into a single feed" `
    -StartupType Automatic | Out-Null

Write-Host "Service installed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Review configuration: %APPDATA%\NotificationAggregator\config.json"
Write-Host "2. Start the service: Start-Service -Name '$ServiceName'"
Write-Host "3. Check logs: %APPDATA%\NotificationAggregator\logs-*.txt"
Write-Host "4. Monitor service: Get-Service -Name '$ServiceName'"
