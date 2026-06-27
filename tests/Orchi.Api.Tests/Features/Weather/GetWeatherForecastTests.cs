using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Behaviours;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Weather.GetForecast;
using Orchi.Api.Tests.Common;
using Microsoft.Extensions.DependencyInjection;

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

    [Fact]
    public async Task PerformanceBehaviour_DelegatesToInnerHandler()
    {
        var innerHandler = new GetWeatherForecast.Handler();
        var behaviour = new PerformanceBehaviour.QueryHandler<GetWeatherForecast.Query, IReadOnlyList<GetWeatherForecast.Response>>(
            innerHandler,
            NullLogger<IQueryHandler<GetWeatherForecast.Query, IReadOnlyList<GetWeatherForecast.Response>>>.Instance,
            Options.Create(new PerformanceOptions { SlowQueryThresholdMs = 500 }));

        Result<IReadOnlyList<GetWeatherForecast.Response>> result =
            await behaviour.Handle(new GetWeatherForecast.Query(Days: 5), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Count);
    }

    [Fact]
    public async Task PerformanceBehaviour_LogsWarningWhenQueryExceedsThreshold()
    {
        var innerHandler = new SlowQueryHandler();
        var logger = new CollectingLogger<IQueryHandler<GetWeatherForecast.Query, IReadOnlyList<GetWeatherForecast.Response>>>();
        var behaviour = new PerformanceBehaviour.QueryHandler<GetWeatherForecast.Query, IReadOnlyList<GetWeatherForecast.Response>>(
            innerHandler,
            logger,
            Options.Create(new PerformanceOptions { SlowQueryThresholdMs = 10 }));

        Result<IReadOnlyList<GetWeatherForecast.Response>> result =
            await behaviour.Handle(new GetWeatherForecast.Query(Days: 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Warning &&
            entry.Message.Contains("Slow Query Query", StringComparison.Ordinal) &&
            entry.Message.Contains("threshold: 10ms", StringComparison.Ordinal));
    }

    private sealed class SlowQueryHandler : IQueryHandler<GetWeatherForecast.Query, IReadOnlyList<GetWeatherForecast.Response>>
    {
        public async Task<Result<IReadOnlyList<GetWeatherForecast.Response>>> Handle(
            GetWeatherForecast.Query query,
            CancellationToken cancellationToken)
        {
            await Task.Delay(50, cancellationToken);
            return await new GetWeatherForecast.Handler().Handle(query, cancellationToken);
        }
    }
}
