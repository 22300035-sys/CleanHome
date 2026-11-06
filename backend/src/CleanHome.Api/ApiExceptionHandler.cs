using System.Text.Json;
using CleanHome.Application.Marketplace;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CleanHome.Api;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var pg = exception as PostgresException ?? exception.InnerException as PostgresException;
        var status = exception switch
        {
            BusinessException business => business.StatusCode,
            DbUpdateConcurrencyException => 409,
            InvalidOperationException when exception.StackTrace?.Contains("CleanHome.Domain.Entities") == true => 409,
            JsonException => 400,
            _ when pg?.SqlState is "23505" or "23503" or "40001" or "40P01" or "23P01" => 409,
            _ => 500
        };
        var detail = exception is BusinessException || status == 409 && exception is InvalidOperationException
            ? exception.Message : status == 409 ? "Conflicto de datos; recarga y reintenta." : status == 400 ? "JSON inválido." : "No fue posible completar la operación.";
        if (status >= 500) logger.LogError(exception, "Error HTTP {TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status, Title = detail, Extensions = { ["traceId"] = context.TraceIdentifier }
        }, cancellationToken: ct);
        return true;
    }
}
