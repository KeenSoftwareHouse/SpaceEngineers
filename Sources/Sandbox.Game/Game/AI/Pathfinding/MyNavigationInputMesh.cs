using Havok;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VRage;
using VRage.Algorithms;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.Models;
using VRage.Profiler;
using VRage.Voxels;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.AI.Pathfinding
{
    class MyNavigationInputMesh
    {
        #region CubeInfo & GridInfo classes
        public class CubeInfo
        {
            public int ID { get; set; }
            public BoundingBoxD BoundingBox { get; set; }
            public List<Vector3D> TriangleVertices { get; set; }
        }

        // DO NOT CACHE THE GRIDS - slimblock should always be calculated when needed, fatblock can have, in the model, the navigation triangles generated on loading.

        public struct GridInfo
        {
            public long ID { get; set; }
            public List<CubeInfo> Cubes { get; set; }
        }
        #endregion

        #region WorldVerticesInfo class
        public class WorldVerticesInfo
        {
            public List<Vector3> Vertices = new List<Vector3>();
            public int VerticesMaxValue = 0;
            public List<int> Triangles = new List<int>();
        }
        #endregion

        #region CacheInterval struct
        public struct CacheInterval
        {
            public Vector3I Min;
            public Vector3I Max;
        }
        #endregion

        #region Fields
        private static IcoSphereMesh m_icosphereMesh = new IcoSphereMesh();
        private static CapsuleMesh m_capsuleMesh = new CapsuleMesh();
        private static WorldVerticesInfo m_worldVerticesInfoPerThread;
        private static Dictionary<string, BoundingBoxD> m_cachedBoxes = new Dictionary<string, BoundingBoxD>();

        [ThreadStatic]
        private List<HkShape> m_tmpShapes = new List<HkShape>();

        private const int NAVMESH_LOD = 0;
        private Dictionary<Vector3I, MyIsoMesh> m_meshCache = new Dictionary<Vector3I, MyIsoMesh>(1024, new Vector3I.EqualityComparer());
        private List<CacheInterval> m_invalidateMeshCacheCoord = new List<CacheInterval>();
        private List<CacheInterval> m_tmpInvalidCache = new List<CacheInterval>();
        private MyPlanet m_planet;
        private Vector3D m_center;
        private Quaternion rdWorldQuaternion;
        private MyRDPathfinding m_rdPathfinding;

        private List<GridInfo> m_lastGridsInfo = new List<GridInfo>();
        private List<CubeInfo> m_lastIntersectedGridsInfoCubes = new List<CubeInfo>();
        #endregion

        #region Properties
        private static WorldVerticesInfo m_worldVertices
        {
            get 
            {
                if (m_worldVerticesInfoPerThread == null)
                    m_worldVerticesInfoPerThread = new WorldVerticesInfo();
                return m_worldVerticesInfoPerThread;
            }
        }
        #endregion

        #region Constructor
        public MyNavigationInputMesh(MyRDPathfinding rdPathfinding, MyPlanet planet, Vector3D center)
        {
            m_rdPathfinding = rdPathfinding;
            m_planet = planet;
            m_center = center;

            Vector3D gravityVector = -Vector3D.Normalize(GameSystems.MyGravityProviderSystem.CalculateTotalGravityInPoint(m_center));
            Vector3D fwd = Vector3D.CalculatePerpendicularVector(gravityVector);
            rdWorldQuaternion = Quaternion.Inverse(Quaternion.CreateFromForwardUp(fwd, gravityVector));
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets the ground vertices and the target grid vertices
        /// </summary>
        public WorldVerticesInfo GetWorldVertices(float border, Vector3D originPosition, MyOrientedBoundingBoxD obb, List<BoundingBoxD> boundingBoxes, List<MyVoxelMap> trackedEntities)
        {
            ClearWorldVertices();

            AddEntities(border, originPosition, obb, boundingBoxes, trackedEntities);
            AddGround(border, originPosition, obb, boundingBoxes);

            return m_worldVertices;
        }

        public void DebugDraw()
        {
            foreach (var gridInfo in m_lastGridsInfo)
                foreach (var cube in gridInfo.Cubes)
                    if (m_lastIntersectedGridsInfoCubes.Contains(cube))
                        VRageRender.MyRenderProxy.DebugDrawAABB(cube.BoundingBox, Color.White);
                    else
                        VRageRender.MyRenderProxy.DebugDrawAABB(cube.BoundingBox, Color.Yellow);            
        }

        public void InvalidateCache(BoundingBoxD box)
        {
            Vector3D minI = Vector3D.Transform(box.Min, m_planet.PositionComp.WorldMatrixInvScaled);
            Vector3D maxI = Vector3D.Transform(box.Max, m_planet.PositionComp.WorldMatrixInvScaled);
            minI += m_planet.SizeInMetresHalf;
            maxI += m_planet.SizeInMetresHalf;

            Vector3I min = new Vector3I(minI);
            Vector3I max = new Vector3I(maxI);

            Vector3I geomMin, geomMax;
            MyVoxelCoordSystems.VoxelCoordToGeometryCellCoord(ref min, out geomMin);
            MyVoxelCoordSystems.VoxelCoordToGeometryCellCoord(ref max, out geomMax);

            m_invalidateMeshCacheCoord.Add(new CacheInterval { Min = geomMin, Max = geomMax });
        }

        public void RefreshCache()
        {
            m_meshCache.Clear();
        }

        public void Clear()
        {
            m_meshCache.Clear();
        }
        #endregion

        #region Private Methods
        private void ClearWorldVertices()
        {
            m_worldVertices.Vertices.Clear();
            m_worldVertices.VerticesMaxValue = 0;
            m_worldVertices.Triangles.Clear();
        }

        #region Entities
        private void BoundingBoxToTranslatedTriangles(BoundingBoxD bbox, Matrix worldMatrix)
        {
            //TODO: use the get corners unsafe
            /*
            unsafe
            {
                Vector3* corners = stackalloc Vector3[8];
                bbox.GetCornersUnsafe(corners);
            }
            */

            #region Boundingbox vertics
            Vector3 vecSuperiorTopLeft = new Vector3(bbox.Min.X, bbox.Max.Y, bbox.Max.Z);
            Vector3 vecSuperiorTopRight = new Vector3(bbox.Max.X, bbox.Max.Y, bbox.Max.Z);
            Vector3 vecSuperiorBottomLeft = new Vector3(bbox.Min.X, bbox.Max.Y, bbox.Min.Z);
            Vector3 vecSuperiorBottomRight = new Vector3(bbox.Max.X, bbox.Max.Y, bbox.Min.Z);
            Vector3 vecInferiorTopLeft = new Vector3(bbox.Min.X, bbox.Min.Y, bbox.Max.Z);
            Vector3 vecInferiorTopRight = new Vector3(bbox.Max.X, bbox.Min.Y, bbox.Max.Z);
            Vector3 vecInferiorBottomLeft = new Vector3(bbox.Min.X, bbox.Min.Y, bbox.Min.Z);
            Vector3 vecInferiorBottomRight = new Vector3(bbox.Max.X, bbox.Min.Y, bbox.Min.Z);

            //TODO: "Vector3.Transform(" in an array!
            Vector3.Transform(ref vecSuperiorTopLeft, ref worldMatrix, out vecSuperiorTopLeft);
            Vector3.Transform(ref vecSuperiorTopRight, ref worldMatrix, out vecSuperiorTopRight);
            Vector3.Transform(ref vecSuperiorBottomLeft, ref worldMatrix, out vecSuperiorBottomLeft);
            Vector3.Transform(ref vecSuperiorBottomRight, ref worldMatrix, out vecSuperiorBottomRight);
            Vector3.Transform(ref vecInferiorTopLeft, ref worldMatrix, out vecInferiorTopLeft);
            Vector3.Transform(ref vecInferiorTopRight, ref worldMatrix, out vecInferiorTopRight);
            Vector3.Transform(ref vecInferiorBottomLeft, ref worldMatrix, out vecInferiorBottomLeft);
            Vector3.Transform(ref vecInferiorBottomRight, ref worldMatrix, out vecInferiorBottomRight);
            #endregion

            // Add vertices
            m_worldVertices.Vertices.Add(vecSuperiorTopLeft);
            m_worldVertices.Vertices.Add(vecSuperiorTopRight);
            m_worldVertices.Vertices.Add(vecSuperiorBottomLeft);
            m_worldVertices.Vertices.Add(vecSuperiorBottomRight);
            m_worldVertices.Vertices.Add(vecInferiorTopLeft);
            m_worldVertices.Vertices.Add(vecInferiorTopRight);
            m_worldVertices.Vertices.Add(vecInferiorBottomLeft);
            m_worldVertices.Vertices.Add(vecInferiorBottomRight);

            int vecSuperiorTopLeftIndex = m_worldVertices.VerticesMaxValue + 0;
            int vecSuperiorTopRightIndex = m_worldVertices.VerticesMaxValue + 1;
            int vecSuperiorBottomLeftIndex = m_worldVertices.VerticesMaxValue + 2;
            int vecSuperiorBottomRightIndex = m_worldVertices.VerticesMaxValue + 3;
            int vecInferiorTopLeftIndex = m_worldVertices.VerticesMaxValue + 4;
            int vecInferiorTopRightIndex = m_worldVertices.VerticesMaxValue + 5;
            int vecInferiorBottomLeftIndex = m_worldVertices.VerticesMaxValue + 6;
            int vecInferiorBottomRightIndex = m_worldVertices.VerticesMaxValue + 7;

            // TOP
            m_worldVertices.Triangles.Add(vecSuperiorBottomRightIndex);
            m_worldVertices.Triangles.Add(vecSuperiorBottomLeftIndex);
            m_worldVertices.Triangles.Add(vecSuperiorTopLeftIndex);

            m_worldVertices.Triangles.Add(vecSuperiorTopLeftIndex);
            m_worldVertices.Triangles.Add(vecSuperiorTopRightIndex);
            m_worldVertices.Triangles.Add(vecSuperiorBottomRightIndex);
            

            //BOTTOM
            m_worldVertices.Triangles.Add(vecInferiorTopLeftIndex);
            m_worldVertices.Triangles.Add(vecInferiorBottomLeftIndex);
            m_worldVertices.Triangles.Add(vecInferiorBottomRightIndex);

            m_worldVertices.Triangles.Add(vecInferiorBottomRightIndex);
            m_worldVertices.Triangles.Add(vecInferiorTopRightIndex);
            m_worldVertices.Triangles.Add(vecInferiorTopLeftIndex);


            //FRONT
            m_worldVertices.Triangles.Add(vecSuperiorBottomLeftIndex);
            m_worldVertices.Triangles.Add(vecInferiorBottomRightIndex);
            m_worldVertices.Triangles.Add(vecInferiorBottomLeftIndex);

            m_worldVertices.Triangles.Add(vecSuperiorBottomLeftIndex);
            m_worldVertices.Triangles.Add(vecSuperiorBottomRightIndex);
            m_worldVertices.Triangles.Add(vecInferiorBottomRightIndex);
            

            //BACK
            m_worldVertices.Triangles.Add(vecSuperiorTopLeftIndex);
            m_worldVertices.Triangles.Add(vecInferiorTopLeftIndex);
            m_worldVertices.Triangles.Add(vecInferiorTopRightIndex);

            m_worldVertices.Triangles.Add(vecInferiorTopRightIndex);
            m_worldVertices.Triangles.Add(vecSuperiorTopRightIndex);
            m_worldVertices.Triangles.Add(vecSuperiorTopLeftIndex);


            //LEFT
            m_worldVertices.Triangles.Add(vecInferiorBottomLeftIndex);
            m_worldVertices.Triangles.Add(vecInferiorTopLeftIndex);
            m_worldVertices.Triangles.Add(vecSuperiorTopLeftIndex);

            m_worldVertices.Triangles.Add(vecSuperiorTopLeftIndex);
            m_worldVertices.Triangles.Add(vecSuperiorBottomLeftIndex);
            m_worldVertices.Triangles.Add(vecInferiorBottomLeftIndex);


            //RIGHT
            m_worldVertices.Triangles.Add(vecSuperiorTopRightIndex);
            m_worldVertices.Triangles.Add(vecInferiorTopRightIndex);
            m_worldVertices.Triangles.Add(vecInferiorBottomRightIndex);

            m_worldVertices.Triangles.Add(vecInferiorBottomRightIndex);
            m_worldVertices.Triangles.Add(vecSuperiorBottomRightIndex);
            m_worldVertices.Triangles.Add(vecSuperiorTopRightIndex);
            

            m_worldVertices.VerticesMaxValue += 8;
        }

        private void AddPhysicalShape(HkShape shape, Matrix rdWorldMatrix)
        {
            switch (shape.ShapeType)
            {
                case Havok.HkShapeType.Box:
                    Havok.HkBoxShape box = (HkBoxShape)shape;

                    Vector3D vMin = new Vector3D(-box.HalfExtents.X, -box.HalfExtents.Y, -box.HalfExtents.Z);
                    Vector3D vMax = new Vector3D(box.HalfExtents.X, box.HalfExtents.Y, box.HalfExtents.Z);
                    BoundingBoxD boundingBox = new BoundingBoxD(vMin, vMax);

                    BoundingBoxToTranslatedTriangles(boundingBox, rdWorldMatrix);
                    break;

                case Havok.HkShapeType.List:
                    var listShape = (HkListShape)shape;
                    var iterator = listShape.GetIterator();
                    while (iterator.IsValid)
                    {
                        AddPhysicalShape(iterator.CurrentValue, rdWorldMatrix);
                        iterator.Next();
                    }
                    break;

                case HkShapeType.Mopp:
                    var compoundShape = (HkMoppBvTreeShape)shape;
                    AddPhysicalShape(compoundShape.ShapeCollection, rdWorldMatrix);
                    break;

                case HkShapeType.ConvexTransform:
                    var transformShape = (HkConvexTransformShape)shape;
                    AddPhysicalShape(transformShape.ChildShape, transformShape.Transform * rdWorldMatrix);
                    break;

                case HkShapeType.ConvexTranslate:
                    var translateShape = (HkConvexTranslateShape)shape;
                    var mat = Matrix.CreateTranslation(translateShape.Translation);
                    AddPhysicalShape((HkShape)translateShape.ChildShape, mat * rdWorldMatrix);
                    break;

                case HkShapeType.Sphere:
                    var sphereShape = (HkSphereShape)shape;
                    m_icosphereMesh.AddTrianglesToWorldVertices(rdWorldMatrix.Translation, sphereShape.Radius);
                    break;

                case HkShapeType.Capsule:
                    return;
                    ProfilerShort.Begin("Capsule");

                    var capsuleShape = (HkCapsuleShape)shape;
                    Line line = new Line(capsuleShape.VertexA, capsuleShape.VertexB);
                    m_capsuleMesh.AddTrianglesToWorldVertices(rdWorldMatrix, capsuleShape.Radius, line);

                    ProfilerShort.End();
                    break;

                case HkShapeType.ConvexVertices:
                    var convexShape = (HkConvexVerticesShape)shape;
                    HkGeometry geometry = new HkGeometry();
                    Vector3 center;
                    convexShape.GetGeometry(geometry, out center);

                    for (int i = 0; i < geometry.TriangleCount; i++)
                    {
                        int i0, i1, i2, materialIndex;
                        geometry.GetTriangle(i, out i0, out i1, out i2, out materialIndex);

                        m_worldVertices.Triangles.Add(m_worldVertices.VerticesMaxValue + i0);
                        m_worldVertices.Triangles.Add(m_worldVertices.VerticesMaxValue + i1);
                        m_worldVertices.Triangles.Add(m_worldVertices.VerticesMaxValue + i2);
                    }

                    for (int i = 0; i < geometry.VertexCount; i++)
                    {
                        Vector3 vec = geometry.GetVertex(i);
                        Vector3.Transform(ref vec, ref rdWorldMatrix, out vec);
                        m_worldVertices.Vertices.Add(vec);
                    }

                    m_worldVertices.VerticesMaxValue += geometry.VertexCount;
                    break;

                default:
                    // For breakpoint. Don't judge me :(
                    break;
            }
        }

        private void AddEntities(float border, Vector3D originPosition, MyOrientedBoundingBoxD obb, List<BoundingBoxD> boundingBoxes, List<MyVoxelMap> trackedEntities)
        {
            obb.HalfExtent += new Vector3D(border, 0, border);
            var aabb = obb.GetAABB();

            List<MyEntity> entities = new List<MyEntity>();
            MyGamePruningStructure.GetAllEntitiesInBox(ref aabb, entities);

            //TODO: remove this? Just for Debug...
            if (entities.Count(e => e is MyCubeGrid) > 0)
            {
                m_lastGridsInfo.Clear();
                m_lastIntersectedGridsInfoCubes.Clear();
            }

            foreach (var entity in entities)
            {
                var grid = entity as MyCubeGrid;
                //TODO: let the static be here?
                if (grid != null && grid.IsStatic)
                {
                    ProfilerShort.Begin("AddEntities.AddGridVerticesInsideOBB");
                    AddGridVerticesInsideOBB(grid, obb);
                    ProfilerShort.End();
                    return;
                }

                var voxelMap = entity as MyVoxelMap;
                if (voxelMap != null)
                {
                    trackedEntities.Add(voxelMap);

                    ProfilerShort.Begin("AddEntities.AddVoxelVertices");
                    AddVoxelVertices(voxelMap, border, originPosition, obb, boundingBoxes);
                    ProfilerShort.End();
                    return;
                }

            }
        }

        /// <summary>
        /// Adds the vertices from the grid blocks that are inside the given OBB
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="obb"></param>
        /// <param name="vertices"></param>
        private void AddGridVerticesInsideOBB(MyCubeGrid grid, MyOrientedBoundingBoxD obb)
        {
            var aabb = obb.GetAABB();

            var gridGroup = MyCubeGridGroups.Static.Logical.GetGroup(grid);
            foreach (var node in gridGroup.Nodes)
            {
                var cubeGrid = (MyCubeGrid)node.NodeData;
                m_rdPathfinding.AddToTrackedGrids(cubeGrid);

                var worldMatrix = cubeGrid.WorldMatrix;
                worldMatrix.Translation -= m_center;
                var toRDWorld = MatrixD.Transform(worldMatrix, rdWorldQuaternion);

                if (MyPerGameSettings.Game == GameEnum.SE_GAME)
                {
                    var box = aabb.TransformFast(cubeGrid.PositionComp.WorldMatrixNormalizedInv);
                    Vector3I start = new Vector3I((int)Math.Round(box.Min.X), (int)Math.Round(box.Min.Y), (int)Math.Round(box.Min.Z));
                    Vector3I end = new Vector3I((int)Math.Round(box.Max.X), (int)Math.Round(box.Max.Y), (int)Math.Round(box.Max.Z));
                    start = Vector3I.Min(start, end);
                    end = Vector3I.Max(start, end);

                    ProfilerShort.Begin("GetShapesInInterval");
                    if (cubeGrid.Physics != null)
                        cubeGrid.Physics.Shape.GetShapesInInterval(start, end, m_tmpShapes);
                    ProfilerShort.End();

                    ProfilerShort.Begin("AddVertices");
                    foreach (var shape in m_tmpShapes)
                        AddPhysicalShape(shape, toRDWorld);

                    m_tmpShapes.Clear();
                    ProfilerShort.End();
                }
            }
        }

        /// <summary>
        /// Adds the vertices from the physical body (rock) that is inside the given OBB
        /// </summary>
        /// <param name="voxelMap"></param>
        /// <param name="border"></param>
        /// <param name="originPosition"></param>
        /// <param name="obb"></param>
        /// <param name="bbList"></param>
        private void AddVoxelVertices(MyVoxelMap voxelMap, float border, Vector3D originPosition, MyOrientedBoundingBoxD obb, List<BoundingBoxD> bbList)
        {
            AddVoxelMesh(voxelMap, voxelMap.Storage, null, border, originPosition, obb, bbList);
        }

        #region IcosphereMesh
        /*
         * Adapted from:
         * Andreas Kahler - Creating an icosphere mesh in code (June 20, 2009)
         * http://blog.andreaskahler.com/2009/06/creating-icosphere-mesh-in-code.html
         */

        public class IcoSphereMesh
        {
            private struct TriangleIndices
            {
                public int v1;
                public int v2;
                public int v3;

                public TriangleIndices(int v1, int v2, int v3)
                {
                    this.v1 = v1;
                    this.v2 = v2;
                    this.v3 = v3;
                }
            }

            //TODO: Find if level 1 is necessary - level 0 MAY BE ENOUGH!
            private const int RECURSION_LEVEL = 1;

            private int index;
            private Dictionary<Int64, int> middlePointIndexCache;
            private List<int> triangleIndices;
            private List<Vector3> positions;

            public IcoSphereMesh()
            {
                create();
            }

            #region Private methods
            // add vertex to mesh, fix position to be on unit sphere, return index
            private int addVertex(Vector3 p)
            {
                double length = Math.Sqrt(p.X * p.X + p.Y * p.Y + p.Z * p.Z);
                this.positions.Add(new Vector3(p.X / length, p.Y / length, p.Z / length));
                return index++;
            }

            // return index of point in the middle of p1 and p2
            private int getMiddlePoint(int p1, int p2)
            {
                // first check if we have it already
                bool firstIsSmaller = p1 < p2;
                Int64 smallerIndex = firstIsSmaller ? p1 : p2;
                Int64 greaterIndex = firstIsSmaller ? p2 : p1;
                Int64 key = (smallerIndex << 32) + greaterIndex;

                int ret;
                if (this.middlePointIndexCache.TryGetValue(key, out ret))
                {
                    return ret;
                }

                // not in cache, calculate it
                Vector3 point1 = this.positions[p1];
                Vector3 point2 = this.positions[p2];
                Vector3 middle = new Vector3(
                    (point1.X + point2.X) / 2.0,
                    (point1.Y + point2.Y) / 2.0,
                    (point1.Z + point2.Z) / 2.0);

                // add vertex makes sure point is on unit sphere
                int i = addVertex(middle);

                // store it, return index
                this.middlePointIndexCache.Add(key, i);
                return i;
            }

            private void create()
            {
                //this.geometry = new MeshGeometry3D();
                this.middlePointIndexCache = new Dictionary<long, int>();
                this.triangleIndices = new List<int>();
                this.positions = new List<Vector3>();
                this.index = 0;

                // create 12 vertices of a icosahedron
                var t = (1.0 + Math.Sqrt(5.0)) / 2.0;

                addVertex(new Vector3(-1, t, 0));
                addVertex(new Vector3(1, t, 0));
                addVertex(new Vector3(-1, -t, 0));
                addVertex(new Vector3(1, -t, 0));

                addVertex(new Vector3(0, -1, t));
                addVertex(new Vector3(0, 1, t));
                addVertex(new Vector3(0, -1, -t));
                addVertex(new Vector3(0, 1, -t));

                addVertex(new Vector3(t, 0, -1));
                addVertex(new Vector3(t, 0, 1));
                addVertex(new Vector3(-t, 0, -1));
                addVertex(new Vector3(-t, 0, 1));


                // create 20 triangles of the icosahedron
                var faces = new List<TriangleIndices>();

                // 5 faces around point 0
                faces.Add(new TriangleIndices(0, 11, 5));
                faces.Add(new TriangleIndices(0, 5, 1));
                faces.Add(new TriangleIndices(0, 1, 7));
                faces.Add(new TriangleIndices(0, 7, 10));
                faces.Add(new TriangleIndices(0, 10, 11));

                // 5 adjacent faces 
                faces.Add(new TriangleIndices(1, 5, 9));
                faces.Add(new TriangleIndices(5, 11, 4));
                faces.Add(new TriangleIndices(11, 10, 2));
                faces.Add(new TriangleIndices(10, 7, 6));
                faces.Add(new TriangleIndices(7, 1, 8));

                // 5 faces around point 3
                faces.Add(new TriangleIndices(3, 9, 4));
                faces.Add(new TriangleIndices(3, 4, 2));
                faces.Add(new TriangleIndices(3, 2, 6));
                faces.Add(new TriangleIndices(3, 6, 8));
                faces.Add(new TriangleIndices(3, 8, 9));

                // 5 adjacent faces 
                faces.Add(new TriangleIndices(4, 9, 5));
                faces.Add(new TriangleIndices(2, 4, 11));
                faces.Add(new TriangleIndices(6, 2, 10));
                faces.Add(new TriangleIndices(8, 6, 7));
                faces.Add(new TriangleIndices(9, 8, 1));


                // refine triangles
                for (int i = 0; i < RECURSION_LEVEL; i++)
                {
                    var faces2 = new List<TriangleIndices>();
                    foreach (var tri in faces)
                    {
                        // replace triangle by 4 triangles
                        int a = getMiddlePoint(tri.v1, tri.v2);
                        int b = getMiddlePoint(tri.v2, tri.v3);
                        int c = getMiddlePoint(tri.v3, tri.v1);

                        faces2.Add(new TriangleIndices(tri.v1, a, c));
                        faces2.Add(new TriangleIndices(tri.v2, b, a));
                        faces2.Add(new TriangleIndices(tri.v3, c, b));
                        faces2.Add(new TriangleIndices(a, b, c));
                    }
                    faces = faces2;
                }

                // done, now add triangles to mesh
                foreach (var tri in faces)
                {
                    this.triangleIndices.Add(tri.v1);
                    this.triangleIndices.Add(tri.v2);
                    this.triangleIndices.Add(tri.v3);
                }
            }
            #endregion

            #region Public methods
            public void AddTrianglesToWorldVertices(Vector3 center, float radius)
            {
                foreach (var index in this.triangleIndices)
                    m_worldVertices.Triangles.Add(m_worldVertices.VerticesMaxValue + index);

                foreach (var position in this.positions)
                    m_worldVertices.Vertices.Add(center + position * radius);

                m_worldVertices.VerticesMaxValue += this.positions.Count;
            }
            #endregion
        }




        #endregion

        #region CapsuleMesh
        /*
         * Adapted from:
         * Paul Bourke - Generating Capsule Geometry (April 2012)
         * http://paulbourke.net/geometry/capsule/
         */
        public class CapsuleMesh
        {
            const double PId2 = Math.PI * 0.5f;
            const double PIm2 = Math.PI * 2;

            List<Vector3> m_verticeList = new List<Vector3>();
            List<int> m_triangleList = new List<int>();

            // Mesh resolution
            int N = 8; // 16; 
            float radius = 1;
            float height = 0;

            public CapsuleMesh()
            {
                Create();
            }

            private void Create()
            {
                int i, j;
                int i1, i2, i3, i4;
                double theta, phi;
                
                for (j = 0; j <= N / 4; j++)
                { // top cap
                    for (i = 0; i <= N; i++)
                    {
                        Vector3 vert = new Vector3();
                        theta = i * PIm2 / N;
                        phi = -PId2 + Math.PI * j / (N / 2);
                        vert.X = radius * (float)(Math.Cos(phi) * Math.Cos(theta));
                        vert.Y = radius * (float)(Math.Cos(phi) * Math.Sin(theta));
                        vert.Z = radius * (float)(Math.Sin(phi)) - height / 2;
                        m_verticeList.Add(vert);
                    }
                }
                
                for (j = N / 4; j <= N / 2; j++)
                { // bottom cap
                    for (i = 0; i <= N; i++)
                    {
                        Vector3 vert = new Vector3();
                        theta = i * PIm2 / N;
                        phi = -PId2 + Math.PI * j / (N / 2);
                        vert.X = radius * (float)(Math.Cos(phi) * Math.Cos(theta));
                        vert.Y = radius * (float)(Math.Cos(phi) * Math.Sin(theta));
                        vert.Z = radius * (float)(Math.Sin(phi)) + height / 2;
                        m_verticeList.Add(vert);
                    }
                }

                for (j = 0; j <= N / 2; j++)
                {
                    for (i = 0; i < N; i++)
                    {
                        i1 = j * (N + 1) + i;
                        i2 = j * (N + 1) + (i + 1);
                        i3 = (j + 1) * (N + 1) + (i + 1);
                        i4 = (j + 1) * (N + 1) + i;

                        m_triangleList.Add(i1);
                        m_triangleList.Add(i2);
                        m_triangleList.Add(i3);

                        m_triangleList.Add(i1);
                        m_triangleList.Add(i3);
                        m_triangleList.Add(i4);
                    }
                }
            }

            public void AddTrianglesToWorldVertices(Matrix transformMatrix, float radius, Line axisLine)
            {
                Matrix mat = Matrix.CreateFromDir(axisLine.Direction);

                Vector3 center = transformMatrix.Translation;
                transformMatrix.Translation = Vector3.Zero;

                int halfVerticeCount = m_verticeList.Count / 2;
                Vector3 halfHeightVector = new Vector3(0, 0, axisLine.Length * 0.5f); 

                // Top part
                for (int i = 0; i < halfVerticeCount; i++)
                    m_worldVertices.Vertices.Add(Vector3.Transform(center + m_verticeList[i] * radius - halfHeightVector, mat));

                // Bottom part
                for (int i = halfVerticeCount; i < m_verticeList.Count; i++)
                    m_worldVertices.Vertices.Add(Vector3.Transform(center + m_verticeList[i] * radius + halfHeightVector, mat));


                // Triangle indices
                foreach (var index in m_triangleList)
                    m_worldVertices.Triangles.Add(m_worldVertices.VerticesMaxValue + index);

                m_worldVertices.VerticesMaxValue += m_verticeList.Count;
            }

        }
        #endregion
        #endregion

        #region Ground
        private void AddMeshTriangles(MyIsoMesh mesh, Vector3 offset, Matrix rotation, Matrix ownRotation)
        {
            for (int i = 0; i < mesh.TrianglesCount; i++)
            {
                ushort a = mesh.Triangles[i].VertexIndex0;
                ushort b = mesh.Triangles[i].VertexIndex1;
                ushort c = mesh.Triangles[i].VertexIndex2;
                m_worldVertices.Triangles.Add(m_worldVertices.VerticesMaxValue + c);
                m_worldVertices.Triangles.Add(m_worldVertices.VerticesMaxValue + b);
                m_worldVertices.Triangles.Add(m_worldVertices.VerticesMaxValue + a);
            }

            for (int i = 0; i < mesh.VerticesCount; i++)
            {
                Vector3 vert;
                mesh.GetUnpackedPosition(i, out vert);
                Vector3.Transform(ref vert, ref ownRotation, out vert);
                vert -= offset;
                Vector3.Transform(ref vert, ref rotation, out vert);

                m_worldVertices.Vertices.Add(vert);
            }

            m_worldVertices.VerticesMaxValue += mesh.VerticesCount;
        }

        private unsafe Vector3* GetMiddleOBBLocalPoints(MyOrientedBoundingBoxD obb, ref Vector3* points)
        {
            Vector3 xDiff = obb.Orientation.Right * (float)obb.HalfExtent.X;
            Vector3 zDiff = obb.Orientation.Forward * (float)obb.HalfExtent.Z;

            Vector3 localPosition = obb.Center - m_planet.PositionComp.GetPosition();

            points[0] = localPosition - xDiff - zDiff;
            points[1] = localPosition + xDiff - zDiff;
            points[2] = localPosition + xDiff + zDiff;
            points[3] = localPosition - xDiff + zDiff;

            return points;
        }

        /// <summary>
        /// Changes the given OBB so it is Y bounded by the range where surface may exist
        /// </summary>
        private bool SetTerrainLimits(ref MyOrientedBoundingBoxD obb)
        {
            float minHeight, maxHeight;
            int pointCount = 4;
            unsafe
            {
                Vector3* points = stackalloc Vector3[4];
                GetMiddleOBBLocalPoints(obb, ref points);
                m_planet.Provider.Shape.GetBounds(points, pointCount, out minHeight, out maxHeight);
            }

            if (minHeight.IsValid() && maxHeight.IsValid())
            {
                Vector3 minPoint = obb.Orientation.Up * minHeight + m_planet.PositionComp.GetPosition();
                Vector3 maxPoint = obb.Orientation.Up * maxHeight + m_planet.PositionComp.GetPosition();

                obb.Center = (minPoint + maxPoint) * 0.5f;
                // maxHeight and minHeight may be the same
                float heightDiff = Math.Max(maxHeight - minHeight, 1);
                obb.HalfExtent.Y = heightDiff * 0.5f;

                return true;
            }

            return false;
        }

        private void AddGround(float border, Vector3D originPosition, MyOrientedBoundingBoxD obb, List<BoundingBoxD> bbList)
        {
            ProfilerShort.Begin("Predict terrain - SetTerrainLimits()");
            bool hasTerrain = SetTerrainLimits(ref obb);
            ProfilerShort.End();

            if (!hasTerrain)
                return;

            AddVoxelMesh(m_planet, m_planet.Storage, m_meshCache, border, originPosition, obb, bbList);
        }

        private void CheckCacheValidity()
        {
            if (m_invalidateMeshCacheCoord.Count > 0)
            {
                m_tmpInvalidCache.AddRange(m_invalidateMeshCacheCoord);
                m_invalidateMeshCacheCoord.Clear();

                foreach (var invalidatedCoord in m_tmpInvalidCache)
                    for (int i = 0; i < m_meshCache.Count; )
                    {
                        var voxelVector = m_meshCache.ElementAt(i).Key;
                        if (voxelVector.X >= invalidatedCoord.Min.X &&
                            voxelVector.Y >= invalidatedCoord.Min.Y &&
                            voxelVector.Z >= invalidatedCoord.Min.Z &&
                            voxelVector.X <= invalidatedCoord.Max.X &&
                            voxelVector.Y <= invalidatedCoord.Max.Y &&
                            voxelVector.Z <= invalidatedCoord.Max.Z)
                        {
                            m_meshCache.Remove(voxelVector);
                            break;
                        }
                        else
                            i++;
                    }

                m_tmpInvalidCache.Clear();
            }
        }

        private void AddVoxelMesh(MyVoxelBase voxelBase, IMyStorage storage, Dictionary<Vector3I, MyIsoMesh> cache, float border, Vector3D originPosition, MyOrientedBoundingBoxD obb, List<BoundingBoxD> bbList)
        {
            bool useCache = cache != null;
            if (useCache)
                CheckCacheValidity();

            obb.HalfExtent += new Vector3D(border, 0, border);
            BoundingBoxD bb = obb.GetAABB();            
            int aabbSideSide = (int)Math.Round(bb.HalfExtents.Max() * 2);
            bb = new BoundingBoxD(bb.Min, bb.Min + aabbSideSide);
            bb.Translate(obb.Center - bb.Center);

            // For debug
            bbList.Add(new BoundingBoxD(bb.Min, bb.Max));

            bb = (BoundingBoxD)bb.TransformFast(voxelBase.PositionComp.WorldMatrixInvScaled);
            bb.Translate(voxelBase.SizeInMetresHalf);

            Vector3I min = Vector3I.Round(bb.Min);
            Vector3I max = min + aabbSideSide;
            Vector3I geomMin, geomMax;
            MyVoxelCoordSystems.VoxelCoordToGeometryCellCoord(ref min, out geomMin);
            MyVoxelCoordSystems.VoxelCoordToGeometryCellCoord(ref max, out geomMax);

            var cullBox = obb;
            cullBox.Transform(voxelBase.PositionComp.WorldMatrixInvScaled);
            cullBox.Center += voxelBase.SizeInMetresHalf;
            ProfilerShort.Begin("WOOOORK");

            Vector3I_RangeIterator it = new Vector3I_RangeIterator(ref geomMin, ref geomMax);
            MyCellCoord coord = new MyCellCoord();
            BoundingBox localAabb;
            coord.Lod = NAVMESH_LOD;
            int hits = 0;
            MyIsoMesh gMesh;
            Vector3 offset = originPosition - voxelBase.PositionLeftBottomCorner;

            // Calculate rotation
            Vector3 gravityVector = -Vector3.Normalize(GameSystems.MyGravityProviderSystem.CalculateTotalGravityInPoint(originPosition));
            Vector3 forwardVector = Vector3.CalculatePerpendicularVector(gravityVector);
            Quaternion quaternion = Quaternion.CreateFromForwardUp(forwardVector, gravityVector);
            Matrix rotation = Matrix.CreateFromQuaternion(Quaternion.Inverse(quaternion));

            Matrix ownRotation = voxelBase.PositionComp.WorldMatrix.GetOrientation();

            while (it.IsValid())
            {
                ProfilerShort.Begin("ITERATOR");

                if (useCache && cache.TryGetValue(it.Current, out gMesh))
                {
                    if (gMesh != null)
                    {
                        AddMeshTriangles(gMesh, offset, rotation, ownRotation);
                    }
                    it.MoveNext();
                    ProfilerShort.End();
                    continue;
                }
                    
                coord.CoordInLod = it.Current;
                MyVoxelCoordSystems.GeometryCellCoordToLocalAABB(ref coord.CoordInLod, out localAabb);
                
                if (!cullBox.Intersects(ref localAabb))
                {
                    hits++;
                    it.MoveNext();
                    ProfilerShort.End();
                    continue;
                }
                ProfilerShort.End();

                var debugBB = new BoundingBoxD(localAabb.Min, localAabb.Max).Translate(-voxelBase.SizeInMetresHalf);
                bbList.Add(debugBB);

                ProfilerShort.Begin("Mesh Calc");
                var voxelStart = coord.CoordInLod * MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS - 1;
                var voxelEnd = voxelStart + MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS //- 1
                    + 1 // overlap to neighbor so geometry is stitched together within same LOD
                    + 1; // for eg. 9 vertices in row we need 9 + 1 samples (voxels)

                var generatedMesh = MyPrecalcComponent.IsoMesher.Precalc(storage, NAVMESH_LOD, voxelStart, voxelEnd, false, false, true);
                ProfilerShort.End();

                if (useCache)
                    cache[it.Current] = generatedMesh;

                if (generatedMesh != null)
                {
                    ProfilerShort.Begin("Mesh NOT NULL");
                    AddMeshTriangles(generatedMesh, offset, rotation, ownRotation);
                    ProfilerShort.End();
                }
                it.MoveNext();
            }
            ProfilerShort.End();
        }
        #endregion
        #endregion
    }
}
