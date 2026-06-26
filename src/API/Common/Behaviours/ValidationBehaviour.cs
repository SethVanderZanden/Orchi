using FluentValidation;
using FluentValidation.Results;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Results;

namespace Orchi.Api.Common.Behaviours;

internal static class ValidationBehaviour
{
    internal sealed class QueryHandler<TQuery, TResponse>(
        IQueryHandler<TQuery, TResponse> innerHandler,
        IEnumerable<IValidator<TQuery>> validators)
        : IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
        {
            ValidationFailure[] failures = await ValidateAsync(query, validators, cancellationToken);

            if (failures.Length > 0)
            {
                return Result.Failure<TResponse>(CreateValidationError(failures));
            }

            return await innerHandler.Handle(query, cancellationToken);
        }
    }

    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        IEnumerable<IValidator<TCommand>> validators)
        : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
        {
            ValidationFailure[] failures = await ValidateAsync(command, validators, cancellationToken);

            if (failures.Length > 0)
            {
                return Result.Failure<TResponse>(CreateValidationError(failures));
            }

            return await innerHandler.Handle(command, cancellationToken);
        }
    }

    internal sealed class CommandBaseHandler<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        IEnumerable<IValidator<TCommand>> validators)
        : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
        {
            ValidationFailure[] failures = await ValidateAsync(command, validators, cancellationToken);

            if (failures.Length > 0)
            {
                return Result.Failure(CreateValidationError(failures));
            }

            return await innerHandler.Handle(command, cancellationToken);
        }
    }

    private static async Task<ValidationFailure[]> ValidateAsync<T>(
        T instance,
        IEnumerable<IValidator<T>> validators,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return [];
        }

        var context = new ValidationContext<T>(instance);

        ValidationResult[] results = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        return results
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToArray();
    }

    private static ValidationError CreateValidationError(ValidationFailure[] failures) =>
        ValidationError.FromErrors(
            failures.Select(f => Error.Validation(f.PropertyName, f.ErrorMessage)));
}
