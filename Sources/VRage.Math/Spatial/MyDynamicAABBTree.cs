#region Using Statements

using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;

#endregion

namespace VRageMath
{
    ////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Dynamic aabb tree implementation as a prunning structure
    /// </summary>
    public class MyDynamicAABBTree 
    {
        /// A dynamic BoundingBox tree broad-phase, inspired by Nathanael Presson's btDbvt.
        //public delegate float RayCastCallbackInternal(ref MyRayCastInput input, int userData);

        /// <summary>
        /// A node in the dynamic tree. The client does not interact with this directly.
        /// </summary>
        internal class DynamicTreeNode
        {
            /// This is the fattened BoundingBox.
            internal BoundingBox Aabb;

            internal int Child1;
            internal int Child2;

            // leaf	= 	0,	 	free 	node 	= 	-1
            internal int Height;


            internal int ParentOrNext;
            internal object UserData;
            internal uint UserFlag;

            internal bool IsLeaf()
            {
                return Child1 == MyDynamicAABBTree.NullNode;
            }
        }

        /// <summary>
        /// A dynamic tree arranges data in a binary tree to accelerate
        /// queries such as volume queries and ray casts. Leafs are proxies
        /// with an BoundingBox. In the tree we expand the proxy BoundingBox by Settings.b2_fatAABBFactor
        /// so that the proxy BoundingBox is bigger than the client object. This allows the client
        /// object to move by small amounts without triggering a tree update.
        /// Nodes are pooled and relocatable, so we use node indices rather than pointers.
        /// </summary>
        public const int NullNode = -1;
        private int m_freeList;
        private int m_insertionCount;
        private int m_nodeCapacity;
        private int m_nodeCount;
        private DynamicTreeNode[] m_nodes;

        private Dictionary<int, DynamicTreeNode> _leafElementCache;

        private int m_root;

        [ThreadStatic]
        private static Stack<int> m_queryStack;
        static List<Stack<int>> m_StackCacheCollection = new List<Stack<int>>();

        Stack<int> CurrentThreadStack
        {
            get
            {
                if (m_queryStack == null)
                {
                    // 32 is good enough, 2^32 is enought
                    m_queryStack = new Stack<int>(32);
                    lock (m_StackCacheCollection)
                    {
                        m_StackCacheCollection.Add(m_queryStack);
                    }
                }
                return m_queryStack;
            }
        }

        private Vector3 m_extension;
        float m_aabbMultiplier;

        //private ReaderWriterLock m_rwLock = new ReaderWriterLock();
        private FastResourceLock m_rwLock = new FastResourceLock();

        /// constructing the tree initializes the node pool.
        public MyDynamicAABBTree(Vector3 extension, float aabbMultiplier = 1)
        {
            m_nodeCapacity = 256;

            m_extension = extension;
            m_aabbMultiplier = aabbMultiplier;

            m_nodes = new DynamicTreeNode[m_nodeCapacity];
            _leafElementCache = new Dictionary<int, DynamicTreeNode>(m_nodeCapacity / 4);

            for (int i = 0; i < m_nodeCapacity; ++i)
            {
                m_nodes[i] = new DynamicTreeNode();
            }

            // Initialize stack for current thread
            var myStack = CurrentThreadStack;

            ResetNodes();
        }

        private Stack<int> GetStack()
        {
            Stack<int> retVal = CurrentThreadStack;
            retVal.Clear();

            return retVal;
        }

        private void PushStack(Stack<int> stack)
        {
            // Nothing to do
        }

        /// <summary>
        /// Create a proxy. Provide a tight fitting BoundingBox and a userData pointer.
        /// </summary>
        /// <param name="aabb">The aabb.</param>
        /// <param name="userData">The user data.</param>
        /// <returns></returns>
        public int AddProxy(ref BoundingBox aabb, object userData, uint userFlags, bool rebalance = true)
        {
            using (m_rwLock.AcquireExclusiveUsing())
            {
                int proxyId = AllocateNode();

                // Fatten the aabb.
                m_nodes[proxyId].Aabb = aabb;
                m_nodes[proxyId].Aabb.Min -= m_extension;
                m_nodes[proxyId].Aabb.Max += m_extension;
                m_nodes[proxyId].UserData = userData;
                m_nodes[proxyId].UserFlag = userFlags;
                m_nodes[proxyId].Height = 0;

                _leafElementCache[proxyId] = m_nodes[proxyId];

                InsertLeaf(proxyId, rebalance);

                return proxyId;
            }
        }

        /// <summary>
        /// Destroy a proxy. This asserts if the id is invalid.
        /// </summary>
        /// <param name="proxyId">The proxy id.</param>
        public void RemoveProxy(int proxyId)
        {
            using (m_rwLock.AcquireExclusiveUsing())
            {
                System.Diagnostics.Debug.Assert(0 <= proxyId && proxyId < m_nodeCapacity);
                System.Diagnostics.Debug.Assert(m_nodes[proxyId].IsLeaf());

                _leafElementCache.Remove(proxyId);

                RemoveLeaf(proxyId);
                FreeNode(proxyId);
            }
        }

