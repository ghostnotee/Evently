using System.Diagnostics;
using Evently.Common.Application.Messaging;
using Evently.Common.Domain;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Evently.Common.Application.Behaviors;

internal static class LoggingDecorator
{
    internal sealed class CommandHandler<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        ILogger<CommandHandler<TCommand>> logger) : ICommandHandler<TCommand> where TCommand : ICommand
    {
        public async Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken)
        {
            string moduleName = GetModuleName(typeof(TCommand).FullName!);
            string commandName = typeof(TCommand).Name;

            Activity.Current?.SetTag("request.module", moduleName);
            Activity.Current?.SetTag("request.name", commandName);

            logger.LogInformation("Processing command {Command}", commandName);

            Result result = await innerHandler.HandleAsync(command, cancellationToken);

            if (result.IsSuccess)
                logger.LogInformation("Completed command {Command}", commandName);
            else
            {
                using (LogContext.PushProperty("Error", result.Error, true))
                {
                    logger.LogError("Completed command {Command} with error", commandName);
                }
            }

            return result;
        }
    }

    internal sealed class CommandHandlerT<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        ILogger<CommandHandlerT<TCommand, TResponse>> logger) : ICommandHandler<TCommand, TResponse> where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken)
        {
            string moduleName = GetModuleName(typeof(TCommand).FullName!);
            string commandName = typeof(TCommand).Name;

            Activity.Current?.SetTag("request.module", moduleName);
            Activity.Current?.SetTag("request.name", commandName);

            logger.LogInformation("Processing command {Command}", commandName);

            Result<TResponse> result = await innerHandler.HandleAsync(command, cancellationToken);

            if (result.IsSuccess)
                logger.LogInformation("Completed command {Command}", commandName);
            else
            {
                using (LogContext.PushProperty("Error", result.Error, true))
                {
                    logger.LogError("Completed command {Command} with error", commandName);
                }
            }

            return result;
        }
    }

    internal sealed class QueryHandler<TQuery, TResponse>(
        IQueryHandler<TQuery, TResponse> innerHandler,
        ILogger<QueryHandler<TQuery, TResponse>> logger) : IQueryHandler<TQuery, TResponse> where TQuery : IQuery<TResponse>
    {
        public async Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken)
        {
            string moduleName = GetModuleName(typeof(TQuery).FullName!);
            string queryName = typeof(TQuery).Name;

            Activity.Current?.SetTag("request.module", moduleName);
            Activity.Current?.SetTag("request.name", queryName);
            
            logger.LogInformation("Processing query {Query}", queryName);

            Result<TResponse> result = await innerHandler.HandleAsync(query, cancellationToken);

            if (result.IsSuccess)
                logger.LogInformation("Completed query {Query}", queryName);
            else
            {
                using (LogContext.PushProperty("Error", result.Error, true))
                {
                    logger.LogError("Completed query {Query} with error", queryName);
                }
            }

            return result;
        }
    }

    private static string GetModuleName(string requestName) => requestName.Split('.')[2];
}
