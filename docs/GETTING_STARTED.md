# Getting Started Guide

## Installation

### Step 1: Verify Prerequisites
```powershell
dotnet --version
# Should show 8.0.x or higher

git --version
# Should show a recent version
```

If you don't have .NET 8 SDK installed, [download it here](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

### Step 2: Clone or Download
```powershell
# Clone from GitHub
git clone https://github.com/yourusername/notification-aggregator.git
cd notification-aggregator

# Or download zip and extract
```

### Step 3: Build the Project
```powershell
# Restore NuGet packages
dotnet restore

# Build in Release mode
dotnet build -c Release

# Run tests (optional but recommended)
dotnet test -c Release
```

### Step 4: Install as Windows Service
```powershell
# Must run as Administrator
.\scripts\install-service.ps1
```

This will:
1. Build the release version
2. Create a Windows Service named "NotificationAggregator"
3. Set it to auto-start on boot

### Step 5: Start the Service
```powershell
# Start
Start-Service -Name "NotificationAggregator"

# Verify it's running
Get-Service -Name "NotificationAggregator"

# View recent logs (follow mode)
Get-Content -Path "$env:APPDATA\NotificationAggregator\logs-*.txt" -Tail 20 -Wait
```

## Configuration

1. **Default config location**: `%APPDATA%\NotificationAggregator\config.json`

2. **Edit config** (optional, defaults work):
   ```powershell
   notepad "$env:APPDATA\NotificationAggregator\config.json"
   ```

3. **Restart service** if you changed config:
   ```powershell
   Restart-Service -Name "NotificationAggregator"
   ```

See [CONFIGURATION.md](./CONFIGURATION.md) for detailed options.

## Running in Development

To test before installing as a service:

```powershell
# Run as console app (for debugging)
dotnet run --project src/NotificationAggregator.Service

# Press Ctrl+C to stop
```

Logs will appear in console.

## Verifying It Works

1. **Check service status**:
   ```powershell
   Get-Service -Name "NotificationAggregator" | Select-Object Status, StartType
   ```

2. **Check logs for errors**:
   ```powershell
   Get-Content "$env:APPDATA\NotificationAggregator\logs-$(Get-Date -Format 'yyyy-MM-dd').txt" -Tail 50
   ```

3. **Query collected notifications** (using SQLite):
   ```powershell
   # Install sqlite3 first: choco install sqlite
   sqlite3 "$env:APPDATA\NotificationAggregator\notifications.db" `
     "SELECT COUNT(*) as Total FROM Notifications"
   ```

## Uninstalling

```powershell
# Stop the service
Stop-Service -Name "NotificationAggregator" -Force

# Remove the service
Remove-Service -Name "NotificationAggregator" -Force

# Delete data (optional)
Remove-Item "$env:APPDATA\NotificationAggregator" -Recurse -Force
```

## Next Steps

- Read [ARCHITECTURE.md](./ARCHITECTURE.md) to understand the system
- Review [CONFIGURATION.md](./CONFIGURATION.md) to customize behavior
- Create filters to suppress noise
- (Coming soon) Use the web dashboard to view and manage notifications

## Common Issues

**Service fails to start**
- Check logs: `Get-Content "$env:APPDATA\NotificationAggregator\logs-*.txt" -Tail 100`
- Ensure you ran install script as Administrator
- Verify .NET 8 SDK is installed: `dotnet --list-sdks`

**No notifications appearing**
- Enable more event logs in config.json
- Check database: `sqlite3 notifications.db "SELECT COUNT(*) FROM Notifications"`
- Verify filters aren't suppressing everything

**High memory usage**
- Reduce retention period in code (default 30 days)
- Add filters to suppress noise
- Lower collection frequency

**Still stuck?**
- File an issue on GitHub with logs attached
- Check service status: `Get-Service -Name "NotificationAggregator"`
- Review event logs: `Get-EventLog -LogName Application -Source NotificationAggregator -Newest 10`
