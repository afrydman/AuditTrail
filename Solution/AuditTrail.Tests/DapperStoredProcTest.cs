using Microsoft.Extensions.Configuration;
using Dapper;
using System.Data;
using Xunit;
using AuditTrail.Infrastructure.Data;

namespace AuditTrail.Tests;

public class DapperStoredProcTest : IDisposable
{
    private readonly DapperContext _dapperContext;

    public DapperStoredProcTest()
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
    public async Task TestStoredProcedureDirect()
    {
        using var connection = _dapperContext.CreateConnection();
        
        // Test with exact parameters from debug output
        var parameters = new DynamicParameters();
        parameters.Add("@Username", "admin");
        parameters.Add("@UserId", Guid.Parse("465ee473-054a-46b3-a088-90d4125b9be9"));
        parameters.Add("@IsSuccess", true);  // C# bool
        parameters.Add("@IPAddress", "127.0.0.1");
        parameters.Add("@UserAgent", (string?)null);

        Console.WriteLine("DEBUG: About to call stored procedure with parameters:");
        Console.WriteLine($"  Username: admin");
        Console.WriteLine($"  UserId: 465ee473-054a-46b3-a088-90d4125b9be9");
        Console.WriteLine($"  IsSuccess: true");

        var result = await connection.QueryFirstOrDefaultAsync<TestAuthResult>(
            "auth.sp_ProcessAuthenticationResult",
            parameters,
            commandType: CommandType.StoredProcedure);

        Console.WriteLine($"DEBUG: Dapper result - Success: {result?.Success}, UserId: {result?.UserId}");
        Console.WriteLine($"DEBUG: Message: {result?.Message}");
        Console.WriteLine($"DEBUG: Username: {result?.Username}");

        Assert.NotNull(result);
        Assert.Equal(1, result.Success);
        Assert.NotNull(result.UserId);
    }

    [Fact]
    public async Task TestStoredProcedureWithExplicitTypes()
    {
        using var connection = _dapperContext.CreateConnection();
        
        // Test with explicit SQL types
        var parameters = new DynamicParameters();
        parameters.Add("@Username", "admin", DbType.String);
        parameters.Add("@UserId", Guid.Parse("465ee473-054a-46b3-a088-90d4125b9be9"), DbType.Guid);
        parameters.Add("@IsSuccess", 1, DbType.Int32);  // Explicit int instead of bool
        parameters.Add("@IPAddress", "127.0.0.1", DbType.String);
        parameters.Add("@UserAgent", null, DbType.String);

        Console.WriteLine("DEBUG: Testing with explicit SQL types");

        var result = await connection.QueryFirstOrDefaultAsync<TestAuthResult>(
            "auth.sp_ProcessAuthenticationResult",
            parameters,
            commandType: CommandType.StoredProcedure);

        Console.WriteLine($"DEBUG: Explicit types result - Success: {result?.Success}, UserId: {result?.UserId}");

        Assert.NotNull(result);
        Assert.Equal(1, result.Success);
        Assert.NotNull(result.UserId);
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}