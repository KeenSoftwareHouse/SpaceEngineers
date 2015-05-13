/*
 * 
 * C# / XNA  port of Bullet (c) 2011 Mark Neale <xexuxjy@hotmail.com>
 *
This source file is part of GIMPACT Library.

For the latest info, see http://gimpact.sourceforge.net/

Copyright (c) 2007 Francisco Leon Najera. C.C. 80087371.
email: projectileman@yahoo.com


This software is provided 'as-is', without any express or implied warranty.
In no event will the authors be held liable for any damages arising from the use of this software.
Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it freely,
subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not claim that you wrote the original software. If you use this software in a product, an acknowledgment in the product documentation would be appreciated but is not required.
2. Altered source versions must be plainly marked as such, and must not be misrepresented as being the original software.
3. This notice may not be removed or altered from any source distribution.
*/

using BulletXNA.LinearMath;
using System;
using System.Diagnostics;
using System.IO;

namespace BulletXNA.BulletCollision
{
    public delegate float? ProcessCollisionHandler(int triangleIndex);

    public class GImpactQuantizedBvh
    {
        private QuantizedBvhTree m_box_tree;
        private IPrimitiveManagerBase m_primitive_manager;
        private int m_size = 0;

        public byte[] Save()
        {
            return m_box_tree.Save();
        }

        public int Size
        {
            get { return m_size; }
        }

        public void Load(byte[] byteArray)
        {
            m_box_tree.Load(byteArray);
            m_size = byteArray.Length;
        }

        //! this constructor doesn't build the tree. you must call	buildSet
        public GImpactQuantizedBvh()
        {
            m_box_tree = new QuantizedBvhTree();
        }

        //! this constructor doesn't build the tree. you must call	buildSet
        public GImpactQuantizedBvh(IPrimitiveManagerBase primitive_manager)
        {
            m_primitive_manager = primitive_manager;
            m_box_tree = new QuantizedBvhTree();
        }

        //! this rebuild the entire set
        public void BuildSet()
        {
            //obtain primitive boxes
            int listSize = m_primitive_manager.GetPrimitiveCount();
            GIM_BVH_DATA_ARRAY primitive_boxes = new GIM_BVH_DATA_ARRAY(listSize);
            // forces boxes to be allocated
            primitive_boxes.Resize(listSize);

            GIM_BVH_DATA[] rawArray = primitive_boxes.GetRawArray();
            for (int i = 0; i < listSize; i++)
            {
                m_primitive_manager.GetPrimitiveBox(i, out rawArray[i].m_bound);
                rawArray[i].m_data = i;
            }

            m_box_tree.BuildTree(primitive_boxes);

        }

        //! returns the indices of the primitives in the m_primitive_manager
        public bool BoxQuery(ref AABB box, ObjectArray<int> collided_results)
        {
            return BoxQuery(ref box, collided_results, false);
        }

        private bool BoxQuery(ref AABB box, ObjectArray<int> collided_results, bool graphics)
        {
            int curIndex = 0;
            int numNodes = GetNodeCount();

            //quantize box

            UShortVector3 quantizedMin;
            UShortVector3 quantizedMax;

            m_box_tree.QuantizePoint(out quantizedMin, ref box.m_min);
            m_box_tree.QuantizePoint(out quantizedMax, ref box.m_max);


            while (curIndex < numNodes)
            {

                //catch bugs in tree data

                bool aabbOverlap = m_box_tree.TestQuantizedBoxOverlap(curIndex, ref quantizedMin, ref quantizedMax);
                bool isLeafNode = IsLeafNode(curIndex);

                if (isLeafNode && aabbOverlap)
                {
                    foreach (var i in GetNodeData(curIndex))
                    {
                        collided_results.Add(i);
                    }
                }

                if (aabbOverlap || isLeafNode)
                {
                    //next subnode
                    curIndex++;
                }
                else
                {
                    //skip node
                    curIndex += GetEscapeNodeIndex(curIndex);
                }
            }
            if (collided_results.Count > 0) return true;
            return false;
        }

