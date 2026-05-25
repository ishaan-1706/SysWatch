# Quick Start

## 5-Minute Setup

### Step 1: Build
```powershell
cd c:\data\notification-aggregator
dotnet build -c Release
```

### Step 2: Install as Windows Service
```powershell
# Run as Administrator
.\scripts\install-service.ps1
```

### Step 3: Start Service
```powershell
Start-Service -Name "NotificationAggregator"
Get-Service -Name "NotificationAggregator"  # Should show "Running"
```

### Step 4: Check Logs
```powershell
Get-Content "$env:APPDATA\NotificationAggregator\logs-$(Get-Date -Format 'yyyy-MM-dd').txt" -Tail 50
```

## Verify It Works

**Query the database:**
```powershell
# Install SQLite if needed: choco install sqlite
sqlite3 "$env:APPDATA\NotificationAggregator\notifications.db" `
  "SELECT COUNT(*) as Notifications FROM Notifications"
```

**Check service status:**
```powershell
Get-Service -Name "NotificationAggregator" | 
  Select-Object Name, Status, StartType
```

## Configuration

Edit: `%APPDATA%\NotificationAggregator\config.json`

Then restart: `Restart-Service -Name "NotificationAggregator"`

## Run in Debug Mode (Console)
```powershell
dotnet run --project src/NotificationAggregator.Service
# Press Ctrl+C to stop
```

---

**See [docs/GETTING_STARTED.md](docs/GETTING_STARTED.md) for full guide.**
