using VRage.Collections;

namespace Sandbox.ModAPI
{
    /// <summary>
    /// Exposes blacklist functionality to allow mods to disallow parts of the scripting API that has been
    /// allowed by the system whitelist.
    /// </summary>
    public interface IMyScriptBlacklist
    {
        /// <summary>
        /// Gets the entries that have been whitelisted by the system. Each entry may represent a whole namespace,
        /// a single type and all its members, or a single member of a type.
        /// </summary>
        /// <returns></returns>
        DictionaryReader<string, MyWhitelistTarget> GetWhitelist();

        /// <summary>
        /// Gets the entries that have been blacklisted for the ingame scripts.
        /// </summary>
        /// <returns></returns>
        HashSetReader<string> GetBlacklistedIngameEntries();

        /// <summary>
        /// Opens a batch to add or remove members to the blacklist.
        /// </summary>
        /// <returns></returns>
        IMyScriptBlacklistBatch OpenIngameBlacklistBatch();
    }
}
