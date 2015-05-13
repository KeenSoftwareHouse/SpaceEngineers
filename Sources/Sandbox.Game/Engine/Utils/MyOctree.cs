using Sandbox.Engine.Voxels;
using System.Collections.Generic;
using VRageMath;

namespace Sandbox.Game.Utils
{
    class MyOctree
    {
        // 73 nodes: 0 = root, 1+(0..7) = first level, 9+(0..63) = leaves
        const int NODE_COUNT = 1 + 8 + 8 * 8;

        byte[] m_childEmpty = new byte[1 + 8];  // default 0: all full
        short[] m_firstTriangleIndex = new short[NODE_COUNT];
        byte[] m_triangleCount = new byte[NODE_COUNT];  // default 0
        Vector3 m_bbMin, m_bbInvScale;

        /// <summary>
        /// Initializes a new instance of the MyOctree class.
        /// </summary>
        /// <param name="triangles">Input triangle array</param>
        /// <param name="sortedTriangles">Output triangle array (can be same as input triangle array)</param>
        public MyOctree()
        {
        }

        public void Init(Vector3[] positions, int vertexCount, MyVoxelTriangle[] triangles, int triangleCount, out MyVoxelTriangle[] sortedTriangles)
        {
            for (int i = 0; i < NODE_COUNT; i++)
            {
                m_firstTriangleIndex[i] = 0;
                m_triangleCount[i] = 0;
            }
            for (int i = 0; i < 9; i++)
            {
                m_childEmpty[i] = 0;
            }

            // compute bounding box
            {
                BoundingBox bbox = BoundingBox.CreateInvalid();
                for (int i = 0; i < vertexCount; i++)
                    bbox.Include(ref positions[i]);
                m_bbMin = bbox.Min;
                var scale = bbox.Max - bbox.Min;

                m_bbInvScale = Vector3.One;  // minimum bounding box size = 1 (in each dimension)
                if (scale.X > 1) m_bbInvScale.X = 1 / scale.X;
                if (scale.Y > 1) m_bbInvScale.Y = 1 / scale.Y;
                if (scale.Z > 1) m_bbInvScale.Z = 1 / scale.Z;
            }

            // compute triangle counts
            for (int i = 0; i < triangleCount; i++)
            {
                var t = triangles[i];
                BoundingBox bbox = BoundingBox.CreateInvalid();
                bbox.Include(ref positions[t.VertexIndex0],
                             ref positions[t.VertexIndex1],
                             ref positions[t.VertexIndex2]);
                short count = m_triangleCount[GetNode(ref bbox)]++;
            }

            // accumulate triangle counts
            m_firstTriangleIndex[0] = m_triangleCount[0];
            for (int i = 1; i < NODE_COUNT; i++)
            {
                m_firstTriangleIndex[i] = (short)(m_firstTriangleIndex[i - 1] + m_triangleCount[i]);
            }
            // m_firstTriangleIndex[i] now contains the first index AFTER where the node's triangles will be, e.g.:
            //   m_triangleCount:      2 0 4 3
            //   m_firstTriangleIndex: 2 2 6 9

            // bucketsort triangles into the output array according to the nodes they're in
            var newSortedTriangles = new MyVoxelTriangle[triangleCount];
            for (int i = 0; i < triangleCount; i++)
            {
                var t = triangles[i];
                BoundingBox bbox = BoundingBox.CreateInvalid();
                bbox.Include(ref positions[t.VertexIndex0],
                             ref positions[t.VertexIndex1],
                             ref positions[t.VertexIndex2]);
                newSortedTriangles[--m_firstTriangleIndex[GetNode(ref bbox)]] = t;
            }
            sortedTriangles = newSortedTriangles;  // "out sortedTriangles" may be the same as "triangles"

            // find empty children
            for (int i = NODE_COUNT - 1; i > 0; i--)
                if (m_triangleCount[i] == 0 && (i > 8 || m_childEmpty[i] == 0xFF)) // no triangles AND (no children OR all children empty)
                    m_childEmpty[i - 1 >> 3] |= (byte)(1 << (i - 1 & 7));
        }

        public void BoxQuery(ref BoundingBox bbox, List<int> triangleIndices)
        {
            BoundingBox transformedBox = new BoundingBox((bbox.Min - m_bbMin) * m_bbInvScale, (bbox.Max - m_bbMin) * m_bbInvScale);

            bool result;

            box[0].Intersects(ref transformedBox, out result);

            if (result)
            {
                for (int k = 0; k < m_triangleCount[0]; k++)
                    triangleIndices.Add(m_firstTriangleIndex[0] + k);

                // children of root
                for (int i = 1, mask = 1; i < 9; i++, mask <<= 1) if ((m_childEmpty[0] & mask) == 0)
                {
                    // first level
                    box[i].Intersects(ref transformedBox, out result);
                    if (result)
                    {
                        for (int k = 0; k < m_triangleCount[i]; k++)
                            triangleIndices.Add(m_firstTriangleIndex[i] + k);

                        // children of first level
                        for (int j = i * 8 + 1, mask2 = 1; j < i * 8 + 9; j++, mask2 <<= 1) if ((m_childEmpty[i] & mask2) == 0)
                        {
                            // second level
                            box[j].Intersects(ref transformedBox, out result);
                            if (result)
                            {
                                for (int k = 0; k < m_triangleCount[j]; k++)
                                    triangleIndices.Add(m_firstTriangleIndex[j] + k);
                            }
                        }
                    }
                }
            }
        }

