using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;

namespace Orchi.Api.Features.Chats.UpdateChatWorkspace;

public static class UpdateChatWorkspace
{
    public sealed record Command(Guid ChatId, Guid WorkspaceId) : ICommand<UpdateChatWorkspaceResponse>;

    internal sealed class Handler(AgentSessionManager sessionManager)
        : ICommandHandler<Command, UpdateChatWorkspaceResponse>
    {
        public async Task<Result<UpdateChatWorkspaceResponse>> Handle(
            Command command,
            CancellationToken cancellationToken)
        {
            Result<ChatSession> result = await sessionManager.UpdateWorkspaceAsync(
                command.ChatId,
                command.WorkspaceId,
                cancellationToken);

            if (result.IsFailure)
            {
                return Result.Failure<UpdateChatWorkspaceResponse>(result.Error);
            }

            ChatSession session = result.Value;
            return Result.Success(new UpdateChatWorkspaceResponse(
                session.Id,
                session.ProjectId,
                session.WorkspaceId,
                session.WorkspacePath));
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.ChatId).NotEmpty();
            RuleFor(command => command.WorkspaceId).NotEmpty();
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("/chats/{chatId:guid}/workspace", Handle)
                .WithName("UpdateChatWorkspace")
                .WithTags("Chats")
                .Produces<UpdateChatWorkspaceResponse>();
        }

        private static async Task<IResult> Handle(
            Guid chatId,
            UpdateChatWorkspaceRequest request,
            ICommandHandler<Command, UpdateChatWorkspaceResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<UpdateChatWorkspaceResponse> result = await handler.Handle(
                new Command(chatId, request.WorkspaceId),
                cancellationToken);

            return result.ToProblem();
        }
    }
}

public sealed record UpdateChatWorkspaceRequest(Guid WorkspaceId);

public sealed record UpdateChatWorkspaceResponse(
    Guid ChatId,
    Guid? ProjectId,
    Guid? WorkspaceId,
    string WorkspacePath);
