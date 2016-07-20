using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Library.Collections;
using VRageMath;

namespace Sandbox.Game.WorldEnvironment
{
    using Node = __helper_namespace.Node;

    // Handler for nodes in a 2D clipmap.
    public interface IMy2DClipmapNodeHandler
    {
        // Init from the parent (happens only for roots)
        void Init(IMy2DClipmapManager parent, int x, int y, int lod, ref BoundingBox2D bounds);

        // Close, called when a parent is split, a child is joined, or when a root is destroyed
        void Close();

        // Init from 4 children.
        void InitJoin(IMy2DClipmapNodeHandler[] children);

        // Split into 4 children
        unsafe void Split(BoundingBox2D* childBoxes, ref IMy2DClipmapNodeHandler[] children);
    }

    // Interface for managers of 2D clipmaps
    public interface IMy2DClipmapManager
    {
    }

    public static class My2DClipmapHelpers
    {
        public static readonly Vector2D[] CoordsFromIndex = { Vector2D.Zero, Vector2D.UnitX, Vector2D.UnitY, Vector2D.One };

        public static readonly Color[] LodColors =
        {
            Color.Red,
            Color.Green,
            Color.Blue,
            Color.Yellow,
            Color.Magenta,
            Color.Cyan,
            new Color(1, .5f, 0),
            new Color(1, 0, .5f),
            new Color(.5f, 0, 1),
            new Color(.5f, 1, 0),
            new Color(0, 1, .5f),
            new Color(0, .5f, 1),
        };
    }

    /**
     * We want the node type outside the My2DClipmap class because that class is generic.
     * 
     * Still we don't want it to be visible elsewhere.
     */
    namespace __helper_namespace
    {
        internal unsafe struct Node
        {
            public fixed int Children[4];
            public int Lod; // Used as next for the free node map.
        }
    }

    public class My2DClipmap<THandler> where THandler : class, IMy2DClipmapNodeHandler, new()
    {
        private int m_root;

        private double m_size;
        private int m_splits;

        private double[] m_lodSizes;
        private double[] m_keepLodSizes;

        public Vector3D LastPosition { get; private set; }

        public MatrixD WorldMatrix { get; private set; }
        public MatrixD InverseWorldMatrix { get; private set; }
        public double FaceHalf { get { return m_lodSizes[m_splits]; } }
        public double LeafSize { get { return m_lodSizes[1]; } }
        public int Depth { get { return m_splits; } }

        #region Memory Management

        // Handlers for leaves
        private MyFreeList<THandler> m_leafHandlers;

        // Nodes
        private Node[] m_nodes;
        private THandler[] m_nodeHandlers; // Handlers for internal nodes

        // Index of the first non-allocated node
        private int m_firstFree;

        // Null value for node index
        private const int NullNode = int.MinValue;

        private unsafe void PrepareAllocator()
        {
            int startingLenth = 16;

            m_nodes = new Node[startingLenth];
            fixed (Node* nodes = m_nodes)
            {
                for (int i = 0; i < startingLenth; ++i)
                    nodes[i].Lod = ~(i + 1);
            }
            m_firstFree = 0;

            m_nodeHandlers = new THandler[startingLenth];
            m_leafHandlers = new MyFreeList<THandler>();
        }

        public int NodeAllocDeallocs;

        private unsafe int AllocNode()
        {
            //Debug.Assert(m_firstFree != 0);

            NodeAllocDeallocs++;
            if (m_firstFree == m_nodes.Length)
            {
                int start = m_nodes.Length;

                Array.Resize(ref m_nodes, m_nodes.Length << 1);
                Array.Resize(ref m_nodeHandlers, m_nodes.Length);

                fixed (Node* nodes = m_nodes)
                {
                    for (int i = start; i < m_nodes.Length; ++i)
                        nodes[i].Lod = ~(i + 1);
                }
                m_firstFree = start;
            }
            var node = m_firstFree;
            fixed (Node* nodes = m_nodes)
            {
                for (int i = 0; i < 4; ++i)
                    nodes[m_firstFree].Children[i] = NullNode;

                m_firstFree = ~nodes[m_firstFree].Lod;
            }

            Debug.Assert(m_nodes[node].Lod < 0, "Allocating node that had been in use after freed!");

            return node;
        }

