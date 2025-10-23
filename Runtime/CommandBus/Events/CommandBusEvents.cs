using System;

namespace com.aqua.system
{
    /// <summary>
    /// Default implementation of command bus events.
    /// </summary>
    public class CommandBusEvents : ICommandBusEvents
    {
        public event Action<CommandEventArgs> OnCommandEnqueued;
        public event Action<CommandEventArgs> OnCommandExecuting;
        public event Action<CommandEventArgs> OnCommandCompleted;
        public event Action<CommandEventArgs> OnCommandFailed;
        public event Action<CommandEventArgs> OnCommandCancelled;

        public void RaiseEnqueued(ICommand command)
        {
            OnCommandEnqueued?.Invoke(new CommandEventArgs(command));
        }

        public void RaiseExecuting(ICommand command)
        {
            OnCommandExecuting?.Invoke(new CommandEventArgs(command));
        }

        public void RaiseCompleted(ICommand command, CommandResult result)
        {
            OnCommandCompleted?.Invoke(new CommandEventArgs(command, result));
        }

        public void RaiseFailed(ICommand command, CommandResult result)
        {
            OnCommandFailed?.Invoke(new CommandEventArgs(command, result));
        }

        public void RaiseCancelled(ICommand command)
        {
            OnCommandCancelled?.Invoke(new CommandEventArgs(command, CommandResult.Cancelled()));
        }

        /// <summary>
        /// Clear all subscribers. Call on cleanup/dispose.
        /// </summary>
        public void ClearAll()
        {
            OnCommandEnqueued = null;
            OnCommandExecuting = null;
            OnCommandCompleted = null;
            OnCommandFailed = null;
            OnCommandCancelled = null;
        }
    }
}

