using CallCenter.Application.Features.WeatherForecast;
using CallCenter.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController(IWeatherForecastService service) : ControllerBase
{
    [HttpGet]
    public IReadOnlyList<WeatherForecast> Get() => service.GetForecasts();
}
