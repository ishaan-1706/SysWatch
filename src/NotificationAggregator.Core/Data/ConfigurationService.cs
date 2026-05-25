namespace NotificationAggregator.Core.Data;

using Interfaces;
using Models;
using Microsoft.Data.Sqlite;
using System.Text.Json;

/// <summary>
/// Configuration service using JSON + SQLite
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly string _configPath;
    private readonly string _connectionString;
    private Dictionary<string, Dictionary<string, string>> _collectorConfigs = new();
    
    public ConfigurationService(string configPath, string dbPath)
    {
        _configPath = configPath;
        _connectionString = $"Data Source={dbPath};";
        LoadCollectorConfigs();
    }
    
    private void LoadCollectorConfigs()
    {
        if (File.Exists(_configPath))
        {
            try
            {
                var json = File.ReadAllText(_configPath);
                _collectorConfigs = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json) 
                    ?? new Dictionary<string, Dictionary<string, string>>();
            }
            catch
            {
                _collectorConfigs = new Dictionary<string, Dictionary<string, string>>();
            }
        }
    }
    
    public Dictionary<string, Dictionary<string, string>> GetCollectorConfigs()
    {
        return _collectorConfigs;
    }
    
    public async Task<List<NotificationFilter>> GetFiltersAsync()
    {
        var filters = new List<NotificationFilter>();
        
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM NotificationFilters ORDER BY Id";
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        filters.Add(new NotificationFilter
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            SourcePattern = reader.GetString(2),
                            TitlePattern = reader.GetString(3),
                            MessagePattern = reader.GetString(4),
                            MinSeverity = (SeverityLevel)reader.GetInt32(5),
                            Action = (FilterAction)reader.GetInt32(6),
                            ActionValue = reader.GetString(7),
                            IsEnabled = reader.GetBoolean(8),
                            CreatedAt = reader.GetDateTime(9)
                        });
                    }
                }
            }
        }
        
        return filters;
    }
    
    public async Task SaveFilterAsync(NotificationFilter filter)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                if (filter.Id == 0)
                {
                    command.CommandText = @"
                        INSERT INTO NotificationFilters 
                        (Name, SourcePattern, TitlePattern, MessagePattern, MinSeverity, Action, ActionValue, IsEnabled)
                        VALUES (@Name, @SourcePattern, @TitlePattern, @MessagePattern, @MinSeverity, @Action, @ActionValue, @IsEnabled)
                    ";
                }
                else
                {
                    command.CommandText = @"
                        UPDATE NotificationFilters 
                        SET Name = @Name, SourcePattern = @SourcePattern, TitlePattern = @TitlePattern, 
                            MessagePattern = @MessagePattern, MinSeverity = @MinSeverity, Action = @Action, 
                            ActionValue = @ActionValue, IsEnabled = @IsEnabled
                        WHERE Id = @Id
                    ";
                    command.Parameters.AddWithValue("@Id", filter.Id);
                }
                
                command.Parameters.AddWithValue("@Name", filter.Name);
                command.Parameters.AddWithValue("@SourcePattern", filter.SourcePattern);
                command.Parameters.AddWithValue("@TitlePattern", filter.TitlePattern);
                command.Parameters.AddWithValue("@MessagePattern", filter.MessagePattern);
                command.Parameters.AddWithValue("@MinSeverity", (int)filter.MinSeverity);
                command.Parameters.AddWithValue("@Action", (int)filter.Action);
                command.Parameters.AddWithValue("@ActionValue", filter.ActionValue ?? "");
                command.Parameters.AddWithValue("@IsEnabled", filter.IsEnabled);
                
                await command.ExecuteNonQueryAsync();
            }
        }
    }
    
    public async Task DeleteFilterAsync(int filterId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM NotificationFilters WHERE Id = @Id";
                command.Parameters.AddWithValue("@Id", filterId);
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
