using CallCenter.Application.Common;
namespace CallCenter.Middlewares;
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error. TraceId: {TraceId}", context.TraceIdentifier);
            if (context.Response.HasStarted) throw;
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new Response<object>
            {
                statusCode = 500,
                title = "خطای سرور",
                message = "دریافت اطلاعات با خطا مواجه شد.",
                messageLevel = MessageLevel.Cif
            }, context.RequestAborted);
        }
    }
}