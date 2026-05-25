# Simple database query tool (no sqlite3 needed)
[Reflection.Assembly]::LoadWithPartialName("System.Data.SQLite") | Out-Null

$dbPath = "$env:APPDATA\NotificationAggregator\notifications.db"

if (!(Test-Path $dbPath)) {
    Write-Host "Database not found at $dbPath" -ForegroundColor Red
    exit 1
}

$connectionString = "Data Source=$dbPath;Version=3;"
$connection = New-Object System.Data.SQLite.SQLiteConnection($connectionString)
$connection.Open()

# Query 1: Total count
$cmd = $connection.CreateCommand()
$cmd.CommandText = "SELECT COUNT(*) as Total FROM Notifications"
$reader = $cmd.ExecuteReader()
$reader.Read() | Out-Null
$total = $reader["Total"]
$reader.Close()

Write-Host "📊 Notification Aggregator Stats" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host "Total Notifications: $total" -ForegroundColor Green

# Query 2: By Severity
Write-Host ""
Write-Host "By Severity:" -ForegroundColor Yellow
$cmd = $connection.CreateCommand()
$cmd.CommandText = @"
SELECT 
  CASE Severity 
    WHEN 0 THEN 'Info' 
    WHEN 1 THEN 'Warning' 
    WHEN 2 THEN 'Error' 
    WHEN 3 THEN 'Critical' 
  END as SeverityName,
  COUNT(*) as Count
FROM Notifications
GROUP BY Severity
ORDER BY Severity DESC
"@
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    $name = $reader["SeverityName"]
    $count = $reader["Count"]
    Write-Host "  $name : $count"
}
$reader.Close()

# Query 3: By Source
Write-Host ""
Write-Host "By Source:" -ForegroundColor Yellow
$cmd = $connection.CreateCommand()
$cmd.CommandText = "SELECT Source, COUNT(*) as Count FROM Notifications GROUP BY Source"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    $source = $reader["Source"]
    $count = $reader["Count"]
    Write-Host "  $source : $count"
}
$reader.Close()

# Query 4: Recent events
Write-Host ""
Write-Host "Recent Events:" -ForegroundColor Yellow
$cmd = $connection.CreateCommand()
$cmd.CommandText = "SELECT Title, OccurredAt, Severity FROM Notifications ORDER BY OccurredAt DESC LIMIT 10"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    $title = $reader["Title"]
    $time = $reader["OccurredAt"]
    $sev = @("Info", "Warning", "Error", "Critical")[$reader["Severity"]]
    Write-Host "  [$sev] $title (at $time)"
}
$reader.Close()

$connection.Close()
Write-Host ""
Write-Host "✓ Service is working!" -ForegroundColor Green
