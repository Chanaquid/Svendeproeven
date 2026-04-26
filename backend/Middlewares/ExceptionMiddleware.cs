using backend.Common;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security;
using System.Text.Json;

namespace backend.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment env)
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
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Bad request: {Message}", ex.Message);
                await HandleExceptionAsync(context, HttpStatusCode.BadRequest, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized: {Message}", ex.Message);
                await HandleExceptionAsync(context, HttpStatusCode.Unauthorized, "Authentication required");
            }
            catch (SecurityException ex)
            {
                _logger.LogWarning(ex, "Forbidden: {Message}", ex.Message);
                await HandleExceptionAsync(context, HttpStatusCode.Forbidden, ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Not found: {Message}", ex.Message);
                await HandleExceptionAsync(context, HttpStatusCode.NotFound, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Conflict: {Message}", ex.Message);
                await HandleExceptionAsync(context, HttpStatusCode.Conflict, ex.Message);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error: {Message}", ex.InnerException?.Message ?? ex.Message);

                var message = _env.IsDevelopment()
                    ? $"Database error: {ex.InnerException?.Message ?? ex.Message}"
                    : "An error occurred while saving data. Please try again.";

                await HandleExceptionAsync(context, HttpStatusCode.BadRequest, message);
            }

            catch (UnauthorizedAppException ex)
            {
                _logger.LogWarning(ex, "Unauthorized: {Message}", ex.Message);
                await HandleExceptionAsync(context, HttpStatusCode.Unauthorized, ex.Message);
            }

            catch (ForbiddenException ex)
            {
                _logger.LogWarning(ex, "Forbidden: {Message}", ex.Message);
                await HandleExceptionAsync(context, HttpStatusCode.Forbidden, ex.Message);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

                var message = _env.IsDevelopment()
                    ? ex.Message
                    : "An unexpected error occurred. Please try again later.";

                await HandleExceptionAsync(context, HttpStatusCode.InternalServerError, message);
            }
        }

        private async Task HandleExceptionAsync(
            HttpContext context,
            HttpStatusCode statusCode,
            string message)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(
                    "Response has already started for request {TraceId}. Cannot return structured error. " +
                    "Original error: {ErrorMessage}",
                    context.TraceIdentifier,
                    message);

                // Just return - don't try to write to response
                return;
            }

            //Clear any existing response content
            context.Response.Clear();
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = new
            {
                Success = false,
                StatusCode = (int)statusCode,
                Message = message,
                TraceId = context.TraceIdentifier
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }

    }
}