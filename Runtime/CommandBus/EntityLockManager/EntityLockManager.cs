using System.Collections.Generic;
using System.Linq;

namespace com.aqua.system
{
    /// <summary>
    /// Default implementation of entity lock manager.
    /// Uses HashSet for O(1) lookup performance.
    /// </summary>
    public class EntityLockManager : IEntityLockManager
    {
        private readonly HashSet<object> _entityLocks = new HashSet<object>();
        private readonly object _selfLockObject = new object();

        public bool TryAcquireLocks(params object[] keys)
        {
            if (keys == null || keys.Length == 0)
                return true;

            // Remove nulls and duplicates
            var validKeys = keys.Where(k => k != null).Distinct().ToArray();

            if (validKeys.Length == 0)
                return true;

            lock (_selfLockObject)
            {
                // Check if any key is already locked
                if (validKeys.Any(key => _entityLocks.Contains(key)))
                    return false;

                // Acquire all locks atomically
                foreach (var key in validKeys)
                {
                    _entityLocks.Add(key);
                }

                return true;
            }
        }

        public void ReleaseLocks(params object[] keys)
        {
            if (keys == null || keys.Length == 0)
                return;

            lock (_selfLockObject)
            {
                foreach (var key in keys)
                {
                    if (key != null)
                        _entityLocks.Remove(key);
                }
            }
        }

        public bool IsLocked(object key)
        {
            if (key == null)
                return false;

            lock (_selfLockObject)
            {
                return _entityLocks.Contains(key);
            }
        }

        public IReadOnlyCollection<object> GetLockedKeys()
        {
            lock (_selfLockObject)
            {
                return _entityLocks.ToArray();
            }
        }

        /// <summary>
        /// Clear all locks. Use with caution - typically only for cleanup/shutdown.
        /// </summary>
        public void ClearAll()
        {
            lock (_selfLockObject)
            {
                _entityLocks.Clear();
            }
        }
    }
}
