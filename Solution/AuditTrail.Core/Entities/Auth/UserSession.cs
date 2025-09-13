namespace AuditTrail.Core.Entities.Auth;

public class UserSession
{
    public Guid SessionId { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string SessionToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public string IPAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ExpiryDate { get; set; }
    public DateTime? RefreshTokenExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? RevokedDate { get; set; }
    public Guid? RevokedBy { get; set; }
    public string? RevokedReason { get; set; }
    
    // Navigation properties
    public virtual User? User { get; set; }
}