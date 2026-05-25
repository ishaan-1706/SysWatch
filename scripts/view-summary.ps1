param(
    [switch]$Minimal,
    [switch]$Json
)

$ErrorActionPreference = "Stop"

$colors = @{
    Info = "Cyan"
    Success = "Green"
    Warning = "Yellow"
    Error = "Red"
    Header = "Magenta"
}

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host "--- $Title ---" -ForegroundColor $colors.Header
}

function Write-InfoLine {
    param([string]$Label, [string]$Value)
    Write-Host ("{0,-30} {1}" -f $Label, $Value) -ForegroundColor $colors.Info
}

try {
    $appDataPath = "$env:APPDATA\NotificationAggregator"
    $dbPath = "$appDataPath\notifications.db"
    
    if (-not (Test-Path $appDataPath)) {
        Write-Host "Error: SysWatch not installed" -ForegroundColor $colors.Error
        exit 1
    }

    $summary = @{}

    Write-Section "SERVICE STATUS"
    
    $service = Get-Service -Name "NotificationAggregator" -ErrorAction SilentlyContinue
    if ($service) {
        Write-Host "Status: $($service.Status)"
        Write-Host "Start Type: $($service.StartType)"
        $summary.ServiceStatus = $service.Status
    } else {
        Write-Host "Service not found!" -ForegroundColor $colors.Error
        exit 1
    }

    Write-Section "RECENT LOGS"
    
    $logFiles = Get-ChildItem "$appDataPath\logs-*.txt" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending
    
    if ($logFiles) {
        $recentLogs = Get-Content $logFiles[0] -Tail 10 -ErrorAction SilentlyContinue
        if ($recentLogs) {
            $recentLogs | ForEach-Object { Write-Host $_ }
        }
    } else {
        Write-Host "No logs found" -ForegroundColor $colors.Warning
    }

    Write-Section "DATABASE"
    
    if (Test-Path $dbPath) {
        $dbSize = (Get-Item $dbPath).Length / 1MB
        Write-Host "Size: $([math]::Round($dbSize, 2)) MB"
        Write-Host "Location: $dbPath"
        
        try {
            $pythonScript = Join-Path (Split-Path $PSCommandPath -Parent) "query-db.py"
            if (Test-Path $pythonScript) {
                $dbOutput = python $pythonScript 2>$null
                if ($dbOutput) {
                    Write-Host $dbOutput
                }
            }
        } catch {
            Write-Host "Could not query database" -ForegroundColor $colors.Warning
        }
    } else {
        Write-Host "Database not found" -ForegroundColor $colors.Warning
    }

    Write-Section "LOG FILES"
    
    if ($logFiles) {
        $logFiles | Select-Object -First 5 | ForEach-Object {
            $size = [math]::Round($_.Length / 1KB, 2)
            Write-Host "$($_.Name) - $size KB"
        }
    }

    Write-Section "QUICK COMMANDS"
    
    Write-Host "View logs live:"
    Write-Host '  Get-Content "$env:APPDATA\NotificationAggregator\logs-*.txt" -Tail 50 -Wait'
    Write-Host ""
    Write-Host "Query database:"
    Write-Host "  python .\scripts\query-db.py"
    Write-Host ""
    Write-Host "Restart service:"
    Write-Host "  Restart-Service -Name NotificationAggregator"
    Write-Host ""
    Write-Host "Open data folder:"
    Write-Host '  Invoke-Item "$env:APPDATA\NotificationAggregator"'
    Write-Host ""

    if ($Json) {
        return $summary | ConvertTo-Json
    }

} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor $colors.Error
    exit 1
}
