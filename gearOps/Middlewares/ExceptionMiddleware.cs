using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using gearOps.Application.Exceptions;

namespace gearOps.Middlewares;

/// <summary>
/// Centralized global exception handler.
/// Catches every unhandled exception in the pipeline and returns a uniform
/// JSON envelope that matches the ApiResponseFilter format.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (ex is AppException or DbUpdateException)
            {
                _logger.LogWarning(ex, "Handled API exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            }
            else
            {
                _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            }
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        int statusCode;
        string userMessage;
        object? errorData;

        switch (exception)
        {
            // ── Application-level exceptions (our custom hierarchy) ──────
            case ValidationException validationEx:
                statusCode = validationEx.StatusCode;
                userMessage = validationEx.Message;
                errorData = new
                {
                    Type = validationEx.ErrorType,
                    Message = validationEx.Message,
                    Errors = validationEx.Errors
                };
                break;

            case AppException appEx:
                statusCode = appEx.StatusCode;
                userMessage = appEx.Message;
                errorData = new
                {
                    Type = appEx.ErrorType,
                    Message = appEx.Message
                };
                break;

            // ── EF Core / Postgres constraint violations ─────────────────
            case DbUpdateException dbUpdateEx when dbUpdateEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505":
                statusCode = (int)HttpStatusCode.Conflict;
                var field = ParseFieldFromDetail(pgEx.Detail) ?? ParseFieldFromConstraint(pgEx.ConstraintName);
                var table = pgEx.TableName ?? "unknown";
                userMessage = string.IsNullOrEmpty(field)
                    ? $"Duplicate value violates unique constraint on table '{table}'."
                    : $"Duplicate value for '{field}' in '{table}'.";
                errorData = new
                {
                    Type = "UniqueConstraintViolation",
                    Message = userMessage,
                    Table = table,
                    Field = field ?? string.Empty
                };
                break;

            case DbUpdateException dbUpdateEx2 when dbUpdateEx2.InnerException is PostgresException pgEx2 && pgEx2.SqlState == "23503":
                statusCode = (int)HttpStatusCode.BadRequest;
                userMessage = "Cannot complete operation due to a foreign key constraint.";
                errorData = new
                {
                    Type = "ForeignKeyViolation",
                    Message = userMessage,
                    Detail = pgEx2.Detail ?? string.Empty
                };
                break;

            case DbUpdateException dbUpdateEx3:
                statusCode = (int)HttpStatusCode.InternalServerError;
                userMessage = "A database error occurred while saving changes.";
                errorData = new
                {
                    Type = "DbUpdateError",
                    Message = _env.IsDevelopment() ? dbUpdateEx3.InnerException?.Message ?? dbUpdateEx3.Message : userMessage
                };
                break;

            // ── Standard .NET exceptions mapped to HTTP codes ────────────
            case UnauthorizedAccessException:
                statusCode = (int)HttpStatusCode.Unauthorized;
                userMessage = exception.Message;
                errorData = new { Type = "Unauthorized", Message = userMessage };
                break;

            case ArgumentException:
                statusCode = (int)HttpStatusCode.BadRequest;
                userMessage = exception.Message;
                errorData = new { Type = "ArgumentError", Message = userMessage };
                break;

            case InvalidOperationException:
                statusCode = (int)HttpStatusCode.BadRequest;
                userMessage = exception.Message;
                errorData = new { Type = "InvalidOperation", Message = userMessage };
                break;

            case TimeoutException:
                statusCode = (int)HttpStatusCode.GatewayTimeout;
                userMessage = "The request timed out. Please try again.";
                errorData = new { Type = "Timeout", Message = userMessage };
                break;

            case OperationCanceledException:
                statusCode = 499; // Client Closed Request
                userMessage = "The request was cancelled.";
                errorData = new { Type = "Cancelled", Message = userMessage };
                break;

            // ── Catch-all for truly unexpected exceptions ────────────────
            default:
                statusCode = (int)HttpStatusCode.InternalServerError;
                userMessage = "An unexpected error occurred.";
                errorData = new
                {
                    Type = "ServerError",
                    Message = _env.IsDevelopment() ? exception.Message : "An internal server error occurred."
                };
                break;
        }

        context.Response.StatusCode = statusCode;

        var envelope = new
        {
            timestamp = DateTime.UtcNow.ToString("o"),
            status = statusCode,
            success = false,
            message = userMessage,
            path = context.Request.Path.Value ?? string.Empty,
            method = context.Request.Method ?? string.Empty,
            data = errorData
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(envelope, JsonOptions));
    }

    // ── Helpers for parsing Postgres constraint details ───────────────────

    private static string? ParseFieldFromDetail(string? detail)
    {
        if (string.IsNullOrEmpty(detail)) return null;
        try
        {
            var start = detail.IndexOf('(');
            var end = detail.IndexOf(')');
            if (start >= 0 && end > start)
            {
                var inside = detail.Substring(start + 1, end - start - 1);
                var cols = inside.Split(',');
                return cols.Length > 0 ? cols[0].Trim() : null;
            }
        }
        catch { /* ignore parse errors */ }
        return null;
    }

    private static string? ParseFieldFromConstraint(string? constraintName)
    {
        if (string.IsNullOrEmpty(constraintName)) return null;
        var parts = constraintName.Split('_');
        return parts.Length >= 3 ? parts[^1] : null;
    }
}
