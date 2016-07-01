using System;

namespace Sandbox.ModAPI
{
    /// <summary>
    ///     A handle which enables adding members to the blacklist in a batch. It is highly
    ///     recommended that you try to group your changes into as few batches as possible.
    /// </summary>
    public interface IMyScriptBlacklistBatch : IDisposable
    {
        /// <summary>
        ///     Adds the entire namespace of one or more given types to the blacklist
        /// </summary>
        /// <param name="types"></param>
        void AddNamespaceOfTypes(params Type[] types);

        /// <summary>
        ///     Removes namespaces previously added with <see cref="AddNamespaceOfTypes"/> from the blacklist.
        /// </summary>
        /// <param name="types"></param>
        void RemoveNamespaceOfTypes(params Type[] types);

        /// <summary>
        ///     Adds one or more specific types and all their members to the blacklist.
        /// </summary>
        /// <param name="types"></param>
        void AddTypes(params Type[] types);

        /// <summary>
        ///     Removes types previously added with <see cref="AddTypes"/> from the blacklist.
        /// </summary>
        /// <param name="types"></param>
        void RemoveTypes(params Type[] types);

        /// <summary>
        ///     Adds only the specified members to the blacklist.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="memberNames"></param>
        void AddMembers(Type type, params string[] memberNames);

        /// <summary>
        ///     Removes types previously added with <see cref="AddMembers"/> to the blacklist.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="memberNames"></param>
        void RemoveMembers(Type type, params string[] memberNames);
    }
}