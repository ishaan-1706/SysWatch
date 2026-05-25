# Simple service installation script
# Run as Administrator!

#Requires -RunAsAdministrator

Write-Host "Installing Notification Aggregator Service" -ForegroundColor Green
Write-Host ""

$ServiceName = "NotificationAggregator"
$ProjectPath = "C:\data\notification-aggregator"
$PublishPath = "$ProjectPath\publish"
$ExePath = "$PublishPath\NotificationAggregator.Service.exe"

# Build
Write-Host "Building..." -ForegroundColor Yellow
Push-Location $ProjectPath
dotnet build -c Release | Out-Null
Pop-Location

# Publish
Write-Host "Publishing..." -ForegroundColor Yellow
dotnet publish "$ProjectPath\src\NotificationAggregator.Service\NotificationAggregator.Service.csproj" `
    -c Release -o $PublishPath -q

# Verify exe exists
if (!(Test-Path $ExePath)) {
    Write-Host "ERROR: $ExePath not found" -ForegroundColor Red
    exit 1
}

Write-Host "Executable ready: $ExePath" -ForegroundColor Green

# Create app data folder
New-Item -ItemType Directory -Path "$env:APPDATA\NotificationAggregator" -Force | Out-Null

# Create service using sc.exe
Write-Host "Creating Windows Service..." -ForegroundColor Yellow

$binPath = "`"$ExePath`""
Write-Host "Service command: sc.exe create" -ForegroundColor Gray
$result = cmd /c "sc.exe create $ServiceName binPath= $binPath start= auto DisplayName= `"Notification Aggregator`""
Write-Host $result

if ($result -match "success" -or $result -match "exists") {
    Write-Host "Service created/exists. Starting..." -ForegroundColor Green
    Start-Sleep -Seconds 1
    Start-Service -Name $ServiceName
    Start-Sleep -Seconds 2
    
    $svc = Get-Service -Name $ServiceName
    Write-Host "Status: $($svc.Status)" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "DONE! Check logs:" -ForegroundColor Cyan
    Write-Host "  Get-Content `"$env:APPDATA\NotificationAggregator\logs-$(Get-Date -Format 'yyyy-MM-dd').txt`" -Tail 50"
} else {
    Write-Host "Failed to create service:" -ForegroundColor Red
    Write-Host $result
}
