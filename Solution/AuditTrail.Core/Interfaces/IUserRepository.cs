using AuditTrail.Core.Entities.Auth;

namespace AuditTrail.Core.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetWithRoleAsync(Guid userId);
    Task<bool> ValidateCredentialsAsync(string username, string password);
    Task<User?> AuthenticateAsync(string username, string password, string ipAddress);
    Task LockUserAccountAsync(Guid userId);
    Task UnlockUserAccountAsync(Guid userId);
    Task RecordLoginAttemptAsync(string username, string ipAddress, bool isSuccessful, string? failureReason = null);
    Task<int> GetFailedLoginAttemptsAsync(string username, TimeSpan period);
}