        /// <summary>
        /// Move a proxy with a swepted BoundingBox. If the proxy has moved outside of its fattened BoundingBox,
        /// then the proxy is removed from the tree and re-inserted. Otherwise
        /// the function returns immediately.
        /// </summary>
        /// <param name="proxyId">The proxy id.</param>
        /// <param name="aabb">The aabb.</param>
        /// <param name="displacement">The displacement.</param>
        /// <returns>true if the proxy was re-inserted.</returns>
        public bool MoveProxy(int proxyId, ref BoundingBox aabb, Vector3 displacement)
        {
            using (m_rwLock.AcquireExclusiveUsing())
            {
                System.Diagnostics.Debug.Assert(0 <= proxyId && proxyId < m_nodeCapacity);
                System.Diagnostics.Debug.Assert(m_nodes[proxyId].IsLeaf());

                ContainmentType contType = m_nodes[proxyId].Aabb.Contains(aabb);
                if (contType == ContainmentType.Contains)
                {
                    return false;
                }

                RemoveLeaf(proxyId);

                // Extend BoundingBox.
                BoundingBox b = aabb;

                Vector3 r = m_extension;
                b.Min -= r;
                b.Max += r;

                // Predict BoundingBox displacement.
                Vector3 d = m_aabbMultiplier * displacement;

                if (d.X < 0.0f)
                {
                    b.Min.X += d.X;
                }
                else
                {
                    b.Max.X += d.X;
                }

                if (d.Y < 0.0f)
                {
                    b.Min.Y += d.Y;
                }
                else
                {
                    b.Max.Y += d.Y;
                }

                if (d.Z < 0.0f)
                {
                    b.Min.Z += d.Z;
                }
                else
                {
                    b.Max.Z += d.Z;
                }

                m_nodes[proxyId].Aabb = b;

                InsertLeaf(proxyId, true);
            }

            return true;
        }


        /// <summary>
        /// Get proxy user data.
        /// </summary>
        /// <param name="proxyId">The proxy id.</param>
        /// <returns>the proxy user data or 0 if the id is invalid.</returns>
        public T GetUserData<T>(int proxyId)
        {
            System.Diagnostics.Debug.Assert(0 <= proxyId && proxyId < m_nodeCapacity);
            return (T)m_nodes[proxyId].UserData;
        }

        public int GetRoot()
        {
            return m_root;
        }

        public int GetLeafCount(int proxyId)
        {
            int result = 0;

            System.Diagnostics.Debug.Assert(0 <= proxyId && proxyId < m_nodeCapacity);

            Stack<int> stack = GetStack();
            stack.Push(proxyId);


            //int g = CountLeaves(m_root);

            while (stack.Count > 0)
            {
                int nodeId = stack.Pop();
                if (nodeId == NullNode)
                {
                    continue;
                }

                DynamicTreeNode node = m_nodes[nodeId];
                                
                if (node.IsLeaf())
                {
                    result++;
                }
                else
                {
                    stack.Push(node.Child1);
                    stack.Push(node.Child2);
                }
            }

            PushStack(stack);
            return result;
        }

        public void GetNodeLeaves(int proxyId, List<int> children)
        {
            System.Diagnostics.Debug.Assert(0 <= proxyId && proxyId < m_nodeCapacity);

            Stack<int> stack = GetStack();
            stack.Push(proxyId);


            //int g = CountLeaves(m_root);

            while (stack.Count > 0)
            {
                int nodeId = stack.Pop();
                if (nodeId == NullNode)
                {
                    continue;
                }

                DynamicTreeNode node = m_nodes[nodeId];

                if (node.IsLeaf())
                {
                    children.Add(nodeId);
                }
                else
                {
                    stack.Push(node.Child1);
                    stack.Push(node.Child2);
                }
            }

            PushStack(stack);
        }

        public BoundingBox GetAabb(int proxyId)
        {
            System.Diagnostics.Debug.Assert(0 <= proxyId && proxyId < m_nodeCapacity);
            return m_nodes[proxyId].Aabb;
        }

        public void GetChildren(int proxyId, out int left, out int right)
        {
            System.Diagnostics.Debug.Assert(0 <= proxyId && proxyId < m_nodeCapacity);
            left = m_nodes[proxyId].Child1;
            right = m_nodes[proxyId].Child2;
        }

        /// <summary>
        /// Get proxy user data.
        /// </summary>
        /// <param name="proxyId">The proxy id.</param>
        /// <returns>the proxy user data or 0 if the id is invalid.</returns>
        uint GetUserFlag(int proxyId)
        {
            System.Diagnostics.Debug.Assert(0 <= proxyId && proxyId < m_nodeCapacity);
            return m_nodes[proxyId].UserFlag;
        }

        /// <summary>
        /// Get the fat BoundingBox for a proxy.
        /// </summary>
        /// <param name="proxyId">The proxy id.</param>
        /// <param name="fatAABB">The fat BoundingBox.</param>
        public void GetFatAABB(int proxyId, out BoundingBox fatAABB)
        {
            using (m_rwLock.AcquireSharedUsing())
            {
                System.Diagnostics.Debug.Assert(0 <= proxyId && proxyId < m_nodeCapacity);
                fatAABB = m_nodes[proxyId].Aabb;
            }
        }

        /// Query an BoundingBox for overlapping proxies. The callback class
        /// is called for each proxy that overlaps the supplied BoundingBox.
        public void Query(Func<int, bool> callback, ref BoundingBox aabb)
        {
            using (m_rwLock.AcquireSharedUsing())
            {
                Stack<int> stack = GetStack();
                stack.Push(m_root);

                while (stack.Count > 0)
                {
                    int nodeId = stack.Pop();
                    if (nodeId == NullNode)
                    {
                        continue;
                    }

                    DynamicTreeNode node = m_nodes[nodeId];

                    if (node.Aabb.Intersects(aabb))
                    {
                        if (node.IsLeaf())
                        {
                            bool proceed = callback(nodeId);
                            if (!proceed)
                            {
                                return;
                            }
                        }
                        else
                        {
                            stack.Push(node.Child1);
                            stack.Push(node.Child2);
                        }
                    }
                }
            }
        }



