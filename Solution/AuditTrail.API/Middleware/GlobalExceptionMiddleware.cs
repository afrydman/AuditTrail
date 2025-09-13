using System.Net;
using System.Text.Json;
using AuditTrail.Core.DTOs;

namespace AuditTrail.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
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
            QueryString = context.Request.QueryString.ToString()
        };

        _logger.LogError(exception, 
            "Unhandled exception occurred. Request: {RequestMethod} {RequestPath} | User: {UserId} | IP: {IpAddress} | RequestId: {RequestId}",
            requestInfo.RequestMethod,
            requestInfo.RequestPath,
            requestInfo.UserId ?? "Anonymous",
            requestInfo.IpAddress ?? "Unknown",
            requestInfo.RequestId);

        // Set response details
        context.Response.ContentType = "application/json";
        
        var response = exception switch
        {
            ArgumentException => CreateErrorResponse(
                HttpStatusCode.BadRequest,
                "Invalid request parameters",
                exception.Message,
                requestInfo.RequestId),
            
            UnauthorizedAccessException => CreateErrorResponse(
                HttpStatusCode.Unauthorized,
                "Access denied",
                "You are not authorized to access this resource",
                requestInfo.RequestId),
            
            KeyNotFoundException => CreateErrorResponse(
                HttpStatusCode.NotFound,
                "Resource not found",
                exception.Message,
                requestInfo.RequestId),
            
            InvalidOperationException => CreateErrorResponse(
                HttpStatusCode.BadRequest,
                "Invalid operation",
                exception.Message,
                requestInfo.RequestId),
            
            TimeoutException => CreateErrorResponse(
                HttpStatusCode.RequestTimeout,
                "Request timeout",
                "The request took too long to process",
                requestInfo.RequestId),
            
            _ => CreateErrorResponse(
                HttpStatusCode.InternalServerError,
                "Internal server error",
                "An unexpected error occurred while processing your request",
                requestInfo.RequestId)
        };

        context.Response.StatusCode = (int)response.StatusCode;

        var jsonResponse = JsonSerializer.Serialize(response.ApiResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private static (HttpStatusCode StatusCode, Result<object> ApiResponse) CreateErrorResponse(
        HttpStatusCode statusCode, 
        string title, 
        string message, 
        string requestId)
    {
        var apiResponse = Result<object>.Failure(message);
        
        // Add additional error details for debugging (only in development)
        apiResponse.RequestId = requestId;
        apiResponse.Timestamp = DateTime.UtcNow;

        return (statusCode, apiResponse);
    }
}