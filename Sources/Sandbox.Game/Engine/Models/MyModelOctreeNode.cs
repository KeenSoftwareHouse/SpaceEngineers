using System.Collections.Generic;
using System.Diagnostics;
using VRageMath;
using Sandbox.Game.Entities;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Physics;
using VRage.Utils;

using System;
using Sandbox.Graphics;
using Sandbox.Common;
using VRageRender;
using VRage;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRage.Game.Components;
using VRage.Game.Models;

namespace Sandbox.Engine.Models
{
    //  This class represents one octree node
    class MyModelOctreeNode
    {
        const int OCTREE_CHILDS_COUNT = 8;                      //  Each node has eight childs. This is the esence of octree, so don't change it.
        const int MAX_RECURSIVE_LEVEL = 8;                      //  How deep in octree parent-child structure can we go.

        /// <summary>
        /// Expand child bounding boxex, expand by 30% on one side of each axis (child BB never get out of parents BB)
        /// </summary>
        const float CHILD_BOUNDING_BOX_EXPAND = 0.3f;

        List<MyModelOctreeNode> m_childs;                           //  Reference to eight childs of this node
        BoundingBox m_boundingBox;                              //  Node bounding box
        BoundingBox m_realBoundingBox;
        List<int> m_triangleIndices;                            //  List of triangles in this node (this are triangleVertexes indices, not vertex indices)
        //byte m_currentLevel;                                    //  List level

        //  We don't support default constructor for this class
        private MyModelOctreeNode() { }

        public MyModelOctreeNode(BoundingBox boundingBox) 
        {
            //  Here we just allocate list of child nodes so during building octree it always has 8 childs. But after OptimizeChilds() it can be less.
            m_childs = new List<MyModelOctreeNode>(OCTREE_CHILDS_COUNT);
            for (int i = 0; i < OCTREE_CHILDS_COUNT; i++)
            {
                m_childs.Add(null);
            }

            m_boundingBox = boundingBox;
            m_realBoundingBox = BoundingBox.CreateInvalid();
            m_triangleIndices = new List<int>();            
        }

        //  This method will look if node has all its childs null, and if yes, destroy childs array (saving memory + making traversal faster, because we don't need to traverse whole array)
        public void OptimizeChilds()
        {
            // Current bounding box is real bounding box (calculated as bounding box of children and triangles)
            m_boundingBox = m_realBoundingBox;

            for (int i = 0; i < m_childs.Count; i++)
            {
                if (m_childs[i] != null)
                {
                    m_childs[i].OptimizeChilds();
                }
            }

            //  Remove all childs that are null, thus empty for us
            while (m_childs.Remove(null) == true)
            {
            }

            // When has only one child
            while (m_childs != null && m_childs.Count == 1)
            {
                // Add child triangles to self
                foreach (var t in m_childs[0].m_triangleIndices)
                {
                    m_triangleIndices.Add(t);
                }

                // Replace current child with children of current child
                m_childs = m_childs[0].m_childs;
            }

            //  If all childs are empty, we set this list to null so we save few value
            if (m_childs != null && m_childs.Count == 0) m_childs = null;
        }

        //  Difference between GetIntersectionWithLine and GetIntersectionWithLineRecursive is that the later doesn't calculate
        //  final result, but is better suited for recursive nature of octree. Don't call GetIntersectionWithLineRecursive() from
        //  the outisde of this class, it's private method.
        public VRage.Game.Models.MyIntersectionResultLineTriangleEx? GetIntersectionWithLine(IMyEntity physObject, MyModel model, ref LineD line, double? minDistanceUntilNow, IntersectionFlags flags)
        {
            VRage.Game.Models.MyIntersectionResultLineTriangle? foundTriangle = GetIntersectionWithLineRecursive(model, ref line, minDistanceUntilNow);

            if (foundTriangle != null)
            {
                return new VRage.Game.Models.MyIntersectionResultLineTriangleEx(foundTriangle.Value, physObject, ref line);
            }
            else
            {
                return null;
            }
        }

