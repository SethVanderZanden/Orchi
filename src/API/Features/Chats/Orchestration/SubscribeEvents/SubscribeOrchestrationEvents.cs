using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents.Orchestration;

namespace Orchi.Api.Features.Chats.Orchestration.SubscribeEvents;

public static class SubscribeOrchestrationEvents
{
    public sealed record Query(Guid ParentChatId) : IQuery<OrchestrationSnapshot>;

    internal sealed class Handler(IOrchestrationWorkflowService workflowService)
        : IQueryHandler<Query, OrchestrationSnapshot>
    {
        public Task<Result<OrchestrationSnapshot>> Handle(
            Query query,
            CancellationToken cancellationToken) =>
            workflowService.GetSnapshotAsync(query.ParentChatId, cancellationToken);
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/chats/{parentChatId:guid}/orchestration/events", Handle)
                .WithName("SubscribeOrchestrationEvents")
                .WithTags("Chats");
        }

        private static async Task Handle(
            Guid parentChatId,
            IQueryHandler<Query, OrchestrationSnapshot> handler,
            OrchestrationEventHub eventHub,
            HttpContext httpContext,
            CancellationToken cancellationToken)
        {
            Result<OrchestrationSnapshot> snapshotResult =
                await handler.Handle(new Query(parentChatId), cancellationToken);

            if (snapshotResult.IsFailure)
            {
                await httpContext.Response.WriteErrorAsync(snapshotResult.Error, cancellationToken);
                return;
            }

            httpContext.Response.Headers.CacheControl = "no-cache";
            httpContext.Response.Headers.Connection = "keep-alive";
            httpContext.Response.ContentType = "text/event-stream";

            await OrchestrationSseWriter.WriteEventAsync(
                eventHub,
                httpContext.Response.Body,
                parentChatId,
                new OrchestrationWorkflowEvent(
                    snapshotResult.Value.Status,
                    snapshotResult.Value.CurrentStep,
                    snapshotResult.Value.TotalSteps,
                    snapshotResult.Value.CurrentPlanId),
                cancellationToken);

            foreach (OrchestrationChildSnapshot child in snapshotResult.Value.Children)
            {
                await OrchestrationSseWriter.WriteEventAsync(
                    eventHub,
                    httpContext.Response.Body,
                    parentChatId,
                    new OrchestrationChatCreatedEvent(
                        child.ChatId,
                        child.Mode,
                        parentChatId,
                        child.PlanId,
                        child.PlanFilePath),
                    cancellationToken);
            }

            System.Threading.Channels.ChannelReader<OrchestrationEvent> reader = eventHub.Subscribe(parentChatId);

            try
            {
                await foreach (OrchestrationEvent orchestrationEvent in reader.ReadAllAsync(cancellationToken))
                {
                    await OrchestrationSseWriter.WriteEventAsync(
                        eventHub,
                        httpContext.Response.Body,
                        parentChatId,
                        orchestrationEvent,
                        cancellationToken);
                }
            }
            finally
            {
                eventHub.Unsubscribe(parentChatId, reader);
            }
        }
    }
}
