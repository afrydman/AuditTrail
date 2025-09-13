namespace AuditTrail.Core.Entities.Audit;

public class AuditTrailEntry
{
    public Guid AuditId { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public string EventCategory { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public string? RoleName { get; set; }
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public Guid? SessionId { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? EntityName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? AdditionalData { get; set; }
    public string Result { get; set; } = "Success";
    public string? ErrorMessage { get; set; }
    public int? Duration { get; set; }
    public string? ServerName { get; set; }
    public string? ApplicationVersion { get; set; }
}