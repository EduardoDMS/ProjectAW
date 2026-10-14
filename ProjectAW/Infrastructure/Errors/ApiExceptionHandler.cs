using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProjectAW.Models;

namespace ProjectAW.Infrastructure.Errors;

/// <summary>
/// Converts known application failures to safe, consistent Problem Details responses.
/// Unexpected exceptions are logged in full but never disclosed to API clients.
/// </summary>
public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<ApiExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            return true;

        var (status, title, safeMessage) = Map(exception);

        if (status >= StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled request error. TraceId: {TraceId}", httpContext.TraceIdentifier);
        else
            _logger.LogWarning(exception, "Request rejected with HTTP {StatusCode}. TraceId: {TraceId}", status, httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = status;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = safeMessage,
            Instance = httpContext.Request.Path
        };

        // Keep the existing Spanish property used by current front-end/API consumers.
        problem.Extensions["mensaje"] = safeMessage;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        await _problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem
        });

        return true;
    }

    private static (int Status, string Title, string SafeMessage) Map(Exception exception)
    {
        return exception switch
        {
            NoEncontradoException => (
                StatusCodes.Status404NotFound,
                "Recurso no encontrado",
                exception.Message),
            ConflictoNegocioException => (
                StatusCodes.Status409Conflict,
                "Conflicto de negocio",
                exception.Message),
            ReglaNegocioException => (
                StatusCodes.Status400BadRequest,
                "Solicitud no válida",
                exception.Message),
            DbUpdateException dbException when FindSqlErrorNumber(dbException) is 2601 or 2627 => (
                StatusCodes.Status409Conflict,
                "Conflicto de datos",
                "Ya existe un registro con esos datos."),
            DbUpdateException dbException when FindSqlErrorNumber(dbException) == 547 => (
                StatusCodes.Status409Conflict,
                "Conflicto de datos",
                "No se puede completar la operación porque el registro está relacionado con otros datos."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Error interno",
                "Ocurrió un error interno. Intenta nuevamente o contacta al administrador.")
        };
    }

    private static int? FindSqlErrorNumber(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is SqlException sqlException)
                return sqlException.Number;
        }

        return null;
    }
}
