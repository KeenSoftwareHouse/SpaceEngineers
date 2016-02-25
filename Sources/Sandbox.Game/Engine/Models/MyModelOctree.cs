using System.Collections.Generic;
using VRageMath;
using Sandbox.Game.Entities;
using Sandbox.Engine.Utils;


using Sandbox.Engine.Physics;
using Sandbox.Graphics;
using Sandbox.Common;
using VRage;
using Sandbox.ModAPI;
using VRage.Utils;
using VRage.ModAPI;
using VRage.Game.Components;
using VRage.Game.Models;
//  This octreee can be used with MyModel to optimize line intersections.
//  The idea of octree is that we have root node that has eight child nodes (by splitting the parent to eight boxes).
//  When building the octree, we check if triangleVertexes can fully fit inside of any child. If yes, we add it to that
//  child, but we also check if we can't fit the triangleVertexes into child's child...etc.
//  So some triangles (especially large) will be in root node, some in child nodes, some child nodes will be empty, etc.

namespace Sandbox.Engine.Models
{
    //  This class represents whole octree structure (it is the root you can use to access content of a octree)
    class MyModelOctree : IMyTriangePruningStructure
    {
        MyModel m_model;
        MyModelOctreeNode m_rootNode;

        //  We don't support default constructor for this class
        private MyModelOctree() { }

        //  Use this constructor to build the octree
        public MyModelOctree(MyModel model)
        {
            // we can't use performance timer, because octree now loaded in parallel tasks
            //MyPerformanceTimer.OctreeBuilding.Start();

            m_model = model;

            //  Bounding box for the root node - get it from model (we need to trust the model that it will be good)
            m_rootNode = new MyModelOctreeNode(model.BoundingBox);            

            //  Add model triangles into octree
            for (int i = 0; i < m_model.Triangles.Length; i++)
            {
                //  Add triangleVertexes to octree
                m_rootNode.AddTriangle(model, i, 0);
            }

            //  This method will look if node has all its childs null, and if yes, destroy childs array (saving memory + making traversal faster, because we don't need to traverse whole array)
            m_rootNode.OptimizeChilds();

            // we can't use performance timer, because octree now loaded in parallel tasks
            //MyPerformanceTimer.OctreeBuilding.End();
        }

        //  Calculates intersection of line with any triangleVertexes in this model. Closest intersection and intersected triangleVertexes will be returned.
        //  This method is fast, it uses octree.
        public VRage.Game.Models.MyIntersectionResultLineTriangleEx? GetIntersectionWithLine(IMyEntity physObject, ref LineD line, IntersectionFlags flags)
        {
            BoundingSphereD vol = physObject.WorldVolume;
            //  Check if line intersects phys object's current bounding sphere, and if not, return 'no intersection'
            if (MyUtils.IsLineIntersectingBoundingSphere(ref line, ref vol) == false) return null;

            //  Transform line into 'model instance' local/object space. Bounding box of a line is needed!!
            MatrixD worldInv = physObject.GetWorldMatrixNormalizedInv();

            return GetIntersectionWithLine(physObject, ref line, ref worldInv, flags);
        }

        public VRage.Game.Models.MyIntersectionResultLineTriangleEx? GetIntersectionWithLine(IMyEntity physObject, ref LineD line, ref MatrixD customInvMatrix, IntersectionFlags flags)
        {
            LineD lineInModelSpace = new LineD(Vector3D.Transform(line.From, ref customInvMatrix), Vector3D.Transform(line.To, ref customInvMatrix));

            VRage.Game.Models.MyIntersectionResultLineTriangleEx? ret = m_rootNode.GetIntersectionWithLine(physObject, m_model, ref lineInModelSpace, null, flags);

            return ret;
        }

        public void GetTrianglesIntersectingLine(IMyEntity entity, ref LineD line, IntersectionFlags flags, List<VRage.Game.Models.MyIntersectionResultLineTriangleEx> result)
        {
            MatrixD worldInv = entity.GetWorldMatrixNormalizedInv();
            GetTrianglesIntersectingLine(entity, ref line, ref worldInv, flags, result);
        }

        public void GetTrianglesIntersectingLine(IMyEntity entity, ref LineD line, ref MatrixD customInvMatrix, IntersectionFlags flags, List<MyIntersectionResultLineTriangleEx> result)
        {
            System.Diagnostics.Debug.Assert(false, "Not implemented");
        }

        //  Return list of triangles intersecting specified sphere. Angle between every triangleVertexes normal vector and 'referenceNormalVector'
        //  is calculated, and if more than 'maxAngle', we ignore such triangleVertexes.
        //  Triangles are returned in 'retTriangles', and this list must be preallocated!
        //  IMPORTANT: Sphere must be in model space, so don't transform it!
        public void GetTrianglesIntersectingSphere(ref BoundingSphereD sphere, Vector3? referenceNormalVector, float? maxAngle, List<MyTriangle_Vertex_Normals> retTriangles, int maxNeighbourTriangles)
        {
            m_rootNode.GetTrianglesIntersectingSphere(m_model, ref sphere, referenceNormalVector, maxAngle, retTriangles, maxNeighbourTriangles);
        }

        //  Return true if object intersects specified sphere.
        //  This method doesn't return exact point of intersection or any additional data.
        //  We don't look for closest intersection - so we stop on first intersection found.
        public bool GetIntersectionWithSphere(IMyEntity physObject, ref BoundingSphereD sphere)
        {
            //  Transform sphere from world space to object space
            MatrixD worldInv = physObject.GetWorldMatrixNormalizedInv();
            Vector3D positionInObjectSpace = Vector3D.Transform(sphere.Center, ref worldInv);
            BoundingSphereD sphereInObjectSpace = new BoundingSphereD(positionInObjectSpace, sphere.Radius);
            
            return m_rootNode.GetIntersectionWithSphere(m_model, ref sphereInObjectSpace);
        }

        //  Return list of triangles intersecting specified sphere. Angle between every triangleVertexes normal vector and 'referenceNormalVector'
        //  is calculated, and if more than 'maxAngle', we ignore such triangleVertexes.
        //  Triangles are returned in 'retTriangles', and this list must be preallocated!
        //  IMPORTANT: Sphere must be in model space, so don't transform it!
        public void GetTrianglesIntersectingSphere(ref BoundingSphereD sphere, List<MyTriangle_Vertex_Normal> retTriangles, int maxNeighbourTriangles)
        {
            m_rootNode.GetTrianglesIntersectingSphere(m_model, ref sphere, null, null, retTriangles, maxNeighbourTriangles);
        }

        public void GetTrianglesIntersectingAABB(ref BoundingBoxD box, List<MyTriangle_Vertex_Normal> retTriangles, int maxNeighbourTriangles)
        {
            System.Diagnostics.Debug.Assert(false, "Not implemented");
            //m_rootNode.ge .GetTrianglesIntersectingSphere(m_model, ref box, null, null, retTriangles, maxNeighbourTriangles);
        }

        public void Close()
        {
        }

        public int Size
        {
            get
            {
                System.Diagnostics.Debug.Assert(false, "Not implemented");
                return 0;
            }
        }

    }
}
