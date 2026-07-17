using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;

namespace Orchi.Api.Features.Chats.UpdateChatApprovalPolicy;

public static class UpdateChatApprovalPolicy
{
    public sealed record Command(Guid ChatId, string? ApprovalPolicyId)
        : ICommand<UpdateChatApprovalPolicyResponse>;

    internal sealed class Handler(AgentSessionManager sessionManager)
        : ICommandHandler<Command, UpdateChatApprovalPolicyResponse>
    {
        public async Task<Result<UpdateChatApprovalPolicyResponse>> Handle(
            Command command,
            CancellationToken cancellationToken)
        {
            Result<ChatSession> result = await sessionManager.UpdateApprovalPolicyAsync(
                command.ChatId,
                command.ApprovalPolicyId,
                cancellationToken);

            if (result.IsFailure)
            {
                return Result.Failure<UpdateChatApprovalPolicyResponse>(result.Error);
            }

            ChatSession session = result.Value;
            return Result.Success(
                new UpdateChatApprovalPolicyResponse(session.Id, session.ApprovalPolicyId));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("/chats/{chatId:guid}/approval-policy", Handle)
                .WithName("UpdateChatApprovalPolicy")
                .WithTags("Chats")
                .Produces<UpdateChatApprovalPolicyResponse>();
        }

        private static async Task<IResult> Handle(
            Guid chatId,
            UpdateChatApprovalPolicyRequest request,
            ICommandHandler<Command, UpdateChatApprovalPolicyResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<UpdateChatApprovalPolicyResponse> result = await handler.Handle(
                new Command(chatId, request.ApprovalPolicyId),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
