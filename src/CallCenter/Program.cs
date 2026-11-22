using System.Threading.RateLimiting;
using CallCenter.Application.Abstractions;
using CallCenter.Application.Services;
using CallCenter.Application.UseCases;
using CallCenter.Configuration;
using CallCenter.Infrastructure.OpenAI;
using CallCenter.Infrastructure.Repositories;
using CallCenter.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("realtime", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1)
        }));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.AddHttpClient<OpenAiSpeechClient>((services, client) =>
{
    var baseUrl = services.GetRequiredService<IConfiguration>()["OpenAI:BaseUrl"]
        ?? "https://api.openai.com/v1/";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    client.Timeout = TimeSpan.FromMinutes(2);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddScoped<ISpeechTranscriber>(services => services.GetRequiredService<OpenAiSpeechClient>());
builder.Services.AddScoped<IIranianCityRecognizer>(services => services.GetRequiredService<OpenAiSpeechClient>());
builder.Services.AddScoped<ISpeechSynthesizer>(services => services.GetRequiredService<OpenAiSpeechClient>());
builder.Services.AddSingleton<ICityRepository, InMemoryCityRepository>();
builder.Services.AddSingleton<PersianCityMatcher>();
builder.Services.AddScoped<CheckVoiceCoverage>();

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRateLimiter();
app.MapControllers();
app.Run();