        public bool RayQueryClosest(ref IndexedVector3 ray_dir, ref IndexedVector3 ray_origin, ProcessCollisionHandler handler)
        {
            int curIndex = 0;
            int numNodes = GetNodeCount();
            float distance = float.PositiveInfinity;

            while (curIndex < numNodes)
            {
                AABB bound;
                GetNodeBound(curIndex, out bound);

                //catch bugs in tree data

                float? aabbDist = bound.CollideRayDistance(ref ray_origin, ref ray_dir);
                bool isLeafNode = IsLeafNode(curIndex);
                bool aabbOverlapSignificant = aabbDist.HasValue && aabbDist.Value < distance;

                if (aabbOverlapSignificant && isLeafNode)
                {
                    foreach (var i in GetNodeData(curIndex))
                    {
                        float? newDist = handler(i);
                        if (newDist.HasValue && newDist.Value < distance)
                        {
                            distance = newDist.Value;
                        }
                    }
                }

                if (aabbOverlapSignificant || isLeafNode)
                {
                    //next subnode
                    curIndex++;
                }
                else
                {
                    //skip node
                    curIndex += GetEscapeNodeIndex(curIndex);
                }
            }
            return distance != float.PositiveInfinity;
        }

        public bool RayQuery(ref IndexedVector3 ray_dir, ref IndexedVector3 ray_origin, ProcessCollisionHandler handler)
        {
            int curIndex = 0;
            int numNodes = GetNodeCount();
            bool res = false;

            while (curIndex < numNodes)
            {
                AABB bound;
                GetNodeBound(curIndex, out bound);

                bool aabbOverlap = bound.CollideRay(ref ray_origin, ref ray_dir);
                bool isLeafNode = IsLeafNode(curIndex);

                if (isLeafNode && aabbOverlap)
                {
                    foreach (var i in GetNodeData(curIndex))
                    {
                        handler(i);
                        res = true;
                    }
                }

                if (aabbOverlap || isLeafNode)
                {
                    //next subnode
                    curIndex++;
                }
                else
                {
                    //skip node
                    curIndex += GetEscapeNodeIndex(curIndex);
                }
            }
            
            return res;
        }

        //! node count
        private int GetNodeCount()
        {
            return m_box_tree.GetNodeCount();
        }

        //! tells if the node is a leaf
        private bool IsLeafNode(int nodeindex)
        {
            return m_box_tree.IsLeafNode(nodeindex);
        }

        private int[] GetNodeData(int nodeindex)
        {
            return m_box_tree.GetNodeData(nodeindex);
        }

        private void GetNodeBound(int nodeindex, out AABB bound)
        {
            m_box_tree.GetNodeBound(nodeindex, out bound);
        }

        private int GetEscapeNodeIndex(int nodeindex)
        {
            return m_box_tree.GetEscapeNodeIndex(nodeindex);
        }
    }

    public class BT_QUANTIZED_BVH_NODE
    {
        //12 bytes
        public UShortVector3 m_quantizedAabbMin;
        public UShortVector3 m_quantizedAabbMax;
        //4 bytes
        public int[] m_escapeIndexOrDataIndex;

        public bool IsLeafNode()
        {
            //skipindex is negative (internal node), triangleindex >=0 (leafnode)
            return (m_escapeIndexOrDataIndex[0] >= 0);
        }

        public int GetEscapeIndex()
        {
            //btAssert(m_escapeIndexOrDataIndex < 0);
            return m_escapeIndexOrDataIndex == null ? 0 : - m_escapeIndexOrDataIndex[0];
        }

        public void SetEscapeIndex(int index)
        {
            m_escapeIndexOrDataIndex = new int[] { -index };
        }

        public void SetDataIndices(int[] indices)
        {
            m_escapeIndexOrDataIndex = indices;
        }

        public int[] GetDataIndices()
        {
            return m_escapeIndexOrDataIndex;
        }

        public bool TestQuantizedBoxOverlapp(ref UShortVector3 quantizedMin, ref UShortVector3 quantizedMax)
        {
            if (m_quantizedAabbMin.X > quantizedMax.X ||
               m_quantizedAabbMax.X < quantizedMin.X ||
               m_quantizedAabbMin.Y > quantizedMax.Y ||
               m_quantizedAabbMax.Y < quantizedMin.Y ||
               m_quantizedAabbMin.Z > quantizedMax.Z ||
               m_quantizedAabbMax.Z < quantizedMin.Z)
            {
                return false;
            }
            return true;
        }
    }

