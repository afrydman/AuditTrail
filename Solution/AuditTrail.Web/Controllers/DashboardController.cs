using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuditTrail.Core.Interfaces;

namespace AuditTrail.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IUserRepository userRepository,
        IAuditRepository auditRepository,
        ILogger<DashboardController> logger)
    {
        _userRepository = userRepository;
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _userRepository.GetWithRoleAsync(userId.Value);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewData["CurrentUser"] = user;
            ViewData["WelcomeMessage"] = $"Welcome, {user.FirstName} {user.LastName}";
            ViewData["RoleName"] = user.Role?.RoleName ?? "Unknown";

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard for user");
            return View("Error");
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}