        public int CountLeaves(int nodeId)
        {
            using (m_rwLock.AcquireSharedUsing())
            {
                if (nodeId == NullNode)
                {
                    return 0;
                }

                System.Diagnostics.Debug.Assert(0 <= nodeId && nodeId < m_nodeCapacity);
                DynamicTreeNode node = m_nodes[nodeId];

                if (node.IsLeaf())
                {
                    System.Diagnostics.Debug.Assert(node.Height == 1);
                    return 1;
                }

                int count1 = CountLeaves(node.Child1);
                int count2 = CountLeaves(node.Child2);
                int count = count1 + count2;
                System.Diagnostics.Debug.Assert(count == node.Height);
                return count;
            }
        }

        private int AllocateNode()
        {
            // Expand the node pool as needed.
            if (m_freeList == NullNode)
            {
                System.Diagnostics.Debug.Assert(m_nodeCount == m_nodeCapacity);

                // The free list is empty. Rebuild a bigger pool.
                DynamicTreeNode[] oldNodes = m_nodes;
                m_nodeCapacity *= 2;
                m_nodes = new DynamicTreeNode[m_nodeCapacity];
                Array.Copy(oldNodes, m_nodes, m_nodeCount);

                // Build a linked list for the free list. The parent
                // pointer becomes the "next" pointer.
                for (int i = m_nodeCount; i < m_nodeCapacity - 1; ++i)
                {
                    m_nodes[i] = new DynamicTreeNode();
                    m_nodes[i].ParentOrNext = i + 1;
                    m_nodes[i].Height = 1;
                }
                m_nodes[m_nodeCapacity - 1] = new DynamicTreeNode();
                m_nodes[m_nodeCapacity - 1].ParentOrNext = NullNode;
                m_nodes[m_nodeCapacity - 1].Height = 1;
                m_freeList = m_nodeCount;
            }

            // Peel a node off the free list.
            int nodeId = m_freeList;
            m_freeList = m_nodes[nodeId].ParentOrNext;
            m_nodes[nodeId].ParentOrNext = NullNode;
            m_nodes[nodeId].Child1 = NullNode;
            m_nodes[nodeId].Child2 = NullNode;
            m_nodes[nodeId].Height = 0;
            m_nodes[nodeId].UserData = null;
            ++m_nodeCount;
            return nodeId;
        }

        private void FreeNode(int nodeId)
        {
            System.Diagnostics.Debug.Assert(0 <= nodeId && nodeId < m_nodeCapacity);
            System.Diagnostics.Debug.Assert(0 < m_nodeCount);
            m_nodes[nodeId].ParentOrNext = m_freeList;
            m_nodes[nodeId].Height = -1;
            m_nodes[nodeId].UserData = null;
            m_freeList = nodeId;
            --m_nodeCount;
        }

        private void InsertLeaf(int leaf, bool rebalance)
        {
            ++m_insertionCount;

            if (m_root == NullNode)
            {
                m_root = leaf;
                m_nodes[m_root].ParentOrNext = NullNode;
                return;
            }

            // Find the best sibling for this node
            BoundingBox leafAABB = m_nodes[leaf].Aabb;
            //Vector3 leafCenter = leafAABB.Min + (leafAABB.Max - leafAABB.Min) * 0.5f;
            int index = m_root;
            while (m_nodes[index].IsLeaf() == false)
            {
                int child1 = m_nodes[index].Child1;
                int child2 = m_nodes[index].Child2;

                if (rebalance)
                {
                    float area = m_nodes[index].Aabb.Perimeter;

                    // Combined area
                    BoundingBox combinedAABB = BoundingBox.CreateMerged(m_nodes[index].Aabb, leafAABB);
                    float combinedArea = combinedAABB.Perimeter;

                    //Cost of creating a new parent for this node and the new leaf
                    float cost = 2.0f * combinedArea;



                    //Minimum cost of pushing the leaf further down the tree
                    float inheritanceCost = 2.0f * (combinedArea - area);
                    //Cost of descending into child1
                    float cost1;
                    if (m_nodes[child1].IsLeaf())
                    {
                        BoundingBox aabb;
                        BoundingBox.CreateMerged(ref leafAABB, ref m_nodes[child1].Aabb, out aabb);

                        cost1 = aabb.Perimeter + inheritanceCost;
                    }
                    else
                    {
                        BoundingBox aabb;
                        BoundingBox.CreateMerged(ref leafAABB, ref m_nodes[child1].Aabb, out aabb);
                        float oldArea = m_nodes[child1].Aabb.Perimeter;
                        float newArea = aabb.Perimeter;
                        cost1 = (newArea - oldArea) + inheritanceCost;
                    }

                    //Cost of descending into child2
                    float cost2;
                    if (m_nodes[child2].IsLeaf())
                    {
                        BoundingBox aabb;
                        BoundingBox.CreateMerged(ref leafAABB, ref m_nodes[child2].Aabb, out aabb);
                        cost2 = aabb.Perimeter + inheritanceCost;
                    }
                    else
                    {
                        BoundingBox aabb;
                        BoundingBox.CreateMerged(ref leafAABB, ref m_nodes[child2].Aabb, out aabb);
                        float oldArea = m_nodes[child2].Aabb.Perimeter;
                        float newArea = aabb.Perimeter;
                        cost2 = newArea - oldArea + inheritanceCost;
                    }

                    //Descend according to the minimum cost.
                    if (cost < cost1 && cost1 < cost2)
                    {
                        break;
                    }

                    //Descend
                    if (cost1 < cost2)
                    {
                        index = child1;
                    }
                    else
                    {
                        index = child2;
                    }
                }
                else
                {
                    // Surface area heuristic
                    BoundingBox aabb1;
                    BoundingBox aabb2;
                    BoundingBox.CreateMerged(ref leafAABB, ref m_nodes[child1].Aabb, out aabb1);
                    BoundingBox.CreateMerged(ref leafAABB, ref m_nodes[child2].Aabb, out aabb2);
                    float norm1 = (m_nodes[child1].Height + 1) * aabb1.Perimeter;
                    float norm2 = (m_nodes[child2].Height + 1) * aabb2.Perimeter;

                    if (norm1 < norm2)
                    {
                        index = child1;
                    }
                    else
                    {
                        index = child2;
                    }
                }
            }

            int sibling = index;

            // Create a new parent for the siblings.
            int oldParent = m_nodes[index].ParentOrNext;
            int newParent = AllocateNode();
            m_nodes[newParent].ParentOrNext = oldParent;
            m_nodes[newParent].UserData = null;
            m_nodes[newParent].Aabb = BoundingBox.CreateMerged(leafAABB, m_nodes[sibling].Aabb);
            m_nodes[newParent].Height = m_nodes[sibling].Height + 1;

            if (oldParent != NullNode)
            {
                // The sibling was not the root.
                if (m_nodes[oldParent].Child1 == sibling)
                {
                    m_nodes[oldParent].Child1 = newParent;
                }
                else
                {
                    m_nodes[oldParent].Child2 = newParent;
                }

                m_nodes[newParent].Child1 = sibling;
                m_nodes[newParent].Child2 = leaf;
                m_nodes[index].ParentOrNext = newParent;
                m_nodes[leaf].ParentOrNext = newParent;
            }
            else
            {
                // The sibling was the root.
                m_nodes[newParent].Child1 = sibling;
                m_nodes[newParent].Child2 = leaf;
                m_nodes[index].ParentOrNext = newParent;
                m_nodes[leaf].ParentOrNext = newParent;
                m_root = newParent;
            }


            // Walk back up the tree fixing heights and AABBs            
            index = m_nodes[leaf].ParentOrNext;
            while (index != NullNode)
            {
                if (rebalance)
                {
                    index = Balance(index);
                }

                int child1 = m_nodes[index].Child1;
                int child2 = m_nodes[index].Child2;

                Debug.Assert(child1 != NullNode);
                Debug.Assert(child2 != NullNode);

                m_nodes[index].Height = 1 + Math.Max(m_nodes[child1].Height, m_nodes[child2].Height);
                BoundingBox.CreateMerged(ref m_nodes[child1].Aabb, ref m_nodes[child2].Aabb, out m_nodes[index].Aabb);
                index = m_nodes[index].ParentOrNext;
            }
        }