        //  Finds intersection between line and model, using octree for speedup the lookup.
        //  Another speedup is, that first we check triangles that are directly in the node and then start
        //  checking node's childs. But only if child node instersection is less than last know min distance.
        VRage.Game.Models.MyIntersectionResultLineTriangle? GetIntersectionWithLineRecursive(MyModel model, ref LineD line, double? minDistanceUntilNow)
        {
            //  Check if line intersects bounding box of this node and if distance to bounding box is less then last know min distance
            Line lineF = (Line)line;
            double? distanceToBoundingBox = MyUtils.GetLineBoundingBoxIntersection(ref lineF, ref m_boundingBox);
            if ((distanceToBoundingBox.HasValue == false) || ((minDistanceUntilNow != null) && (minDistanceUntilNow < distanceToBoundingBox.Value))) return null;

            //  Triangles that are directly in this node
            VRage.Game.Models.MyIntersectionResultLineTriangle? foundIntersection = null;

            // temporary variable for storing tirngle boundingbox info
            BoundingBox triangleBoundingBox = new BoundingBox();
            BoundingBox lineBB = BoundingBox.CreateInvalid();
            lineBB = lineBB.Include(line.From);
            lineBB = lineBB.Include(line.To);

            for (int i = 0; i < m_triangleIndices.Count; i++)
            {
                int triangleIndex = m_triangleIndices[i];

                model.GetTriangleBoundingBox(triangleIndex, ref triangleBoundingBox);

                //  First test intersection of triangleVertexes's bounding box with line's bounding box. And only if they overlap or intersect, do further intersection tests.
                if (triangleBoundingBox.Intersects(ref lineBB))
                {
                    //  See that we swaped vertex indices!!
                    MyTriangle_Vertices triangle;
                    MyTriangleVertexIndices triangleIndices = model.Triangles[triangleIndex];
                    triangle.Vertex0 = model.GetVertex(triangleIndices.I0);
                    triangle.Vertex1 = model.GetVertex(triangleIndices.I2);
                    triangle.Vertex2 = model.GetVertex(triangleIndices.I1);

                    double? distance = MyUtils.GetLineTriangleIntersection(ref lineF, ref triangle);

                    //  If intersection occured and if distance to intersection is closer to origin than any previous intersection
                    if ((distance != null) && ((foundIntersection == null) || (distance.Value < foundIntersection.Value.Distance)))
                    {
                        Vector3 calculatedTriangleNormal = MyUtils.GetNormalVectorFromTriangle(ref triangle);

                        //  We need to remember original triangleVertexes coordinates (not transformed by world matrix)
                        foundIntersection = new VRage.Game.Models.MyIntersectionResultLineTriangle(triangleIndex, ref triangle, ref calculatedTriangleNormal, distance.Value);
                    }
                }
            }

            //  Get intersection with childs of this node
            if (m_childs != null)
            {
                for (int i = 0; i < m_childs.Count; i++)
                {
                    VRage.Game.Models.MyIntersectionResultLineTriangle? childIntersection = m_childs[i].GetIntersectionWithLineRecursive(model, ref line,
                        (foundIntersection == null) ? (double?)null : foundIntersection.Value.Distance);

                    //  If intersection occured and if distance to intersection is closer to origin than any previous intersection
                    foundIntersection = VRage.Game.Models.MyIntersectionResultLineTriangle.GetCloserIntersection(ref foundIntersection, ref childIntersection);
                }
            }

            return foundIntersection;
        }

