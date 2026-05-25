namespace NotificationAggregator.Core.Data;

using Microsoft.Data.Sqlite;

/// <summary>
/// SQLite database initialization and schema management
/// </summary>
public class DatabaseInitializer
{
    private readonly string _connectionString;
    
    public DatabaseInitializer(string dbPath)
    {
        _connectionString = $"Data Source={dbPath};";
    }
    
    public void Initialize()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            
            // Create tables
            ExecuteScript(connection, GetCreateTablesScript());
            
            // Create indices
            ExecuteScript(connection, GetCreateIndicesScript());
        }
    }
    
    private void ExecuteScript(SqliteConnection connection, string script)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = script;
            command.ExecuteNonQuery();
        }
    }
    
    private string GetCreateTablesScript()
    {
        return @"
CREATE TABLE IF NOT EXISTS Notifications (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SourceId TEXT NOT NULL,
    Source TEXT NOT NULL,
    Severity INTEGER NOT NULL,
    Title TEXT NOT NULL,
    Message TEXT NOT NULL,
    Tags TEXT,
    OccurredAt DATETIME NOT NULL,
    IngestedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IsAcknowledged BOOLEAN NOT NULL DEFAULT 0,
    AcknowledgedAt DATETIME,
    Metadata TEXT,
    IsFiltered BOOLEAN NOT NULL DEFAULT 0,
    UNIQUE(Source, SourceId)
);

CREATE TABLE IF NOT EXISTS NotificationFilters (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    SourcePattern TEXT NOT NULL DEFAULT '*',
    TitlePattern TEXT NOT NULL DEFAULT '*',
    MessagePattern TEXT NOT NULL DEFAULT '*',
    MinSeverity INTEGER NOT NULL DEFAULT 0,
    Action INTEGER NOT NULL,
    ActionValue TEXT,
    IsEnabled BOOLEAN NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS AuditLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Action TEXT NOT NULL,
    Details TEXT,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);
";
    }
    
    private string GetCreateIndicesScript()
    {
        return @"
CREATE INDEX IF NOT EXISTS idx_notifications_source ON Notifications(Source);
CREATE INDEX IF NOT EXISTS idx_notifications_severity ON Notifications(Severity);
CREATE INDEX IF NOT EXISTS idx_notifications_ingestedat ON Notifications(IngestedAt);
CREATE INDEX IF NOT EXISTS idx_notifications_isacknowledged ON Notifications(IsAcknowledged);
CREATE INDEX IF NOT EXISTS idx_notifications_isfiltered ON Notifications(IsFiltered);
CREATE INDEX IF NOT EXISTS idx_filters_enabled ON NotificationFilters(IsEnabled);
";
    }
}
