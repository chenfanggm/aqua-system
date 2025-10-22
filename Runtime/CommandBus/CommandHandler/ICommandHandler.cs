using System.Threading;
using Cysharp.Threading.Tasks;

namespace com.aqua.command
{
    /// <summary>
    /// Command handler interface for async execution.
    /// Handlers contain the business logic for executing commands.
    /// </summary>
    /// <typeparam name="TCommand">The command type this handler processes</typeparam>
    public interface ICommandHandler<in TCommand> where TCommand : ICommand
    {
        /// <summary>
        /// Validate the command against current game state and rules.
        /// </summary>
        /// <param name="command">The command to validate</param>
        /// <returns>Validation result</returns>
        ValidationResult Validate(TCommand command);

        /// <summary>
        /// Execute the command asynchronously.
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <param name="cancellationToken">Token to cancel execution</param>
        /// <returns>Result of command execution</returns>
        UniTask<CommandResult> ExecuteAsync(TCommand command, CancellationToken cancellationToken);
    }
}

