namespace VRage.ModAPI
{
    public interface IMyRemapHelper
    {
        /// <summary>
        /// Returns a new entity ID for the entity with the given old entity ID.
        /// The function will return the same new entityId only if the saveMapping argument is set to true.
        /// </summary>
        long RemapEntityId(long oldEntityId);

        /// <summary>
        /// Returns a new ID for the given old ID for specific group (multiblockIDs, ...).
        /// </summary>
        int RemapGroupId(string group, int oldValue);

        /// <summary>
        /// Clears all the saved mappings from the remap helper and gets it ready for the next remapping operation.
        /// </summary>
        void Clear();
    }
}
