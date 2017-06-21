using System;
using System.Reflection;
using Sandbox.ModAPI;

namespace VRage.Scripting
{
    /// <summary>
    ///     A handle which enables adding members to the whitelist in a batch. It is highly
    ///     recommended that you try to group your changes into as few batches as possible.
    /// </summary>
    public interface IMyWhitelistBatch : IDisposable
    {
        /// <summary>
        ///     Adds the entire namespace of one or more given types.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="types"></param>
        void AllowNamespaceOfTypes(MyWhitelistTarget target, params Type[] types);

        /// <summary>
        ///     Adds one or more specific types and all their members to the whitelist.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="types"></param>
        void AllowTypes(MyWhitelistTarget target, params Type[] types);

        /// <summary>
        ///     Adds only the specified members to the whitelist.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="members"></param>
        void AllowMembers(MyWhitelistTarget target, params MemberInfo[] members);
    }
}