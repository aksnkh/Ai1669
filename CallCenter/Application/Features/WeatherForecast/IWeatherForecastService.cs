namespace CallCenter.Application.Features.WeatherForecast;

public interface IWeatherForecastService
{
    IReadOnlyList<Domain.Entities.WeatherForecast> GetForecasts();
}
