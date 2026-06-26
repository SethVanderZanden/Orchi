using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;

namespace Orchi.Api.Features.Weather.GetForecast;

public static class GetWeatherForecast
{
    public sealed record Query(int Days = 5) : IQuery<IReadOnlyList<Response>>;

    public sealed record Response(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }

    internal sealed class Handler : IQueryHandler<Query, IReadOnlyList<Response>>
    {
        private static readonly string[] Summaries =
        [
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        ];

        public Task<Result<IReadOnlyList<Response>>> Handle(Query query, CancellationToken cancellationToken)
        {
            IReadOnlyList<Response> forecasts = Enumerable.Range(1, query.Days)
                .Select(index => new Response(
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    Summaries[Random.Shared.Next(Summaries.Length)]))
                .ToArray();

            return Task.FromResult(Result.Success(forecasts));
        }
    }

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.Days)
                .InclusiveBetween(1, 14)
                .WithMessage("Days must be between 1 and 14.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/WeatherForecast", Handle)
                .WithName("GetWeatherForecast")
                .WithTags("Weather")
                .Produces<IReadOnlyList<Response>>();
        }

        private static async Task<IResult> Handle(
            [AsParameters] Query query,
            IQueryHandler<Query, IReadOnlyList<Response>> handler,
            CancellationToken cancellationToken)
        {
            Result<IReadOnlyList<Response>> result = await handler.Handle(query, cancellationToken);
            return result.ToProblem();
        }
    }
}
