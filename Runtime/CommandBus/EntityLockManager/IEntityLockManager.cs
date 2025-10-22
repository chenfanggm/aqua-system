using System.Collections.Generic;

namespace com.aqua.command
{
    /// <summary>
    /// Manages entity locks to prevent concurrent operations on the same entity.
    /// Supports both single and composite (multi-entity) locks.
    /// Thread-safe for concurrent access.
    /// </summary>
    public interface IEntityLockManager
    {
        /// <summary>
        /// Try to acquire locks for the specified keys.
        /// </summary>
        /// <param name="keys">Keys to lock (entities/resources)</param>
        /// <returns>True if all locks acquired, false if any key is already locked</returns>
        bool TryAcquireLocks(params object[] keys);

        /// <summary>
        /// Release locks for the specified keys.
        /// </summary>
        void ReleaseLocks(params object[] keys);

        /// <summary>
        /// Check if a key is currently locked.
        /// </summary>
        bool IsLocked(object key);

        /// <summary>
        /// Get all currently locked keys.
        /// </summary>
        IReadOnlyCollection<object> GetLockedKeys();
    }
}

