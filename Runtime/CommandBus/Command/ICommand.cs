namespace com.aqua.command
{
    /// <summary>
    /// Marker interface for all commands.
    /// Commands are immutable data objects representing user intent.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Whether this command can run in parallel with other commands.
        /// Default: true (parallel execution allowed)
        /// </summary>
        bool AllowParallelExecution => true;
    }
}

