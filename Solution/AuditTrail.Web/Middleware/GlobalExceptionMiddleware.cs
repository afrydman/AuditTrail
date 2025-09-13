namespace AuditTrail.Web.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Log the exception with context
        var requestInfo = new
        {
            RequestId = context.TraceIdentifier,
            RequestPath = context.Request.Path,
            RequestMethod = context.Request.Method,
            UserId = context.User?.Identity?.Name,
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
            QueryString = context.Request.QueryString.ToString(),
            Referrer = context.Request.Headers.Referer.ToString()
        };

        _logger.LogError(exception,
            "Unhandled exception occurred in Web application. Request: {RequestMethod} {RequestPath} | User: {UserId} | IP: {IpAddress} | RequestId: {RequestId} | Referrer: {Referrer}",
            requestInfo.RequestMethod,
            requestInfo.RequestPath,
            requestInfo.UserId ?? "Anonymous",
            requestInfo.IpAddress ?? "Unknown",
            requestInfo.RequestId,
            requestInfo.Referrer ?? "Direct");

        // Don't handle response if already started
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("The response has already started, the exception middleware will not be executed for RequestId: {RequestId}", requestInfo.RequestId);
            return;
        }

        // Clear response and set error status
        context.Response.Clear();
        context.Response.StatusCode = 500;
        
        // Handle different types of requests
        if (IsApiRequest(context))
        {
            // API request - return JSON error
            await HandleApiError(context, exception, requestInfo.RequestId);
        }
        else
        {
            // Web request - redirect to error page
            await HandleWebError(context, exception, requestInfo.RequestId);
        }
    }

    private static bool IsApiRequest(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments("/api") ||
               context.Request.Headers.Accept.Any(h => h?.Contains("application/json") == true);
    }

    private async Task HandleApiError(HttpContext context, Exception exception, string requestId)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            isSuccess = false,
            errorMessage = _environment.IsDevelopment() 
                ? exception.Message 
                : "An error occurred while processing your request",
            requestId = requestId,
            timestamp = DateTime.UtcNow
        };

        var jsonResponse = System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private async Task HandleWebError(HttpContext context, Exception exception, string requestId)
    {
        // Store error details in TempData for error page
        if (context.Features.Get<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory>() != null)
        {
            try
            {
                var tempDataProvider = context.RequestServices.GetService<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>();
                if (tempDataProvider != null)
                {
                    var tempDataFactory = context.RequestServices.GetService<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory>();
                    var tempData = tempDataFactory?.GetTempData(context);
                    
                    if (tempData != null)
                    {
                        tempData["ErrorRequestId"] = requestId;
                        tempData["ErrorTimestamp"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
                        
                        if (_environment.IsDevelopment())
                        {
                            tempData["ErrorDetails"] = exception.Message;
                            tempData["ErrorStackTrace"] = exception.StackTrace;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not set TempData for error page");
            }
        }

        // Redirect to error page
        context.Response.Redirect("/Home/Error");
        await context.Response.CompleteAsync();
    }
}