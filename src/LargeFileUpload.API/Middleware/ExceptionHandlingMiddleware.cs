using System.Net;
using System.Text.Json;
using FluentValidation;
using LargeFileUpload.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace LargeFileUpload.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        var response = context.Response;
        response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Instance = context.Request.Path
        };

        switch (exception)
        {
            case ValidationException validationEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Validation Failed";
                problemDetails.Detail = "One or more validation errors occurred.";
                problemDetails.Extensions["errors"] = validationEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );
                break;

            case DomainValidationException domainEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Domain Validation Error";
                problemDetails.Detail = domainEx.Message;
                break;

            case InvalidUploadStateException stateEx:
                response.StatusCode = (int)HttpStatusCode.Conflict;
                problemDetails.Status = StatusCodes.Status409Conflict;
                problemDetails.Title = "Invalid Upload State";
                problemDetails.Detail = stateEx.Message;
                break;

            case KeyNotFoundException notFoundEx:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                problemDetails.Status = StatusCodes.Status404NotFound;
                problemDetails.Title = "Resource Not Found";
                problemDetails.Detail = notFoundEx.Message;
                break;

            case UnauthorizedAccessException unauthEx:
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                problemDetails.Status = StatusCodes.Status403Forbidden;
                problemDetails.Title = "Access Denied";
                problemDetails.Detail = unauthEx.Message;
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Title = "Internal Server Error";
                problemDetails.Detail = "An unexpected error occurred while processing the request.";
                break;
        }

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(json);
    }
}
