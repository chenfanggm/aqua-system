namespace com.aqua.system
{
    /// <summary>
    /// Commands that can be cancelled mid-execution.
    /// </summary>
    public interface ICancellableCommand : ICommand
    {
        /// <summary>
        /// Called when command is cancelled.
        /// Use this to clean up resources, reset state, etc.
        /// </summary>
        void OnCancelled();
    }
}

