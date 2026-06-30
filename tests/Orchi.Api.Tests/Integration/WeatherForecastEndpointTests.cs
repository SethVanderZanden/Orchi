using System.Net;
using System.Net.Http.Json;
using Orchi.Api.Features.Weather.GetForecast;
using Orchi.Api.Tests.Common;

namespace Orchi.Api.Tests.Integration;

public class WeatherForecastEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public WeatherForecastEndpointTests(TestWebApplicationFactory factory)
    {
        factory.InitializeDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetWeatherForecast_ReturnsOkWithForecasts()
    {
        HttpResponseMessage response = await _client.GetAsync("/WeatherForecast");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        List<GetWeatherForecast.Response>? forecasts =
            await response.Content.ReadFromJsonAsync<List<GetWeatherForecast.Response>>();

        Assert.NotNull(forecasts);
        Assert.Equal(5, forecasts.Count);
    }

    [Fact]
    public async Task GetWeatherForecast_WithDaysQuery_ReturnsRequestedCount()
    {
        HttpResponseMessage response = await _client.GetAsync("/WeatherForecast?days=3");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        List<GetWeatherForecast.Response>? forecasts =
            await response.Content.ReadFromJsonAsync<List<GetWeatherForecast.Response>>();

        Assert.NotNull(forecasts);
        Assert.Equal(3, forecasts.Count);
    }

    [Fact]
    public async Task GetWeatherForecast_WithInvalidDays_ReturnsValidationProblem()
    {
        HttpResponseMessage response = await _client.GetAsync("/WeatherForecast?days=99");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