        //  Return list of triangles intersecting specified sphere. Angle between every triangleVertexes normal vector and 'referenceNormalVector' 
        //  is calculated, and if more than 'maxAngle', we ignore such triangleVertexes.
        //  Triangles are returned in 'retTriangles', and this list must be preallocated!
        //  IMPORTANT: Sphere must be in model space, so don't transform it!
        public void GetTrianglesIntersectingSphere(MyModel model, ref BoundingSphereD sphere, Vector3? referenceNormalVector, float? maxAngle, List<MyTriangle_Vertex_Normal> retTriangles, int maxNeighbourTriangles)
        {
            BoundingSphere sphereF = (BoundingSphere)sphere;
            //  Check if sphere intersects bounding box of this node
            //if (m_boundingBox.Contains(sphere) == ContainmentType.Disjoint) return;
            if (m_boundingBox.Intersects(ref sphere) == false) return;

            // temporary variable for storing tirngle boundingbox info
            BoundingBox triangleBoundingBox = new BoundingBox();

            //  Triangles that are directly in this node
            for (int i = 0; i < m_triangleIndices.Count; i++)
            {
                //  If we reached end of the buffer of neighbour triangles, we stop adding new ones. This is better behavior than throwing exception because of array overflow.
                if (retTriangles.Count == maxNeighbourTriangles) return;

                int triangleIndex = m_triangleIndices[i];

                model.GetTriangleBoundingBox(triangleIndex, ref triangleBoundingBox);

                //  First test intersection of triangleVertexes's bounding box with bounding sphere. And only if they overlap or intersect, do further intersection tests.
                if (triangleBoundingBox.Intersects(ref sphere))
                {
                    //if (m_triangleIndices[value] != ignoreTriangleWithIndex)
                    {
                        //  See that we swaped vertex indices!!
                        MyTriangle_Vertices triangle;

                        MyTriangleVertexIndices triangleIndices = model.Triangles[triangleIndex];
                        triangle.Vertex0 = model.GetVertex(triangleIndices.I0);
                        triangle.Vertex1 = model.GetVertex(triangleIndices.I2);
                        triangle.Vertex2 = model.GetVertex(triangleIndices.I1);
                        Vector3 calculatedTriangleNormal = MyUtils.GetNormalVectorFromTriangle(ref triangle);

                        PlaneD trianglePlane = new PlaneD(triangle.Vertex0, triangle.Vertex1, triangle.Vertex2);

                        if (MyUtils.GetSphereTriangleIntersection(ref sphere, ref trianglePlane, ref triangle) != null)
                        {
                            Vector3 triangleNormal = MyUtils.GetNormalVectorFromTriangle(ref triangle);

                            if ((referenceNormalVector.HasValue == false) || (maxAngle.HasValue == false) ||
                                ((MyUtils.GetAngleBetweenVectors(referenceNormalVector.Value, triangleNormal) <= maxAngle)))
                            {
                                MyTriangle_Vertex_Normal retTriangle;
                                retTriangle.Vertexes = triangle;
                                retTriangle.Normal = calculatedTriangleNormal;

                                retTriangles.Add(retTriangle);
                            }
                        }
                    }
                }
            }

            //  Get intersection with childs of this node
            if (m_childs != null)
            {
                for (int i = 0; i < m_childs.Count; i++)
                {
                    //m_childs[value].GetTrianglesIntersectingSphere(physObject, ref sphere, referenceNormalVector, maxAngle, ignoreTriangleWithIndex, retTriangles, maxNeighbourTriangles);
                    m_childs[i].GetTrianglesIntersectingSphere(model, ref sphere, referenceNormalVector, maxAngle, retTriangles, maxNeighbourTriangles);
                }
            }
        }

        public void GetTrianglesIntersectingSphere(MyModel model, ref BoundingSphereD sphere, Vector3? referenceNormalVector, float? maxAngle, List<MyTriangle_Vertex_Normals> retTriangles, int maxNeighbourTriangles)
        {
            //  Check if sphere intersects bounding box of this node
            //if (m_boundingBox.Contains(sphere) == ContainmentType.Disjoint) return;
            if (m_boundingBox.Intersects(ref sphere) == false) return;

            // temporary variable for storing tirngle boundingbox info
            BoundingBox triangleBoundingBox = new BoundingBox();

            //  Triangles that are directly in this node
            for (int i = 0; i < m_triangleIndices.Count; i++)
            {
                //  If we reached end of the buffer of neighbour triangles, we stop adding new ones. This is better behavior than throwing exception because of array overflow.
                if (retTriangles.Count == maxNeighbourTriangles) return;

                int triangleIndex = m_triangleIndices[i];

                model.GetTriangleBoundingBox(triangleIndex, ref triangleBoundingBox);

                //  First test intersection of triangleVertexes's bounding box with bounding sphere. And only if they overlap or intersect, do further intersection tests.
                if (triangleBoundingBox.Intersects(ref sphere))
                {
                    //if (m_triangleIndices[value] != ignoreTriangleWithIndex)
                    {
                        //  See that we swaped vertex indices!!
                        MyTriangle_Vertices triangle;
                        MyTriangle_Normals triangleNormals;
                        //MyTriangle_Normals triangleTangents;

                        MyTriangleVertexIndices triangleIndices = model.Triangles[triangleIndex];
                        triangle.Vertex0 = model.GetVertex(triangleIndices.I0);
                        triangle.Vertex1 = model.GetVertex(triangleIndices.I2);
                        triangle.Vertex2 = model.GetVertex(triangleIndices.I1);
                        triangleNormals.Normal0 = model.GetVertexNormal(triangleIndices.I0);
                        triangleNormals.Normal1 = model.GetVertexNormal(triangleIndices.I2);
                        triangleNormals.Normal2 = model.GetVertexNormal(triangleIndices.I1);
                        /*
                        triangleTangents.Normal0 = model.GetVertexTangent(triangleIndices.I0);
                        triangleTangents.Normal1 = model.GetVertexTangent(triangleIndices.I2);
                        triangleTangents.Normal2 = model.GetVertexTangent(triangleIndices.I1);
                        */
                        PlaneD trianglePlane = new PlaneD(triangle.Vertex0, triangle.Vertex1, triangle.Vertex2);

                        if (MyUtils.GetSphereTriangleIntersection(ref sphere, ref trianglePlane, ref triangle) != null)
                        {
                            Vector3 triangleNormal = MyUtils.GetNormalVectorFromTriangle(ref triangle);

                            if ((referenceNormalVector.HasValue == false) || (maxAngle.HasValue == false) ||
                                ((MyUtils.GetAngleBetweenVectors(referenceNormalVector.Value, triangleNormal) <= maxAngle)))
                            {
                                MyTriangle_Vertex_Normals retTriangle;
                                retTriangle.Vertices = triangle;
                                retTriangle.Normals = triangleNormals;
                          //      retTriangle.Tangents = triangleTangents;

                                retTriangles.Add(retTriangle);
                            }
                        }
                    }
                }
            }

            //  Get intersection with childs of this node
            if (m_childs != null)
            {
                for (int i = 0; i < m_childs.Count; i++)
                {
                    //m_childs[value].GetTrianglesIntersectingSphere(physObject, ref sphere, referenceNormalVector, maxAngle, ignoreTriangleWithIndex, retTriangles, maxNeighbourTriangles);
                    m_childs[i].GetTrianglesIntersectingSphere(model, ref sphere, referenceNormalVector, maxAngle, retTriangles, maxNeighbourTriangles);
                }
            }
        }

