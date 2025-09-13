using Dapper;
using System.Data;
using AuditTrail.Core.Entities.Audit;
using AuditTrail.Core.Interfaces;
using AuditTrail.Infrastructure.Data;

namespace AuditTrail.Infrastructure.Repositories;

// Helper class for stored procedure result mapping
public class AuditIdResult
{
    public Guid AuditId { get; set; }
}

public class AuditRepository : IAuditRepository
{
    private readonly IDapperContext _dapperContext;

    public AuditRepository(IDapperContext dapperContext)
    {
        _dapperContext = dapperContext;
    }

    public async Task LogAuditEventAsync(AuditTrailEntry entry)
    {
        using var connection = _dapperContext.CreateConnection();
        
        var parameters = new DynamicParameters();
        parameters.Add("@EventType", entry.EventType);
        parameters.Add("@EventCategory", entry.EventCategory);
        parameters.Add("@UserId", entry.UserId);
        parameters.Add("@Username", entry.Username);
        parameters.Add("@RoleName", entry.RoleName);
        parameters.Add("@IPAddress", entry.IPAddress);
        parameters.Add("@UserAgent", entry.UserAgent);
        parameters.Add("@SessionId", entry.SessionId);
        parameters.Add("@EntityType", entry.EntityType);
        parameters.Add("@EntityId", entry.EntityId);
        parameters.Add("@EntityName", entry.EntityName);
        parameters.Add("@Action", entry.Action);
        parameters.Add("@OldValue", entry.OldValue);
        parameters.Add("@NewValue", entry.NewValue);
        parameters.Add("@Result", entry.Result);
        parameters.Add("@ErrorMessage", entry.ErrorMessage);
        parameters.Add("@Duration", entry.Duration);

        var result = await connection.QueryFirstOrDefaultAsync<AuditIdResult>(
            "audit.sp_LogAuditEvent",
            parameters,
            commandType: CommandType.StoredProcedure);
        
        entry.AuditId = result?.AuditId ?? Guid.Empty;
    }

    public async Task LogAuditEventAsync(
        string eventType,
        string action,
        Guid? userId,
        string? username,
        string? entityType,
        string? entityId,
        string? oldValue,
        string? newValue,
        string? ipAddress,
        string result = "Success",
        string? errorMessage = null)
    {
        var entry = new AuditTrailEntry
        {
            EventType = eventType,
            EventCategory = DetermineCategory(eventType),
            Action = action,
            UserId = userId,
            Username = username,
            EntityType = entityType,
            EntityId = entityId,
            OldValue = oldValue,
            NewValue = newValue,
            IPAddress = ipAddress,
            Result = result,
            ErrorMessage = errorMessage
        };

        await LogAuditEventAsync(entry);
    }

    public async Task<IEnumerable<AuditTrailEntry>> SearchAuditTrailAsync(
        DateTime? startDate,
        DateTime? endDate,
        Guid? userId,
        string? eventType,
        string? entityType,
        int pageNumber = 1,
        int pageSize = 50)
    {
        using var connection = _dapperContext.CreateConnection();
        
        var parameters = new DynamicParameters();
        parameters.Add("@StartDate", startDate);
        parameters.Add("@EndDate", endDate);
        parameters.Add("@UserId", userId);
        parameters.Add("@EventType", eventType);
        parameters.Add("@EntityType", entityType);
        parameters.Add("@PageNumber", pageNumber);
        parameters.Add("@PageSize", pageSize);

        var result = await connection.QueryAsync<AuditTrailEntry>(
            "audit.sp_SearchAuditTrail",
            parameters,
            commandType: CommandType.StoredProcedure);

        return result;
    }

    public async Task<IEnumerable<AuditTrailEntry>> GetUserAuditTrailAsync(Guid userId, int days = 30)
    {
        using var connection = _dapperContext.CreateConnection();
        
        var startDate = DateTime.UtcNow.AddDays(-days);
        
        var result = await connection.QueryAsync<AuditTrailEntry>(
            @"SELECT * FROM audit.AuditTrail 
              WHERE UserId = @UserId 
                AND Timestamp >= @StartDate 
              ORDER BY Timestamp DESC",
            new { UserId = userId, StartDate = startDate });

        return result;
    }

    public async Task<IEnumerable<AuditTrailEntry>> GetFileAuditTrailAsync(Guid fileId)
    {
        using var connection = _dapperContext.CreateConnection();
        
        var result = await connection.QueryAsync<AuditTrailEntry>(
            @"SELECT a.* 
              FROM audit.AuditTrail a
              INNER JOIN audit.FileAuditTrail f ON a.AuditId = f.AuditId
              WHERE f.FileId = @FileId 
              ORDER BY a.Timestamp DESC",
            new { FileId = fileId });

        return result;
    }

    private string DetermineCategory(string eventType)
    {
        if (eventType.StartsWith("User") || eventType.StartsWith("Login"))
            return "User";
        if (eventType.StartsWith("File") || eventType.StartsWith("Document"))
            return "Document";
        if (eventType.StartsWith("System"))
            return "System";
        
        return "General";
    }
}