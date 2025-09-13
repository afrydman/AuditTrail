using AuditTrail.Core.Entities.Audit;

namespace AuditTrail.Core.Interfaces;

public interface IAuditRepository
{
    Task LogAuditEventAsync(AuditTrailEntry entry);
    Task LogAuditEventAsync(
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
        string? errorMessage = null);
    Task<IEnumerable<AuditTrailEntry>> SearchAuditTrailAsync(
        DateTime? startDate,
        DateTime? endDate,
        Guid? userId,
        string? eventType,
        string? entityType,
        int pageNumber = 1,
        int pageSize = 50);
    Task<IEnumerable<AuditTrailEntry>> GetUserAuditTrailAsync(Guid userId, int days = 30);
    Task<IEnumerable<AuditTrailEntry>> GetFileAuditTrailAsync(Guid fileId);
}