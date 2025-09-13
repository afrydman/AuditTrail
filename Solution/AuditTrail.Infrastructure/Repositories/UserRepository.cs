using Microsoft.EntityFrameworkCore;
using Dapper;
using System.Data;
using AuditTrail.Core.Entities.Auth;
using AuditTrail.Core.Interfaces;
using AuditTrail.Infrastructure.Data;
using BCrypt.Net;

namespace AuditTrail.Infrastructure.Repositories;

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

        // Using BCrypt to verify password
        return BCrypt.Net.BCrypt.Verify(password + user.PasswordSalt, user.PasswordHash);
    }

    public async Task<User?> AuthenticateAsync(string username, string password, string ipAddress)
    {
        using var connection = _dapperContext.CreateConnection();
        
        // Call the stored procedure sp_AuthenticateUser
        var parameters = new DynamicParameters();
        parameters.Add("@Username", username);
        parameters.Add("@PasswordHash", BCrypt.Net.BCrypt.HashPassword(password)); // This is simplified - in real scenario, get salt first
        parameters.Add("@IPAddress", ipAddress);
        parameters.Add("@UserId", dbType: DbType.Guid, direction: ParameterDirection.Output);
        parameters.Add("@IsAuthenticated", dbType: DbType.Boolean, direction: ParameterDirection.Output);
        parameters.Add("@Message", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            "auth.sp_AuthenticateUser",
            parameters,
            commandType: CommandType.StoredProcedure);

        var isAuthenticated = parameters.Get<bool>("@IsAuthenticated");
        
        if (isAuthenticated)
        {
            var userId = parameters.Get<Guid>("@UserId");
            return await GetWithRoleAsync(userId);
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