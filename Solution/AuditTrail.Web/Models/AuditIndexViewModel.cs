namespace AuditTrail.Web.Models
{
    public class AuditIndexViewModel
    {
        public List<UserSelectOption> Users { get; set; } = new List<UserSelectOption>();
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid? SelectedUserId { get; set; }
        public string? SelectedEventType { get; set; }
        public string? SelectedEntityType { get; set; }
        public string? SelectedResult { get; set; }
        
        // View mode (table/dashboard for future)
        public string ViewMode { get; set; } = "table";
        
        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        
        // Available filter options (populated via AJAX)
        public List<string> EventTypes { get; set; } = new List<string>();
        public List<string> EntityTypes { get; set; } = new List<string>();
        public List<string> ResultTypes { get; set; } = new List<string> { "Success", "Failed", "Warning" };
        
        // Date range presets for quick selection
        public List<DateRangePreset> DatePresets { get; set; } = new List<DateRangePreset>
        {
            new DateRangePreset { Name = "Hoy", Days = 0 },
            new DateRangePreset { Name = "Últimos 7 días", Days = 7 },
            new DateRangePreset { Name = "Últimos 30 días", Days = 30 },
            new DateRangePreset { Name = "Últimos 90 días", Days = 90 }
        };
    }

    public class UserSelectOption
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    public class DateRangePreset
    {
        public string Name { get; set; } = string.Empty;
        public int Days { get; set; }
    }

    public class AuditTrailEntryViewModel
    {
        public Guid AuditId { get; set; }
        public string EventDateTime { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string EventCategory { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        
        // UI helper properties
        public string ResultClass => Result switch
        {
            "Success" => "text-success",
            "Failed" => "text-danger",
            "Warning" => "text-warning",
            _ => "text-muted"
        };
        
        public string ResultIcon => Result switch
        {
            "Success" => "bi-check-circle-fill",
            "Failed" => "bi-x-circle-fill",
            "Warning" => "bi-exclamation-triangle-fill",
            _ => "bi-info-circle"
        };
        
        public string ActionIcon => Action switch
        {
            var a when a.Contains("Login") => "bi-box-arrow-in-right",
            var a when a.Contains("Logout") => "bi-box-arrow-right",
            var a when a.Contains("Create") => "bi-plus-circle",
            var a when a.Contains("Update") || a.Contains("Modify") => "bi-pencil",
            var a when a.Contains("Delete") => "bi-trash",
            var a when a.Contains("View") => "bi-eye",
            var a when a.Contains("Download") => "bi-download",
            var a when a.Contains("Upload") => "bi-upload",
            _ => "bi-clock"
        };
        
        public string CategoryBadgeClass => EventCategory switch
        {
            var c when c.Contains("Security") => "badge bg-danger",
            var c when c.Contains("Document") => "badge bg-primary",
            var c when c.Contains("User") => "badge bg-info",
            var c when c.Contains("System") => "badge bg-secondary",
            _ => "badge bg-light text-dark"
        };
    }
}