using System.Diagnostics;
using System.Text;

namespace AuditTrail.API.Middleware;

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
        var stopwatch = Stopwatch.StartNew();
        var requestId = context.TraceIdentifier;
        
        // Log request start
        var requestInfo = new
        {
            RequestId = requestId,
            Method = context.Request.Method,
            Path = context.Request.Path,
            QueryString = context.Request.QueryString.ToString(),
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
            UserId = context.User?.Identity?.Name,
            ContentType = context.Request.ContentType,
            ContentLength = context.Request.ContentLength
        };

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestId"] = requestId,
            ["RequestMethod"] = requestInfo.Method,
            ["RequestPath"] = requestInfo.Path,
            ["UserId"] = requestInfo.UserId,
            ["UserName"] = requestInfo.UserId,
            ["IpAddress"] = requestInfo.IpAddress,
            ["UserAgent"] = requestInfo.UserAgent
        }))
        {
            _logger.LogInformation(
                "API Request started: {Method} {Path}{QueryString} | User: {UserId} | IP: {IpAddress} | RequestId: {RequestId}",
                requestInfo.Method,
                requestInfo.Path,
                requestInfo.QueryString,
                requestInfo.UserId ?? "Anonymous",
                requestInfo.IpAddress ?? "Unknown",
                requestId);
        }

        // Store original response body stream
        var originalResponseBodyStream = context.Response.Body;
        
        try
        {
            // Create new memory stream for response body
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            // Execute next middleware
            await _next(context);

            stopwatch.Stop();

            // Log response
            var responseSize = responseBodyStream.Length;
            responseBodyStream.Position = 0;

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["RequestId"] = requestId,
                ["RequestMethod"] = requestInfo.Method,
                ["RequestPath"] = requestInfo.Path,
                ["StatusCode"] = context.Response.StatusCode,
                ["ResponseTime"] = stopwatch.ElapsedMilliseconds,
                ["UserId"] = requestInfo.UserId,
                ["UserName"] = requestInfo.UserId,
                ["IpAddress"] = requestInfo.IpAddress
            }))
            {
                _logger.LogInformation(
                    "API Request completed: {Method} {Path} | Status: {StatusCode} | Duration: {Duration}ms | Size: {ResponseSize} bytes | User: {UserId} | RequestId: {RequestId}",
                    requestInfo.Method,
                    requestInfo.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    responseSize,
                    requestInfo.UserId ?? "Anonymous",
                    requestId);
            }

            // Copy response back to original stream
            await responseBodyStream.CopyToAsync(originalResponseBodyStream);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex,
                "Request failed: {Method} {Path} | Duration: {Duration}ms | User: {UserId} | RequestId: {RequestId}",
                requestInfo.Method,
                requestInfo.Path,
                stopwatch.ElapsedMilliseconds,
                requestInfo.UserId ?? "Anonymous",
                requestId);

            context.Response.Body = originalResponseBodyStream;
            throw;
        }
    }
}