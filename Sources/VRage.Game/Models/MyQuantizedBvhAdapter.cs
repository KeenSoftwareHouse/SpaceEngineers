using System;
using System.Collections.Generic;
using VRageMath;
using BulletXNA.BulletCollision;
using BulletXNA.LinearMath;
using VRageRender;

namespace VRage.Game.Models
{
    using VRage.Utils;
    using VRage.ModAPI;
    using VRage.Game.Components;

    public static class BulletXnaExtensions
    {
        public static IndexedVector3 ToBullet(this Vector3 v)
        {
            return new IndexedVector3(v.X, v.Y, v.Z);
        }

        public static IndexedVector3 ToBullet(this Vector3D v)
        {
            return new IndexedVector3((float)v.X, (float)v.Y, (float)v.Z);
        }

        public static Vector3 FromBullet(this IndexedVector3 v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }
    }

    public class MyQuantizedBvhAdapter : IMyTriangePruningStructure
    {
        GImpactQuantizedBvh m_bvh;
        MyModel m_model;

        [ThreadStatic]
        static ObjectArray<int> m_overlappedTrianglesThreadStatic;
        static List<ObjectArray<int>> m_overlappedTrianglesThreadStaticCollection = new List<ObjectArray<int>>();

        [ThreadStatic]
        static MyQuantizedBvhResult m_resultThreadStatic;
        [ThreadStatic]
        static MyQuantizedBvhAllTrianglesResult m_resultAllThreadStatic;
        

        private static MyQuantizedBvhResult m_result
        {
            get
            {
                if (m_resultThreadStatic == null)
                {
                    m_resultThreadStatic = new MyQuantizedBvhResult();
                }
                return m_resultThreadStatic;
            }
        }

        private static MyQuantizedBvhAllTrianglesResult m_resultAll
        {
            get
            {
                if (m_resultAllThreadStatic == null)
                {
                    m_resultAllThreadStatic = new MyQuantizedBvhAllTrianglesResult();
                }
                return m_resultAllThreadStatic;
            }
        }

        private static ObjectArray<int> m_overlappedTriangles
        {
            get
            {
                if (m_overlappedTrianglesThreadStatic == null)
                {
                    m_overlappedTrianglesThreadStatic = new ObjectArray<int>(1024);
                    lock (m_overlappedTrianglesThreadStaticCollection)
                    {
                        m_overlappedTrianglesThreadStaticCollection.Add(m_overlappedTrianglesThreadStatic);
                    }
                }
                return m_overlappedTrianglesThreadStatic;
            }
        }

        public MyQuantizedBvhAdapter(GImpactQuantizedBvh bvh, MyModel model)
        {
            m_bvh = bvh;
            m_model = model;
        }

        public VRage.Game.Models.MyIntersectionResultLineTriangleEx? GetIntersectionWithLine(IMyEntity entity, ref LineD line, IntersectionFlags flags)
        {
            BoundingSphereD vol = entity.PositionComp.WorldVolume;
            //  Check if line intersects phys object's current bounding sphere, and if not, return 'no intersection'
            if (MyUtils.IsLineIntersectingBoundingSphere(ref line, ref vol) == false) return null;

            //  Transform line into 'model instance' local/object space. Bounding box of a line is needed!!
            MatrixD worldInv = entity.PositionComp.WorldMatrixInvScaled;

            return GetIntersectionWithLine(entity, ref line, ref worldInv, flags);
        }

        public VRage.Game.Models.MyIntersectionResultLineTriangleEx? GetIntersectionWithLine(IMyEntity entity, ref LineD line, ref MatrixD customInvMatrix, IntersectionFlags flags)
        {
            LineD lineInModelSpace = new LineD(Vector3D.Transform(line.From, ref customInvMatrix), Vector3D.Transform(line.To, ref customInvMatrix));

            //MyIntersectionResultLineTriangle? result = null;

            m_result.Start(m_model, lineInModelSpace, flags);

            var dir = lineInModelSpace.Direction.ToBullet();
            var from = lineInModelSpace.From.ToBullet();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("m_bvh.RayQueryClosest()");
            m_bvh.RayQueryClosest(ref dir, ref from, m_result.ProcessTriangleHandler);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            if (m_result.Result.HasValue)
            {
                return new VRage.Game.Models.MyIntersectionResultLineTriangleEx(m_result.Result.Value, entity, ref lineInModelSpace);
            }
            else
            {
                return null;
            }
        }