        private unsafe void FreeNode(int node)
        {
            Debug.Assert(node != 0, "Double free!");
            NodeAllocDeallocs++;
            fixed (Node* nodes = m_nodes)
            {
                Debug.Assert(nodes[node].Lod >= 0, "Double free!");
                nodes[node].Lod = ~m_firstFree;
                m_firstFree = node;

                m_nodeHandlers[node] = null;
            }
        }

        // Will push all allocated nodes to the start of the array and then shrink it to the smallest suficient power of two.
        private unsafe void Compact()
        {
            //TODO
        }

        #endregion

        #region Node Utils

        private unsafe int Child(int node, int index)
        {
            fixed (Node* nodes = m_nodes)
                return nodes[node].Children[index];
        }

        private readonly List<int> m_nodesToDealloc = new List<int>();
        private unsafe void CollapseSubtree(int parent, int childIndex, Node* nodes)
        {
            var subtree = nodes[parent].Children[childIndex];

            m_nodesToDealloc.Add(subtree);

            Debug.Assert(subtree != 0);

            // Recursively cleanup nodes.
            Node* cur = nodes + subtree;
            for (var i = 0; i < 4; ++i)
                if (cur->Children[i] >= 0)
                    CollapseSubtree(subtree, i, nodes);

            // Take the leaf handlers from children
            var childHandlers = m_tmpNodeHandlerList;

            for (var i = 0; i < 4; ++i)
                childHandlers[i] = m_leafHandlers[~cur->Children[i]];

            var subtreeHandler = m_nodeHandlers[subtree];
            subtreeHandler.InitJoin(childHandlers);

            for (var i = 0; i < 4; ++i)
            {
                m_leafHandlers.Free(~cur->Children[i]);
                childHandlers[i].Close();
            }

            int leafHandler = m_leafHandlers.Allocate();
            nodes[parent].Children[childIndex] = ~leafHandler;
            m_leafHandlers[leafHandler] = subtreeHandler;
        }

        private unsafe void CollapseRoot()
        {
            fixed (Node* nodes = m_nodes)
            {
                Node* cur = nodes + m_root;

                if (cur->Children[0] == NullNode)
                    return; // Already clean

                // Recursivelly cleanup nodes.
                for (var i = 0; i < 4; ++i)
                    if (cur->Children[i] >= 0)
                        CollapseSubtree(m_root, i, nodes);

                // Take the leaf handlers from children
                var childHandlers = m_tmpNodeHandlerList;

                for (var i = 0; i < 4; ++i)
                    childHandlers[i] = m_leafHandlers[~cur->Children[i]];

                // Find our handler
                var subtreeHandler = m_nodeHandlers[m_root];
                subtreeHandler.InitJoin(childHandlers);

                // Close children after join
                for (var i = 0; i < 4; ++i)
                {
                    m_leafHandlers.Free(~cur->Children[i]);
                    childHandlers[i].Close();

                    cur->Children[i] = NullNode;
                }
            }

            foreach (var node in m_nodesToDealloc)
            {
                Debug.Assert(node != 0);
                FreeNode(node);
            }

            m_nodesToDealloc.Clear();
        }

        #endregion

        private IMy2DClipmapManager m_manager;

        public unsafe void Init(IMy2DClipmapManager manager, ref MatrixD worldMatrix, double sectorSize, double faceSize)
        {
            m_manager = manager;

            WorldMatrix = worldMatrix;
            InverseWorldMatrix = Matrix.Invert(worldMatrix);

            m_size = faceSize;

            m_splits = Math.Max(MathHelper.Log2Floor((int)(faceSize / sectorSize)), 1);

            m_lodSizes = new double[m_splits + 1];
            for (int i = 0; i <= m_splits; i++)
                m_lodSizes[m_splits - i] = faceSize / (1 << (i + 1));

            m_keepLodSizes = new double[m_splits + 1];
            for (int i = 0; i <= m_splits; i++)
                m_keepLodSizes[i] = 1.5 * m_lodSizes[i];

            m_queryBounds = new BoundingBox2D[m_splits + 1];
            m_keepBounds = new BoundingBox2D[m_splits + 1];

            PrepareAllocator();

            m_root = AllocNode();

            fixed (Node* nodes = m_nodes)
            {
                Node* root = nodes + m_root;
                root->Lod = m_splits;
            }

            BoundingBox2D rootBounds = new BoundingBox2D(new Vector2D(-faceSize / 2), new Vector2D(faceSize / 2));

            m_nodeHandlers[m_root] = new THandler();
            m_nodeHandlers[m_root].Init(m_manager, 0, 0, m_splits, ref rootBounds);
        }


