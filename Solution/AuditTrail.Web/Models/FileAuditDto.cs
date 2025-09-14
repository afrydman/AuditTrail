namespace AuditTrail.Web.Models
{
    public class FileAuditDto
    {
        public int LogId { get; set; }
        public DateTime EventDateTime { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string UserDisplayName { get; set; } = string.Empty;
    }
}