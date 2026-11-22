using System.Text.Json;
using CallCenter.Infrastructure.OpenAI;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Middleware;

public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            OpenAiConfigurationException => (503, "تنظیمات سرویس صوتی کامل نیست."),
            OperationCanceledException => (504, "زمان انتظار برای سرویس صوتی به پایان رسید."),
            HttpRequestException or JsonException => (502, "سرویس صوتی پاسخ معتبری ارائه نکرد."),
            _ => (500, "خطای داخلی سرور.")
        };

        context.Response.StatusCode = status;
        var details = new ProblemDetails { Status = status, Title = title };
        if (!await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = details
        }))
        {
            await context.Response.WriteAsJsonAsync(details, options: null, contentType: "application/problem+json", cancellationToken);
        }
        return true;
    }
}
