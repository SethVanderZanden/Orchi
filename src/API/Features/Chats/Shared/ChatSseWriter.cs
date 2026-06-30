using System.Text.Json;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;

namespace Orchi.Api.Features.Chats.Shared;

internal static class ChatSseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task WriteEventAsync(
        Stream stream,
        string eventName,
        object payload,
        CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(payload, JsonOptions);
        await using var writer = new StreamWriter(stream, leaveOpen: true);

        await writer.WriteAsync($"event: {eventName}\n");
        await writer.WriteAsync($"data: {json}\n\n");
        await writer.FlushAsync(cancellationToken);
    }

    public static async Task WriteAgentEventAsync(
        Stream stream,
        AgentEvent agentEvent,
        Guid assistantMessageId,
        CancellationToken cancellationToken)
    {
        switch (agentEvent)
        {
            case AgentStatusEvent status:
                await WriteEventAsync(stream, "status", new SseStatusPayload(status.Phase), cancellationToken);
                break;

            case AgentTextDeltaEvent delta:
                await WriteEventAsync(stream, "token", new SseTokenPayload(delta.Text), cancellationToken);
                break;

            case AgentToolEvent tool:
                await WriteEventAsync(
                    stream,
                    "tool",
                    new SseToolPayload(tool.Label),
                    cancellationToken);
                break;

            case AgentCompletedEvent:
                await WriteEventAsync(
                    stream,
                    "done",
                    new SendMessageDoneResponse(assistantMessageId),
                    cancellationToken);
                break;

            case AgentErrorEvent error:
                await WriteEventAsync(
                    stream,
                    "error",
                    new SseErrorPayload(error.Code, error.Message),
                    cancellationToken);
                break;
        }
    }
}
