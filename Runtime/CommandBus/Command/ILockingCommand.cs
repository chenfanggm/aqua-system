namespace com.aqua.command
{
    /// <summary>
    /// Commands that need to lock entities during execution.
    /// Prevents concurrent operations on the same entity.
    /// </summary>
    public interface ILockingCommand : ICommand
    {
        /// <summary>
        /// The entities/resources this command needs to lock.
        /// Can be single object or composite (multiple entities).
        /// </summary>
        object[] GetLockKeys();
    }
}