        public void GetIntersectionWithLine(ref Ray ray, List<int> triangleIndices)
        {
            Ray transformedRay = new Ray((ray.Position - m_bbMin) * m_bbInvScale, ray.Direction * m_bbInvScale);

            float? result;

            // root
            box[0].Intersects(ref transformedRay, out result);
            if (result.HasValue)
            {
                for (int k = 0; k < m_triangleCount[0]; k++)
                    triangleIndices.Add(m_firstTriangleIndex[0] + k);

                // children of root
                for (int i = 1, mask = 1; i < 9; i++, mask <<= 1) if ((m_childEmpty[0] & mask) == 0)
                {
                    // first level
                    box[i].Intersects(ref transformedRay, out result);
                    if (result.HasValue)
                    {
                        for (int k = 0; k < m_triangleCount[i]; k++)
                            triangleIndices.Add(m_firstTriangleIndex[i] + k);

                        // children of first level
                        for (int j = i * 8 + 1, mask2 = 1; j < i * 8 + 9; j++, mask2 <<= 1) if ((m_childEmpty[i] & mask2) == 0)
                        {
                            // second level
                            box[j].Intersects(ref transformedRay, out result);
                            if (result.HasValue)
                            {
                                for (int k = 0; k < m_triangleCount[j]; k++)
                                    triangleIndices.Add(m_firstTriangleIndex[j] + k);
                            }
                        }
                    }
                }
            }
        }

        #region Precomputed bounding boxes of nodes

        const float CHILD_SIZE = 0.65f;

        static readonly BoundingBox[] box;  // precomputed boxes for each node (as if it was in [0,1])
        static MyOctree()
        {
            box = new BoundingBox[NODE_COUNT];

            int node = 0;
            for (int i = 0; i < 1; i++, node++)
            {
                box[node].Min = Vector3.Zero;
                box[node].Max = Vector3.One;
            }
            for (int i = 0; i < 8; i++, node++)
            {
                if ((i & 4) == 0) { box[node].Min.Z = 0; box[node].Max.Z = CHILD_SIZE; }
                else { box[node].Min.Z = 1 - CHILD_SIZE; box[node].Max.Z = 1; }
                if ((i & 2) == 0) { box[node].Min.Y = 0; box[node].Max.Y = CHILD_SIZE; }
                else { box[node].Min.Y = 1 - CHILD_SIZE; box[node].Max.Y = 1; }
                if ((i & 1) == 0) { box[node].Min.X = 0; box[node].Max.X = CHILD_SIZE; }
                else { box[node].Min.X = 1 - CHILD_SIZE; box[node].Max.X = 1; }
            }
            for (int i = 0; i < 8 * 8; i++, node++)
            {
                if ((i & 32) == 0) { box[node].Min.Z = 0; box[node].Max.Z = CHILD_SIZE; }
                else { box[node].Min.Z = 1 - CHILD_SIZE; box[node].Max.Z = 1; }
                if ((i & 16) == 0) { box[node].Min.Y = 0; box[node].Max.Y = CHILD_SIZE; }
                else { box[node].Min.Y = 1 - CHILD_SIZE; box[node].Max.Y = 1; }
                if ((i & 8) == 0) { box[node].Min.X = 0; box[node].Max.X = CHILD_SIZE; }
                else { box[node].Min.X = 1 - CHILD_SIZE; box[node].Max.X = 1; }
                if ((i & 4) == 0) { box[node].Max.Z = box[node].Min.Z + (box[node].Max.Z - box[node].Min.Z) * CHILD_SIZE; }
                else { box[node].Min.Z = box[node].Min.Z + (box[node].Max.Z - box[node].Min.Z) * (1 - CHILD_SIZE); }
                if ((i & 2) == 0) { box[node].Max.Y = box[node].Min.Y + (box[node].Max.Y - box[node].Min.Y) * CHILD_SIZE; }
                else { box[node].Min.Y = box[node].Min.Y + (box[node].Max.Y - box[node].Min.Y) * (1 - CHILD_SIZE); }
                if ((i & 1) == 0) { box[node].Max.X = box[node].Min.X + (box[node].Max.X - box[node].Min.X) * CHILD_SIZE; }
                else { box[node].Min.X = box[node].Min.X + (box[node].Max.X - box[node].Min.X) * (1 - CHILD_SIZE); }
            }
        }

        #endregion

        #region Node corresponding to a bounding box

        /// <summary>
        /// Get the node id this triangle belongs to.
        /// </summary>
        private int GetNode(ref BoundingBox triangleAabb)
        {
            BoundingBox transformed = new BoundingBox((triangleAabb.Min - m_bbMin) * m_bbInvScale, (triangleAabb.Max - m_bbMin) * m_bbInvScale);

            int node = 0;
            for (int level = 0; level < 2; level++)
            {
                int next = node * 8 + 1;  // node in lower level with x=y=z=0

                if (transformed.Min.X > box[next + 1].Min.X) next += 1;  // x++
                else if (transformed.Max.X >= box[next].Max.X) break;  // overlaps both x children

                if (transformed.Min.Y > box[next + 2].Min.Y) next += 2;  // y++
                else if (transformed.Max.Y >= box[next].Max.Y) break;  // overlaps both y children

                if (transformed.Min.Z > box[next + 4].Min.Z) next += 4;  // z++
                else if (transformed.Max.Z >= box[next].Max.Z) break;  // overlaps both z children

                node = next;
            }
            return node;
        }

        #endregion
    }
}
