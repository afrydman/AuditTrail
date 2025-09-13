using Microsoft.EntityFrameworkCore;
using Dapper;
using System.Data;
using AuditTrail.Core.Entities.Auth;
using AuditTrail.Core.Interfaces;
using AuditTrail.Infrastructure.Data;
using BCrypt.Net;

namespace AuditTrail.Infrastructure.Repositories;

// Helper class for stored procedure result mapping
public class AuthenticationResult
{
    public int Success { get; set; }  // SQL returns int (0/1), not bool
    public string? Message { get; set; }
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? RoleName { get; set; }
    public bool MustChangePassword { get; set; }
}

public class UserRepository : Repository<User>, IUserRepository
{
    private readonly IDapperContext _dapperContext;

    public UserRepository(AuditTrailDbContext context, IDapperContext dapperContext) : base(context)
    {
        _dapperContext = dapperContext;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetWithRoleAsync(Guid userId)
    {
        return await _dbSet
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        var user = await GetByUsernameAsync(username);
        if (user == null || !user.IsActive || user.IsLocked)
            return false;

        // BCrypt already includes salt in the hash, don't concatenate PasswordSalt
        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }

    public async Task<User?> AuthenticateAsync(string username, string password, string ipAddress)
    {
        using var connection = _dapperContext.CreateConnection();
        
        // First, get the user and validate password locally with BCrypt
        var user = await GetByUsernameAsync(username);
        if (user == null)
        {
            // Call stored procedure to log failed attempt for non-existent user
            var failParameters = new DynamicParameters();
            failParameters.Add("@Username", username);
            failParameters.Add("@IsSuccess", false);
            failParameters.Add("@IPAddress", ipAddress);
            failParameters.Add("@UserAgent", (string?)null);

            await connection.QueryAsync(
                "auth.sp_LogAuthenticationAttempt",
                failParameters,
                commandType: CommandType.StoredProcedure);
                
            return null;
        }

        // Check if account is active and not locked
        if (!user.IsActive || user.IsLocked)
        {
            // Call stored procedure to log failed attempt
            var failParameters = new DynamicParameters();
            failParameters.Add("@Username", username);
            failParameters.Add("@UserId", user.Id);
            failParameters.Add("@IsSuccess", false);
            failParameters.Add("@FailureReason", !user.IsActive ? "Account deactivated" : "Account locked");
            failParameters.Add("@IPAddress", ipAddress);
            failParameters.Add("@UserAgent", (string?)null);

            await connection.QueryAsync(
                "auth.sp_LogAuthenticationAttempt",
                failParameters,
                commandType: CommandType.StoredProcedure);
                
            return null;
        }

        // Verify password using BCrypt
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            // Call stored procedure to handle failed authentication
            var failParameters = new DynamicParameters();
            failParameters.Add("@Username", username);
            failParameters.Add("@UserId", user.Id);
            failParameters.Add("@IsSuccess", false);
            failParameters.Add("@FailureReason", "Invalid password");
            failParameters.Add("@IPAddress", ipAddress);
            failParameters.Add("@UserAgent", (string?)null);

            await connection.QueryAsync(
                "auth.sp_ProcessAuthenticationResult",
                failParameters,
                commandType: CommandType.StoredProcedure);
                
            return null;
        }

        // Successful authentication - call stored procedure to handle success
        var successParameters = new DynamicParameters();
        successParameters.Add("@Username", username);
        successParameters.Add("@UserId", user.Id);
        successParameters.Add("@IsSuccess", true);
        successParameters.Add("@IPAddress", ipAddress);
        successParameters.Add("@UserAgent", (string?)null);

        // Use QueryMultiple to handle multiple result sets from stored procedure
        using var multi = await connection.QueryMultipleAsync(
            "auth.sp_ProcessAuthenticationResult",
            successParameters,
            commandType: CommandType.StoredProcedure);

        // Skip the first result set (AuditId from sp_LogAuthenticationAttempt)
        await multi.ReadAsync();
        
        // Read the second result set (actual authentication result)
        var result = await multi.ReadFirstOrDefaultAsync<AuthenticationResult>();

        if (result?.Success == 1 && result.UserId.HasValue)
        {
            return await GetWithRoleAsync(result.UserId.Value);
        }
        
        return null;
    }

    public async Task LockUserAccountAsync(Guid userId)
    {
        var user = await GetByIdAsync(userId);
        if (user != null)
        {
            user.IsLocked = true;
            user.LockoutEnd = DateTime.UtcNow.AddYears(100); // Permanent lock until admin unlocks
            await UpdateAsync(user);
        }
    }

    public async Task UnlockUserAccountAsync(Guid userId)
    {
        var user = await GetByIdAsync(userId);
        if (user != null)
        {
            user.IsLocked = false;
            user.LockoutEnd = null;
            user.FailedLoginAttempts = 0;
            await UpdateAsync(user);
        }
    }

    public async Task RecordLoginAttemptAsync(string username, string ipAddress, bool isSuccessful, string? failureReason = null)
    {
        using var connection = _dapperContext.CreateConnection();
        
        await connection.ExecuteAsync(
            @"INSERT INTO auth.LoginAttempts (Username, IPAddress, IsSuccessful, FailureReason, AttemptDate)
              VALUES (@Username, @IPAddress, @IsSuccessful, @FailureReason, GETUTCDATE())",
            new { Username = username, IPAddress = ipAddress, IsSuccessful = isSuccessful, FailureReason = failureReason });
    }

    public async Task<int> GetFailedLoginAttemptsAsync(string username, TimeSpan period)
    {
        using var connection = _dapperContext.CreateConnection();
        
        var since = DateTime.UtcNow.Subtract(period);
        
        return await connection.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM auth.LoginAttempts 
              WHERE Username = @Username 
                AND IsSuccessful = 0 
                AND AttemptDate >= @Since",
            new { Username = username, Since = since });
    }
}