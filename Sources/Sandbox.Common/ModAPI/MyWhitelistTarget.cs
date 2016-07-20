using System;

namespace Sandbox.ModAPI
{
    /// <summary>
    ///     Determines what target a whitelisting entry does or should support.
    /// </summary>
    [Flags]
    public enum MyWhitelistTarget
    {
        /// <summary>
        ///     No target. Depending on the context, this may mean no support at all or unrestricted support.
        /// </summary>
        None,

        /// <summary>
        ///     The entry supports or must support ModAPI level entry.
        /// </summary>
        ModApi = 0x1,

        /// <summary>
        ///     The entry supports or must support Ingame level entry.
        /// </summary>
        Ingame = 0x2,

        /// <summary>
        ///     A shortcut flag meaning the entry supports or must support both ModAPI and Ingame level entries.
        /// </summary>
        Both = ModApi | Ingame
    }
}