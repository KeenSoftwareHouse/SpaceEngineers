namespace VRage.Scripting
{
    /// <summary>
    /// Determines what quantity a given type key should represent (see individual members)
    /// </summary>
    internal enum TypeKeyQuantity
    {
        /// <summary>
        /// No quantity
        /// </summary>
        None,

        /// <summary>
        /// This specific member only
        /// </summary>
        ThisOnly,

        /// <summary>
        /// This and all nested members
        /// </summary>
        AllMembers,
    }
}