        //  Return true if object intersects specified sphere.
        //  This method doesn't return exact point of intersection or any additional data.
        //  We don't look for closest intersection - so we stop on first intersection found.
        //  IMPORTANT: Sphere must be in model space, so don't transform it!
        public bool GetIntersectionWithSphere(MyModel model, ref BoundingSphereD sphere)
        {
            //  Check if sphere intersects bounding box of this node
            if (m_boundingBox.Intersects(ref sphere) == false)
            {
                return false;         
            }

            // temporary variable for storing tirngle boundingbox info
            BoundingBox triangleBoundingBox = new BoundingBox();

            //  Triangles that are directly in this node
            for (int i = 0; i < m_triangleIndices.Count; i++)
            {
                int triangleIndex = m_triangleIndices[i];

                model.GetTriangleBoundingBox(triangleIndex, ref triangleBoundingBox);

                //  First test intersection of triangleVertexes's bounding box with bounding sphere. And only if they overlap or intersect, do further intersection tests.
                if (triangleBoundingBox.Intersects(ref sphere))
                {
                    //  See that we swaped vertex indices!!
                    MyTriangle_Vertices triangle;
                    MyTriangleVertexIndices triangleIndices = model.Triangles[triangleIndex];
                    triangle.Vertex0 = model.GetVertex(triangleIndices.I0);
                    triangle.Vertex1 = model.GetVertex(triangleIndices.I2);
                    triangle.Vertex2 = model.GetVertex(triangleIndices.I1);

                    PlaneD trianglePlane = new PlaneD(triangle.Vertex0, triangle.Vertex1, triangle.Vertex2);

                    if (MyUtils.GetSphereTriangleIntersection(ref sphere, ref trianglePlane, ref triangle) != null)
                    {
                        //  If we found intersection we can stop and dont need to look further
                        return true;
                    }                    
                }
            }

            //  Get intersection with childs of this node
            if (m_childs != null)
            {
                for (int i = 0; i < m_childs.Count; i++)
                {
                    if (m_childs[i].GetIntersectionWithSphere(model, ref sphere))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        //  Add triangleVertexes into this node or its child or child's child...
        public void AddTriangle(MyModel model, int triangleIndex, int recursiveLevel)
        {
            BoundingBox triangleBoundingBox = new BoundingBox();
            model.GetTriangleBoundingBox(triangleIndex, ref triangleBoundingBox);

            if (recursiveLevel != MAX_RECURSIVE_LEVEL)
            {
                //  If we didn't reach max recursive level, we look for child where triangleVertexes can be completely contained
                for (int i = 0; i < OCTREE_CHILDS_COUNT; i++)
                {
                    BoundingBox childBoundingBox = GetChildBoundingBox(m_boundingBox, i);

                    //  If child completely contains the triangleVertexes, we add it to this child (or its child...child).
                    if (childBoundingBox.Contains(triangleBoundingBox) == ContainmentType.Contains)
                    {
                        if (m_childs[i] == null) m_childs[i] = new MyModelOctreeNode(childBoundingBox);

                        m_childs[i].AddTriangle(model, triangleIndex, recursiveLevel + 1);

                        // Child completely contains triangle, so also current bounding box must contain that triangle
                        m_realBoundingBox = m_realBoundingBox.Include(ref triangleBoundingBox.Min);
                        m_realBoundingBox = m_realBoundingBox.Include(ref triangleBoundingBox.Max);
                        return;
                    }
                }
            }

            //  If we get here, it was because we reached max recursive level or no child completely contained the triangleVertexes, so we add triangleVertexes to this node
            m_triangleIndices.Add(triangleIndex);
            m_realBoundingBox = m_realBoundingBox.Include(ref triangleBoundingBox.Min);
            m_realBoundingBox = m_realBoundingBox.Include(ref triangleBoundingBox.Max);
        }

        //private static Stack<MyModelOctreeNode> m_nodeStack = new Stack<MyModelOctreeNode>(8*8*8*8*8*8*8*8);        

        ////  Add triangleVertexes into this node or its child or child's child...
        //public void AddTriangle(MyModel model, int triangleIndex, int recursiveLevel)
        //{
        //    BoundingBox triangleBoundingBox;
        //    triangleBoundingBox = new BoundingBox();
        //    model.GetTriangleBoundingBox(triangleIndex, ref triangleBoundingBox);
        //    MyModelOctreeNode currentNode = null;        
        //    m_nodeStack.Clear();
        //    m_nodeStack.Push(this);                                
        //    bool currentContains = false;

        //    while(m_nodeStack.Count > 0)
        //    {                                
        //        currentContains = false;
        //        currentNode = m_nodeStack.Pop();                                                                
                
        //        if (currentNode.m_currentLevel != MAX_RECURSIVE_LEVEL)
        //        {                    
        //            //  If we didn't reach max recursive level, we look for child where triangleVertexes can be completely contained
        //            for (int i = 0; i < OCTREE_CHILDS_COUNT; i++)
        //            {
        //                BoundingBox childBoundingBox = currentNode.GetChildBoundingBox(currentNode.m_boundingBox, i);

        //                //  If child completely contains the triangleVertexes, we add it to this child (or its child...child).
        //                if (childBoundingBox.Contains(triangleBoundingBox) == ContainmentType.Contains)
        //                {
        //                    if (currentNode.m_childs[i] == null) currentNode.m_childs[i] = new MyModelOctreeNode(childBoundingBox, (byte)(currentNode.m_currentLevel + 1));
                            
        //                    Debug.Assert(currentNode.m_childs[i] != null);
        //                    m_nodeStack.Push(currentNode.m_childs[i]);                            

        //                    // Child completely contains triangle, so also current bounding box must contain that triangle
        //                    currentNode.m_realBoundingBox = currentNode.m_realBoundingBox.Include(ref triangleBoundingBox.Min);
        //                    currentNode.m_realBoundingBox = currentNode.m_realBoundingBox.Include(ref triangleBoundingBox.Max);
        //                    currentContains = true;
        //                    break;
        //                }
        //            }
        //        }                
                
        //        if (!currentContains)
        //        {
        //            currentNode.m_triangleIndices.Add(triangleIndex);
        //            currentNode.m_realBoundingBox = currentNode.m_realBoundingBox.Include(ref triangleBoundingBox.Min);
        //            currentNode.m_realBoundingBox = currentNode.m_realBoundingBox.Include(ref triangleBoundingBox.Max);
        //        }                                
        //    }                        
        //}

        //  Calculate min/max coordinates of a child's bounding box
        BoundingBox GetChildBoundingBox(BoundingBox parentBoundingBox, int childIndex)
        {
            //  Get child offset
            Vector3 offset;
            switch (childIndex)
            {
                case 0: offset = new Vector3(0, 0, 0); break;
                case 1: offset = new Vector3(1, 0, 0); break;
                case 2: offset = new Vector3(1, 0, 1); break;
                case 3: offset = new Vector3(0, 0, 1); break;
                case 4: offset = new Vector3(0, 1, 0); break;
                case 5: offset = new Vector3(1, 1, 0); break;
                case 6: offset = new Vector3(1, 1, 1); break;
                case 7: offset = new Vector3(0, 1, 1); break;
                default: throw new InvalidBranchException(); break;
            }

            //  Calc size of child's bounding box
            Vector3 size = (parentBoundingBox.Max - parentBoundingBox.Min) / 2.0f;

            BoundingBox ret = new BoundingBox();
            ret.Min = parentBoundingBox.Min + offset * size;
            ret.Max = ret.Min + size;

            ret.Min -= size * CHILD_BOUNDING_BOX_EXPAND;
            ret.Max += size * CHILD_BOUNDING_BOX_EXPAND;

            // Make sure we stay in parent bounding box
            ret.Min = Vector3.Max(ret.Min, parentBoundingBox.Min);
            ret.Max = Vector3.Min(ret.Max, parentBoundingBox.Max);
            
            return ret;
        }
    }
}
