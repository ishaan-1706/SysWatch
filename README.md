# SysWatch

**System event watcher for Windows. Centralize, filter, and monitor everything.**

A reliable, production-ready notification aggregator service for Windows that collects events from Windows Event Logs, filters noise, and provides a unified view of what matters.

## Features

✅ **Multi-source Collection**
- Windows Event Logs (Application, System, Security, Custom)
- Custom webhook/API endpoints
- Extensible collector framework for other sources

✅ **Intelligent Filtering**
- User-defined rules to suppress, highlight, or tag notifications
- Wildcard pattern matching
- Severity-based filtering

✅ **Reliable Storage**
- SQLite database with automatic schema management
- 30-day retention with automatic cleanup
- Full audit trail

✅ **Real-time Aggregation**
- Background service runs 24/7
- Collects notifications every 30 seconds
- Fast, non-blocking collection

✅ **Production Ready**
- Runs as Windows Service
- Comprehensive logging (daily rotating)
- Error recovery and health checks
- Fully tested with xUnit

## Quick Start

### Prerequisites
- Windows 10/11 or Windows Server 2016+
- .NET 8.0 SDK or Runtime
- Administrator privileges (to run as Windows Service)

### Installation

1. **Download latest release**
   ```powershell
   # Or build from source
   git clone https://github.com/ishaan-1706/SysWatch.git
   cd SysWatch
   ```

2. **Build the service**
   ```powershell
   dotnet build -c Release
   ```

3. **Install as Windows Service**
   ```powershell
   # Run as Administrator
   .\scripts\install-service.ps1
   ```

4. **Start the service**
   ```powershell
   Start-Service -Name "NotificationAggregator"
   ```

5. **Verify it's running**
   ```powershell
   Get-Service -Name "NotificationAggregator"
   ```

### Configuration

Edit `%APPDATA%\NotificationAggregator\config.json`:

```json
{
  "windows-eventlog": {
    "enabled": "true",
    "logs": "Application;System;Security"
  },
  "custom-api": {
    "enabled": "true",
    "webhookUrl": "https://your-api.com/notifications",
    "apiKey": "your-api-key"
  }
}
```

### Adding Filters

Filters suppress noise, highlight important events, or tag notifications for categorization.

**Example: Suppress informational events**
```powershell
# Use the API or database directly (dashboard coming soon)
INSERT INTO NotificationFilters 
VALUES (NULL, 'Suppress Info', '*', '*', '*', 0, 0, '', 1, CURRENT_TIMESTAMP);
```

### Viewing Logs & Data

All data is stored in `%APPDATA%\NotificationAggregator\` (typically `C:\Users\YourUsername\AppData\Roaming\NotificationAggregator\`)

**View Logs**
```powershell
# Show last 50 lines of today's log
Get-Content "$env:APPDATA\NotificationAggregator\logs-*.txt" -Tail 50

# Open log file directly
Invoke-Item "$env:APPDATA\NotificationAggregator"
```

**Query Database**
```powershell
# Count notifications by severity
python "C:\path\to\SysWatch\scripts\query-db.py"

# Or use sqlite3 directly (if installed)
sqlite3 "$env:APPDATA\NotificationAggregator\notifications.db" "SELECT COUNT(*) FROM Notifications;"
```

**Files Created**
- `logs-YYYY-MM-DD.txt` - Daily rotating logs (30 days retention)
- `notifications.db` - SQLite database with all collected events
- `config.json` - Service configuration

## Architecture

```
┌─────────────────────────────────────────┐
│  Notification Aggregator Service (.NET) │
└──────────────┬──────────────────────────┘
               │
     ┌─────────┼─────────┬──────────┐
     ▼         ▼         ▼          ▼
 Windows   Custom API  File Watch  Webhooks
 Event Log           (Planned)      (Planned)
     │         │         │          │
     └─────────┴─────────┴──────────┘
               │
        ┌──────▼──────┐
        │  Filter     │
        │  Engine     │
        └──────┬──────┘
               │
        ┌──────▼──────┐
        │  SQLite DB  │
        │  (30d TTL)  │
        └──────┬──────┘
               │
     ┌─────────┴──────────┐
     ▼                    ▼
  REST API          HTML Dashboard
  (Planned)         (Planned)
```

## Development

### Building from Source

```powershell
# Clone repo
git clone https://github.com/yourusername/notification-aggregator.git
cd notification-aggregator

# Restore dependencies
dotnet restore

# Build
dotnet build -c Release

# Run tests
dotnet test

# Run as console app (for debugging)
dotnet run --project src/NotificationAggregator.Service
```

### Project Structure

```
src/
├── NotificationAggregator.Core/       # Models, interfaces, services
├── NotificationAggregator.Service/    # Windows Service entry point
├── NotificationAggregator.Collectors/ # Event collectors
└── NotificationAggregator.Tests/      # xUnit tests

web/
└── dashboard/                         # React dashboard (future)

scripts/
├── install-service.ps1               # Install as Windows Service
└── setup-db.sql                      # Database setup

docs/
├── ARCHITECTURE.md
├── API.md
├── CONFIGURATION.md
└── PLUGIN_DEVELOPMENT.md
```

### Core Concepts

**Notification**: A single event from any source with title, message, severity, and metadata.

**Collector**: Pluggable component that fetches notifications from a source (Windows Event Log, API, file, etc.). Implement `INotificationCollector`.

**Filter**: User-defined rule that suppresses, highlights, or tags notifications based on patterns.

**Repository**: Persistence layer (currently SQLite, can swap for PostgreSQL/SQL Server).

## Roadmap

- [ ] **Phase 1 (Current)**: Core service + Windows Event Log collector
- [ ] **Phase 2**: REST API + Web dashboard
- [ ] **Phase 3**: Discord/Teams/Slack integration
- [ ] **Phase 4**: Plugin system for custom collectors
- [ ] **Phase 5**: Multi-tenant cloud deployment

## Contributing

1. Fork the repo
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

MIT License - see [LICENSE](LICENSE) file.

## Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/notification-aggregator/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/notification-aggregator/discussions)

## Changelog

### v1.0.0 (Initial Release)
- Windows Event Log collector
- Filter engine with wildcard patterns
- SQLite persistence
- Windows Service support
- Comprehensive logging
- xUnit test suite

---

**Built with**: C# .NET 8 | SQLite | Serilog | xUnit
