namespace VRage.Scripting
{
    /// <summary>
    ///     API target
    /// </summary>
    public enum MyApiTarget
    {
        /// <summary>
        ///     No API target. Unrestricted compilation (no whitelisting, no instruction counting)
        /// </summary>
        None,

        /// <summary>
        ///     Mod API target. Whitelisted, but no instruction counting.
        /// </summary>
        Mod,

        /// <summary>
        ///     Ingame API target. Whitelisted and instruction counted.
        /// </summary>
        Ingame
    }
}