namespace Sandbox.ModAPI.Ingame
{
    /// <summary>
    /// Structure returned in list by IMyAssembler.GetQueueItems();
    /// </summary>
    public struct AssemblerQueueItem
    {
        /// <summary>
        /// Index of item/position in queue, 0 = first slot
        /// </summary>
        public int idx;
        /// <summary>
        /// Item type, i.e. 'MyObjectBuilder_Component' or 'MyObjectBuilder_Ingot'
        /// </summary>
        public string itemType;
        /// <summary>
        /// Short name of item, i.e. 'SteelPlate' or 'Reactor'
        /// </summary>
        public string subtypeName;
        /// <summary>
        /// Amount of item
        /// </summary>
        public int amount;
    }
}
