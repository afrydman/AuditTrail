using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AuditTrail.Web.Models;
using AuditTrail.Core.Interfaces;
using AuditTrail.Core.Entities.Audit;
using AuditTrail.Infrastructure.Data;
using AuditTrail.Infrastructure.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace AuditTrail.Web.Controllers
{
    [Authorize]
    public class AuditController : Controller
    {
        private readonly ILogger<AuditController> _logger;
        private readonly IAuditRepository _auditRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly AuditTrailDbContext _dbContext;

        public AuditController(
            ILogger<AuditController> logger,
            IAuditRepository auditRepository,
            ICurrentUserService currentUserService,
            AuditTrailDbContext dbContext)
        {
            _logger = logger;
            _auditRepository = auditRepository;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Main audit trail page with filtering and table view
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                // Get list of users for filter dropdown
                var usersQuery = await _dbContext.Users
                    .Where(u => u.IsActive)
                    .Select(u => new { u.Id, u.FirstName, u.LastName })
                    .ToListAsync();

                var users = usersQuery
                    .Select(u => new { u.Id, DisplayName = $"{u.FirstName} {u.LastName}".Trim() })
                    .OrderBy(u => u.DisplayName)
                    .ToList();

                var viewModel = new AuditIndexViewModel
                {
                    Users = users.Select(u => new UserSelectOption 
                    { 
                        Id = u.Id, 
                        DisplayName = u.DisplayName 
                    }).ToList(),
                    // Default to last 7 days
                    StartDate = DateTime.Today.AddDays(-7),
                    EndDate = DateTime.Today
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading audit page");
                TempData["ErrorMessage"] = "Error al cargar la página de auditoría";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        /// <summary>
        /// AJAX endpoint to search and return audit trail data for the table
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchAuditData(
            DateTime? startDate,
            DateTime? endDate,
            Guid? userId,
            string? eventType,
            string? entityType,
            string? result,
            int page = 1,
            int pageSize = 50)
        {
            try
            {
                // Validate date range
                if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                {
                    return Json(new { success = false, message = "La fecha de inicio no puede ser mayor que la fecha de fin" });
                }

                // Search audit trail using repository
                var auditEntries = await _auditRepository.SearchAuditTrailAsync(
                    startDate, 
                    endDate, 
                    userId, 
                    eventType, 
                    entityType, 
                    page, 
                    pageSize);

                // Apply result filter (since repository doesn't support it yet)
                if (!string.IsNullOrEmpty(result))
                {
                    auditEntries = auditEntries.Where(a => a.Result.Equals(result, StringComparison.OrdinalIgnoreCase));
                }

                // Get user information for display
                var userIds = auditEntries.Where(a => a.UserId.HasValue).Select(a => a.UserId!.Value).Distinct();
                var users = await _dbContext.Users
                    .Where(u => userIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim());

                var auditData = auditEntries.Select(entry => new
                {
                    auditId = entry.AuditId,
                    eventDateTime = entry.Timestamp.ToString("dd/MM/yyyy HH:mm:ss"),
                    eventType = entry.EventType,
                    eventCategory = entry.EventCategory,
                    action = entry.Action,
                    result = entry.Result,
                    userName = entry.UserId.HasValue && users.ContainsKey(entry.UserId.Value) 
                        ? users[entry.UserId.Value] 
                        : entry.Username ?? "Sistema",
                    entityType = TranslateEntityTypeToSpanish(entry.EntityType ?? "-"),
                    entityName = entry.EntityName ?? "-",
                    ipAddress = entry.IPAddress ?? "-",
                    duration = entry.Duration?.ToString() + "ms" ?? "-",
                    errorMessage = entry.ErrorMessage ?? "",
                    oldValue = entry.OldValue ?? "",
                    newValue = entry.NewValue ?? ""
                }).ToList();

                return Json(new { success = true, data = auditData, totalRecords = auditData.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching audit data with parameters: StartDate={StartDate}, EndDate={EndDate}, UserId={UserId}", 
                    startDate, endDate, userId);
                return Json(new { success = false, message = "Error al buscar datos de auditoría" });
            }
        }

        /// <summary>
        /// Get available event types for filter dropdown
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetEventTypes()
        {
            try
            {
                // Get distinct event types from audit trail
                var eventTypes = await _dbContext.Set<AuditTrailEntry>()
                    .Select(a => a.EventType)
                    .Distinct()
                    .Where(et => !string.IsNullOrEmpty(et))
                    .OrderBy(et => et)
                    .ToListAsync();

                return Json(new { success = true, eventTypes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event types");
                return Json(new { success = false, message = "Error al obtener tipos de eventos" });
            }
        }

        /// <summary>
        /// Get available entity types for filter dropdown
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetEntityTypes()
        {
            try
            {
                // Get distinct entity types from audit trail
                var entityTypes = await _dbContext.Set<AuditTrailEntry>()
                    .Select(a => a.EntityType)
                    .Distinct()
                    .Where(et => !string.IsNullOrEmpty(et))
                    .OrderBy(et => et)
                    .ToListAsync();

                return Json(new { success = true, entityTypes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting entity types");
                return Json(new { success = false, message = "Error al obtener tipos de entidades" });
            }
        }

        /// <summary>
        /// Export audit trail data to CSV
        /// TODO: Implement in future phase
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ExportAuditTrail(
            DateTime? startDate,
            DateTime? endDate,
            Guid? userId,
            string? eventType,
            string? entityType,
            string? result)
        {
            // Placeholder for future implementation
            TempData["InfoMessage"] = "Funcionalidad de exportación en desarrollo";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Translate entity types to Spanish for display
        /// </summary>
        private static string TranslateEntityTypeToSpanish(string entityType)
        {
            return entityType switch
            {
                "FileCategory" => "Carpetas",
                "FileEntity" => "Archivo",
                "File" => "Archivo",
                "User" => "Usuario",
                "System" => "Sistema",
                _ => entityType
            };
        }
    }
}