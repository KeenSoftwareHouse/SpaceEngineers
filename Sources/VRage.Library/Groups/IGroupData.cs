using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Groups
{
    public interface IGroupData<TNode>
        where TNode: class
    {
        /// <summary>
        /// Group is taken from pool
        /// </summary>
        void OnCreate<TGroupData>(MyGroups<TNode, TGroupData>.Group group)
            where TGroupData : IGroupData<TNode>, new();

        /// <summary>
        /// Group is returned to pool
        /// </summary>
        void OnRelease();

        /// <summary>
        /// Node is added to group
        /// </summary>
        void OnNodeAdded(TNode entity);

        /// <summary>
        /// Node is removed from group
        /// </summary>
        void OnNodeRemoved(TNode entity);
    }
}
