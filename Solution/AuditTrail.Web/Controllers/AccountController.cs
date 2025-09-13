using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Text.Json;
using AuditTrail.Core.Interfaces;
using AuditTrail.Core.DTOs;
using AuditTrail.Web.Models;

namespace AuditTrail.Web.Controllers;

public class AccountController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IUserRepository userRepository,
        IAuditRepository auditRepository,
        IHttpClientFactory httpClientFactory,
        ILogger<AccountController> logger)
    {
        _userRepository = userRepository;
        _auditRepository = auditRepository;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            // Call API for authentication
            var httpClient = _httpClientFactory.CreateClient("AuditTrailApi");
            var loginRequest = new LoginRequest
            {
                Username = model.Username,
                Password = model.Password
            };

            var response = await httpClient.PostAsJsonAsync("/api/auth/login", loginRequest);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Result<LoginResponse>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.IsSuccess == true && result.Data != null)
                {
                    // Create claims for the cookie
                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, result.Data.User.Id.ToString()),
                        new(ClaimTypes.Name, result.Data.User.Username),
                        new(ClaimTypes.Email, result.Data.User.Email),
                        new(ClaimTypes.Role, result.Data.User.RoleName),
                        new("UserId", result.Data.User.Id.ToString()),
                        new("FirstName", result.Data.User.FirstName),
                        new("LastName", result.Data.User.LastName)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = result.Data.ExpiresAt
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    _logger.LogInformation("User {Username} logged in successfully", model.Username);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToAction("Index", "Dashboard");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, result?.ErrorMessage ?? "Invalid login attempt");
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Login service unavailable");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}", model.Username);
            ModelState.AddModelError(string.Empty, "An error occurred during login");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userId = GetCurrentUserId();
            var username = User.Identity?.Name;
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

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            _logger.LogInformation("User {Username} logged out", username);
            
            return RedirectToAction("Login");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return RedirectToAction("Login");
        }
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}