        private struct StackInfo
        {
            public int Node;
            public Vector2D Center;
            public double Size;
            public int Lod;

            public StackInfo(int node, Vector2D center, double size, int lod)
            {
                Node = node;
                Center = center;
                Size = size;
                Lod = lod;
            }
        }

        private readonly Stack<StackInfo> m_nodesToScanNext = new Stack<StackInfo>();

        private BoundingBox2D[] m_queryBounds;
        private BoundingBox2D[] m_keepBounds;

        // Preallocate this because stackalloc is way too annoying.
        private readonly BoundingBox2D[] m_nodeBoundsTmp = new BoundingBox2D[4];
        private readonly IMy2DClipmapNodeHandler[] m_tmpNodeHandlerList = new IMy2DClipmapNodeHandler[4];

        /**
         * What is this?
         * 
         * Partial implementation of a 2D Clipmap.
         * 
         * Beware that my implementation uses a quadtree for 1:2 lod size ratios. This makes the implementation
         * simpler but does differ from the implementations in literature.
         * 
         * Additionally literature proposes that there be a invalid zone where data is preloaded. In our case
         * that zone is of size 0.
         * 
         * Other than that we have what I jugde some pretty efficient and compact code, the results seem
         * quite agreeable.
         * 
         */
        public unsafe void Update(Vector3D localPosition)
        {
            double updateRange = localPosition.Z * 0.1;
            updateRange *= updateRange;

            // Avoid updating unnecessarily when not moving
            if (Vector3D.DistanceSquared(LastPosition, localPosition) < updateRange)
                return;

            LastPosition = localPosition;

            //Local space in plane position
            Vector2D pos = new Vector2D(localPosition.X, localPosition.Y);

            // Prepare per lod queries, since we do depth first this can save up some work.
            for (int lod = m_splits; lod >= 0; --lod)
            {
                m_queryBounds[lod] = new BoundingBox2D(pos - m_lodSizes[lod], pos + m_lodSizes[lod]);
                m_keepBounds[lod] = new BoundingBox2D(pos - m_keepLodSizes[lod], pos + m_keepLodSizes[lod]);
            }

            // Treat the root specially
            if (localPosition.Z > m_keepLodSizes[m_splits])
            {
                if (Child(m_root, 0) != NullNode)
                    CollapseRoot();
                goto cleanup;
            }

            m_nodesToScanNext.Push(new StackInfo(m_root, Vector2D.Zero, m_size / 2, m_splits));

            // While we have interesting nodes to scan we analyze them.
            fixed (BoundingBox2D* childBounds = m_nodeBoundsTmp)
            fixed (BoundingBox2D* keepBounds = m_keepBounds)
            fixed (BoundingBox2D* queryBounds = m_queryBounds)
                while (m_nodesToScanNext.Count != 0)
                {
                    // Current node to work
                    StackInfo current = m_nodesToScanNext.Pop();

                    // quick local shorthands
                    double size2 = current.Size / 2;
                    int childLod = current.Lod - 1;

                    // Mask for the expected status changes for each child
                    int collapseMask = 0;

                    for (int childIndex = 0; childIndex < 4; ++childIndex)
                    {
                        // Calculate bounds for each child
                        childBounds[childIndex].Min = current.Center + My2DClipmapHelpers.CoordsFromIndex[childIndex] * current.Size - current.Size;
                        childBounds[childIndex].Max = current.Center + My2DClipmapHelpers.CoordsFromIndex[childIndex] * current.Size;

                        // Store if the child is interesting
                        if (childBounds[childIndex].Intersects(ref queryBounds[childLod])
                            && localPosition.Z <= queryBounds[childLod].Height) collapseMask |= 1 << childIndex;
                    }

                    // If some child is expected to be split we need to ensure that we have at least leaf children.
                    if (Child(current.Node, 0) == NullNode)
                    {
                        var handler = m_nodeHandlers[current.Node];

                        // Prepare children for splitting
                        IMy2DClipmapNodeHandler[] childHandlers = m_tmpNodeHandlerList;
                        fixed (Node* nodes = m_nodes)
                            for (int childIndex = 0; childIndex < 4; ++childIndex)
                            {
                                var chHandler = m_leafHandlers.Allocate();
                                m_leafHandlers[chHandler] = new THandler();

                                childHandlers[childIndex] = m_leafHandlers[chHandler];
                                nodes[current.Node].Children[childIndex] = ~chHandler;
                            }

                        handler.Split(childBounds, ref childHandlers);

                        handler.Close();
                    }

                    // Skip investigating past LOD 1
                    if (current.Lod == 1) continue;

                    // Check children, queue if interesting
                    // Interesting means it either is supposed to have content or it has content but is not supposed to.
                    for (int childIndex = 0; childIndex < 4; ++childIndex)
                    {
                        int chNode = Child(current.Node, childIndex);

                        // If child is interesting we introduce a new node
                        if ((collapseMask & (1 << childIndex)) != 0)
                        {
                            Debug.Assert(chNode != NullNode);

                            // If node is leaf
                            if (chNode < 0)
                            {
                                var handler = m_leafHandlers[~chNode];
                                m_leafHandlers.Free(~chNode);

                                // Allocate new concrete node
                                chNode = AllocNode();
                                m_nodeHandlers[chNode] = handler;

                                fixed (Node* nodes = m_nodes)
                                {
                                    Node* child = nodes + chNode;
                                    child->Lod = childLod;
                                    nodes[current.Node].Children[childIndex] = chNode;
                                }
                            }
                        }
                        // Lazy close to avoid sweetspot
                        else if (chNode >= 0
                            && !(childBounds[childIndex].Intersects(ref keepBounds[childLod])
                                && localPosition.Z <= keepBounds[childLod].Height))
                        {
                            // If the subtree is utterly uninteresting we clean it up
                            Debug.Assert(chNode != NullNode);
                            fixed (Node* nodes = m_nodes)
                                CollapseSubtree(current.Node, childIndex, nodes);
                        }

                        // Investigate subtrees for nodes to clean and/or spawn
                        if (chNode >= 0)
                        {
                            m_nodesToScanNext.Push(new StackInfo(
                                chNode,
                                current.Center + My2DClipmapHelpers.CoordsFromIndex[childIndex] * current.Size - size2,
                                size2,
                                childLod
                            ));
                        }
                    }
                }

        cleanup:
            foreach (var node in m_nodesToDealloc)
            {
                Debug.Assert(node != 0);
                FreeNode(node);
            }

            m_nodesToDealloc.Clear();
        }

