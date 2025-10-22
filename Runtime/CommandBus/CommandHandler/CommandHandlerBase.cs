using System.Threading;
using Cysharp.Threading.Tasks;

namespace com.aqua.command
{
    public abstract class CommandHandlerBase<TCommand> : ICommandHandler<TCommand> where TCommand : ICommand
    {
        public virtual ValidationResult Validate(TCommand command) => ValidationResult.Valid();
        public abstract UniTask<CommandResult> ExecuteAsync(TCommand command, CancellationToken cancellationToken);
    }
}
