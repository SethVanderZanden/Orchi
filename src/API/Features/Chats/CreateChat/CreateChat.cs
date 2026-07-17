using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;

namespace Orchi.Api.Features.Chats.CreateChat;

public static class CreateChat
{
    public sealed record Command(
        Guid WorkspaceId,
        string? Agent,
        string? Mode,
        string? ModelId,
        string? ContextSizeId,
        string? ReasoningEffortId,
        string? ApprovalPolicyId) : ICommand<CreateChatResponse>;

    internal sealed class Handler(AgentSessionManager sessionManager)
        : ICommandHandler<Command, CreateChatResponse>
    {
        public async Task<Result<CreateChatResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            Result<ChatSession> result = await sessionManager.CreateSessionAsync(
                command.WorkspaceId,
                agentId: command.Agent,
                mode: command.Mode,
                modelId: command.ModelId,
                contextSizeId: command.ContextSizeId,
                reasoningEffortId: command.ReasoningEffortId,
                approvalPolicyId: command.ApprovalPolicyId,
                cancellationToken: cancellationToken);

            if (result.IsFailure)
            {
                return Result.Failure<CreateChatResponse>(result.Error);
            }

            ChatSession session = result.Value;
            return Result.Success(new CreateChatResponse(
                session.Id,
                session.AgentId,
                session.ProjectId,
                session.WorkspaceId,
                session.WorkspacePath,
                session.Mode,
                session.ModelId,
                session.ContextSizeId,
                session.ReasoningEffortId,
                session.ApprovalPolicyId,
                session.ParentChatId,
                session.PlanFilePath));
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.WorkspaceId)
                .NotEmpty()
                .WithMessage("Workspace id is required.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/chats", Handle)
                .WithName("CreateChat")
                .WithTags("Chats")
                .Produces<CreateChatResponse>(StatusCodes.Status201Created);
        }

        private static async Task<IResult> Handle(
            CreateChatRequest request,
            ICommandHandler<Command, CreateChatResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<CreateChatResponse> result = await handler.Handle(
                new Command(
                    request.WorkspaceId,
                    request.Agent,
                    request.Mode,
                    request.ModelId,
                    request.ContextSizeId,
                    request.ReasoningEffortId,
                    request.ApprovalPolicyId),
                cancellationToken);

            if (result.IsSuccess)
            {
                CreateChatResponse response = result.Value;
                return Results.Created($"/chats/{response.Id}", response);
            }

            return result.ToProblem();
        }
    }
}
