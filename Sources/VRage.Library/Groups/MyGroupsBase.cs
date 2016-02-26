using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Groups
{
    public abstract partial class MyGroupsBase<TNode>
        where TNode : class
    {
        /// <summary>
        /// Adds node, asserts when node already exists
        /// </summary>
        public abstract void AddNode(TNode nodeToAdd);

        /// <summary>
        /// Removes node, asserts when node is not here or node has some existing links
        /// </summary>
        public abstract void RemoveNode(TNode nodeToRemove);

        /// <summary>
        /// Creates link between parent and child.
        /// Parent is owner of constraint.
        /// LinkId must be unique only for parent, for grid it can be packed position of block which created constraint.
        /// </summary>
        public abstract void CreateLink(long linkId, TNode parentNode, TNode childNode);

        /// <summary>
        /// Breaks link between parent and child, you can set child to null to find it by linkId.
        /// Returns true when link was removed, returns false when link was not found.
        /// </summary>
        public abstract bool BreakLink(long linkId, TNode parentNode, TNode childNode = null);

        /// <summary>
        /// Returns true if the given link between parent and child exists, you can set child to null to find it by linkId.
        /// </summary>
        public abstract bool LinkExists(long linkId, TNode parentNode, TNode childNode = null);

        /// <summary>
        /// Allocates!!
        /// Returns list of nodes datas in group
        /// </summary>
        /// <param name="nodeInGroup"></param>
        /// <returns></returns>
        public abstract List<TNode> GetGroupNodes(TNode nodeInGroup);
        public abstract void GetGroupNodes(TNode nodeInGroup, List<TNode> result);
    }
}
