namespace com.aqua.command
{
    /// <summary>
    /// Main command bus interface.
    /// Follows Command Pattern and Mediator Pattern.
    /// Decouples command creation from execution.
    /// </summary>
    public interface ICommandBus
    {
        /// <summary>
        /// Get the events for the command bus.
        /// </summary>
        ICommandBusEvents Events { get; }

        /// <summary>
        /// Register a command handler.
        /// </summary>
        void RegisterHandler<T>(ICommandHandler<T> handler) where T : ICommand;

        /// <summary>
        /// Enqueue a command for execution.
        /// </summary>
        void Enqueue(ICommand command);

        /// <summary>
        /// Cancel a specific command if it's still in queue or executing.
        /// </summary>
        bool TryCancel(ICommand command);

        /// <summary>
        /// Cancel all commands in queue and stop processing.
        /// </summary>
        void CancelAll();

        /// <summary>
        /// Get number of commands in queue.
        /// </summary>
        int QueuedCommandCount { get; }

        /// <summary>
        /// Get number of currently executing commands.
        /// </summary>
        int ExecutingCommandCount { get; }
    }
}

