using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRageMath;

namespace VRageMath
{
    // TODO: move this into it's own file.
    public struct Box2D
    {
        public Vector2 Min, Max;

        public bool Overlaps(ref Box2D rect)
        {
            return !(Min.X > rect.Max.X || rect.Min.X > Min.X || Min.X > rect.Max.Y || rect.Min.Y > Min.X);
        }

        public void Glob(ref Box2D rect)
        {
            Max.X = Max.X < rect.Max.X ? rect.Max.X : Max.X;
            Min.X = Min.X > rect.Min.X ? rect.Min.X : Min.X;
            Max.Y = Max.Y < rect.Max.Y ? rect.Max.Y : Max.Y;
            Min.Y = Min.Y > rect.Min.Y ? rect.Min.Y : Min.Y;
        }

        public static void Union(ref Box2D b1, ref Box2D b2, out Box2D box)
        {
            box.Max.X = b1.Max.X < b2.Max.X ? b2.Max.X : b1.Max.X;
            box.Min.X = b1.Min.X > b2.Min.X ? b2.Min.X : b1.Min.X;
            box.Max.Y = b1.Max.Y < b2.Max.Y ? b2.Max.Y : b1.Max.Y;
            box.Min.Y = b1.Min.Y > b2.Min.Y ? b2.Min.Y : b1.Min.Y;
        }

        public Vector2 Center
        {
            get { return (Min + Max) * .5f; }
        }
    }

    public class Tree2D<ValueType>
    {
        /*********************************************************************
         * 
         * Data Types
         * 
         */

        class TreeNode
        {
            public Box2D Bounds;
            public bool Internal { get; private set; }

            protected TreeNode(bool __intern) {
                Internal = __intern;
            }
        }

        class InternalNode : TreeNode
        {
            public TreeNode Left, Right;

            public InternalNode() : base(true)
            {
            }
        }

        class LeafNode : TreeNode
        {
            public ValueType Value;

            public LeafNode(GetBox b, ref ValueType t) : base(false)
            {
                // TODO: Complete member initialization
                this.Value = t;
                b(ref this.Value, out this.Bounds);
            }
        }

        public delegate void GetBox(ref ValueType val, out Box2D box);

        /*********************************************************************
         * 
         * Static Methods
         * 
         */

        private class ComparerX : IComparer<Tree2D<ValueType>.LeafNode>
        {
            public int Compare(Tree2D<ValueType>.LeafNode node1, Tree2D<ValueType>.LeafNode node2)
            {
                return Math.Sign(node2.Bounds.Center.X - node1.Bounds.Center.X);
            }
        }

        private class ComparerY : IComparer<Tree2D<ValueType>.LeafNode>
        {
            public int Compare(Tree2D<ValueType>.LeafNode node1, Tree2D<ValueType>.LeafNode node2)
            {
                return Math.Sign(node2.Bounds.Center.Y - node1.Bounds.Center.Y);
            }
        }

        private TreeNode BuildSubtree(LeafNode[] values, int rangeStart, int rangeEnd, int axis, int depth = 0)
        {
            int range = rangeEnd - rangeStart;
            Debug.Assert(range > 0, "Invalid range");

            if (depth > m_depth) m_depth = depth;

            if (range == 1)
            {
                return values[rangeStart];
            }

            if (axis == 1)
                Array.Sort(values, rangeStart, range, new ComparerX());
            else
                Array.Sort(values, rangeStart, range, new ComparerY());

            var node = new InternalNode();

            if (range == 2)
            {

                node.Left = values[rangeStart];
                node.Right = values[rangeStart + 1];
            }
            else
            {
                int mid = (rangeStart + rangeEnd) / 2;

                node.Left = BuildSubtree(values, rangeStart, mid, 1 - axis);
                node.Right = BuildSubtree(values, mid, rangeEnd, 1 - axis);
            }

            Box2D.Union(ref node.Left.Bounds, ref node.Right.Bounds, out node.Bounds);
            return node;
        }

        public static Tree2D<Type> Create<Type>(Type[] values, Tree2D<Type>.GetBox getBox) {
            Tree2D<Type> tree = new Tree2D<Type>();

            tree.m_leaves = new Tree2D<Type>.LeafNode[values.Length];

            for (int i = 0; i < values.Length; ++i)
            {
                tree.m_leaves[i] = new Tree2D<Type>.LeafNode(getBox, ref values[i]);
            }

            tree.m_root = tree.BuildSubtree(tree.m_leaves, 0, values.Length, 0);

            tree.m_stack = new Stack<Tree2D<Type>.TreeNode>(tree.m_depth);

            return tree;
        }

        /*********************************************************************
         * 
         * Members
         * 
         */

        // getter for value boxes.
        //private GetBox m_box;

        // Root node.
        private TreeNode m_root;

        // List of preallocated tree leves.
        private LeafNode[] m_leaves;

        private int m_depth;

        private Stack<TreeNode> m_stack;

        public bool QueryFirst(ref Box2D range, out ValueType value)
        {
            m_stack.Clear();

            while (m_stack.Count > 0)
            {
                var node = m_stack.Pop();
                while (node.Internal)
                {
                    var intern = node as InternalNode;
                    if (intern.Left.Bounds.Overlaps(ref range))
                    {
                        node = intern.Left;
                    }
                    else
                    {
                        node = null;
                    }

                    if (intern.Right.Bounds.Overlaps(ref range))
                    {
                        if (node != null)
                            m_stack.Push(intern.Right);
                        else
                            node = intern.Right;
                    }
                }

                // Already checked box at this stage.
                if (!node.Internal)
                {
                    var leaf = node as LeafNode;
                    value = leaf.Value;
                    return true;
                }
            }

            value = default(ValueType);
            return false;
        }

        /*public IEnumerable<ValueType> QueryTree(Box2D range)
        {
            Stack<TreeNode> parents = new Stack<TreeNode>();
        }*/
    }
}