        public unsafe THandler GetHandler(Vector2D point)
        {
            m_nodesToScanNext.Push(new StackInfo(m_root, Vector2D.Zero, m_size / 2, m_splits));

            int interestNode = m_root;

            // While we have interesting nodes to scan we analyze them.
            BoundingBox2D childBounds;
            fixed (Node* nodes = m_nodes)
                while (m_nodesToScanNext.Count != 0)
                {
                    // Current node to work
                    StackInfo current = m_nodesToScanNext.Pop();

                    // quick local shorthands
                    double size2 = current.Size / 2;
                    int childLod = current.Lod - 1;

                    for (int childIndex = 0; childIndex < 4; ++childIndex)
                    {
                        // Calculate bounds for each child
                        childBounds.Min = current.Center + My2DClipmapHelpers.CoordsFromIndex[childIndex] * current.Size - current.Size;
                        childBounds.Max = current.Center + My2DClipmapHelpers.CoordsFromIndex[childIndex] * current.Size;

                        // Store if the child is interesting
                        if (childBounds.Contains(point) != ContainmentType.Disjoint)
                        {
                            var child = nodes[current.Node].Children[childIndex];

                            if (child != NullNode)
                            {
                                interestNode = child;
                                if (childLod > 0 && child >= 0)
                                    m_nodesToScanNext.Push(new StackInfo(
                                        interestNode,
                                        current.Center + My2DClipmapHelpers.CoordsFromIndex[childIndex] * current.Size - size2,
                                        size2,
                                        childLod
                                        ));
                            }
                        }
                    }
                }

            if (interestNode < 0)
                return m_leafHandlers[~interestNode];
            return m_nodeHandlers[interestNode];
        }

        public void Clear()
        {
            CollapseRoot();
        }
    }
}