    public class GIM_QUANTIZED_BVH_NODE_ARRAY : ObjectArray<BT_QUANTIZED_BVH_NODE>
    {
        public GIM_QUANTIZED_BVH_NODE_ARRAY()
        {
        }

        public GIM_QUANTIZED_BVH_NODE_ARRAY(int capacity)
            : base(capacity)
        {
        }
    }

    //! Basic Box tree structure
    public class QuantizedBvhTree
    {
        private const int MAX_INDICES_PER_NODE = 6;

        private int m_num_nodes;
        private GIM_QUANTIZED_BVH_NODE_ARRAY m_node_array;
        private AABB m_global_bound;
        private IndexedVector3 m_bvhQuantization;

        static void WriteIndexedVector3(IndexedVector3 vector, BinaryWriter bw)
        {
            bw.Write(vector.X);
            bw.Write(vector.Y);
            bw.Write(vector.Z);
        }

        static IndexedVector3 ReadIndexedVector3(BinaryReader br)
        {
            return new IndexedVector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        }

        static void WriteUShortVector3(UShortVector3 vector, BinaryWriter bw)
        {
            bw.Write(vector.X);
            bw.Write(vector.Y);
            bw.Write(vector.Z);
        }

        static UShortVector3 ReadUShortVector3(BinaryReader br)
        {
            var vec = new UShortVector3();
            vec.X = br.ReadUInt16();
            vec.Y = br.ReadUInt16();
            vec.Z = br.ReadUInt16();
            return vec;
        }

        internal byte[] Save()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(m_num_nodes);
                WriteIndexedVector3(m_global_bound.m_min, bw);
                WriteIndexedVector3(m_global_bound.m_max, bw);
                WriteIndexedVector3(m_bvhQuantization, bw);

                for (int i = 0; i < m_num_nodes; i++)
                {
                    bw.Write((Int32)m_node_array[i].m_escapeIndexOrDataIndex.Length);
                    for (int j = 0; j < m_node_array[i].m_escapeIndexOrDataIndex.Length; j++)
                    {
                        bw.Write((Int32)m_node_array[i].m_escapeIndexOrDataIndex[j]);
                    }
                    WriteUShortVector3(m_node_array[i].m_quantizedAabbMin, bw);
                    WriteUShortVector3(m_node_array[i].m_quantizedAabbMax, bw);
                }

