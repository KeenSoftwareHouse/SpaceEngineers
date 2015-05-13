namespace VRage
{
    public interface IResourceLock
    {
        /// <summary>
        /// Acquires the lock in exclusive mode, blocking if necessary.
        /// </summary>
        void AcquireExclusive();

        /// <summary>
        /// Acquires the lock in shared mode, blocking if necessary.
        /// </summary>
        void AcquireShared();

        /// <summary>
        /// Releases the lock in exclusive mode.
        /// </summary>
        void ReleaseExclusive();

        /// <summary>
        /// Releases the lock in shared mode.
        /// </summary>
        void ReleaseShared();

        /// <summary>
        /// Attempts to acquire the lock in exclusive mode.
        /// </summary>
        /// <returns>Whether the lock was acquired.</returns>
        bool TryAcquireExclusive();

        /// <summary>
        /// Attempts to acquire the lock in shared mode.
        /// </summary>
        /// <returns>Whether the lock was acquired.</returns>
        bool TryAcquireShared();
    }
}
