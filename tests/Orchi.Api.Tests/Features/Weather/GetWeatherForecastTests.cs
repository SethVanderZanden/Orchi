using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Behaviours;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Weather.GetForecast;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Orchi.Api.Tests.Features.Weather;

public class GetWeatherForecastTests
{
    [Fact]
    public async Task Handler_ReturnsRequestedNumberOfForecasts()
    {
        var handler = new GetWeatherForecast.Handler();

        Result<IReadOnlyList<GetWeatherForecast.Response>> result =
            await handler.Handle(new GetWeatherForecast.Query(Days: 3), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
    }

    [Fact]
    public async Task Handler_ReturnsForecastsWithExpectedShape()
    {
        var handler = new GetWeatherForecast.Handler();

        Result<IReadOnlyList<GetWeatherForecast.Response>> result =
            await handler.Handle(new GetWeatherForecast.Query(Days: 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        GetWeatherForecast.Response forecast = result.Value[0];
        Assert.NotEqual(default, forecast.Date);
        Assert.False(string.IsNullOrEmpty(forecast.Summary));
    }

    [Fact]
    public async Task ValidationBehaviour_RejectsDaysOutsideRange()
    {
        var innerHandler = new GetWeatherForecast.Handler();
        var validator = new GetWeatherForecast.Validator();
        var behaviour = new ValidationBehaviour.QueryHandler<GetWeatherForecast.Query, IReadOnlyList<GetWeatherForecast.Response>>(
            innerHandler,
            [validator]);

        Result<IReadOnlyList<GetWeatherForecast.Response>> result =
            await behaviour.Handle(new GetWeatherForecast.Query(Days: 99), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.IsType<ValidationError>(result.Error);
    }

    [Fact]
    public async Task LoggingBehaviour_DelegatesToInnerHandler()
    {
        var innerHandler = new GetWeatherForecast.Handler();
        var behaviour = new LoggingBehaviour.QueryHandler<GetWeatherForecast.Query, IReadOnlyList<GetWeatherForecast.Response>>(
            innerHandler,
            NullLogger<IQueryHandler<GetWeatherForecast.Query, IReadOnlyList<GetWeatherForecast.Response>>>.Instance);

        Result<IReadOnlyList<GetWeatherForecast.Response>> result =
            await behaviour.Handle(new GetWeatherForecast.Query(Days: 5), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Count);
    }
}
