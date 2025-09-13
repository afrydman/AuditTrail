using System.Diagnostics;

namespace AuditTrail.Web.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip logging for static files and health checks
        if (ShouldSkipLogging(context))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var requestId = context.TraceIdentifier;
        
        // Log request start  
        string? sessionId = null;
        try
        {
            sessionId = context.Session?.Id;
        }
        catch (InvalidOperationException)
        {
            // Session not available for this request (e.g., static files)
            sessionId = null;
        }
        var requestInfo = new
        {
            RequestId = requestId,
            Method = context.Request.Method,
            Path = context.Request.Path,
            QueryString = context.Request.QueryString.ToString(),
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
            UserId = context.User?.Identity?.Name,
            Referrer = context.Request.Headers.Referer.ToString(),
            ContentType = context.Request.ContentType,
            ContentLength = context.Request.ContentLength,
            SessionId = sessionId
        };

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestId"] = requestId,
            ["RequestMethod"] = requestInfo.Method,
            ["RequestPath"] = requestInfo.Path,
            ["UserId"] = requestInfo.UserId,
            ["UserName"] = requestInfo.UserId,
            ["IpAddress"] = requestInfo.IpAddress,
            ["UserAgent"] = requestInfo.UserAgent,
            ["Referrer"] = requestInfo.Referrer,
            ["SessionId"] = requestInfo.SessionId
        }))
        {
            _logger.LogInformation(
                "Web request started: {Method} {Path}{QueryString} | User: {UserId} | IP: {IpAddress} | Session: {SessionId} | Referrer: {Referrer} | RequestId: {RequestId}",
                requestInfo.Method,
                requestInfo.Path,
                requestInfo.QueryString,
                requestInfo.UserId ?? "Anonymous",
                requestInfo.IpAddress ?? "Unknown",
                requestInfo.SessionId ?? "None",
                requestInfo.Referrer ?? "Direct",
                requestId);
        }

        try
        {
            await _next(context);
            
            stopwatch.Stop();

            // Log successful response
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["RequestId"] = requestId,
                ["RequestMethod"] = requestInfo.Method,
                ["RequestPath"] = requestInfo.Path,
                ["StatusCode"] = context.Response.StatusCode,
                ["ResponseTime"] = stopwatch.ElapsedMilliseconds,
                ["UserId"] = requestInfo.UserId,
                ["UserName"] = requestInfo.UserId,
                ["IpAddress"] = requestInfo.IpAddress,
                ["SessionId"] = requestInfo.SessionId
            }))
            {
                _logger.LogInformation(
                    "Web request completed: {Method} {Path} | Status: {StatusCode} | Duration: {Duration}ms | Session: {SessionId} | User: {UserId} | RequestId: {RequestId}",
                    requestInfo.Method,
                    requestInfo.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    requestInfo.SessionId ?? "None",
                    requestInfo.UserId ?? "Anonymous",
                    requestId);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex,
                "Web request failed: {Method} {Path} | Duration: {Duration}ms | User: {UserId} | RequestId: {RequestId}",
                requestInfo.Method,
                requestInfo.Path,
                stopwatch.ElapsedMilliseconds,
                requestInfo.UserId ?? "Anonymous",
                requestId);

            throw;
        }
    }

    private static bool ShouldSkipLogging(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower();
        
        if (string.IsNullOrEmpty(path))
            return false;

        // Skip static files
        var staticExtensions = new[] { ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".svg", ".woff", ".woff2", ".ttf", ".eot" };
        if (staticExtensions.Any(ext => path.EndsWith(ext)))
            return true;

        // Skip common paths
        var skipPaths = new[] { "/health", "/favicon.ico", "/robots.txt" };
        if (skipPaths.Any(skipPath => path.Equals(skipPath)))
            return true;

        return false;
    }
}