        private void RemoveLeaf(int leaf)
        {
            if (m_root == NullNode)
                return;

            if (leaf == m_root)
            {
                m_root = NullNode;
                return;
            }

            int parent = m_nodes[leaf].ParentOrNext;
            int grandParent = m_nodes[parent].ParentOrNext;
            int sibling;
            if (m_nodes[parent].Child1 == leaf)
            {
                sibling = m_nodes[parent].Child2;
            }
            else
            {
                sibling = m_nodes[parent].Child1;
            }

            if (grandParent != NullNode)
            {
                // Destroy node2 and connect node1 to sibling.
                if (m_nodes[grandParent].Child1 == parent)
                {
                    m_nodes[grandParent].Child1 = sibling;
                }
                else
                {
                    m_nodes[grandParent].Child2 = sibling;
                }
                m_nodes[sibling].ParentOrNext = grandParent;
                FreeNode(parent);


                // Adjust ancestor bounds.
                int index = grandParent;
                while (index != NullNode)
                {
                    index = Balance(index);

                    int child1 = m_nodes[index].Child1;
                    int child2 = m_nodes[index].Child2;

                    m_nodes[index].Aabb = BoundingBox.CreateMerged(m_nodes[child1].Aabb, m_nodes[child2].Aabb);
                    m_nodes[index].Height = 1 + Math.Max(m_nodes[child1].Height, m_nodes[child2].Height);

                    index = m_nodes[index].ParentOrNext;
                }
            }
            else
            {
                m_root = sibling;
                m_nodes[sibling].ParentOrNext = NullNode;
                FreeNode(parent);
            }
        }

        /// Compute the height of the binary tree in O(N) time. Should not be
        /// called often.
        public int GetHeight()
        {
            using (m_rwLock.AcquireSharedUsing())
            {

                if (m_root == NullNode)
                    return 0;

                return m_nodes[m_root].Height;
            }
        }


