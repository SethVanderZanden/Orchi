using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents.Orchestration;

namespace Orchi.Api.Features.Chats.Orchestration.SubscribeEvents;

public static class SubscribeOrchestrationEvents
{
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
            IOrchestrationWorkflowService workflowService,
            OrchestrationEventHub eventHub,
            HttpContext httpContext,
            CancellationToken cancellationToken)
        {
            Result<OrchestrationSnapshot> snapshotResult =
                await workflowService.GetSnapshotAsync(parentChatId, cancellationToken);

            if (snapshotResult.IsFailure)
            {
                int statusCode = snapshotResult.Error.Code == "NotFound"
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status400BadRequest;

                httpContext.Response.StatusCode = statusCode;

                await httpContext.Response.WriteAsJsonAsync(new
                {
                    Code = snapshotResult.Error.Code,
                    Message = snapshotResult.Error.Message
                }, cancellationToken);
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
