using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;

namespace Orchi.Api.Features.Chats.CreateChat;

public static class CreateChat
{
    public sealed record Command(string Agent, string WorkspacePath) : ICommand<CreateChatResponse>;

    internal sealed class Handler(AgentSessionManager sessionManager)
        : ICommandHandler<Command, CreateChatResponse>
    {
        public Task<Result<CreateChatResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            Result<ChatSession> result = sessionManager.CreateSession(command.Agent, command.WorkspacePath);

            if (result.IsFailure)
            {
                return Task.FromResult(Result.Failure<CreateChatResponse>(result.Error));
            }

            ChatSession session = result.Value;
            return Task.FromResult(Result.Success(new CreateChatResponse(
                session.Id,
                session.AgentId,
                session.WorkspacePath)));
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
            Result<CreateChatResponse> result = await handler.Handle(
                new Command(request.Agent, request.WorkspacePath),
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
