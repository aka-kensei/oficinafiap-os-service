using System.Net;
using System.Text.Json;
using Oficina.OS.Domain.Exceptions;

namespace Oficina.OS.Api.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
        catch (DomainException ex)
        {
            _logger.LogWarning(
                "Regra de negócio violada: {Mensagem} | Path: {Path} | TraceId: {TraceId}",
                ex.Message, context.Request.Path, context.TraceIdentifier);
            await EscreverRespostaAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado | Path: {Path} | TraceId: {TraceId}",
                context.Request.Path, context.TraceIdentifier);
            await EscreverRespostaAsync(context, HttpStatusCode.InternalServerError,
                "Ocorreu um erro interno. Por favor, tente novamente.");
        }
    }

    private static async Task EscreverRespostaAsync(HttpContext context, HttpStatusCode statusCode, string mensagem)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var corpo = JsonSerializer.Serialize(new
        {
            error = mensagem,
            traceId = context.TraceIdentifier
        }, JsonOpts);

        await context.Response.WriteAsync(corpo);
    }
}