        // Perform a left or right rotation if node A is imbalanced.
        // Returns the new root index.
        public int Balance(int iA)
        {
            Debug.Assert(iA != NullNode);
            DynamicTreeNode A = m_nodes[iA];

            if (A.IsLeaf() || A.Height < 2)
            {
                return iA;
            }

            int iB = A.Child1;
            int iC = A.Child2;
            Debug.Assert(0 <= iB && iB < m_nodeCapacity);
            Debug.Assert(0 <= iC && iC < m_nodeCapacity);

            DynamicTreeNode B = m_nodes[iB];
            DynamicTreeNode C = m_nodes[iC];

            int balance = C.Height - B.Height;

            //Rotate C up
            if (balance > 1)
            {
                int iF = C.Child1;
                int iG = C.Child2;
                DynamicTreeNode F = m_nodes[iF];
                DynamicTreeNode G = m_nodes[iG];
                Debug.Assert(0 <= iF && iF < m_nodeCapacity);
                Debug.Assert(0 <= iG && iG < m_nodeCapacity);

                // Swap A and C
                C.Child1 = iA;
                C.ParentOrNext = A.ParentOrNext;
                A.ParentOrNext = iC;

                // A's old parent should point to C

                if (C.ParentOrNext != NullNode)
                {
                    if (m_nodes[C.ParentOrNext].Child1 == iA)
                    {
                        m_nodes[C.ParentOrNext].Child1 = iC;
                    }
                    else
                    {
                        Debug.Assert(m_nodes[C.ParentOrNext].Child2 == iA);
                        m_nodes[C.ParentOrNext].Child2 = iC;
                    }
                }
                else
                {
                    m_root = iC;
                }

                // Rotate
                if (F.Height > G.Height)
                {
                    C.Child2 = iF;
                    A.Child2 = iG;
                    G.ParentOrNext = iA;

                    BoundingBox.CreateMerged(ref B.Aabb, ref G.Aabb, out A.Aabb);
                    BoundingBox.CreateMerged(ref A.Aabb, ref F.Aabb, out C.Aabb);

                    A.Height = 1 + Math.Max(B.Height, G.Height);
                    C.Height = 1 + Math.Max(A.Height, F.Height);
                }
                else
                {
                    C.Child2 = iG;
                    A.Child2 = iF;
                    F.ParentOrNext = iA;

                    BoundingBox.CreateMerged(ref B.Aabb, ref F.Aabb, out A.Aabb);
                    BoundingBox.CreateMerged(ref A.Aabb, ref G.Aabb, out C.Aabb);

                    A.Height = 1 + Math.Max(B.Height, F.Height);
                    C.Height = 1 + Math.Max(A.Height, G.Height);
                }
                return iC;
            }

            // Rotate B up
            if (balance < -1)
            {
                int iD = B.Child1;
                int iE = B.Child2;
                DynamicTreeNode D = m_nodes[iD];
                DynamicTreeNode E = m_nodes[iE];
                Debug.Assert(0 <= iD && iD < m_nodeCapacity);
                Debug.Assert(0 <= iE && iE < m_nodeCapacity);

                // Swap A and B
                B.Child1 = iA;
                B.ParentOrNext = A.ParentOrNext;
                A.ParentOrNext = iB;

                // A's old parent should point to B
                if (B.ParentOrNext != NullNode)
                {
                    if (m_nodes[B.ParentOrNext].Child1 == iA)
                    {
                        m_nodes[B.ParentOrNext].Child1 = iB;
                    }
                    else
                    {
                        Debug.Assert(m_nodes[B.ParentOrNext].Child2 == iA);
                        m_nodes[B.ParentOrNext].Child2 = iB;
                    }
                }
                else
                {
                    m_root = iB;
                }

                // Rotate
                if (D.Height > E.Height)
                {
                    B.Child2 = iD;
                    A.Child1 = iE;
                    E.ParentOrNext = iA;
                    BoundingBox.CreateMerged(ref C.Aabb, ref E.Aabb, out A.Aabb);
                    BoundingBox.CreateMerged(ref A.Aabb, ref D.Aabb, out B.Aabb);

                    A.Height = 1 + Math.Max(C.Height, E.Height);
                    B.Height = 1 + Math.Max(A.Height, D.Height);
                }
                else
                {
                    B.Child2 = iE;
                    A.Child1 = iD;
                    D.ParentOrNext = iA;

                    BoundingBox.CreateMerged(ref C.Aabb, ref D.Aabb, out A.Aabb);
                    BoundingBox.CreateMerged(ref A.Aabb, ref E.Aabb, out B.Aabb);

                    A.Height = 1 + Math.Max(C.Height, D.Height);
                    B.Height = 1 + Math.Max(A.Height, E.Height);
                }
                return iB;
            }
            return iA;
        }

        public void OverlapAllFrustum<T>(ref BoundingFrustum frustum, List<T> elementsList, bool clear = true)
        {
            OverlapAllFrustum<T>(ref frustum, elementsList, 0, clear);
        }

        public void OverlapAllFrustum<T>(ref BoundingFrustum frustum, List<T> elementsList, uint requiredFlags, bool clear = true)
        {
            if (clear)
                elementsList.Clear();

            if (m_root == NullNode)
                return;

            using (m_rwLock.AcquireSharedUsing())
            {
                Stack<int> stack = GetStack();
                stack.Push(m_root);

                //BoundingBox bbox = BoundingBoxHelper.InitialBox;
                //BoundingBoxHelper.AddFrustum(ref frustum, ref bbox);

                while (stack.Count > 0)
                {
                    int nodeId = stack.Pop();
                    if (nodeId == NullNode)
                    {
                        continue;
                    }

                    DynamicTreeNode node = m_nodes[nodeId];

                    //if (node.Aabb.Intersects(bbox))
                    {
                        ContainmentType result;
                        frustum.Contains(ref node.Aabb, out result);
                        if (result == ContainmentType.Contains)
                        {
                            int baseCount = stack.Count;
                            stack.Push(nodeId);

                            while (stack.Count > baseCount)
                            {
                                int nodeIdToAdd = stack.Pop();

                                DynamicTreeNode nodeToAdd = m_nodes[nodeIdToAdd];

                                if (nodeToAdd.IsLeaf())
                                {
                                    uint flags = GetUserFlag(nodeIdToAdd);
                                    if ((flags & requiredFlags) == requiredFlags)
                                    {
                                        elementsList.Add(GetUserData<T>(nodeIdToAdd));
                                    }
                                }
                                else
                                {
                                    if (nodeToAdd.Child1 != NullNode)
                                        stack.Push(nodeToAdd.Child1);
                                    if (nodeToAdd.Child2 != NullNode)
                                        stack.Push(nodeToAdd.Child2);
                                }
                            }
                        }
                        else
                            if (result == ContainmentType.Intersects)
                            {
                                if (node.IsLeaf())
                                {
                                    uint flags = GetUserFlag(nodeId);
                                    if ((flags & requiredFlags) == requiredFlags)
                                    {
                                        elementsList.Add(GetUserData<T>(nodeId));
                                    }
                                }
                                else
                                {
                                    stack.Push(node.Child1);
                                    stack.Push(node.Child2);
                                }
                            }
                    }
                }

                PushStack(stack);
            }
        }