        public void GetTrianglesIntersectingSphere(ref BoundingSphereD sphere, Vector3? referenceNormalVector, float? maxAngle, List<MyTriangle_Vertex_Normals> retTriangles, int maxNeighbourTriangles)
        {
            var aabb = BoundingBox.CreateInvalid();
            BoundingSphere sphereF = (BoundingSphere)sphere;
            aabb.Include(ref sphereF);
            AABB gi_aabb = new AABB(aabb.Min.ToBullet(), aabb.Max.ToBullet());
            m_overlappedTriangles.Clear();
            if (m_bvh.BoxQuery(ref gi_aabb, m_overlappedTriangles))
            {
                // temporary variable for storing tirngle boundingbox info
                BoundingBox triangleBoundingBox = new BoundingBox();

                for (int i = 0; i < m_overlappedTriangles.Count; i++)
                {
                    var triangleIndex = m_overlappedTriangles[i];

                    //  If we reached end of the buffer of neighbour triangles, we stop adding new ones. This is better behavior than throwing exception because of array overflow.
                    if (retTriangles.Count == maxNeighbourTriangles) return;

                    m_model.GetTriangleBoundingBox(triangleIndex, ref triangleBoundingBox);

                    //  First test intersection of triangleVertexes's bounding box with bounding sphere. And only if they overlap or intersect, do further intersection tests.
                    if (triangleBoundingBox.Intersects(ref sphere))
                    {
                        //if (m_triangleIndices[value] != ignoreTriangleWithIndex)
                        {
                            //  See that we swaped vertex indices!!
                            MyTriangle_Vertices triangle;
                            MyTriangle_Normals triangleNormals;
                            //MyTriangle_Normals triangleTangents;

                            MyTriangleVertexIndices triangleIndices = m_model.Triangles[triangleIndex];
                            m_model.GetVertex(triangleIndices.I0, triangleIndices.I2, triangleIndices.I1, out triangle.Vertex0, out triangle.Vertex1, out triangle.Vertex2);

                            /*
                            triangle.Vertex0 = m_model.GetVertex(triangleIndices.I0);
                            triangle.Vertex1 = m_model.GetVertex(triangleIndices.I2);
                            triangle.Vertex2 = m_model.GetVertex(triangleIndices.I1);
                              */

                            triangleNormals.Normal0 = m_model.GetVertexNormal(triangleIndices.I0);
                            triangleNormals.Normal1 = m_model.GetVertexNormal(triangleIndices.I2);
                            triangleNormals.Normal2 = m_model.GetVertexNormal(triangleIndices.I1);

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

                                    retTriangles.Add(retTriangle);
                                }
                            }
                        }
                    }
                }
            }
        }

        public bool GetIntersectionWithSphere(IMyEntity entity, ref BoundingSphereD sphere)
        {
            //  Transform sphere from world space to object space
            MatrixD worldInv = entity.PositionComp.WorldMatrixNormalizedInv;
            Vector3 positionInObjectSpace = (Vector3)Vector3D.Transform(sphere.Center, ref worldInv);
            BoundingSphereD sphereInObjectSpace = new BoundingSphereD(positionInObjectSpace, (float)sphere.Radius);

            var aabb = BoundingBox.CreateInvalid();
            BoundingSphere sphereF = (BoundingSphere)sphereInObjectSpace;
            aabb.Include(ref sphereF);
            AABB gi_aabb = new AABB(aabb.Min.ToBullet(), aabb.Max.ToBullet());
            m_overlappedTriangles.Clear();
            if (m_bvh.BoxQuery(ref gi_aabb, m_overlappedTriangles))
            {
                // temporary variable for storing tirngle boundingbox info
                BoundingBox triangleBoundingBox = new BoundingBox();

                //  Triangles that are directly in this node
                for (int i = 0; i < m_overlappedTriangles.Count; i++)
                {
                    var triangleIndex = m_overlappedTriangles[i];

                    m_model.GetTriangleBoundingBox(triangleIndex, ref triangleBoundingBox);

                    //  First test intersection of triangleVertexes's bounding box with bounding sphere. And only if they overlap or intersect, do further intersection tests.
                    if (triangleBoundingBox.Intersects(ref sphereInObjectSpace))
                    {
                        //  See that we swaped vertex indices!!
                        MyTriangle_Vertices triangle;
                        MyTriangleVertexIndices triangleIndices = m_model.Triangles[triangleIndex];
                        triangle.Vertex0 = m_model.GetVertex(triangleIndices.I0);
                        triangle.Vertex1 = m_model.GetVertex(triangleIndices.I2);
                        triangle.Vertex2 = m_model.GetVertex(triangleIndices.I1);

                        PlaneD trianglePlane = new PlaneD(triangle.Vertex0, triangle.Vertex1, triangle.Vertex2);

                        if (MyUtils.GetSphereTriangleIntersection(ref sphereInObjectSpace, ref trianglePlane, ref triangle) != null)
                        {
                            //  If we found intersection we can stop and dont need to look further
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        //slow
        [Obsolete("Slow,use aabb")]
        public void GetTrianglesIntersectingSphere(ref BoundingSphereD sphere, List<MyTriangle_Vertex_Normal> retTriangles, int maxNeighbourTriangles)
        {
            Vector3? referenceNormalVector = null;
            float? maxAngle = null;

            var aabb = BoundingBox.CreateInvalid();
            BoundingSphere sphereF = (BoundingSphere)sphere;
            aabb.Include(ref sphereF);
            AABB gi_aabb = new AABB(aabb.Min.ToBullet(), aabb.Max.ToBullet());
            m_overlappedTriangles.Clear();

           // VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("m_bvh.BoxQuery"); // This code is called recursively and cause profiler to lag
            bool res = m_bvh.BoxQuery(ref gi_aabb, m_overlappedTriangles);
           // VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            if (res)
            {
                //VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("m_overlappedTriangles");  // This code is called recursively and cause profiler to lag

                // temporary variable for storing tirngle boundingbox info
                BoundingBox triangleBoundingBox = new BoundingBox();

                for (int i = 0; i < m_overlappedTriangles.Count; i++)
                {
                    var triangleIndex = m_overlappedTriangles[i];

                    //  If we reached end of the buffer of neighbour triangles, we stop adding new ones. This is better behavior than throwing exception because of array overflow.
                    if (retTriangles.Count == maxNeighbourTriangles) return;

                    m_model.GetTriangleBoundingBox(triangleIndex, ref triangleBoundingBox);
                    
                    //gi_aabb.CollideTriangleExact

                    //  First test intersection of triangleVertexes's bounding box with bounding sphere. And only if they overlap or intersect, do further intersection tests.
                    if (triangleBoundingBox.Intersects(ref sphere))
                    {
                        //if (m_triangleIndices[value] != ignoreTriangleWithIndex)
                        {
                            //  See that we swaped vertex indices!!
                            MyTriangle_Vertices triangle;

                            MyTriangleVertexIndices triangleIndices = m_model.Triangles[triangleIndex];
                            triangle.Vertex0 = m_model.GetVertex(triangleIndices.I0);
                            triangle.Vertex1 = m_model.GetVertex(triangleIndices.I2);
                            triangle.Vertex2 = m_model.GetVertex(triangleIndices.I1);
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

                //VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            }
        }

        public void GetTrianglesIntersectingLine(IMyEntity entity, ref LineD line, IntersectionFlags flags, List<MyIntersectionResultLineTriangleEx> result)
        {
            MatrixD worldInv = entity.PositionComp.WorldMatrixNormalizedInv;
            GetTrianglesIntersectingLine(entity, ref line, ref worldInv, flags, result);
        }

        public void GetTrianglesIntersectingLine(IMyEntity entity, ref LineD line, ref MatrixD customInvMatrix, IntersectionFlags flags, List<MyIntersectionResultLineTriangleEx> result)
        {
            LineD lineInModelSpace = new LineD(Vector3D.Transform(line.From, ref customInvMatrix), Vector3D.Transform(line.To, ref customInvMatrix));

            m_resultAll.Start(m_model, lineInModelSpace, flags);

            var dir = lineInModelSpace.Direction.ToBullet();
            var from = lineInModelSpace.From.ToBullet();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("m_bvh.RayQueryClosest()");
            m_bvh.RayQuery(ref dir, ref from, m_resultAll.ProcessTriangleHandler);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            m_resultAll.End();

            foreach (var intersection in m_resultAll.Result)
            {
                result.Add(new VRage.Game.Models.MyIntersectionResultLineTriangleEx(intersection, entity, ref lineInModelSpace));
            }
        }

        public void GetTrianglesIntersectingAABB(ref BoundingBoxD aabb, List<MyTriangle_Vertex_Normal> retTriangles, int maxNeighbourTriangles)  
        {
            BoundingBox boxF = (BoundingBox)aabb;
            IndexedVector3 min = boxF.Min.ToBullet();
            IndexedVector3 max = boxF.Max.ToBullet();
            AABB gi_aabb = new AABB(ref min, ref max);

            m_overlappedTriangles.Clear();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("m_bvh.BoxQuery");
            bool res = m_bvh.BoxQuery(ref gi_aabb, m_overlappedTriangles);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            if (res)
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("m_overlappedTriangles");

                //foreach (var triangleIndex in m_overlappedTriangles)
                for (int i = 0; i < m_overlappedTriangles.Count; i++)
                {
                    var triangleIndex = m_overlappedTriangles[i];

                    //  If we reached end of the buffer of neighbour triangles, we stop adding new ones. This is better behavior than throwing exception because of array overflow.
                    if (retTriangles.Count == maxNeighbourTriangles)
                    {
                        VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                        return;
                    }


                    MyTriangleVertexIndices triangle = m_model.Triangles[triangleIndex];
                    MyTriangle_Vertices triangleVertices = new MyTriangle_Vertices();
                    m_model.GetVertex(triangle.I0, triangle.I1, triangle.I2, out triangleVertices.Vertex0, out triangleVertices.Vertex1, out triangleVertices.Vertex2);

                    IndexedVector3 iv0 = triangleVertices.Vertex0.ToBullet();
                    IndexedVector3 iv1 = triangleVertices.Vertex1.ToBullet();
                    IndexedVector3 iv2 = triangleVertices.Vertex2.ToBullet();

                    MyTriangle_Vertex_Normal retTriangle;
                    retTriangle.Vertexes = triangleVertices;
                    retTriangle.Normal = Vector3.Forward;

                    retTriangles.Add(retTriangle);
                }

                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            }
        }

        public void Close()
        {
            lock (m_overlappedTrianglesThreadStaticCollection)
            {
                foreach (var item in m_overlappedTrianglesThreadStaticCollection)
                {
                    item.Clear();
                }
            }
        }

        public int Size
        {
            get
            {
                return m_bvh.Size;
            }
        }
    }
}
