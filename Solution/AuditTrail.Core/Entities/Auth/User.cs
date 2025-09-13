namespace AuditTrail.Core.Entities.Auth;

public class User : BaseEntityWithId
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsEmailVerified { get; set; }
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationExpiry { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetExpiry { get; set; }
    public int FailedLoginAttempts { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public DateTime? LastPasswordChangeDate { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public string? LastLoginIP { get; set; }
    
    // Navigation properties
    public virtual Role? Role { get; set; }
    public virtual ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
}