        public void OverlapAllFrustum<T>(ref BoundingFrustum frustum, List<T> elementsList, List<bool> isInsideList, uint requiredFlags, bool clear = true)
        {
            if (clear)
            {
                elementsList.Clear();
                isInsideList.Clear();
            }

            if (m_root == NullNode)
            { 
                return;
            }

            using (m_rwLock.AcquireSharedUsing())
            {
                Stack<int> stack = GetStack();
                stack.Push(m_root);

                //BoundingBox bbox = BoundingBoxHelper.InitialBox;
                //BoundingBoxHelper.AddFrustum(ref frustum, ref bbox);

                while (stack.Count > 0)
                {
                    int nodeId = stack.Pop();
                    if (nodeId == NullNode)
                    {
                        continue;
                    }

                    DynamicTreeNode node = m_nodes[nodeId];

                    //if (node.Aabb.Intersects(bbox))
                    {
                        ContainmentType result;
                        frustum.Contains(ref node.Aabb, out result);
                        if (result == ContainmentType.Contains)
                        {
                            int baseCount = stack.Count;
                            stack.Push(nodeId);

                            while (stack.Count > baseCount)
                            {
                                int nodeIdToAdd = stack.Pop();

                                DynamicTreeNode nodeToAdd = m_nodes[nodeIdToAdd];

                                if (nodeToAdd.IsLeaf())
                                {
                                    uint flags = GetUserFlag(nodeIdToAdd);
                                    if ((flags & requiredFlags) == requiredFlags)
                                    {
                                        elementsList.Add(GetUserData<T>(nodeIdToAdd));
                                        isInsideList.Add(true);
                                    }
                                }
                                else
                                {
                                    if (nodeToAdd.Child1 != NullNode)
                                        stack.Push(nodeToAdd.Child1);
                                    if (nodeToAdd.Child2 != NullNode)
                                        stack.Push(nodeToAdd.Child2);
                                }
                            }
                        }
                        else
                            if (result == ContainmentType.Intersects)
                            {
                                if (node.IsLeaf())
                                {
                                    uint flags = GetUserFlag(nodeId);
                                    if ((flags & requiredFlags) == requiredFlags)
                                    {
                                        elementsList.Add(GetUserData<T>(nodeId));
                                        isInsideList.Add(false);
                                    }
                                }
                                else
                                {
                                    stack.Push(node.Child1);
                                    stack.Push(node.Child2);
                                }
                            }
                    }
                }

                PushStack(stack);
            }
        }

        public void OverlapAllFrustum<T>(ref BoundingFrustum frustum, List<T> elementsList, List<bool> isInsideList, 
            Vector3 projectionDir, float projectionFactor, float ignoreThr, 
            uint requiredFlags, bool clear = true)
        {
            if (clear)
            {
                elementsList.Clear();
                isInsideList.Clear();
            }

            if (m_root == NullNode)
            {
                return;
            }

            using (m_rwLock.AcquireSharedUsing())
            {
                Stack<int> stack = GetStack();
                stack.Push(m_root);

                //BoundingBox bbox = BoundingBoxHelper.InitialBox;
                //BoundingBoxHelper.AddFrustum(ref frustum, ref bbox);

                while (stack.Count > 0)
                {
                    int nodeId = stack.Pop();
                    if (nodeId == NullNode)
                    {
                        continue;
                    }

                    DynamicTreeNode node = m_nodes[nodeId];

                    //if (node.Aabb.Intersects(bbox))
                    {
                        ContainmentType result;
                        frustum.Contains(ref node.Aabb, out result);
                        if (result == ContainmentType.Contains)
                        {
                            int baseCount = stack.Count;
                            stack.Push(nodeId);

                            while (stack.Count > baseCount)
                            {
                                int nodeIdToAdd = stack.Pop();

                                DynamicTreeNode nodeToAdd = m_nodes[nodeIdToAdd];

                                if (nodeToAdd.IsLeaf())
                                {
                                    if(nodeToAdd.Aabb.ProjectedArea(projectionDir) * projectionFactor > ignoreThr)
                                    { 
                                        uint flags = GetUserFlag(nodeIdToAdd);
                                        if ((flags & requiredFlags) == requiredFlags)
                                        {
                                            elementsList.Add(GetUserData<T>(nodeIdToAdd));
                                            isInsideList.Add(true);
                                        }
                                    }
                                }
                                else
                                {
                                    if (nodeToAdd.Child1 != NullNode)
                                        stack.Push(nodeToAdd.Child1);
                                    if (nodeToAdd.Child2 != NullNode)
                                        stack.Push(nodeToAdd.Child2);
                                }
                            }
                        }
                        else
                            if (result == ContainmentType.Intersects)
                            {
                                if (node.IsLeaf())
                                {
                                    if (node.Aabb.ProjectedArea(projectionDir) * projectionFactor > ignoreThr)
                                    {
                                        uint flags = GetUserFlag(nodeId);
                                        if ((flags & requiredFlags) == requiredFlags)
                                        {
                                            elementsList.Add(GetUserData<T>(nodeId));
                                            isInsideList.Add(false);
                                        }
                                    }
                                }
                                else
                                {
                                    stack.Push(node.Child1);
                                    stack.Push(node.Child2);
                                }
                            }
                    }
                }

                PushStack(stack);
            }
        }

