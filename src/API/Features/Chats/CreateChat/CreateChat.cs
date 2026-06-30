using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;

namespace Orchi.Api.Features.Chats.CreateChat;

public static class CreateChat
{
    public sealed record Command(
        string Agent,
        string WorkspacePath,
        ChatMode Mode,
        Guid? ParentChatId,
        Guid? AttachedPlanId) : ICommand<CreateChatResponse>;

    internal sealed class Handler(AgentSessionManager sessionManager)
        : ICommandHandler<Command, CreateChatResponse>
    {
        public async Task<Result<CreateChatResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            Result<ChatSession> result = await sessionManager.CreateSessionAsync(
                command.Agent,
                command.WorkspacePath,
                command.Mode,
                command.ParentChatId,
                command.AttachedPlanId,
                cancellationToken);

            if (result.IsFailure)
            {
                return Result.Failure<CreateChatResponse>(result.Error);
            }

            ChatSession session = result.Value;
            return Result.Success(new CreateChatResponse(
                session.Id,
                session.AgentId,
                session.WorkspacePath,
                ChatModeParser.ToApiString(session.Mode),
                session.ParentChatId,
                session.AttachedPlanId,
                session.GoalChatId));
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.Agent)
                .NotEmpty()
                .WithMessage("Agent is required.");

            RuleFor(command => command.WorkspacePath)
                .NotEmpty()
                .WithMessage("Workspace path is required.");
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
            if (!ChatModeParser.TryParse(request.Mode, out ChatMode mode))
            {
                return Results.BadRequest(new
                {
                    Code = "Mode.Invalid",
                    Message = $"Invalid chat mode '{request.Mode}'."
                });
            }

            Result<CreateChatResponse> result = await handler.Handle(
                new Command(request.Agent, request.WorkspacePath, mode, request.ParentChatId, request.AttachedPlanId),
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
