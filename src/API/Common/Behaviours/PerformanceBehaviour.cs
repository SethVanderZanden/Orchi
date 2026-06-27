using System.Diagnostics;
using Microsoft.Extensions.Options;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Results;

namespace Orchi.Api.Common.Behaviours;

internal static class PerformanceBehaviour
{
    internal sealed class QueryHandler<TQuery, TResponse>(
        IQueryHandler<TQuery, TResponse> innerHandler,
        ILogger<IQueryHandler<TQuery, TResponse>> logger,
        IOptions<PerformanceOptions> options)
        : IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
        {
            string queryName = typeof(TQuery).Name;
            var stopwatch = Stopwatch.StartNew();

            Result<TResponse> result = await innerHandler.Handle(query, cancellationToken);

            stopwatch.Stop();
            LogExecution(logger, "Query", queryName, stopwatch.ElapsedMilliseconds, options.Value.SlowQueryThresholdMs, result);

            return result;
        }
    }

    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        ILogger<ICommandHandler<TCommand, TResponse>> logger,
        IOptions<PerformanceOptions> options)
        : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
        {
            string commandName = typeof(TCommand).Name;
            var stopwatch = Stopwatch.StartNew();

            Result<TResponse> result = await innerHandler.Handle(command, cancellationToken);

            stopwatch.Stop();
            LogExecution(logger, "Command", commandName, stopwatch.ElapsedMilliseconds, options.Value.SlowCommandThresholdMs, result);

            return result;
        }
    }

    internal sealed class CommandBaseHandler<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        ILogger<ICommandHandler<TCommand>> logger,
        IOptions<PerformanceOptions> options)
        : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
        {
            string commandName = typeof(TCommand).Name;
            var stopwatch = Stopwatch.StartNew();

            Result result = await innerHandler.Handle(command, cancellationToken);

            stopwatch.Stop();
            LogExecution(logger, "Command", commandName, stopwatch.ElapsedMilliseconds, options.Value.SlowCommandThresholdMs, result);

            return result;
        }
    }

    private static void LogExecution(
        ILogger logger,
        string requestType,
        string name,
        long elapsedMs,
        int thresholdMs,
        Result result)
    {
        if (elapsedMs >= thresholdMs)
        {
            if (result.IsSuccess)
            {
                logger.LogWarning(
                    "Slow {RequestType} {Name} completed in {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
                    requestType,
                    name,
                    elapsedMs,
                    thresholdMs);
            }
            else
            {
                logger.LogWarning(
                    "Slow {RequestType} {Name} completed in {ElapsedMs}ms with error {ErrorCode} (threshold: {ThresholdMs}ms)",
                    requestType,
                    name,
                    elapsedMs,
                    result.Error.Code,
                    thresholdMs);
            }

            return;
        }

        if (result.IsSuccess)
        {
            logger.LogInformation(
                "{RequestType} {Name} completed in {ElapsedMs}ms",
                requestType,
                name,
                elapsedMs);
        }
        else
        {
            logger.LogInformation(
                "{RequestType} {Name} completed in {ElapsedMs}ms with error {ErrorCode}",
                requestType,
                name,
                elapsedMs,
                result.Error.Code);
        }
    }
}