        public void OverlapAllFrustumConservative<T>(ref BoundingFrustum frustum, List<T> elementsList, uint requiredFlags, bool clear = true)
        {
            if (clear)
                elementsList.Clear();

            if (m_root == NullNode)
                return;

            using (m_rwLock.AcquireSharedUsing())
            {
                Stack<int> stack = GetStack();
                stack.Push(m_root);

                BoundingBox bbox = BoundingBox.CreateInvalid();
                bbox.Include(ref frustum);

                while (stack.Count > 0)
                {
                    int nodeId = stack.Pop();
                    if (nodeId == NullNode)
                    {
                        continue;
                    }

                    DynamicTreeNode node = m_nodes[nodeId];

                    if (node.Aabb.Intersects(bbox))
                    {
                        ContainmentType result;
                        frustum.Contains(ref node.Aabb, out result);
                        if (result == ContainmentType.Contains)
                        {
                            int baseCount = stack.Count;
                            stack.Push(nodeId);

                            while (stack.Count > baseCount)
                            {
                                int nodeIdToAdd = stack.Pop();

                                DynamicTreeNode nodeToAdd = m_nodes[nodeIdToAdd];

                                if (nodeToAdd.IsLeaf())
                                {
                                    uint flags = GetUserFlag(nodeIdToAdd);
                                    if ((flags & requiredFlags) == requiredFlags)
                                    {
                                        elementsList.Add(GetUserData<T>(nodeIdToAdd));
                                    }
                                }
                                else
                                {
                                    if (nodeToAdd.Child1 != NullNode)
                                        stack.Push(nodeToAdd.Child1);
                                    if (nodeToAdd.Child2 != NullNode)
                                        stack.Push(nodeToAdd.Child2);
                                }
                            }
                        }
                        else
                            if (result == ContainmentType.Intersects)
                            {
                                if (node.IsLeaf())
                                {
                                    uint flags = GetUserFlag(nodeId);
                                    if ((flags & requiredFlags) == requiredFlags)
                                    {
                                        elementsList.Add(GetUserData<T>(nodeId));
                                    }
                                }
                                else
                                {
                                    stack.Push(node.Child1);
                                    stack.Push(node.Child2);
                                }
                            }
                    }
                }

                PushStack(stack);
            }
        }

        public void OverlapAllFrustumAny<T>(ref BoundingFrustum frustum, List<T> elementsList, bool clear = true)
        {
            if (clear)
                elementsList.Clear();

            if (m_root == NullNode)
                return;

            using (m_rwLock.AcquireSharedUsing())
            {
                Stack<int> stack = GetStack();
                stack.Push(m_root);

                //BoundingBox bbox = BoundingBoxHelper.InitialBox;
                //BoundingBoxHelper.AddFrustum(ref frustum, ref bbox);

                while (stack.Count > 0)
                {
                    int nodeId = stack.Pop();
                    if (nodeId == NullNode)
                    {
                        continue;
                    }

                    DynamicTreeNode node = m_nodes[nodeId];

                    //if (node.Aabb.Intersects(bbox))
                    {
                        ContainmentType result;
                        frustum.Contains(ref node.Aabb, out result);
                        if (result == ContainmentType.Contains)
                        {
                            int baseCount = stack.Count;
                            stack.Push(nodeId);

                            while (stack.Count > baseCount)
                            {
                                int nodeIdToAdd = stack.Pop();

                                DynamicTreeNode nodeToAdd = m_nodes[nodeIdToAdd];

                                if (nodeToAdd.IsLeaf())
                                {
                                    T el = GetUserData<T>(nodeIdToAdd);
                                    elementsList.Add((T)el);
                                }
                                else
                                {
                                    if (nodeToAdd.Child1 != NullNode)
                                        stack.Push(nodeToAdd.Child1);
                                    if (nodeToAdd.Child2 != NullNode)
                                        stack.Push(nodeToAdd.Child2);
                                }
                            }
                        }
                        else
                            if (result == ContainmentType.Intersects)
                            {
                                if (node.IsLeaf())
                                {
                                    T el = GetUserData<T>(nodeId);
                                    elementsList.Add((T)el);
                                }
                                else
                                {
                                    stack.Push(node.Child1);
                                    stack.Push(node.Child2);
                                }
                            }
                    }
                }

                PushStack(stack);
            }
        }
               
        public void OverlapAllLineSegment<T>(ref Line line, List<MyLineSegmentOverlapResult<T>> elementsList)
        {
            OverlapAllLineSegment<T>(ref line, elementsList, 0);
        }

        public void OverlapAllLineSegment<T>(ref Line line, List<MyLineSegmentOverlapResult<T>> elementsList, uint requiredFlags)
        {
            elementsList.Clear();

            if (m_root == NullNode)
                return;

            using (m_rwLock.AcquireSharedUsing())
            {
                Stack<int> stack = GetStack();
                stack.Push(m_root);

                BoundingBox bbox = BoundingBox.CreateInvalid();
                bbox.Include(ref line);

                var ray = new Ray(line.From, line.Direction);

                while (stack.Count > 0)
                {
                    int nodeId = stack.Pop();
                    if (nodeId == NullNode)
                    {
                        continue;
                    }

                    DynamicTreeNode node = m_nodes[nodeId];

                    if (node.Aabb.Intersects(bbox))
                    {
                        float? distance = node.Aabb.Intersects(ray);
                        if (distance.HasValue && distance.Value <= line.Length && distance.Value >= 0)
                        {
                            if (node.IsLeaf())
                            {
                                uint flags = GetUserFlag(nodeId);
                                if ((flags & requiredFlags) == requiredFlags)
                                {
                                    elementsList.Add(new MyLineSegmentOverlapResult<T> 
                                    { 
                                        Element =GetUserData<T>(nodeId), 
                                        Distance = distance.Value 
                                    });
                                }
                            }
                            else
                            {
                                stack.Push(node.Child1);
                                stack.Push(node.Child2);
                            }
                        }
                    }
                }

                PushStack(stack);
            }
        }       

