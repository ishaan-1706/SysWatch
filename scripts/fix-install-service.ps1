# Fix and Reinstall Notification Aggregator Service
# Must run as Administrator!

#Requires -RunAsAdministrator

Write-Host "Notification Aggregator Service Fix" -ForegroundColor Cyan
Write-Host "===================================" -ForegroundColor Cyan
Write-Host ""

$ServiceName = "NotificationAggregator"
$ProjectPath = "C:\data\notification-aggregator"

# Step 1: Stop and remove old service
Write-Host "Step 1: Removing old service..." -ForegroundColor Yellow
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($service) {
    Write-Host "  Found existing service. Stopping..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
    
    Write-Host "  Removing service..." -ForegroundColor Yellow
    $null = cmd /c "sc delete $ServiceName"
    Start-Sleep -Seconds 2
    Write-Host "  ✓ Old service removed" -ForegroundColor Green
} else {
    Write-Host "  No existing service found" -ForegroundColor Green
}

Write-Host ""

# Step 2: Build release
Write-Host "Step 2: Building release..." -ForegroundColor Yellow
Push-Location $ProjectPath
dotnet build -c Release 2>&1 | Where-Object { $_ -match "error|failed" } | ForEach-Object { Write-Host $_ -ForegroundColor Red }
Write-Host "  ✓ Build complete" -ForegroundColor Green
Pop-Location

Write-Host ""

# Step 3: Publish
Write-Host "Step 3: Publishing..." -ForegroundColor Yellow
$PublishPath = "$ProjectPath\publish"
dotnet publish "$ProjectPath\src\NotificationAggregator.Service\NotificationAggregator.Service.csproj" `
    -c Release -o $PublishPath | Out-Null
$ExePath = "$PublishPath\NotificationAggregator.Service.exe"

if (!(Test-Path $ExePath)) {
    Write-Host "  ✗ ERROR: Executable not found at $ExePath" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Published to $PublishPath" -ForegroundColor Green

Write-Host ""

# Step 4: Create new service with absolute path
Write-Host "Step 4: Creating new Windows Service..." -ForegroundColor Yellow
Write-Host "  Service name: $ServiceName" -ForegroundColor Gray
Write-Host "  Executable: $ExePath" -ForegroundColor Gray

New-Service -Name $ServiceName `
    -BinaryPathName "`"$ExePath`"" `
    -DisplayName "Notification Aggregator" `
    -Description "Consolidates system and app notifications into a single feed" `
    -StartupType Automatic `
    -ErrorAction Stop | Out-Null

Write-Host "  ✓ Service created" -ForegroundColor Green

Write-Host ""

# Step 5: Create app data directory
Write-Host "Step 5: Creating app data directories..." -ForegroundColor Yellow
$AppDataPath = "$env:APPDATA\NotificationAggregator"
New-Item -ItemType Directory -Path $AppDataPath -Force | Out-Null
Write-Host "  ✓ Created: $AppDataPath" -ForegroundColor Green

Write-Host ""

# Step 6: Verify and start
Write-Host "Step 6: Starting service..." -ForegroundColor Yellow
Start-Sleep -Seconds 1
Start-Service -Name $ServiceName -ErrorAction Stop
Start-Sleep -Seconds 2

$svc = Get-Service -Name $ServiceName
if ($svc.Status -eq "Running") {
    Write-Host "  ✓ Service is RUNNING" -ForegroundColor Green
} else {
    Write-Host "  ✗ Service status: $($svc.Status)" -ForegroundColor Red
    Write-Host "  Check logs for errors:" -ForegroundColor Yellow
    Write-Host "    $AppDataPath\logs-$(Get-Date -Format 'yyyy-MM-dd').txt" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Check service status: Get-Service -Name 'NotificationAggregator'" -ForegroundColor Gray
Write-Host "2. View logs: Get-Content '$AppDataPath\logs-$(Get-Date -Format 'yyyy-MM-dd').txt' -Tail 50" -ForegroundColor Gray
Write-Host "3. Query DB: sqlite3 '$AppDataPath\notifications.db' \"SELECT COUNT(*) FROM Notifications\"" -ForegroundColor Gray
Write-Host ""
