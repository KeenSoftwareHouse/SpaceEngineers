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
        private int _freeList;
        private int _insertionCount;
        private int _nodeCapacity;
        private int _nodeCount;
        private DynamicTreeNode[] _nodes;

        private Dictionary<int, DynamicTreeNode> _leafElementCache;

        private int _root;

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
            _nodeCapacity = 256;

            m_extension = extension;
            m_aabbMultiplier = aabbMultiplier;

            _nodes = new DynamicTreeNode[_nodeCapacity];
            _leafElementCache = new Dictionary<int, DynamicTreeNode>(_nodeCapacity / 4);

            for (int i = 0; i < _nodeCapacity; ++i)
            {
                _nodes[i] = new DynamicTreeNode();
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
                _nodes[proxyId].Aabb = aabb;
                _nodes[proxyId].Aabb.Min -= m_extension;
                _nodes[proxyId].Aabb.Max += m_extension;
                _nodes[proxyId].UserData = userData;
                _nodes[proxyId].UserFlag = userFlags;
                _nodes[proxyId].Height = 0;

                _leafElementCache[proxyId] = _nodes[proxyId];

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
                System.Diagnostics.Debug.Assert(0 <= proxyId && proxyId < _nodeCapacity);
                System.Diagnostics.Debug.Assert(_nodes[proxyId].IsLeaf());

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
                System.Diagnostics.Debug.Assert(0 <= proxyId && proxyId < _nodeCapacity);
                System.Diagnostics.Debug.Assert(_nodes[proxyId].IsLeaf());

                ContainmentType contType = _nodes[proxyId].Aabb.Contains(aabb);
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

                _nodes[proxyId].Aabb = b;

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
            System.Diagnostics.Debug.Assert(0 <= proxyId && proxyId < _nodeCapacity);
            return (T)_nodes[proxyId].UserData;
        }

        public int GetRoot()
        {
            return _root;
        }

        public int GetLeafCount(int proxyId)
        {
            int result = 0;

            System.Diagnostics.Debug.Assert(0 <= proxyId && proxyId < _nodeCapacity);

            Stack<int> stack = GetStack();
            stack.Push(proxyId);


            //int g = CountLeaves(_root);

            while (stack.Count > 0)
            {
                int nodeId = stack.Pop();
                if (nodeId == NullNode)
                {
                    continue;
                }

                DynamicTreeNode node = _nodes[nodeId];
                                
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
            System.Diagnostics.Debug.Assert(0 <= proxyId && proxyId < _nodeCapacity);

            Stack<int> stack = GetStack();
            stack.Push(proxyId);


            //int g = CountLeaves(_root);

            while (stack.Count > 0)
            {
                int nodeId = stack.Pop();
                if (nodeId == NullNode)
                {
                    continue;
                }

                DynamicTreeNode node = _nodes[nodeId];

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
            System.Diagnostics.Debug.Assert(0 <= proxyId && proxyId < _nodeCapacity);
            return _nodes[proxyId].Aabb;
        }

        public void GetChildren(int proxyId, out int left, out int right)
        {
            System.Diagnostics.Debug.Assert(0 <= proxyId && proxyId < _nodeCapacity);
            left = _nodes[proxyId].Child1;
            right = _nodes[proxyId].Child2;
        }

        /// <summary>
        /// Get proxy user data.
        /// </summary>
        /// <param name="proxyId">The proxy id.</param>
        /// <returns>the proxy user data or 0 if the id is invalid.</returns>
        uint GetUserFlag(int proxyId)
        {
            System.Diagnostics.Debug.Assert(0 <= proxyId && proxyId < _nodeCapacity);
            return _nodes[proxyId].UserFlag;
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
                System.Diagnostics.Debug.Assert(0 <= proxyId && proxyId < _nodeCapacity);
                fatAABB = _nodes[proxyId].Aabb;
            }
        }

        /// Query an BoundingBox for overlapping proxies. The callback class
        /// is called for each proxy that overlaps the supplied BoundingBox.
        public void Query(Func<int, bool> callback, ref BoundingBox aabb)
        {
            using (m_rwLock.AcquireSharedUsing())
            {
                Stack<int> stack = new Stack<int>(256);
                stack.Push(_root);

                while (stack.Count > 0)
                {
                    int nodeId = stack.Pop();
                    if (nodeId == NullNode)
                    {
                        continue;
                    }

                    DynamicTreeNode node = _nodes[nodeId];

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

                System.Diagnostics.Debug.Assert(0 <= nodeId && nodeId < _nodeCapacity);
                DynamicTreeNode node = _nodes[nodeId];

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
            if (_freeList == NullNode)
            {
                System.Diagnostics.Debug.Assert(_nodeCount == _nodeCapacity);

                // The free list is empty. Rebuild a bigger pool.
                DynamicTreeNode[] oldNodes = _nodes;
                _nodeCapacity *= 2;
                _nodes = new DynamicTreeNode[_nodeCapacity];
                Array.Copy(oldNodes, _nodes, _nodeCount);

                // Build a linked list for the free list. The parent
                // pointer becomes the "next" pointer.
                for (int i = _nodeCount; i < _nodeCapacity - 1; ++i)
                {
                    _nodes[i] = new DynamicTreeNode();
                    _nodes[i].ParentOrNext = i + 1;
                    _nodes[i].Height = 1;
                }
                _nodes[_nodeCapacity - 1] = new DynamicTreeNode();
                _nodes[_nodeCapacity - 1].ParentOrNext = NullNode;
                _nodes[_nodeCapacity - 1].Height = 1;
                _freeList = _nodeCount;
            }

            // Peel a node off the free list.
            int nodeId = _freeList;
            _freeList = _nodes[nodeId].ParentOrNext;
            _nodes[nodeId].ParentOrNext = NullNode;
            _nodes[nodeId].Child1 = NullNode;
            _nodes[nodeId].Child2 = NullNode;
            _nodes[nodeId].Height = 0;
            _nodes[nodeId].UserData = null;
            ++_nodeCount;
            return nodeId;
        }

        private void FreeNode(int nodeId)
        {
            System.Diagnostics.Debug.Assert(0 <= nodeId && nodeId < _nodeCapacity);
            System.Diagnostics.Debug.Assert(0 < _nodeCount);
            _nodes[nodeId].ParentOrNext = _freeList;
            _nodes[nodeId].Height = -1;
            _nodes[nodeId].UserData = null;
            _freeList = nodeId;
            --_nodeCount;
        }

        private void InsertLeaf(int leaf, bool rebalance)
        {
            ++_insertionCount;

            if (_root == NullNode)
            {
                _root = leaf;
                _nodes[_root].ParentOrNext = NullNode;
                return;
            }

            // Find the best sibling for this node
            BoundingBox leafAABB = _nodes[leaf].Aabb;
            //Vector3 leafCenter = leafAABB.Min + (leafAABB.Max - leafAABB.Min) * 0.5f;
            int index = _root;
            while (_nodes[index].IsLeaf() == false)
            {
                int child1 = _nodes[index].Child1;
                int child2 = _nodes[index].Child2;

                if (rebalance)
                {
                    float area = _nodes[index].Aabb.Perimeter;

                    // Combined area
                    BoundingBox combinedAABB = BoundingBox.CreateMerged(_nodes[index].Aabb, leafAABB);
                    float combinedArea = combinedAABB.Perimeter;

                    //Cost of creating a new parent for this node and the new leaf
                    float cost = 2.0f * combinedArea;



                    //Minimum cost of pushing the leaf further down the tree
                    float inheritanceCost = 2.0f * (combinedArea - area);
                    //Cost of descending into child1
                    float cost1;
                    if (_nodes[child1].IsLeaf())
                    {
                        BoundingBox aabb;
                        BoundingBox.CreateMerged(ref leafAABB, ref _nodes[child1].Aabb, out aabb);

                        cost1 = aabb.Perimeter + inheritanceCost;
                    }
                    else
                    {
                        BoundingBox aabb;
                        BoundingBox.CreateMerged(ref leafAABB, ref _nodes[child1].Aabb, out aabb);
                        float oldArea = _nodes[child1].Aabb.Perimeter;
                        float newArea = aabb.Perimeter;
                        cost1 = (newArea - oldArea) + inheritanceCost;
                    }

                    //Cost of descending into child2
                    float cost2;
                    if (_nodes[child2].IsLeaf())
                    {
                        BoundingBox aabb;
                        BoundingBox.CreateMerged(ref leafAABB, ref _nodes[child2].Aabb, out aabb);
                        cost2 = aabb.Perimeter + inheritanceCost;
                    }
                    else
                    {
                        BoundingBox aabb;
                        BoundingBox.CreateMerged(ref leafAABB, ref _nodes[child2].Aabb, out aabb);
                        float oldArea = _nodes[child2].Aabb.Perimeter;
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
                    BoundingBox.CreateMerged(ref leafAABB, ref _nodes[child1].Aabb, out aabb1);
                    BoundingBox.CreateMerged(ref leafAABB, ref _nodes[child2].Aabb, out aabb2);
                    float norm1 = (_nodes[child1].Height + 1) * aabb1.Perimeter;
                    float norm2 = (_nodes[child2].Height + 1) * aabb2.Perimeter;

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
            int oldParent = _nodes[index].ParentOrNext;
            int newParent = AllocateNode();
            _nodes[newParent].ParentOrNext = oldParent;
            _nodes[newParent].UserData = null;
            _nodes[newParent].Aabb = BoundingBox.CreateMerged(leafAABB, _nodes[sibling].Aabb);
            _nodes[newParent].Height = _nodes[sibling].Height + 1;

            if (oldParent != NullNode)
            {
                // The sibling was not the root.
                if (_nodes[oldParent].Child1 == sibling)
                {
                    _nodes[oldParent].Child1 = newParent;
                }
                else
                {
                    _nodes[oldParent].Child2 = newParent;
                }

                _nodes[newParent].Child1 = sibling;
                _nodes[newParent].Child2 = leaf;
                _nodes[index].ParentOrNext = newParent;
                _nodes[leaf].ParentOrNext = newParent;
            }
            else
            {
                // The sibling was the root.
                _nodes[newParent].Child1 = sibling;
                _nodes[newParent].Child2 = leaf;
                _nodes[index].ParentOrNext = newParent;
                _nodes[leaf].ParentOrNext = newParent;
                _root = newParent;
            }


            // Walk back up the tree fixing heights and AABBs            
            index = _nodes[leaf].ParentOrNext;
            while (index != NullNode)
            {
                if (rebalance)
                {
                    index = Balance(index);
                }

                int child1 = _nodes[index].Child1;
                int child2 = _nodes[index].Child2;

                Debug.Assert(child1 != NullNode);
                Debug.Assert(child2 != NullNode);

                _nodes[index].Height = 1 + Math.Max(_nodes[child1].Height, _nodes[child2].Height);
                BoundingBox.CreateMerged(ref _nodes[child1].Aabb, ref _nodes[child2].Aabb, out _nodes[index].Aabb);
                index = _nodes[index].ParentOrNext;
            }
        }

        private void RemoveLeaf(int leaf)
        {
            if (_root == NullNode)
                return;

            if (leaf == _root)
            {
                _root = NullNode;
                return;
            }

            int parent = _nodes[leaf].ParentOrNext;
            int grandParent = _nodes[parent].ParentOrNext;
            int sibling;
            if (_nodes[parent].Child1 == leaf)
            {
                sibling = _nodes[parent].Child2;
            }
            else
            {
                sibling = _nodes[parent].Child1;
            }

            if (grandParent != NullNode)
            {
                // Destroy node2 and connect node1 to sibling.
                if (_nodes[grandParent].Child1 == parent)
                {
                    _nodes[grandParent].Child1 = sibling;
                }
                else
                {
                    _nodes[grandParent].Child2 = sibling;
                }
                _nodes[sibling].ParentOrNext = grandParent;
                FreeNode(parent);


                // Adjust ancestor bounds.
                int index = grandParent;
                while (index != NullNode)
                {
                    index = Balance(index);

                    int child1 = _nodes[index].Child1;
                    int child2 = _nodes[index].Child2;

                    _nodes[index].Aabb = BoundingBox.CreateMerged(_nodes[child1].Aabb, _nodes[child2].Aabb);
                    _nodes[index].Height = 1 + Math.Max(_nodes[child1].Height, _nodes[child2].Height);

                    index = _nodes[index].ParentOrNext;
                }
            }
            else
            {
                _root = sibling;
                _nodes[sibling].ParentOrNext = NullNode;
                FreeNode(parent);
            }
        }

        /// Compute the height of the binary tree in O(N) time. Should not be
        /// called often.
        public int GetHeight()
        {
            using (m_rwLock.AcquireSharedUsing())
            {

                if (_root == NullNode)
                    return 0;

                return _nodes[_root].Height;
            }
        }


        // Perform a left or right rotation if node A is imbalanced.
        // Returns the new root index.
        public int Balance(int iA)
        {
            Debug.Assert(iA != NullNode);
            DynamicTreeNode A = _nodes[iA];

            if (A.IsLeaf() || A.Height < 2)
            {
                return iA;
            }

            int iB = A.Child1;
            int iC = A.Child2;
            Debug.Assert(0 <= iB && iB < _nodeCapacity);
            Debug.Assert(0 <= iC && iC < _nodeCapacity);

            DynamicTreeNode B = _nodes[iB];
            DynamicTreeNode C = _nodes[iC];

            int balance = C.Height - B.Height;

            //Rotate C up
            if (balance > 1)
            {
                int iF = C.Child1;
                int iG = C.Child2;
                DynamicTreeNode F = _nodes[iF];
                DynamicTreeNode G = _nodes[iG];
                Debug.Assert(0 <= iF && iF < _nodeCapacity);
                Debug.Assert(0 <= iG && iG < _nodeCapacity);

                // Swap A and C
                C.Child1 = iA;
                C.ParentOrNext = A.ParentOrNext;
                A.ParentOrNext = iC;

                // A's old parent should point to C

                if (C.ParentOrNext != NullNode)
                {
                    if (_nodes[C.ParentOrNext].Child1 == iA)
                    {
                        _nodes[C.ParentOrNext].Child1 = iC;
                    }
                    else
                    {
                        Debug.Assert(_nodes[C.ParentOrNext].Child2 == iA);
                        _nodes[C.ParentOrNext].Child2 = iC;
                    }
                }
                else
                {
                    _root = iC;
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
                DynamicTreeNode D = _nodes[iD];
                DynamicTreeNode E = _nodes[iE];
                Debug.Assert(0 <= iD && iD < _nodeCapacity);
                Debug.Assert(0 <= iE && iE < _nodeCapacity);

                // Swap A and B
                B.Child1 = iA;
                B.ParentOrNext = A.ParentOrNext;
                A.ParentOrNext = iB;

                // A's old parent should point to B
                if (B.ParentOrNext != NullNode)
                {
                    if (_nodes[B.ParentOrNext].Child1 == iA)
                    {
                        _nodes[B.ParentOrNext].Child1 = iB;
                    }
                    else
                    {
                        Debug.Assert(_nodes[B.ParentOrNext].Child2 == iA);
                        _nodes[B.ParentOrNext].Child2 = iB;
                    }
                }
                else
                {
                    _root = iB;
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

            if (_root == NullNode)
                return;

            using (m_rwLock.AcquireSharedUsing())
            {
                Stack<int> stack = GetStack();
                stack.Push(_root);

                //BoundingBox bbox = BoundingBoxHelper.InitialBox;
                //BoundingBoxHelper.AddFrustum(ref frustum, ref bbox);

                while (stack.Count > 0)
                {
                    int nodeId = stack.Pop();
                    if (nodeId == NullNode)
                    {
                        continue;
                    }

                    DynamicTreeNode node = _nodes[nodeId];

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

                                DynamicTreeNode nodeToAdd = _nodes[nodeIdToAdd];

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

            if (_root == NullNode)
            { 
                return;
            }

            using (m_rwLock.AcquireSharedUsing())
            {
                Stack<int> stack = GetStack();
                stack.Push(_root);

                //BoundingBox bbox = BoundingBoxHelper.InitialBox;
                //BoundingBoxHelper.AddFrustum(ref frustum, ref bbox);

                while (stack.Count > 0)
                {
                    int nodeId = stack.Pop();
                    if (nodeId == NullNode)
                    {
                        continue;
                    }

                    DynamicTreeNode node = _nodes[nodeId];

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

                                DynamicTreeNode nodeToAdd = _nodes[nodeIdToAdd];

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

            if (_root == NullNode)
            {
                return;
            }

            using (m_rwLock.AcquireSharedUsing())
            {
                Stack<int> stack = GetStack();
                stack.Push(_root);

                //BoundingBox bbox = BoundingBoxHelper.InitialBox;
                //BoundingBoxHelper.AddFrustum(ref frustum, ref bbox);

                while (stack.Count > 0)
                {
                    int nodeId = stack.Pop();
                    if (nodeId == NullNode)
                    {
                        continue;
                    }

                    DynamicTreeNode node = _nodes[nodeId];

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

                                DynamicTreeNode nodeToAdd = _nodes[nodeIdToAdd];

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

            if (_root == NullNode)
                return;

            using (m_rwLock.AcquireSharedUsing())
            {
                Stack<int> stack = GetStack();
                stack.Push(_root);

                BoundingBox bbox = BoundingBox.CreateInvalid();
                bbox.Include(ref frustum);

                while (stack.Count > 0)
                {
                    int nodeId = stack.Pop();
                    if (nodeId == NullNode)
                    {
                        continue;
                    }

                    DynamicTreeNode node = _nodes[nodeId];

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

                                DynamicTreeNode nodeToAdd = _nodes[nodeIdToAdd];

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

            if (_root == NullNode)
                return;

            using (m_rwLock.AcquireSharedUsing())
            {
                Stack<int> stack = GetStack();
                stack.Push(_root);

                //BoundingBox bbox = BoundingBoxHelper.InitialBox;
                //BoundingBoxHelper.AddFrustum(ref frustum, ref bbox);

                while (stack.Count > 0)
                {
                    int nodeId = stack.Pop();
                    if (nodeId == NullNode)
                    {
                        continue;
                    }

                    DynamicTreeNode node = _nodes[nodeId];

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

                                DynamicTreeNode nodeToAdd = _nodes[nodeIdToAdd];

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

            if (_root == NullNode)
                return;

            using (m_rwLock.AcquireSharedUsing())
            {
                Stack<int> stack = GetStack();
                stack.Push(_root);

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

                    DynamicTreeNode node = _nodes[nodeId];

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

            if (_root == NullNode)
                return;

            using (m_rwLock.AcquireSharedUsing())
            {
                Stack<int> stack = GetStack();
                stack.Push(_root);


                //int g = CountLeaves(_root);

                while (stack.Count > 0)
                {
                    int nodeId = stack.Pop();
                    if (nodeId == NullNode)
                    {
                        continue;
                    }

                    DynamicTreeNode node = _nodes[nodeId];

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

        public void OverlapAllBoundingSphere<T>(ref BoundingSphere sphere, List<T> overlapElementsList, bool clear = true)
        {
            if (clear)
                overlapElementsList.Clear();

            if (_root == NullNode)
                return;

            using (m_rwLock.AcquireSharedUsing())
            {
                Stack<int> stack = GetStack();
                stack.Push(_root);


                //int g = CountLeaves(_root);

                while (stack.Count > 0)
                {
                    int nodeId = stack.Pop();
                    if (nodeId == NullNode)
                    {
                        continue;
                    }

                    DynamicTreeNode node = _nodes[nodeId];

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

        private void ResetNodes()
        {
            _leafElementCache.Clear();

            _root = NullNode;

            _nodeCount = 0;
            _insertionCount = 0;

            // Build a linked list for the free list.
            for (int i = 0; i < _nodeCapacity - 1; ++i)
            {
                _nodes[i].ParentOrNext = i + 1;
                _nodes[i].Height = 1;
                _nodes[i].UserData = null;
            }
            _nodes[_nodeCapacity - 1].ParentOrNext = NullNode;
            _nodes[_nodeCapacity - 1].Height = 1;
            _freeList = 0;
        }

        public void Clear()
        {
            using (m_rwLock.AcquireExclusiveUsing())
            {
                lock (m_StackCacheCollection)
                {
                    foreach (var item in m_StackCacheCollection)
                    {
                        item.Clear();
                    }

                    m_StackCacheCollection.Clear();
                }

                ResetNodes();
            }
        }
    }
}

