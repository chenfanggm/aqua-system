using System;

namespace com.aqua.command
{
    /// <summary>
    /// Event args for command lifecycle events.
    /// Provides context for logging, debugging, and UI feedback.
    /// </summary>
    public class CommandEventArgs
    {
        public ICommand Command { get; }
        public CommandResult Result { get; }
        public DateTime Timestamp { get; }

        public CommandEventArgs(ICommand command, CommandResult result = default)
        {
            Command = command;
            Result = result;
            Timestamp = DateTime.UtcNow;
        }
    }
}

