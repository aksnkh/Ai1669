using CallCenter.Application.Abstractions;
using CallCenter.Application.Features.VoiceCoverage;
using CallCenter.Infrastructure.Persistence.Repositories;
using CallCenter.Infrastructure.Speech;
using CallCenter.Infrastructure.Persistence;
using CallCenter.Middlewares;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<SpeechOptions>(builder.Configuration.GetSection("Speech"));
builder.Services.AddHttpClient<ISpeechTranscriber, LocalWhisperTranscriber>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<SpeechOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl.AbsoluteUri.TrimEnd('/') + "/");
    client.Timeout = options.Timeout;
});
builder.Services.AddScoped<ICityRepository, CityRepository>();
builder.Services.AddScoped<VoiceCoverageService>();
builder.Services.AddSingleton<SqlConnectionFactory>(services =>
{
    var configuration = services.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("MainConnection");
    return new SqlConnectionFactory(connectionString ?? string.Empty);
});

var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
