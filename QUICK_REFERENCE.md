# Quick Reference

## Check Service Status
```powershell
Get-Service -Name "NotificationAggregator"
```

## View Logs
```powershell
Get-Content "$env:APPDATA\NotificationAggregator\logs-*.txt" -Tail 50
```

## Query Notifications
```powershell
python "C:\data\notification-aggregator\scripts\query-db.py"
```

## Stop/Start Service
```powershell
Stop-Service -Name "NotificationAggregator"
Start-Service -Name "NotificationAggregator"
```

## Restart Service
```powershell
Restart-Service -Name "NotificationAggregator"
```

## View Database Directly (if sqlite3 installed)
```powershell
sqlite3 "$env:APPDATA\NotificationAggregator\notifications.db"
sqlite> SELECT COUNT(*) FROM Notifications;
sqlite> SELECT Title, OccurredAt FROM Notifications LIMIT 10;
```

## Modify Collection Frequency
Edit `src/NotificationAggregator.Service/AggregatorBackgroundService.cs`:
```csharp
// Currently checks every 30 seconds
_collectionTimer = new Timer(..., null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

// Change to 60 seconds:
_collectionTimer = new Timer(..., null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
```

Then rebuild and restart service.