                return ms.ToArray();
            }
        }

        internal void Load(byte[] byteArray)
        {
            using (MemoryStream ms = new MemoryStream(byteArray))
            using (BinaryReader br = new BinaryReader(ms))
            {
                m_num_nodes = br.ReadInt32();
                var min = ReadIndexedVector3(br);
                var max = ReadIndexedVector3(br);
                m_global_bound = new AABB(ref min, ref max);
                m_bvhQuantization = ReadIndexedVector3(br);

                m_node_array = new GIM_QUANTIZED_BVH_NODE_ARRAY(m_num_nodes);
                for (int i = 0; i < m_num_nodes; i++)
                {
                    int count = br.ReadInt32();

                    BT_QUANTIZED_BVH_NODE node = new BT_QUANTIZED_BVH_NODE();
                    node.m_escapeIndexOrDataIndex = new int[count];
                    for (int j = 0; j < count; j++)
                    {
                        node.m_escapeIndexOrDataIndex[j] = br.ReadInt32();
                    }
                    node.m_quantizedAabbMin = ReadUShortVector3(br);
                    node.m_quantizedAabbMax = ReadUShortVector3(br);
                    m_node_array.Add(node);
                }
            }
        }

        private void CalcQuantization(GIM_BVH_DATA_ARRAY primitive_boxes)
        {
            CalcQuantization(primitive_boxes, 1.0f);
        }
        private void CalcQuantization(GIM_BVH_DATA_ARRAY primitive_boxes, float boundMargin)
        {
            //calc globa box
            AABB global_bound = new AABB();
            global_bound.Invalidate();

            int count = primitive_boxes.Count;
            for (int i = 0; i < count; i++)
            {
                global_bound.Merge(ref primitive_boxes.GetRawArray()[i].m_bound);
            }

            GImpactQuantization.CalcQuantizationParameters(out m_global_bound.m_min, out m_global_bound.m_max, out m_bvhQuantization, ref global_bound.m_min, ref global_bound.m_max, boundMargin);

        }

        private int SortAndCalcSplittingIndex(GIM_BVH_DATA_ARRAY primitive_boxes, int startIndex, int endIndex, int splitAxis)
        {
            int i;
            int splitIndex = startIndex;
            int numIndices = endIndex - startIndex;

            // average of centers
            float splitValue = 0.0f;

            IndexedVector3 means = IndexedVector3.Zero;
            for (i = startIndex; i < endIndex; i++)
            {
                IndexedVector3 center = 0.5f * (primitive_boxes[i].m_bound.m_max +
                             primitive_boxes[i].m_bound.m_min);
                means += center;
            }
            means *= ((1.0f) / (float)numIndices);

            splitValue = means[splitAxis];


            //sort leafNodes so all values larger then splitValue comes first, and smaller values start from 'splitIndex'.
            for (i = startIndex; i < endIndex; i++)
            {
                IndexedVector3 center = 0.5f * (primitive_boxes[i].m_bound.m_max +
                             primitive_boxes[i].m_bound.m_min);
                if (center[splitAxis] > splitValue)
                {
                    //swap
                    primitive_boxes.Swap(i, splitIndex);
                    //swapLeafNodes(i,splitIndex);
                    splitIndex++;
                }
            }

            //if the splitIndex causes unbalanced trees, fix this by using the center in between startIndex and endIndex
            //otherwise the tree-building might fail due to stack-overflows in certain cases.
            //unbalanced1 is unsafe: it can cause stack overflows
            //bool unbalanced1 = ((splitIndex==startIndex) || (splitIndex == (endIndex-1)));

            //unbalanced2 should work too: always use center (perfect balanced trees)
            //bool unbalanced2 = true;

            //this should be safe too:
            int rangeBalancedIndices = numIndices / 3;
            bool unbalanced = ((splitIndex <= (startIndex + rangeBalancedIndices)) || (splitIndex >= (endIndex - 1 - rangeBalancedIndices)));

            if (unbalanced)
            {
                splitIndex = startIndex + (numIndices >> 1);
            }

            Debug.Assert(!((splitIndex == startIndex) || (splitIndex == (endIndex))));

            return splitIndex;


        }

        private int CalcSplittingAxis(GIM_BVH_DATA_ARRAY primitive_boxes, int startIndex, int endIndex)
        {
            int i;

            IndexedVector3 means = IndexedVector3.Zero;
            IndexedVector3 variance = IndexedVector3.Zero;
            int numIndices = endIndex - startIndex;

            for (i = startIndex; i < endIndex; i++)
            {
                IndexedVector3 center = 0.5f * (primitive_boxes[i].m_bound.m_max +
                             primitive_boxes[i].m_bound.m_min);
                means += center;
            }
            means *= (1.0f / (float)numIndices);

            for (i = startIndex; i < endIndex; i++)
            {
                IndexedVector3 center = 0.5f * (primitive_boxes[i].m_bound.m_max +
                             primitive_boxes[i].m_bound.m_min);
                IndexedVector3 diff2 = center - means;
                diff2 = diff2 * diff2;
                variance += diff2;
            }
            variance *= (1.0f) / ((float)numIndices - 1);

            return MathUtil.MaxAxis(ref variance);
        }

        private void BuildSubTree(GIM_BVH_DATA_ARRAY primitive_boxes, int startIndex, int endIndex)
        {
            int curIndex = m_num_nodes;
            m_num_nodes++;

            Debug.Assert((endIndex - startIndex) > 0);

            if ((endIndex - startIndex) <= MAX_INDICES_PER_NODE)
            {
                //We have a leaf node
                int count = endIndex - startIndex;
                int[] indices = new int[count];
                AABB bounds = new AABB();
                bounds.Invalidate();

                for (int i = 0; i < count; i++)
                {
                    indices[i] = primitive_boxes[startIndex + i].m_data;
                    bounds.Merge(primitive_boxes.GetRawArray()[startIndex + i].m_bound);
                }
                SetNodeBound(curIndex, ref bounds);
                m_node_array[curIndex].SetDataIndices(indices);

                return;
            }
            //calculate Best Splitting Axis and where to split it. Sort the incoming 'leafNodes' array within range 'startIndex/endIndex'.

            //split axis
            int splitIndex = CalcSplittingAxis(primitive_boxes, startIndex, endIndex);

            splitIndex = SortAndCalcSplittingIndex(
                    primitive_boxes, startIndex, endIndex,
                    splitIndex//split axis
                    );


            //calc this node bounding box

            AABB node_bound = new AABB();
            node_bound.Invalidate();

            for (int i = startIndex; i < endIndex; i++)
            {
                node_bound.Merge(ref primitive_boxes.GetRawArray()[i].m_bound);
            }

            SetNodeBound(curIndex, ref node_bound);


            //build left branch
            BuildSubTree(primitive_boxes, startIndex, splitIndex);


            //build right branch
            BuildSubTree(primitive_boxes, splitIndex, endIndex);

            m_node_array.GetRawArray()[curIndex].SetEscapeIndex(m_num_nodes - curIndex);

        }

        internal QuantizedBvhTree()
        {
            m_num_nodes = 0;
            m_node_array = new GIM_QUANTIZED_BVH_NODE_ARRAY();
        }

        //! prototype functions for box tree management
        //!@{
        internal void BuildTree(GIM_BVH_DATA_ARRAY primitive_boxes)
        {
            CalcQuantization(primitive_boxes);
            // initialize node count to 0
            m_num_nodes = 0;
            // allocate nodes
            m_node_array.Resize(primitive_boxes.Count * 2);

            BuildSubTree(primitive_boxes, 0, primitive_boxes.Count);
        }

        internal void QuantizePoint(out UShortVector3 quantizedpoint, ref IndexedVector3 point)
        {
            GImpactQuantization.QuantizeClamp(out quantizedpoint, ref point, ref m_global_bound.m_min, ref m_global_bound.m_max, ref m_bvhQuantization);
        }

        internal bool TestQuantizedBoxOverlap(int node_index, ref UShortVector3 quantizedMin, ref UShortVector3 quantizedMax)
        {
            return m_node_array[node_index].TestQuantizedBoxOverlapp(ref quantizedMin, ref quantizedMax);
        }

        //! node count
        internal int GetNodeCount()
        {
            return m_num_nodes;
        }

        //! tells if the node is a leaf
        internal bool IsLeafNode(int nodeindex)
        {
            return m_node_array[nodeindex].IsLeafNode();
        }

        internal int[] GetNodeData(int nodeindex)
        {
            return m_node_array[nodeindex].GetDataIndices();
        }

        internal void GetNodeBound(int nodeindex, out AABB bound)
        {
            bound.m_min = GImpactQuantization.Unquantize(
                ref m_node_array.GetRawArray()[nodeindex].m_quantizedAabbMin,
                ref m_global_bound.m_min, ref m_bvhQuantization);

            bound.m_max = GImpactQuantization.Unquantize(
                ref m_node_array.GetRawArray()[nodeindex].m_quantizedAabbMax,
                ref m_global_bound.m_min, ref m_bvhQuantization);
        }

        private void SetNodeBound(int nodeindex, ref AABB bound)
        {
            GImpactQuantization.QuantizeClamp(out m_node_array.GetRawArray()[nodeindex].m_quantizedAabbMin,
                                ref bound.m_min,
                                ref m_global_bound.m_min,
                                ref m_global_bound.m_max,
                                ref m_bvhQuantization);

            GImpactQuantization.QuantizeClamp(out m_node_array.GetRawArray()[nodeindex].m_quantizedAabbMax,
                                ref bound.m_max,
                                ref m_global_bound.m_min,
                                ref m_global_bound.m_max,
                                ref m_bvhQuantization);
        }

        internal int GetEscapeNodeIndex(int nodeindex)
        {
            return m_node_array[nodeindex].GetEscapeIndex();
        }
    }


}
