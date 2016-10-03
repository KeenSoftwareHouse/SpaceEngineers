using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;

namespace VRage.Groups
{
    public partial class MyGroups<TNode, TGroupData> : MyGroupsBase<TNode>
        where TGroupData : IGroupData<TNode>, new()
        where TNode : class
	{
#if XB1
		// Internal members starting with 'm_' are for internal use only, there's no friends in c#
        public class Node
        {
            Group m_currentGroup;

            internal TNode m_node;
            internal Group m_group
            {
                get { return m_currentGroup; }
                set
                {
                    Debug.Assert(m_currentGroup != value, "Setting group which is already set");
                    if (m_currentGroup != null)
                        m_currentGroup.GroupData.OnNodeRemoved(m_node);
                    m_currentGroup = value;
                    if (m_currentGroup != null)
                        m_currentGroup.GroupData.OnNodeAdded(m_node);
                }
            }

            internal Dictionary<long, Node> m_children = new Dictionary<long, Node>();
            internal Dictionary<long, Node> m_parents = new Dictionary<long, Node>();

            public int LinkCount { get { return m_children.Count + m_parents.Count; } }
            public TNode NodeData { get { return m_node; } }
            public Group Group { get { return m_group; } }

            public DictionaryValuesReader<long, Node> Children
            {
                get { return new DictionaryValuesReader<long, Node>(m_children); }
            }

            public DictionaryReader<long, Node> ChildLinks
            {
                get { return new DictionaryReader<long, Node>(m_children); }
            }

            public DictionaryReader<long, Node> ParentLinks
            {
                get { return new DictionaryReader<long, Node>(m_parents); }
            }

            public override string ToString()
            {
                return m_node.ToString();
            }
        }

        // Internal members starting with 'm_' are for internal use only, there's no friends in c#
        public class Group
        {
            internal HashSet<Node> m_members = new HashSet<Node>();

            public readonly TGroupData GroupData = new TGroupData();

            public HashSetReader<Node> Nodes
            {
                get { return new HashSetReader<Node>(m_members); }
            }
        }
#endif

		/// <summary>
        /// Return true when "major" is really major group, otherwise false.
        /// </summary>
        public delegate bool MajorGroupComparer(Group major, Group minor);

        /// <summary>
        /// When true, groups with one member are supported.
        /// You can use AddNode and RemoveNode.
        /// You have to manually call RemoveNode!
        /// </summary>
        public bool SupportsOphrans { get; protected set; }

        Stack<Group> m_groupPool = new Stack<Group>(32);
        Stack<Node> m_nodePool = new Stack<Node>(32);

        Dictionary<TNode, Node> m_nodes = new Dictionary<TNode, Node>();
        HashSet<Group> m_groups = new HashSet<Group>();

        HashSet<Node> m_disconnectHelper = new HashSet<Node>();
        MajorGroupComparer m_groupSelector;
        bool m_isRecalculating = false;
        
        /// <summary>
        /// Initializes a new instance of MyGroups class.
        /// </summary>
        /// <param name="supportOphrans">When true, groups with one member are supported and you have to manually call RemoveNode!</param>
        /// <param name="groupSelector">Major group selector, when merging two groups, major group is preserved. By default it's larger group.</param>
        public MyGroups(bool supportOphrans = false, MajorGroupComparer groupSelector = null)
        {
            SupportsOphrans = supportOphrans;
            m_groupSelector = groupSelector ?? IsMajorGroup;
        }

        public bool HasSameGroup(TNode a, TNode b)
        {
            var groupA = GetGroup(a);
            var groupB = GetGroup(b);
            return groupA != null && groupA == groupB;
        }

        public Group GetGroup(TNode Node)
        {
            Node node;
            if (m_nodes.TryGetValue(Node, out node))
            {
                return node.m_group;
            }
            return null;
        }

        public HashSetReader<Group> Groups
        {
            get { return new HashSetReader<Group>(m_groups); }
        }

        /// <summary>
        /// Adds node, asserts when node already exists
        /// </summary>
        public override void AddNode(TNode nodeToAdd)
        {
            if (!SupportsOphrans)
                throw new InvalidOperationException("Cannot add/remove node when ophrans are not supported");

            Debug.Assert(!m_nodes.ContainsKey(nodeToAdd), "Node to add already exists!");

            Node node = GetOrCreateNode(nodeToAdd);
            if (node.m_group == null)
            {
                node.m_group = AcquireGroup();
                node.m_group.m_members.Add(node);
            }
        }

        /// <summary>
        /// Removes node, asserts when node is not here or node has some existing links
        /// </summary>
        public override void RemoveNode(TNode nodeToRemove)
        {
            if (!SupportsOphrans)
                throw new InvalidOperationException("Cannot add/remove node when ophrans are not supported");

            Debug.Assert(m_nodes.ContainsKey(nodeToRemove), "Node to remove not found!");

            Node node;
            if (m_nodes.TryGetValue(nodeToRemove, out node))
            {
                BreakAllLinks(node);

                bool released = TryReleaseNode(node);
                Debug.Assert(released, "Node to remove cannot be released!");
            }
        }

        private void BreakAllLinks(Node node)
        {
            // Remove existing links
            while (node.m_parents.Count > 0)
            {
                var parentIt = node.m_parents.GetEnumerator();
                parentIt.MoveNext();
                var parent = parentIt.Current;
                BreakLinkInternal(parent.Key, parent.Value, node);
            }
            while (node.m_children.Count > 0)
            {
                var childIt = node.m_children.GetEnumerator();
                childIt.MoveNext();
                var child = childIt.Current;
                BreakLinkInternal(child.Key, node, child.Value);
            }
        }

        /// <summary>
        /// Creates link between parent and child.
        /// Parent is owner of constraint.
        /// LinkId must be unique for parent and for child; LinkId is unique node-node identifier.
        /// </summary>
        public override void CreateLink(long linkId, TNode parentNode, TNode childNode)
        {
            Node parent = GetOrCreateNode(parentNode);
            Node child = GetOrCreateNode(childNode);

            if (parent.m_group != null && child.m_group != null)
            {
                // Both have group
                if (parent.m_group == child.m_group)
                {
                    // Both have same group
                    AddLink(linkId, parent, child);
                }
                else
                {
                    // Both have different groups
                    MergeGroups(parent.m_group, child.m_group);
                    AddLink(linkId, parent, child);
                }
            }
            else if (parent.m_group != null)
            {
                // Parent has group
                // Set child group
                child.m_group = parent.m_group;

                // Add child to group members
                parent.m_group.m_members.Add(child);

                AddLink(linkId, parent, child);
            }
            else if (child.m_group != null)
            {
                // Child has group
                // Set parent group
                parent.m_group = child.m_group;

                // Add parent to group members
                child.m_group.m_members.Add(parent);

                AddLink(linkId, parent, child);
            }
            else
            {
                // None has group
                var group = AcquireGroup();

                // Set groups to parent and child
                parent.m_group = group;
                child.m_group = group;

                // Add parent and child to group members
                group.m_members.Add(parent);
                group.m_members.Add(child);

                AddLink(linkId, parent, child);
            }

            Debug.Assert(parent.m_group == child.m_group, "Parent and child is in different group, inconsistency");
        }

        /// <summary>
        /// Breaks link between parent and child, you can set child to null to find it by linkId.
        /// Returns true when link was removed, returns false when link was not found.
        /// </summary>
        public override bool BreakLink(long linkId, TNode parentNode, TNode childNode = null)
        {
            Node parent;
            Node child;
            if (m_nodes.TryGetValue(parentNode, out parent) && parent.m_children.TryGetValue(linkId, out child)) // Parent and link found
            {
                Debug.Assert(childNode == null || child.m_node == childNode, "Invalid request, linkId does not match child");
                Debug.Assert(parent.m_group == child.m_group, "Parent and child is in different group, inconsistency!");

                return BreakLinkInternal(linkId, parent, child);
            }
            return false;
        }

        public void BreakAllLinks(TNode node)
        {
            Node n;
            if (m_nodes.TryGetValue(node, out n))
                BreakAllLinks(n);
        }

        public Node GetNode(TNode node)
        {
            return m_nodes.GetValueOrDefault(node);
        }

        public override bool LinkExists(long linkId, TNode parentNode, TNode childNode = null)
        {
            Node parent;
            Node child;
            if (m_nodes.TryGetValue(parentNode, out parent) && parent.m_children.TryGetValue(linkId, out child)) // Parent and link found
            {
                Debug.Assert(parent.m_group == child.m_group, "Parent and child is in different group, inconsistency!");

                if (childNode == null)
                    return true;

                return childNode == child.m_node;
            }
            return false;
        }

        private bool BreakLinkInternal(long linkId, Node parent, Node child)
        {
            DebugCheckConsistency(linkId,parent,child);
            bool removed = parent.m_children.Remove(linkId); // Remove link parent-child
            removed &= child.m_parents.Remove(linkId); // Remove link child-parent
            RecalculateConnectivity(parent, child);
            return removed;
        }

        [Conditional("DEBUG")]
        private void DebugCheckConsistency(long linkId, Node parent, Node child)
        {
            Debug.Assert(parent.m_children.ContainsKey(linkId) == child.m_parents.ContainsKey(linkId));
        }

        private void AddNeighbours(HashSet<Node> nodes, Node nodeToAdd)
        {
            if (!nodes.Contains(nodeToAdd))
            {
                nodes.Add(nodeToAdd);
                foreach (var child in nodeToAdd.m_children)
                {
                    AddNeighbours(nodes, child.Value);
                }
                foreach (var parent in nodeToAdd.m_parents)
                {
                    AddNeighbours(nodes, parent.Value);
                }
            }
        }

        /// <summary>
        /// Returns true when node was released completely and returned to pool.
        /// </summary>
        private bool TryReleaseNode(Node node)
        {
            // Node is completely disconnected
            if (node.m_node != null && node.m_group != null && node.m_children.Count == 0 && node.m_parents.Count == 0)
            {
                var group = node.m_group;

                // Remove from group and nodes
                node.m_group.m_members.Remove(node);
                m_nodes.Remove(node.m_node);

                // Reset fields, return to pool
                node.m_group = null;
                node.m_node = null;
                ReturnNode(node);

                // Group is empty
                if (group.m_members.Count == 0)
                {
                    // Return to pool
                    ReturnGroup(group);
                }
                return true;
            }
            return false;
        }

        // Recalculates consistency, splits groups when disconnected and remove ophrans (Nodes with no links)
        private void RecalculateConnectivity(Node parent, Node child)
        {
            if (m_isRecalculating)
            {
                return;
            }
            if (parent == null || parent.Group==null || child == null || child.Group == null)
            {
                Debug.Fail("Null in RecalculateConnectivity");
                return;
            }
            try
            {

                m_isRecalculating = true;
                // When no ophran was removed
                if (SupportsOphrans || (!TryReleaseNode(parent) & !TryReleaseNode(child)))
                {
                    // Both parent and child has some neighbours
                    AddNeighbours(m_disconnectHelper, parent);
                    if (m_disconnectHelper.Contains(child)) // There's still path from parent to child through graph, no disconnect
                        return;

                    // When there's more blocks than half in disconnect helper, clear it and add smaller half
                    if (m_disconnectHelper.Count > (parent.Group.m_members.Count / 2.0f))
                    {
                        foreach (var node in parent.Group.m_members)
                        {
                            // Remove existing and add non-existing
                            if (!m_disconnectHelper.Add(node))
                                m_disconnectHelper.Remove(node);
                        }
                    }

                    // Parent and child is in different group, split
                    Group newGroup = AcquireGroup();
                    Debug.Assert(m_disconnectHelper.Count > 0, "There is supposed to be something, inconsistency");
                    foreach (var node in m_disconnectHelper)
                    {
                        // Remove from current group members
                        if(node.m_group == null || node.m_group.m_members == null)
                        {
                            continue;
                        }
                        bool removed = node.m_group.m_members.Remove(node);
                        Debug.Assert(removed, "Node is not in group members, inconsistency");

                        // Set new group
                        node.m_group = newGroup;
                        // Add to new group members
                        newGroup.m_members.Add(node);
                    }
                }
            }
            finally
            {
                m_disconnectHelper.Clear();
                m_isRecalculating = false;
            }
        }

        public static bool IsMajorGroup(Group groupA, Group groupB)
        {
            return groupA.m_members.Count >= groupB.m_members.Count;
        }

        private List<Node> m_tmpList = new List<Node>();
        private void MergeGroups(Group groupA, Group groupB)
        {
            Debug.Assert(groupA != groupB, "Cannot merge group with itself");

            // Swap it so groupA is bigger group
            if (!m_groupSelector(groupA, groupB))
            {
                var tmp = groupA; groupA = groupB; groupB = tmp;
            }

            if(m_tmpList.Capacity < groupB.m_members.Count)
                m_tmpList.Capacity = groupB.m_members.Count;

            m_tmpList.AddHashset(groupB.m_members);

            foreach(var node in m_tmpList)
            {
                // Set: Group.Members, Node.Group
                // Keep: Node.Children (children are still the same)

                groupB.m_members.Remove(node);
                // Add between members of groupA
                groupA.m_members.Add(node);
                // Set group to groupA
                node.m_group = groupA;
            }

            m_tmpList.Clear();

            //foreach (var node in groupB.m_members)
            //{
            //    // Set: Group.Members, Node.Group
            //    // Keep: Node.Children (children are still the same)

            //    // Set group to groupA
            //    node.m_group = groupA;

            //    // Add between members of groupA
            //    groupA.m_members.Add(node);
            //}

            // Clear members of groupB
            groupB.m_members.Clear();

            // Return groupB to pool
            ReturnGroup(groupB);
        }

        private void AddLink(long linkId, Node parent, Node child)
        {
            //jn:TODO use infinario to log override?
            Debug.Assert(!parent.m_children.ContainsKey(linkId));
            Debug.Assert(!child.m_parents.ContainsKey(linkId));
            parent.m_children[linkId] = child;
            child.m_parents[linkId] = parent;
        }

        private Node GetOrCreateNode(TNode nodeData)
        {
            Node node;
            if (!m_nodes.TryGetValue(nodeData, out node))
            {
                node = AcquireNode();
                node.m_node = nodeData;
                m_nodes[nodeData] = node;
            }
            return node;
        }

        // Returns group or null
        private Group GetNodeOrNull(TNode Node)
        {
            Node node;
            m_nodes.TryGetValue(Node, out node);
            return node != null ? node.m_group : null;
        }

        private Group AcquireGroup()
        {
            var group = m_groupPool.Count > 0 ? m_groupPool.Pop() : new Group();
            m_groups.Add(group);
            group.GroupData.OnCreate(group);
            Debug.Assert(group.m_members.Count == 0, "New group is supposed to be empty, inconsistency!");
            return group;
        }

        private void ReturnGroup(Group group)
        {
            Debug.Assert(group.m_members.Count == 0, "Returning non-empty group, inconsistency!");
            group.GroupData.OnRelease();
            m_groups.Remove(group);
            m_groupPool.Push(group);
        }

        private Node AcquireNode()
        {
            var node = m_nodePool.Count > 0 ? m_nodePool.Pop() : new Node();
            Debug.Assert(node.m_children.Count == 0 && node.m_parents.Count == 0 && node.m_group == null && node.m_node == null, "Acquired node is not clear!");
            return node;
        }

        private void ReturnNode(Node node)
        {
            Debug.Assert(node.m_children.Count == 0 && node.m_parents.Count == 0 && node.m_group == null && node.m_node == null, "Returning node was not cleared!");
            m_nodePool.Push(node);
        }

        public override List<TNode> GetGroupNodes(TNode nodeInGroup)
        {
            List<TNode> list = null;
            var g = GetGroup(nodeInGroup);
            if (g != null)
            {
                list = new List<TNode>(g.Nodes.Count);
                foreach (var node in g.Nodes)
                    list.Add(node.NodeData);
                return list;
            }

            list = new List<TNode>(1);
            list.Add(nodeInGroup);
            return list;
        }

        public override void GetGroupNodes(TNode nodeInGroup, List<TNode> result)
        {
            var g = GetGroup(nodeInGroup);
            if (g != null)
            {
                foreach (var node in g.Nodes)
                    result.Add(node.NodeData);
            }
            else
                result.Add(nodeInGroup);
        }
    }
}
