namespace CallCenter.Application.Features.WeatherForecast;

public sealed class WeatherForecastService : IWeatherForecastService
{
    private static readonly string[] Summaries =
        ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

    public IReadOnlyList<Domain.Entities.WeatherForecast> GetForecasts() =>
        Enumerable.Range(1, 5).Select(index => new Domain.Entities.WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        }).ToArray();
}
