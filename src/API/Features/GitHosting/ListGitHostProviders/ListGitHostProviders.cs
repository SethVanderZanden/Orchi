using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Git.Hosting;

namespace Orchi.Api.Features.GitHosting.ListGitHostProviders;

public static class ListGitHostProviders
{
    public sealed record Response(
        string ProviderId,
        string DisplayName,
        string RequiredCli,
        string ConfigureHint);

    public sealed record Query : IQuery<IReadOnlyList<Response>>;

    internal sealed class Handler(IGitHostingFacade facade)
        : IQueryHandler<Query, IReadOnlyList<Response>>
    {
        public Task<Result<IReadOnlyList<Response>>> Handle(Query query, CancellationToken cancellationToken)
        {
            IReadOnlyList<Response> providers = facade.ListProviders()
                .Select(provider => new Response(
                    provider.ProviderId,
                    provider.DisplayName,
                    provider.RequiredCli,
                    provider.ConfigureHint))
                .ToArray();

            return Task.FromResult(Result.Success(providers));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/git/hosting/providers", Handle)
                .WithName("ListGitHostProviders")
                .WithTags("GitHosting")
                .Produces<IReadOnlyList<Response>>();
        }

        private static async Task<IResult> Handle(
            IQueryHandler<Query, IReadOnlyList<Response>> handler,
            CancellationToken cancellationToken)
        {
            Result<IReadOnlyList<Response>> result = await handler.Handle(new Query(), cancellationToken);
            return result.ToProblem();
        }
    }
}