        public void OverlapAllBoundingBox<T>(ref BoundingBox bbox, List<T> elementsList, uint requiredFlags = 0, bool clear = true)
        {
            if (clear)
                elementsList.Clear();

            if (m_root == NullNode)
                return;

            using (m_rwLock.AcquireSharedUsing())
            {
                Stack<int> stack = GetStack();
                stack.Push(m_root);


                //int g = CountLeaves(m_root);

                while (stack.Count > 0)
                {
                    int nodeId = stack.Pop();
                    if (nodeId == NullNode)
                    {
                        continue;
                    }

                    DynamicTreeNode node = m_nodes[nodeId];

                    if (node.Aabb.Intersects(bbox))
                    {
                        if (node.IsLeaf())
                        {
                            uint flags = GetUserFlag(nodeId);
                            if ((flags & requiredFlags) == requiredFlags)
                            {
                                elementsList.Add(GetUserData<T>(nodeId));
                            }
                        }
                        else
                        {
                            stack.Push(node.Child1);
                            stack.Push(node.Child2);
                        }
                    }
                }

                PushStack(stack);
            }
        }

        public bool OverlapsAnyLeafBoundingBox(ref BoundingBox bbox)
        {
            if (m_root == NullNode)
                return false;

            using (m_rwLock.AcquireSharedUsing())
            {
                Stack<int> stack = GetStack();
                stack.Push(m_root);

                while (stack.Count > 0)
                {
                    int nodeId = stack.Pop();
                    if (nodeId == NullNode)
                    {
                        continue;
                    }

                    DynamicTreeNode node = m_nodes[nodeId];

                    if (node.Aabb.Intersects(bbox))
                    {
                        if (node.IsLeaf())
                        {
                            return true;
                        }
                        else
                        {
                            stack.Push(node.Child1);
                            stack.Push(node.Child2);
                        }
                    }
                }

                PushStack(stack);
            }

            return false;
        }

        /**
         * Use the tree to produce a list of clusters aproximatelly the requested size, while intersecting those with the provided bounding box.
         */
        public void OverlapSizeableClusters(ref BoundingBox bbox, List<BoundingBox> boundList, double minSize)
        {
            if (m_root == NullNode)
                return;

            using (m_rwLock.AcquireSharedUsing())
            {
                Stack<int> stack = GetStack();
                stack.Push(m_root);

                //int g = CountLeaves(m_root);

                while (stack.Count > 0)
                {
                    int nodeId = stack.Pop();
                    if (nodeId == NullNode)
                    {
                        continue;
                    }

                    DynamicTreeNode node = m_nodes[nodeId];

                    if (node.Aabb.Intersects(bbox))
                    {
                        if (node.IsLeaf() || node.Aabb.Size.Max() <= minSize)
                        {
                            boundList.Add(node.Aabb);
                        }
                        else
                        {
                            stack.Push(node.Child1);
                            stack.Push(node.Child2);
                        }
                    }
                }

                PushStack(stack);
            }
        }

        public void OverlapAllBoundingSphere<T>(ref BoundingSphere sphere, List<T> overlapElementsList, bool clear = true)
        {
            if (clear)
                overlapElementsList.Clear();

            if (m_root == NullNode)
                return;

            using (m_rwLock.AcquireSharedUsing())
            {
                Stack<int> stack = GetStack();
                stack.Push(m_root);


                //int g = CountLeaves(m_root);

                while (stack.Count > 0)
                {
                    int nodeId = stack.Pop();
                    if (nodeId == NullNode)
                    {
                        continue;
                    }

                    DynamicTreeNode node = m_nodes[nodeId];

                    if (node.Aabb.Intersects(sphere))
                    {
                        if (node.IsLeaf())
                        {
                            overlapElementsList.Add(GetUserData<T>(nodeId));
                        }
                        else
                        {
                            stack.Push(node.Child1);
                            stack.Push(node.Child2);
                        }
                    }
                }

                PushStack(stack);
            }
        }

        public void GetAll<T>(List<T> elementsList, bool clear, List<BoundingBox> boxsList = null)
        {
            if (clear)
            {
                elementsList.Clear();
                if (boxsList != null)
                    boxsList.Clear();
            }

            using (m_rwLock.AcquireSharedUsing())
            {
                foreach (var node in _leafElementCache)
                {
                    elementsList.Add((T)node.Value.UserData);
                }

                if (boxsList != null)
                {
                    foreach (var node in _leafElementCache)
                    {
                        boxsList.Add(node.Value.Aabb);
                    }
                }
            }
        }

        public void GetAllNodeBounds(List<BoundingBox> boxsList)
        {
            using (m_rwLock.AcquireSharedUsing())
            {
                for (int i = 0, j = 0; i < m_nodeCapacity && j < m_nodeCount; ++i)
                {
                    if (m_nodes[i].Height != -1)
                    {
                        j++;
                        boxsList.Add(m_nodes[i].Aabb);
                    }
                }
            }
        }

        private void ResetNodes()
        {
            _leafElementCache.Clear();

            m_root = NullNode;

            m_nodeCount = 0;
            m_insertionCount = 0;

            // Build a linked list for the free list.
            for (int i = 0; i < m_nodeCapacity - 1; ++i)
            {
                m_nodes[i].ParentOrNext = i + 1;
                m_nodes[i].Height = 1;
                m_nodes[i].UserData = null;
            }
            m_nodes[m_nodeCapacity - 1].ParentOrNext = NullNode;
            m_nodes[m_nodeCapacity - 1].Height = 1;
            m_freeList = 0;
        }

        public void Clear()
        {
            using (m_rwLock.AcquireExclusiveUsing())
            {
                ResetNodes();
            }
        }

        public static void Dispose()
        {
            lock (m_StackCacheCollection)
            {
                foreach (var item in m_StackCacheCollection)
                {
                    item.Clear();
                }

                m_StackCacheCollection.Clear();
            }
        }
    }
}

