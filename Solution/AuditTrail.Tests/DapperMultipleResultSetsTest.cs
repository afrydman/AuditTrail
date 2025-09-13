using Microsoft.Extensions.Configuration;
using Dapper;
using System.Data;
using Xunit;
using AuditTrail.Infrastructure.Data;

namespace AuditTrail.Tests;

public class DapperMultipleResultSetsTest : IDisposable
{
    private readonly DapperContext _dapperContext;

    public DapperMultipleResultSetsTest()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"ConnectionStrings:DefaultConnection", "Server=.;Database=AuditTrail;Trusted_Connection=true;TrustServerCertificate=true;"}
            })
            .Build();
            
        _dapperContext = new DapperContext(config);
    }

    public class TestAuthResult
    {
        public int Success { get; set; }
        public string? Message { get; set; }
        public Guid? UserId { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? RoleName { get; set; }
        public bool MustChangePassword { get; set; }
    }

    [Fact]
    public async Task TestMultipleResultSets()
    {
        using var connection = _dapperContext.CreateConnection();
        
        var parameters = new DynamicParameters();
        parameters.Add("@Username", "admin");
        parameters.Add("@UserId", Guid.Parse("465ee473-054a-46b3-a088-90d4125b9be9"));
        parameters.Add("@IsSuccess", true);
        parameters.Add("@IPAddress", "127.0.0.1");
        parameters.Add("@UserAgent", (string?)null);

        Console.WriteLine("DEBUG: Testing multiple result sets handling");

        // Use QueryMultiple to handle multiple result sets
        using var multi = await connection.QueryMultipleAsync(
            "auth.sp_ProcessAuthenticationResult",
            parameters,
            commandType: CommandType.StoredProcedure);

        // Skip the first result set (AuditId from sp_LogAuthenticationAttempt)
        var firstResultSet = await multi.ReadAsync();
        Console.WriteLine($"DEBUG: First result set count: {firstResultSet.Count()}");
        
        // Read the second result set (actual authentication result)
        var authResult = await multi.ReadFirstOrDefaultAsync<TestAuthResult>();
        
        Console.WriteLine($"DEBUG: Auth result - Success: {authResult?.Success}, UserId: {authResult?.UserId}");
        Console.WriteLine($"DEBUG: Username: {authResult?.Username}");

        Assert.NotNull(authResult);
        Assert.Equal(1, authResult.Success);
        Assert.NotNull(authResult.UserId);
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}