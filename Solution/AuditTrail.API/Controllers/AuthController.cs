using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuditTrail.Core.Interfaces;
using AuditTrail.Core.DTOs;

namespace AuditTrail.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserRepository userRepository,
        IAuditRepository auditRepository,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _auditRepository = auditRepository;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<Result<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            
            // Record login attempt
            await _userRepository.RecordLoginAttemptAsync(request.Username, ipAddress, false);

            // Validate credentials
            var user = await _userRepository.AuthenticateAsync(request.Username, request.Password, ipAddress);

            if (user == null)
            {
                _logger.LogWarning("Failed login attempt for username: {Username} from IP: {IpAddress}", 
                    request.Username, ipAddress);

                await _auditRepository.LogAuditEventAsync(
                    "UserLoginFailed",
                    "Login",
                    null,
                    request.Username,
                    "User",
                    null,
                    null,
                    null,
                    ipAddress,
                    "Failed",
                    "Invalid credentials");

                return Ok(Result<LoginResponse>.Failure("Invalid username or password"));
            }

            // Generate JWT token
            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            // Update successful login attempt
            await _userRepository.RecordLoginAttemptAsync(request.Username, ipAddress, true);

            // Log successful login
            await _auditRepository.LogAuditEventAsync(
                "UserLogin",
                "Login",
                user.Id,
                user.Username,
                "User",
                user.Id.ToString(),
                null,
                null,
                ipAddress);

            var response = new LoginResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes()),
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    RoleName = user.Role?.RoleName ?? "Unknown",
                    IsActive = user.IsActive,
                    LastLoginDate = user.LastLoginDate
                }
            };

            return Ok(Result<LoginResponse>.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for username: {Username}", request.Username);
            return Ok(Result<LoginResponse>.Failure("An error occurred during login"));
        }
    }

    [HttpPost("logout")]
    public async Task<ActionResult<Result<string>>> Logout()
    {
        try
        {
            var userId = GetCurrentUserId();
            var username = GetCurrentUsername();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (userId.HasValue)
            {
                await _auditRepository.LogAuditEventAsync(
                    "UserLogout",
                    "Logout",
                    userId,
                    username,
                    "User",
                    userId.ToString(),
                    null,
                    null,
                    ipAddress);
            }

            return Ok(Result<string>.Success("Logged out successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return Ok(Result<string>.Failure("An error occurred during logout"));
        }
    }

    [HttpGet("me")]
    public async Task<ActionResult<Result<UserDto>>> GetCurrentUser()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Ok(Result<UserDto>.Failure("User not authenticated"));

            var user = await _userRepository.GetWithRoleAsync(userId.Value);
            if (user == null)
                return Ok(Result<UserDto>.Failure("User not found"));

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                RoleName = user.Role?.RoleName ?? "Unknown",
                IsActive = user.IsActive,
                LastLoginDate = user.LastLoginDate
            };

            return Ok(Result<UserDto>.Success(userDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user");
            return Ok(Result<UserDto>.Failure("An error occurred"));
        }
    }

    private string GenerateJwtToken(Core.Entities.Auth.User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("UserId", user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Unknown"),
            new Claim("FirstName", user.FirstName),
            new Claim("LastName", user.LastName)
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes()),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    }

    private int GetTokenExpirationMinutes()
    {
        return _configuration.GetValue<int>("JwtSettings:ExpirationInMinutes", 30);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private string? GetCurrentUsername()
    {
        return User.Identity?.Name;
    }
}