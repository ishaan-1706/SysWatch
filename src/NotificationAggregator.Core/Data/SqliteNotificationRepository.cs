namespace NotificationAggregator.Core.Data;

using Interfaces;
using Models;
using Microsoft.Data.Sqlite;
using System.Text.Json;

/// <summary>
/// SQLite implementation of notification repository
/// </summary>
public class SqliteNotificationRepository : INotificationRepository
{
    private readonly string _connectionString;
    
    public SqliteNotificationRepository(string dbPath)
    {
        _connectionString = $"Data Source={dbPath};";
    }
    
    public async Task<int> AddAsync(Notification notification)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO Notifications 
                    (SourceId, Source, Severity, Title, Message, Tags, OccurredAt, IsAcknowledged, Metadata, IsFiltered)
                    VALUES (@SourceId, @Source, @Severity, @Title, @Message, @Tags, @OccurredAt, @IsAcknowledged, @Metadata, @IsFiltered);
                    SELECT last_insert_rowid();
                ";
                
                command.Parameters.AddWithValue("@SourceId", notification.SourceId);
                command.Parameters.AddWithValue("@Source", notification.Source);
                command.Parameters.AddWithValue("@Severity", (int)notification.Severity);
                command.Parameters.AddWithValue("@Title", notification.Title);
                command.Parameters.AddWithValue("@Message", notification.Message);
                command.Parameters.AddWithValue("@Tags", notification.Tags ?? "");
                command.Parameters.AddWithValue("@OccurredAt", notification.OccurredAt);
                command.Parameters.AddWithValue("@IsAcknowledged", notification.IsAcknowledged);
                command.Parameters.AddWithValue("@Metadata", notification.Metadata ?? "");
                command.Parameters.AddWithValue("@IsFiltered", notification.IsFiltered);
                
                return Convert.ToInt32(await command.ExecuteScalarAsync());
            }
        }
    }
    
    public async Task<(List<Notification> Items, int Total)> GetAsync(
        int pageNumber = 1,
        int pageSize = 50,
        SeverityLevel? minSeverity = null,
        string? source = null,
        bool? acknowledged = null,
        DateTime? since = null)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            
            // Get total count
            var countQuery = "SELECT COUNT(*) FROM Notifications WHERE IsFiltered = 0";
            var parameters = new List<SqliteParameter>();
            
            if (minSeverity.HasValue)
            {
                countQuery += " AND Severity >= @MinSeverity";
                parameters.Add(new SqliteParameter("@MinSeverity", (int)minSeverity.Value));
            }
            if (!string.IsNullOrEmpty(source))
            {
                countQuery += " AND Source = @Source";
                parameters.Add(new SqliteParameter("@Source", source));
            }
            if (acknowledged.HasValue)
            {
                countQuery += " AND IsAcknowledged = @Acknowledged";
                parameters.Add(new SqliteParameter("@Acknowledged", acknowledged.Value));
            }
            if (since.HasValue)
            {
                countQuery += " AND OccurredAt >= @Since";
                parameters.Add(new SqliteParameter("@Since", since.Value));
            }
            
            using (var command = connection.CreateCommand())
            {
                command.CommandText = countQuery;
                foreach (var param in parameters)
                    command.Parameters.Add(param);
                    
                var total = Convert.ToInt32(await command.ExecuteScalarAsync());
                
                // Get paginated results
                var skip = (pageNumber - 1) * pageSize;
                var query = "SELECT * FROM Notifications WHERE IsFiltered = 0";
                
                if (minSeverity.HasValue)
                    query += " AND Severity >= @MinSeverity";
                if (!string.IsNullOrEmpty(source))
                    query += " AND Source = @Source";
                if (acknowledged.HasValue)
                    query += " AND IsAcknowledged = @Acknowledged";
                if (since.HasValue)
                    query += " AND OccurredAt >= @Since";
                    
                query += " ORDER BY OccurredAt DESC LIMIT @Limit OFFSET @Offset";
                
                using (var selectCommand = connection.CreateCommand())
                {
                    selectCommand.CommandText = query;
                    foreach (var param in parameters)
                        selectCommand.Parameters.Add(param);
                    selectCommand.Parameters.AddWithValue("@Limit", pageSize);
                    selectCommand.Parameters.AddWithValue("@Offset", skip);
                    
                    using (var reader = await selectCommand.ExecuteReaderAsync())
                    {
                        var items = new List<Notification>();
                        while (await reader.ReadAsync())
                        {
                            items.Add(ReadNotification(reader));
                        }
                        return (items, total);
                    }
                }
            }
        }
    }
    
    public async Task AcknowledgeAsync(int notificationId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    UPDATE Notifications 
                    SET IsAcknowledged = 1, AcknowledgedAt = CURRENT_TIMESTAMP
                    WHERE Id = @Id
                ";
                command.Parameters.AddWithValue("@Id", notificationId);
                await command.ExecuteNonQueryAsync();
            }
        }
    }
    
    public async Task DeleteOlderThanAsync(DateTime cutoffDate)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    DELETE FROM Notifications 
                    WHERE OccurredAt < @CutoffDate AND IsAcknowledged = 1
                ";
                command.Parameters.AddWithValue("@CutoffDate", cutoffDate);
                await command.ExecuteNonQueryAsync();
            }
        }
    }
    
    public async Task<Dictionary<SeverityLevel, int>> GetUnacknowledgedCountBySeverityAsync()
    {
        var result = new Dictionary<SeverityLevel, int>();
        
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT Severity, COUNT(*) as Count 
                    FROM Notifications 
                    WHERE IsAcknowledged = 0 AND IsFiltered = 0
                    GROUP BY Severity
                ";
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var severity = (SeverityLevel)reader.GetInt32(0);
                        var count = reader.GetInt32(1);
                        result[severity] = count;
                    }
                }
            }
        }
        
        return result;
    }
    
    private static Notification ReadNotification(SqliteDataReader reader)
    {
        return new Notification
        {
            Id = reader.GetInt32(0),
            SourceId = reader.GetString(1),
            Source = reader.GetString(2),
            Severity = (SeverityLevel)reader.GetInt32(3),
            Title = reader.GetString(4),
            Message = reader.GetString(5),
            Tags = reader.GetString(6),
            OccurredAt = reader.GetDateTime(7),
            IngestedAt = reader.GetDateTime(8),
            IsAcknowledged = reader.GetBoolean(9),
            AcknowledgedAt = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
            Metadata = reader.IsDBNull(11) ? null : reader.GetString(11),
            IsFiltered = reader.GetBoolean(12)
        };
    }
}
