#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Display a comprehensive summary of SysWatch service status, logs, and database statistics.

.DESCRIPTION
    Shows:
    - Service status and uptime
    - Recent log entries
    - Database statistics (total notifications, by severity, by source)
    - Recent events with timestamps
    - Disk usage

.EXAMPLE
    .\scripts\view-summary.ps1

.NOTES
    Run without Administrator if just viewing. Requires Administrator for starting/stopping service.
#>

param(
    [switch]$Minimal,  # Show minimal output
    [switch]$Json      # Output as JSON
)

$ErrorActionPreference = "Stop"

# Colors
$colors = @{
    Info    = "Cyan"
    Success = "Green"
    Warning = "Yellow"
    Error   = "Red"
    Header  = "Magenta"
}

function Write-Section {
    param([string]$Title)
    Write-Host "`n" -NoNewline
    Write-Host "═" * 60 -ForegroundColor $colors.Header
    Write-Host "  $Title" -ForegroundColor $colors.Header
    Write-Host "═" * 60 -ForegroundColor $colors.Header
}

function Write-InfoLine {
    param([string]$Label, [string]$Value)
    Write-Host ("{0,-30} {1}" -f $Label, $Value) -ForegroundColor $colors.Info
}

try {
    $appDataPath = "$env:APPDATA\NotificationAggregator"
    $dbPath = "$appDataPath\notifications.db"
    
    # Check if paths exist
    if (-not (Test-Path $appDataPath)) {
        Write-Host "Error: SysWatch not installed or not yet initialized." -ForegroundColor $colors.Error
        exit 1
    }

    $summary = @{}

    # ============ SERVICE STATUS ============
    Write-Section "SERVICE STATUS"
    
    $service = Get-Service -Name "NotificationAggregator" -ErrorAction SilentlyContinue
    if ($service) {
        $statusColor = if ($service.Status -eq "Running") { $colors.Success } else { $colors.Error }
        Write-Host ("Status: {0}" -f $service.Status) -ForegroundColor $statusColor
        Write-InfoLine "Start Type" $service.StartType
        Write-InfoLine "Service Name" $service.Name
        
        $summary.ServiceStatus = $service.Status
        $summary.ServiceStartType = $service.StartType
    } else {
        Write-Host "Service not found!" -ForegroundColor $colors.Error
        exit 1
    }

    # ============ RECENT LOGS ============
    Write-Section "RECENT LOGS (Last 10 entries)"
    
    $logFiles = Get-ChildItem "$appDataPath\logs-*.txt" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending
    
    if ($logFiles) {
        $recentLogs = Get-Content $logFiles[0] -Tail 10 -ErrorAction SilentlyContinue
        if ($recentLogs) {
            $recentLogs | ForEach-Object {
                Write-Host $_
            }
            $summary.LastLog = $recentLogs[-1]
        }
    } else {
        Write-Host "No logs found" -ForegroundColor $colors.Warning
    }

    # ============ DATABASE STATISTICS ============
    Write-Section "DATABASE STATISTICS"
    
    if (Test-Path $dbPath) {
        $dbSize = (Get-Item $dbPath).Length / 1MB
        Write-InfoLine "Database Size" ("{0:F2} MB" -f $dbSize)
        Write-InfoLine "Database Location" $dbPath
        
        # Query database using SQLite
        try {
            # Try using query-db.py first
            $pythonScript = Join-Path (Split-Path $PSCommandPath -Parent) "query-db.py"
            
            if (Test-Path $pythonScript) {
                $dbOutput = python $pythonScript 2>$null
                if ($dbOutput) {
                    Write-Host $dbOutput
                    $summary.DatabaseOutput = $dbOutput
                }
            } else {
                # Fallback: use direct SQLite query (if sqlite3 is installed)
                $countQuery = & sqlite3 $dbPath "SELECT COUNT(*) as total FROM Notifications;" 2>$null
                if ($countQuery) {
                    Write-InfoLine "Total Notifications" $countQuery
                    $summary.TotalNotifications = $countQuery
                }
            }
        } catch {
            Write-Host "Could not query database (Python/sqlite3 may not be installed)" -ForegroundColor $colors.Warning
            Write-Host "Run: python $pythonScript" -ForegroundColor $colors.Info
        }
    } else {
        Write-Host "Database not found. Service may not have initialized yet." -ForegroundColor $colors.Warning
    }

    # ============ LOG FILES ============
    Write-Section "LOG FILES"
    
    if ($logFiles) {
        $logFiles | Select-Object -First 5 | ForEach-Object {
            Write-InfoLine $_.Name ("{0:F2} KB, Modified: {1}" -f ($_.Length/1KB), $_.LastWriteTime)
        }
    } else {
        Write-Host "No log files found" -ForegroundColor $colors.Warning
    }

    # ============ CONFIGURATION ============
    Write-Section "CONFIGURATION"
    
    $configPath = "$appDataPath\config.json"
    if (Test-Path $configPath) {
        Write-InfoLine "Config Location" $configPath
        Write-Host "`nConfig Content:"
        Get-Content $configPath | ConvertFrom-Json | ConvertTo-Json -Depth 3 | Write-Host -ForegroundColor $colors.Info
    }

    # ============ QUICK COMMANDS ============
    Write-Section "QUICK COMMANDS"
    
    Write-Host "View logs in real-time (last 50):"
    Write-Host '  Get-Content "$env:APPDATA\NotificationAggregator\logs-*.txt" -Tail 50 -Wait' -ForegroundColor $colors.Info
    
    Write-Host "`nQuery database:"
    Write-Host "  python .\scripts\query-db.py" -ForegroundColor $colors.Info
    
    Write-Host "`nRestart service:"
    Write-Host "  Restart-Service -Name NotificationAggregator" -ForegroundColor $colors.Info
    
    Write-Host "`nView all data:"
    Write-Host '  Invoke-Item "$env:APPDATA\NotificationAggregator"' -ForegroundColor $colors.Info

    Write-Host "`n"

    # Return JSON if requested
    if ($Json) {
        return $summary | ConvertTo-Json
    }

} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor $colors.Error
    exit 1
}
