using System;

namespace com.aqua.command
{
    /// <summary>
    /// Event dispatcher for command bus events.
    /// Uses instance-based events instead of static to support multiple command buses
    /// and proper cleanup. Follows Dependency Inversion Principle.
    /// </summary>
    public interface ICommandBusEvents
    {
        /// <summary>
        /// Fired when a command is enqueued.
        /// </summary>
        event Action<CommandEventArgs> OnCommandEnqueued;

        /// <summary>
        /// Fired when a command starts executing.
        /// </summary>
        event Action<CommandEventArgs> OnCommandExecuting;

        /// <summary>
        /// Fired when a command completes successfully.
        /// </summary>
        event Action<CommandEventArgs> OnCommandCompleted;

        /// <summary>
        /// Fired when a command fails.
        /// </summary>
        event Action<CommandEventArgs> OnCommandFailed;

        /// <summary>
        /// Fired when a command is cancelled.
        /// </summary>
        event Action<CommandEventArgs> OnCommandCancelled;
    }
}

