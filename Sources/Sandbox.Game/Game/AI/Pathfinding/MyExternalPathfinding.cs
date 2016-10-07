using Sandbox.Engine.Physics;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VRage.Algorithms;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.Models;
using VRage.Voxels;
using VRageMath;
using VRage;
using Sandbox.Game.World;
using Havok;
using VRageRender;
using RecastDetour;
using ParallelTasks;
using Sandbox.Game.GameSystems;
using VRage.Profiler;
using VRageRender.Messages;

namespace Sandbox.Game.AI.Pathfinding
{
    /*
    public static class PFConst
    {
        public const int DIMENSIONS_MULT = 100;
    }
    */

    public class MyExternalPathfinding : IMyPathfinding
    {
        #region GeometryCenterPair class
        class GeometryCenterPair
        {
            public HkGeometry Geometry
            { get; set; }

            public Vector3D Center
            { get; set; }
        }
        #endregion


        MyRecastOptions m_recastOptions;
        List<MyRecastDetourPolygon> m_polygons = new List<MyRecastDetourPolygon>();
        MyPolyMeshDetail m_polyMesh;
        private Vector3D m_meshCenter;
        private Vector3D m_currentCenter;
        private int m_meshMaxSize, m_singleTileSize, m_singleTileHeight, m_tileLineCount;
        //private int m_semiColumnCount;
        float m_border;
        bool m_isNavmeshInitialized = false;
        MyNavmeshOBBs m_navmeshOBBs;
        MyRDWrapper rdWrapper;

        List<MyRDPath> m_debugDrawPaths = new List<MyRDPath>();
        List<BoundingBoxD> m_lastGroundMeshQuery = new List<BoundingBoxD>();
        private Dictionary<string, GeometryCenterPair> m_cachedGeometry = new Dictionary<string, GeometryCenterPair>();

        public bool DrawDebug { get; set; }
        public bool DrawPhysicalMesh { get; set; }
        private bool drawMesh = false;
        public bool DrawNavmesh //{ get; set; }
        {
            get { return drawMesh; }
            set
            {
                drawMesh = value;
                if (drawMesh)
                    DrawPersistentDebugNavmesh(true);
                else
                    HidePersistentDebugNavmesh();

                /*if(mesh != null && !value)
                {
                    MyRenderProxy.RemoveRenderObject(mesh.ID);
                    mesh = null;
                }*/
            }
        }

        public MyExternalPathfinding() 
        {
        }


        #region Interface - IMyPathfinding Implementation

        public IMyPath FindPathGlobal(Vector3D begin, IMyDestinationShape end, MyEntity relativeEntity)
        {
            // TODO: relativeEntity NOT IMPLEMENTED
            //if (m_visualNavmesh == null || m_navmeshOBBs.GetOBB(begin) == null)
            //    CreateNavmesh(begin);

            // FOR DEBUG DRAW
            /*var path = new MyRDPath(this, end);
            m_debugDrawPaths.Add(path);

            return path;  */
            return null;
        }

        public bool ReachableUnderThreshold(Vector3D begin, IMyDestinationShape end, float thresholdDistance)
        {
            // TODO: IMPLEMENT THIS!!!
            return true;
        }

        /// <summary>
        /// Backwards compatibility
        /// </summary>
        public IMyPathfindingLog GetPathfindingLog()
        {
            return null;
        }

        public void Update()
        {
        }

        public void UnloadData()
        {
            HidePersistentDebugNavmesh();

            m_visualNavmesh.Clear();
            if (m_newVisualNavmesh != null)
                m_newVisualNavmesh.Clear();
            m_newVisualNavmesh = null;
        }

        public void DebugDraw()
        {
            DebugDrawInternal();

            // Debug Draw Path
            int lstCount = m_debugDrawPaths.Count;
            for (int i = 0; i < lstCount; )
            {
                var path = m_debugDrawPaths[i];
                if (!path.IsValid || path.PathCompleted)
                {
                    m_debugDrawPaths.RemoveAt(i);
                    lstCount = m_debugDrawPaths.Count;
                }
                else
                {
                    path.DebugDraw();
                    i++;
                }
            }
        }

        #endregion

        //#region Public Methods
        //#endregion

        #region OBB corners
        /* MyOBBs corners
        * 00 - Upper Front Left
        * 01 - Upper Back Left
        * 02 - Lower Back Left
        * 03 - Lower Front Left
        * 04 - Upper Front Right
        * 05 - Upper Back Right
        * 06 - Lower Back Right
        * 07 - Lower Front Right
        */

        public enum OBBCorner
        {
            UpperFrontLeft = 0,
            UpperBackLeft,
            LowerBackLeft,
            LowerFrontLeft,
            UpperFrontRight,
            UpperBackRight,
            LowerBackRight,
            LowerFrontRight
        }

        public static Vector3D GetOBBCorner(MyOrientedBoundingBoxD obb, OBBCorner corner)
        {
            Vector3D[] corners = new Vector3D[8];
            obb.GetCorners(corners, 0);
            return corners[(int)corner];
        }

        public static List<Vector3D> GetOBBCorners(MyOrientedBoundingBoxD obb, List<OBBCorner> corners)
        {
            Vector3D[] cornerArray = new Vector3D[8];
            obb.GetCorners(cornerArray, 0);

            List<Vector3D> result = new List<Vector3D>();
            foreach (var corner in corners)
                result.Add(cornerArray[(int)corner]);

            return result;
        }
        #endregion

        //private NavmeshOBBs m_navmeshOBBs;

        #region NavmeshOBBs class
        /*
        // TODO: class should have a datastructure to encapsulate OBB with their coordinate and gravity vector        
        
        /// <summary>
        /// Class that contains navmesh OBBs
        /// The middle of the matrix has 0,0 (x,y) coordinate, left is -x, bottom is -y
        /// ATTENTION -> the above is not true anymore -> search for the truth
        /// </summary>
        private class NavmeshOBBs
        {
            #region Fields
            private MyOrientedBoundingBox?[][] m_obbs;
            private float m_tileHalfSize, m_tileHalfHeight;
            Vector3D m_centerPoint;
            #endregion

            public int OBBsPerLine { get; private set; }

            #region Contructor
            // TODO: accept a rotation so we define what is front,back,left, right....
            //       or maybe notttttt
            public NavmeshOBBs(Vector3D centerPoint, int obbsPerLine, int tileSize, int tileHeight)
            {
                // There will always be an odd number of obbs in a line
                OBBsPerLine = obbsPerLine;
                if (OBBsPerLine % 2 == 0)
                    OBBsPerLine += 1;

                m_tileHalfSize = tileSize * 0.5f;
                m_tileHalfHeight = tileHeight * 0.5f;
                m_centerPoint = centerPoint;

                m_obbs = new MyOrientedBoundingBox?[OBBsPerLine][];
                for (int i = 0; i < OBBsPerLine; i++)
                    m_obbs[i] = new MyOrientedBoundingBox?[OBBsPerLine];

                Initialize();
            }
            #endregion

            #region public Methods
            /// <summary>
            /// Return the OBB at the specific coordinate or null, if is out of bounds
            /// </summary>
            public MyOrientedBoundingBox? GetOBB(int coordX, int coordY)
            {
                if (coordX < 0 || coordX >= OBBsPerLine ||
                    coordY < 0 || coordY >= OBBsPerLine)
                    return null;

                return m_obbs[coordY][coordX];
            }

            public MyOrientedBoundingBox? GetOBB(Vector3D worldPosition)
            {
                // TODO: silly search needs to get smarter
                foreach (var obbLine in m_obbs)
                    foreach(var obb in obbLine)
                    {
                        Vector3D diff = obb.Value.Center - worldPosition;
                        if (Math.Abs(diff.X) <= obb.Value.HalfExtent.X &&
                            Math.Abs(diff.Y) <= obb.Value.HalfExtent.Y &&
                            Math.Abs(diff.Z) <= obb.Value.HalfExtent.Z)
                            return obb;
                    }

                return null;
            }

            /// <summary>
            /// TEMPORARY - Returns the coords for the OBB - remove this after creating a data structure to encapsulate...
            /// </summary>
            public bool GetCoords(MyOrientedBoundingBoxD obb, out int xCoord, out int yCoord)
            {
                xCoord = yCoord = -1;

                for (int i = 0; i < m_obbs.Length; i++)
                    for (int j = 0; j < m_obbs[0].Length; j++)
                        if (obb == m_obbs[i][j])
                        {
                            xCoord = j;
                            yCoord = i;
                            return true;
                        }

                return false;
            }

            /// <summary>
            /// Returns a list of OBBs intersected by a line
            /// </summary>
            public List<MyOrientedBoundingBoxD> GetIntersectedOBB(Line line)
            {
                //List<MyOrientedBoundingBoxD> intersectedOBBs = new List<MyOrientedBoundingBoxD>();
                Dictionary<MyOrientedBoundingBox, float> intersectedOBBs = new Dictionary<MyOrientedBoundingBox, float>();

                foreach (var obbLine in m_obbs)
                    foreach (var obb in obbLine)
                        if (obb.Value.Contains(ref line.From) || 
                            obb.Value.Contains(ref line.To) || 
                            obb.Value.Intersects(ref line).HasValue)
                            //intersectedOBBs.Add(obb.Value);
                            intersectedOBBs.Add(obb.Value, Vector3D.Distance(line.From, obb.Value.Center));

                //if (intersectedOBBs.Count > 0)
                //    ;

                return intersectedOBBs.OrderBy(d => d.Value).Select(kvp => kvp.Key).ToList();
            }
            
            #endregion

            #region Private Methods
            private void Initialize()
            {
                /* MyOBBs corners
                 * 00 - Upper Front Left <--
                 * 01 - Upper Back Left <--
                 * 02 - Lower Back Left
                 * 03 - Lower Front Left
                 * 04 - Upper Front Right <--
                 * 05 - Upper Back Right <--
                 * 06 - Lower Back Right
                 * 07 - Lower Front Right
                 */
        /*
                // TODO: use GetOBBCorners
                int middleCoord = (OBBsPerLine - 1) / 2;
                MyOrientedBoundingBoxD obb = CreateOBB(m_centerPoint);
                m_obbs[middleCoord][middleCoord] = obb;

                Vector3[] corners = new Vector3[8];
                obb.GetCorners(corners, 0);

                Vector3D offset = corners[4];
                Vector3D newGravity = -Vector3D.Normalize(MyGravityProviderSystem.CalculateTotalGravityInPoint(offset));                

                Vector3D newPoint = Vector3D.Transform(m_centerPoint - offset, Quaternion.CreateFromAxisAngle(newGravity, (float)(-90 * Math.PI / 180f)));
                Vector3D centerHorizontalDiff = newPoint - (m_centerPoint - offset);

                // //

                // For each point, calculate up and down points //

                Vector3D newCenter = m_centerPoint;
                Vector2I index = new Vector2I(middleCoord,middleCoord);
                for (int i = middleCoord; i >= 0; i--)
                {
                    FillOBBLine(newCenter, index);
                    newCenter -= centerHorizontalDiff;
                    index += new Vector2I(-1,0);
                }

                newCenter = m_centerPoint;
                index.X = index.Y = middleCoord;
                for (int i = middleCoord + 1; i < OBBsPerLine; i++)
                {
                    newCenter += centerHorizontalDiff;
                    index += new Vector2I(1, 0);
                    FillOBBLine(newCenter, index);
                }
            }


            private void FillOBBLine(Vector3D center, Vector2I currentIndex)
            {
                Vector3D gravityVector = -Vector3D.Normalize(GameSystems.MyGravityProviderSystem.CalculateTotalGravityInPoint(center));
                Vector3D perpedicularVector = Vector3D.CalculatePerpendicularVector(gravityVector);
                MyOrientedBoundingBoxD obb = new MyOrientedBoundingBox(center, new Vector3(m_tileHalfSize, m_tileHalfHeight, m_tileHalfSize), Quaternion.CreateFromForwardUp(perpedicularVector, gravityVector));
                if (m_obbs[currentIndex.Y][currentIndex.X] == null)
                    m_obbs[currentIndex.Y][currentIndex.X] = obb;

                Vector3[] corners = new Vector3[8];
                obb.GetCorners(corners, 0);
                Vector3D offset = corners[1];
                Vector3D newGravity = -Vector3D.Normalize(MyGravityProviderSystem.CalculateTotalGravityInPoint(offset));

                Vector3D newPoint = Vector3D.Transform(center - offset, Quaternion.CreateFromAxisAngle(newGravity, (float)(90 * Math.PI / 180f)));
                Vector3D centerVerticalDiff = newPoint - (center - offset);

                FillOBBSemiLine(center, centerVerticalDiff, currentIndex, new Vector2I(0, -1));
                FillOBBSemiLine(center, -centerVerticalDiff, currentIndex, new Vector2I(0, 1));
            }

            

            private void FillOBBSemiLine(Vector3D currentCenter, Vector3D diffVector, Vector2I currentIndex, Vector2I indexAddition)
            {
                if (currentIndex.X < 0 || currentIndex.X >= OBBsPerLine ||
                    currentIndex.Y < 0 || currentIndex.Y >= OBBsPerLine)
                    return;

                if(m_obbs[currentIndex.Y][currentIndex.X] == null)
                    m_obbs[currentIndex.Y][currentIndex.X] = CreateOBB(currentCenter);

                FillOBBSemiLine(currentCenter + diffVector, diffVector, currentIndex + indexAddition, indexAddition);
            }

            private MyOrientedBoundingBoxD CreateOBB(Vector3D center)
            {
                Vector3D gravityVector = -Vector3D.Normalize(GameSystems.MyGravityProviderSystem.CalculateTotalGravityInPoint(center));
                Vector3D perpedicularVector = Vector3D.CalculatePerpendicularVector(gravityVector);
                return new MyOrientedBoundingBox(center, new Vector3(m_tileHalfSize, m_tileHalfHeight, m_tileHalfSize), Quaternion.CreateFromForwardUp(perpedicularVector, gravityVector));
            }
            #endregion
        }
         */
        #endregion

        public void InitializeNavmesh(Vector3D center)
        {
            m_isNavmeshInitialized = true;
            float cellSize = 0.2f;
            m_singleTileSize = 20;
            m_tileLineCount = 50;

            m_singleTileHeight = 70;

            //m_semiColumnCount = ((m_singleTileHeight / m_singleTileSize) - 1) / 2;


            m_recastOptions = new MyRecastOptions()
            {
                cellHeight = 0.2f,
                agentHeight = 1.5f,
                agentRadius = 0.5f,
                agentMaxClimb = 0.5f,
                agentMaxSlope = 50,
                regionMinSize = 1,
                regionMergeSize = 10,
                edgeMaxLen = 50,
                edgeMaxError = 3f,
                vertsPerPoly = 6,
                detailSampleDist = 6,
                detailSampleMaxError = 1,
                partitionType = 1
            };

            float horizontalOrigin = (m_singleTileSize * 0.5f + m_singleTileSize * (float)Math.Floor(m_tileLineCount * 0.5f));
            var verticalOrigin = m_singleTileHeight * 0.5f;
            m_border = m_recastOptions.agentRadius + 3 * cellSize;

            float[] bmin = new float[3] { -horizontalOrigin, -verticalOrigin, -horizontalOrigin };
            float[] bmax = new float[3] { horizontalOrigin, verticalOrigin, horizontalOrigin };

            rdWrapper = new MyRDWrapper();
            rdWrapper.Init(cellSize, m_singleTileSize, bmin, bmax);

            //Vector3D direction = MySession.Static.ControlledEntity.ControllerInfo.Controller.Player.Character.WorldMatrix.Forward;
            Vector3D gravityVector = -Vector3D.Normalize(GameSystems.MyGravityProviderSystem.CalculateTotalGravityInPoint(center));
            var direction = Vector3D.CalculatePerpendicularVector(gravityVector);

            UnloadData();
            m_navmeshOBBs = new MyNavmeshOBBs(GetPlanet(center), center, direction, m_tileLineCount, m_singleTileSize, m_singleTileHeight);
            // TODO: m_meshCenter is used for pathfinding position transformation -> probably use the center point of the OBB
            m_meshCenter = center;

            // To get the center point difference
            //var planet = GetPlanet(center);
            //int halfMaxSize = (int)(m_singleTileSize / 2f);

            //MyNavigationInputMesh.RefreshCache();
            m_visualNavmesh.Clear();
        }

        
        bool m_isNavmeshCreationRunning = false;
        /*
        public void StartNavmeshCreation_OLD(Vector3D center)
        {
            m_isNavmeshCreationRunning = !m_isNavmeshCreationRunning;

            if (m_isNavmeshCreationRunning)
                Parallel.Start(() => { InitializeNavmesh(center); });
        }
        */
        public void StartNavmeshTileCreation(List<MyNavmeshOBBs.OBBCoords> obbList)
        {
            // TODO: remove the return - only here to prevent pathfinding
            //return;

            //m_isNavmeshCreationRunning = !m_isNavmeshCreationRunning;
            // TODO: what to do with the ignored OBBs??

            if (!m_isNavmeshCreationRunning)
            {
                // TODO: real thread safety!
                m_isNavmeshCreationRunning = true;
                Parallel.Start(() => { GenerateTiles(obbList); });
            }

        }

        private MyPlanet GetPlanet(Vector3D position)
        {
            int voxelDistance = 100;
            BoundingBoxD box = new BoundingBoxD(position - voxelDistance * 0.5f, position + voxelDistance * 0.5f);
            return MyGamePruningStructure.GetClosestPlanet(ref box);
        }

        private void GenerateTiles(List<MyNavmeshOBBs.OBBCoords> obbList)
        {
            // To get the center point difference
            
            var planet = GetPlanet(m_meshCenter);

            foreach(var obbPair in obbList)
            {
                var obb = obbPair.OBB;
                Vector3D localCenter = WorldPositionToLocalNavmeshPosition(obb.Center, 0);
                //float[] centerlocalNavmeshPoint = new float[] { (float)localCenter.X, (float)localCenter.Y, (float)localCenter.Z };

                /*int i, j;
                if (!m_navmeshOBBs.GetCoords(obb, out j, out i))
                    continue;
                */
                if (rdWrapper.TileAlreadyGenerated(localCenter))//centerlocalNavmeshPoint))
                    continue;

                List<Vector3D> vertices = new List<Vector3D>();

                //m_lastGroundMeshQuery = MyNavigationInputMesh.GetWorldVertices(planet, m_border, m_meshCenter, obb, m_singleTileSize, vertices);

                int triangleCount = vertices.Count / 3;
                float[] rcVertices = new float[vertices.Count * 3];

                for (int n = 0, index = 0; n < vertices.Count; n++)
                {
                    rcVertices[index++] = (float)vertices[n].X;
                    rcVertices[index++] = (float)vertices[n].Y;
                    rcVertices[index++] = (float)vertices[n].Z;
                }

                int[] triangles = new int[triangleCount * 3];
                for (int n = 0; n < triangleCount * 3; n++)
                    triangles[n] = n;

                m_polygons.Clear();

                // Build navmesh
                if (triangleCount > 0)
                {
                    /////////////////////////////////////////rdWrapper.CreateNavmeshTile(localCenter/*centerlocalNavmeshPoint*/, ref m_recastOptions, ref m_polygons, obbPair.Coords.X, obbPair.Coords.Y, 0, rcVertices, vertices.Count, triangles, triangleCount);

                    // add triangles to the debugDrawingMesh
                    var navmesh = new List<Vertex>();
                    GenerateDebugDrawPolygonNavmesh(planet, obb, navmesh, obbPair.Coords.X, obbPair.Coords.Y);
                    m_newVisualNavmesh = navmesh;

                    // Necessary to make it sleep? Here? How much?
                    System.Threading.Thread.Sleep(10);
                }
            }
            m_isNavmeshCreationRunning = false;
        }

        

        /*private void CreateNavmesh_OLD(Vector3D center)
        {
            UnloadData();
            InitializeNavmesh(center);
            m_navmeshOBBs = new NavmeshOBBs(center, m_tileLineCount, m_singleTileSize, m_singleTileHeight);
            // TODO: m_meshCenter is used for pathfinding position transformation -> probably use the center point of the OBB
            m_meshCenter = center;

            // To get the center point difference
            var planet = GetPlanet(center);
            int halfMaxSize = (int)(m_singleTileSize / 2f);

            MyNavigationInputMesh.RefreshCache();

            for (int i = 0; i < m_navmeshOBBs.OBBsPerLine && m_isNavmeshCreationRunning; i++)
                for (int j = 0; j < m_navmeshOBBs.OBBsPerLine && m_isNavmeshCreationRunning; j++)
                {
                    MyOrientedBoundingBox? obb = m_navmeshOBBs.GetOBB(j, i);
                    List<Vector3D> vertices = new List<Vector3D>();

                    m_lastGroundMeshQuery = MyNavigationInputMesh.GetWorldVertices(planet, m_border, m_meshCenter, obb.Value, m_singleTileSize, vertices);

                    int triangleCount = vertices.Count / 3;
                    float[] rcVertices = new float[vertices.Count * 3];

                    for (int n = 0, index = 0; n < vertices.Count; n++)
                    {
                        rcVertices[index++] = vertices[n].X;
                        rcVertices[index++] = vertices[n].Y;
                        rcVertices[index++] = vertices[n].Z;
                    }

                    int[] triangles = new int[triangleCount * 3];
                    for (int n = 0; n < triangleCount * 3; n++)
                        triangles[n] = n;

                    m_polygons.Clear();

                    // Build navmesh
                    if (triangleCount > 0)
                    {
                        Vector3D localCenter = WorldPositionToLocalNavmeshPosition(obb.Value.Center, 0);
                        float[] centerlocalNavmeshPoint = new float[] { localCenter.X, localCenter.Y, localCenter.Z };

                        MyRDWrapper.CreateNavmeshTile(centerlocalNavmeshPoint, ref m_recastOptions, ref m_polygons, j, i, 0, rcVertices, vertices.Count, triangles, triangleCount);

                        // add triangles to the debugDrawingMesh
                        GenerateDebugDrawPolygonNavmesh(!!, j, i);

                        System.Threading.Thread.Sleep(20);
                    }
                }
        }*/

        #region CLI performance test
        private void TestCliPerformance()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            long level0, level1, level2;
            int n = 10000000;

            // Level0
            sw.Start();
            for (int i = 0; i < n; i++)
                Test.SimpleTest(i);
            sw.Stop();
            level0 = sw.ElapsedMilliseconds;

            // Level1
            sw.Start();
            for (int i = 0; i < n; i++)
                MyRDWrapper.SimpleTestShallow(i);
            sw.Stop();
            level1 = sw.ElapsedMilliseconds;

            // Level2
            sw.Start();
            for (int i = 0; i < n; i++)
                MyRDWrapper.SimpleTestDeep(i);
            sw.Stop();
            level2 = sw.ElapsedMilliseconds;
        }

        private class Test
        {
             static public int SimpleTest(int i)
            {
                return 0;
            }
        }
        #endregion


        private void GenerateDebugDrawPolygonNavmesh(MyPlanet planet, MyOrientedBoundingBoxD obb, List<Vertex> navmesh, int xCoord, int yCoord)
        {
            //Vector3D localOffset = m_meshCenter - obb.Center;

            int greenBase = 10;
            int greenValue = 0;
            int maxExclusiveGreenValue = 95;
            int greenStep = 10;

            foreach (var polygon in m_polygons)
            {
                //var obb = m_navmeshOBBs.GetOBB(polygon.X, polygon.Y);
                foreach (var vertice in polygon.Vertices)
                {
                    Vector3D p1 = LocalNavmeshPositionToWorldPosition(obb, vertice, m_meshCenter, Vector3D.Zero);
                    //Vector3D p2 = Vector3D.Transform(vertice, obb.Orientation) + m_meshCenter;
                    //Vector3D p2 = Vector3D.Transform(vertice + m_meshCenter - obb.Value.Center, obb.Value.Orientation) + obb.Value.Center;

                    var v = new Vertex()
                    {
                        // TODO: LocalNavmeshPositionToWorldPosition not local?? rename or redo...
                        //pos = LocalNavmeshPositionToWorldPosition(vertice, /*m_meshCenter*/obb.Value.Center, Vector3D.Zero), //+ localOffset,
                        pos = p1,
                        color = new Color(0, greenBase + greenValue, 0)
                    };
                    navmesh.Add(v);
                }
                greenValue += greenStep;
                greenValue %= maxExclusiveGreenValue;
            }
        }


        private MatrixD LocalNavmeshPositionToWorldPositionTransform(MyOrientedBoundingBoxD obb, Vector3D center)
        {
            // TODO: should use the obb?
            
            Vector3D gravityVector = -Vector3D.Normalize(GameSystems.MyGravityProviderSystem.CalculateTotalGravityInPoint(center));                
            var fwd = Vector3D.CalculatePerpendicularVector(gravityVector);
            Quaternion quaternion = Quaternion.CreateFromForwardUp(fwd, gravityVector);
             
            //Quaternion quaternion = Quaternion.CreateFromForwardUp(obb.Orientation.Right, obb.Orientation.Up);
            return MatrixD.CreateFromQuaternion(quaternion);
        }

        private Vector3D LocalNavmeshPositionToWorldPosition(MyOrientedBoundingBoxD obb, Vector3D position, Vector3D center, Vector3D heightIncrease)
        {
            var transformationMatrix = LocalNavmeshPositionToWorldPositionTransform(obb, center);

            Vector3D transformedPosition = Vector3D.Transform(position, transformationMatrix) + m_meshCenter;// center;
            return transformedPosition;
        }




        Vector3D? m_pathfindingDebugTarget;
        public void SetTarget(Vector3D? target)
        {
            //if (/*target.HasValue &&*/m_colorVisualNavmesh != null && m_colorVisualNavmesh.Count > 0)
                //if (MyPerGameSettings.Game == GameEnum.SE_GAME)
            m_pathfindingDebugTarget = target;

            /*
            // TODO: remove this.... It's only to debug
            // target is being used to generate NavmeshOBBs....
            int singleTileSize = 20;
            int singleTileHeight = 20;
            int tileLineCount = 100;

            if (target.HasValue)
            {
                var initialDirection = MySession.Static.ControlledEntity.ControllerInfo.Controller.Player.Character.WorldMatrix.Forward;
                ProfilerShort.Begin("MyNavmeshOBBs Creation");
                m_navmeshOBB = new MyNavmeshOBBs(GetPlanet(target.Value), target.Value, initialDirection, tileLineCount, singleTileSize, singleTileHeight);
                ProfilerShort.End();
            }
            else
                m_navmeshOBB = null;
             */
        }

        private Vector3D WorldPositionToLocalNavmeshPosition(Vector3D position, float heightIncrease)
        {
            //is this okkkk?????
            // TODO: ask for the tile and its center, to the navmeshOBBs
            Vector3D center;
            var obb = m_navmeshOBBs.GetOBB(position);
            if (obb != null)
                center = obb.Value.Center;
            else
                center = m_meshCenter;

            Vector3D gravityVector = -Vector3D.Normalize(GameSystems.MyGravityProviderSystem.CalculateTotalGravityInPoint(m_meshCenter));
            Vector3D forwardVector = Vector3D.CalculatePerpendicularVector(gravityVector);
            Quaternion quaternion = Quaternion.CreateFromForwardUp(forwardVector, gravityVector);
            MatrixD rotationMatrix = MatrixD.CreateFromQuaternion(Quaternion.Inverse(quaternion));

            Vector3D transPosition = (position - /*center*/m_meshCenter) + heightIncrease * gravityVector;
            transPosition = Vector3D.Transform(transPosition, rotationMatrix);
            return transPosition;
        }


        private Vector3D LocalPositionToWorldPosition(Vector3D position)
        {
            //var obb = m_navmeshOBBs.GetOBB(position);
            Vector3D center = position;
            if (m_navmeshOBBs != null)
            //if (obb != null)
            //    center = obb.Value.Center;
            //else
                center = m_meshCenter;

            Vector3D gravityVector = -Vector3D.Normalize(GameSystems.MyGravityProviderSystem.CalculateTotalGravityInPoint(center));
            return LocalNavmeshPositionToWorldPosition(m_navmeshOBBs.CenterOBB, position, center, 0.5f * gravityVector);
        }

        /*
        public Vector3D? GetNextPathPoint_Old(Vector3D initialPosition, Vector3D targetPosition)
        {
            var iniPos = WorldPositionToLocalNavmeshPosition(initialPosition, 0.5f);
            var endPos = WorldPositionToLocalNavmeshPosition(targetPosition, 0.5f);
            Vector3D? localVector = MyRDWrapper.GetPathNextPosition(iniPos, endPos);
            if (localVector.HasValue)
                return LocalPositionToWorldPosition(localVector.Value);
            else
                return null;
        }
        */

        List<MyNavmeshOBBs.OBBCoords> m_debugDrawIntersectedOBBs = new List<MyNavmeshOBBs.OBBCoords>();

        public List<Vector3D> GetPathPoints(Vector3D initialPosition, Vector3D targetPosition)
        {
            List<Vector3D> worldPath = new List<Vector3D>();

            if (m_isNavmeshCreationRunning)
                return worldPath;

            if (!m_isNavmeshInitialized)
                InitializeNavmesh(initialPosition);

            //if (m_polygons.Count == 0 || m_navmeshOBBs.GetOBB(initialPosition) == null)
            //{
                //StartNavmeshCreation(initialPosition);

            //    return worldPath;
            //}

            var iniPos = WorldPositionToLocalNavmeshPosition(initialPosition, 0.5f);
            var endPos = WorldPositionToLocalNavmeshPosition(targetPosition, 0.5f);

            var localPath = rdWrapper.GetPath(iniPos, endPos);

            if (localPath.Count == 0)
            {
                var intersectedOBBs = m_navmeshOBBs.GetIntersectedOBB(new LineD(initialPosition, targetPosition));
                StartNavmeshTileCreation(intersectedOBBs);
                ////// DEBUGGGIN
                m_debugDrawIntersectedOBBs = intersectedOBBs;
            }
            else
                foreach (var point in localPath)
                    worldPath.Add(LocalPositionToWorldPosition(point));

            return worldPath;
        }
        /*
        public Vector3D? GetNextPathPoint(Vector3D initialPosition, Vector3D targetPosition)
        {
            List<Vector3D> path = GetPathPoints(initialPosition, targetPosition);

            for(int i = 0; i < path.Count-1 ; i++)
            {
                var worldPoint = path[i];// LocalPositionToWorldPosition(path[i]);
                var nextWorldPoint = path[i + 1]; //LocalPositionToWorldPosition(path[i+1]);
                MyRenderProxy.DebugDrawLine3D(worldPoint, nextWorldPoint, Color.Red, Color.Red, true);
                MyRenderProxy.DebugDrawSphere(nextWorldPoint, 0.3f, Color.Yellow, 1f, true);
            }

            // First point is initialPosition
            if (path.Count >= 2)
                return path[1]; //LocalPositionToWorldPosition(path[1]);
            else
                return null;
        }
        */
        #region DebugDraw
        public void DrawGeometry(HkGeometry geometry, MatrixD worldMatrix, Color color, bool depthRead = false, bool shaded = false)
        {
            var msg = MyRenderProxy.PrepareDebugDrawTriangles();
            try
            {
                ProfilerShort.Begin("MyExternalPathfinfing.GetGeometryTriangles");

                for (int i = 0; i < geometry.TriangleCount; i++)
                {
                    int a, b, c, m;
                    geometry.GetTriangle(i, out a, out b, out c, out m);
                    msg.AddIndex(a);
                    msg.AddIndex(b);
                    msg.AddIndex(c);
                }

                for (int i = 0; i < geometry.VertexCount; i++)
                    msg.AddVertex(geometry.GetVertex(i));

                ProfilerShort.End();
            }
            finally
            {
                MyRenderProxy.DebugDrawTriangles(msg, worldMatrix, color, depthRead, shaded);
            }
        }


        private void DebugDrawShape(string blockName, HkShape shape, MatrixD worldMatrix)
        {
            float expandRatio = 1.05f;
            float expandSize = 0.02f;

            if (MyPerGameSettings.Game == GameEnum.SE_GAME)
                expandSize = 0.1f;

            switch (shape.ShapeType)
            {
                case Havok.HkShapeType.Box:
                    Havok.HkBoxShape box = (HkBoxShape) shape;
                    MyRenderProxy.DebugDrawOBB(MatrixD.CreateScale(box.HalfExtents * 2 + new Vector3(expandSize)) * worldMatrix, Color.Red, 0, true, false);
                    break;

                case Havok.HkShapeType.List:
                    var listShape = (HkListShape)shape;
                    var iterator = listShape.GetIterator();
                    int shapeIndex = 0;
                    while (iterator.IsValid)
                    {
                        DebugDrawShape(blockName + shapeIndex++, iterator.CurrentValue, worldMatrix);
                        iterator.Next();
                    }
                    break;

                case HkShapeType.Mopp:
                    var compoundShape = (HkMoppBvTreeShape)shape;
                    DebugDrawShape(blockName, compoundShape.ShapeCollection, worldMatrix);
                    break;

                case HkShapeType.ConvexTransform:
                    var transformShape = (HkConvexTransformShape)shape;
                    DebugDrawShape(blockName, transformShape.ChildShape, transformShape.Transform * worldMatrix);
                    break;

                case HkShapeType.ConvexTranslate:
                    var translateShape = (HkConvexTranslateShape)shape;
                    DebugDrawShape(blockName, (HkShape)translateShape.ChildShape, Matrix.CreateTranslation(translateShape.Translation) * worldMatrix);
                    break;

                case HkShapeType.ConvexVertices:
                    var convexShape = (HkConvexVerticesShape)shape;

                    GeometryCenterPair geometryCenterPair;

                    ProfilerShort.Begin("MyExternalPathfinfing.m_cachedGeometry.TryGetValue");

                    if (!m_cachedGeometry.TryGetValue(blockName, out geometryCenterPair))
                    {
                        HkGeometry debugGeometry = new HkGeometry();
                        Vector3 center;
                        convexShape.GetGeometry(debugGeometry, out center);

                        geometryCenterPair = new GeometryCenterPair() { Geometry = debugGeometry, Center = center };

                        if (!string.IsNullOrEmpty(blockName))
                            m_cachedGeometry.Add(blockName, geometryCenterPair);
                    }
                    ProfilerShort.End();

                    Vector3D transformedCenter = Vector3D.Transform(geometryCenterPair.Center, worldMatrix.GetOrientation());                    

                    var matrix = worldMatrix;
                    matrix = MatrixD.CreateScale(expandRatio) * matrix;
                    matrix.Translation -= transformedCenter * (expandRatio - 1);

                    DrawGeometry(geometryCenterPair.Geometry, matrix, Color.Olive);
                    break;

                default:
                    // For breakpoint. Don't judge me :(
                    break;
                    
            }

        }

        /// <summary>
        /// Draws physical mesh
        /// </summary>
        public void DebugDrawPhysicalShapes()
        {
            // Draw physic forms
            var targetGrid = MyCubeGrid.GetTargetGrid();
            if (targetGrid != null)
            {
                List<MyCubeGrid> gridsToExport = new List<MyCubeGrid>();
                var gridGroup = MyCubeGridGroups.Static.Logical.GetGroup(targetGrid);
                foreach (var node in gridGroup.Nodes)
                    gridsToExport.Add(node.NodeData);


                var baseGridWorldInv = MatrixD.Invert(gridsToExport[0].WorldMatrix);
                foreach (var grid in gridsToExport)
                {
                    if (MyPerGameSettings.Game == GameEnum.SE_GAME)
                    {
                        HkGridShape root = new HkGridShape(grid.GridSize, HkReferencePolicy.None);

                        Entities.Cube.MyCubeBlockCollector blockCollector = new Entities.Cube.MyCubeBlockCollector();
                        Engine.Utils.MyVoxelSegmentation segmenter = new Engine.Utils.MyVoxelSegmentation();
                        Dictionary<Vector3I, HkMassElement> massElements = new Dictionary<Vector3I, HkMassElement>();

                        blockCollector.Collect(grid, segmenter, Engine.Utils.MyVoxelSegmentationType.Simple, massElements);

                        foreach(var shape in blockCollector.Shapes)
                            DebugDrawShape("", shape, grid.WorldMatrix);
                    }
                    else
                        foreach (var block in grid.GetBlocks())
                        {
                            if (block.FatBlock != null)
                            {
                                // For ME
                                if (block.FatBlock is MyCompoundCubeBlock)
                                {
                                    var compoundBlock = block.FatBlock as MyCompoundCubeBlock;
                                    foreach (var blockInCompound in compoundBlock.GetBlocks())
                                    {
                                        HkShape shape = blockInCompound.FatBlock.ModelCollision.HavokCollisionShapes[0];
                                        DebugDrawShape(blockInCompound.BlockDefinition.Id.SubtypeName, shape, blockInCompound.FatBlock.PositionComp.WorldMatrix);
                                    }
                                    continue;
                                }

                                if (block.FatBlock.ModelCollision.HavokCollisionShapes != null)
                                {
                                    foreach(HkShape blockShape in block.FatBlock.ModelCollision.HavokCollisionShapes)
                                    //HkShape blockShape = block.FatBlock.ModelCollision.HavokCollisionShapes[0];
                                        DebugDrawShape(block.BlockDefinition.Id.SubtypeName, blockShape, block.FatBlock.PositionComp.WorldMatrix);
                                }
                                else
                                {
                                

                                    //HkShape blockShape = block.FatBlock.GetPhysicsBody().GetShape();
                                    //DebugDrawShape(block.BlockDefinition.Id.SubtypeName, blockShape, block.FatBlock.PositionComp.WorldMatrix);
                                }
                            }
                            else
                            {
                                //HkShape blockShape = block.CubeGrid.Physics.RigidBody.GetShape();
                                //DebugDrawShape(block.BlockDefinition.Id.SubtypeName, blockShape, block.CubeGrid.PositionComp.WorldMatrix/* + MatrixD.CreateScale(block.Position)*/);

                               // MyRenderProxy.DebugDrawOBB(MatrixD.CreateScale(block.BlockDefinition.Size + 0.1f) * (gridsToExport[0].WorldMatrix + MatrixD.CreateScale(block.Position)), Color.Red, 0, true, false);
                            }
                        }
                }
            }
        }

        //Dictionary<MyRecastDetourPolygon, Color> m_visualPolygonNavmesh;
        struct Vertex
        {
            public Vector3D pos;
            public Color color;
        }
        List<Vertex> m_visualNavmesh = new List<Vertex>();
        List<Vertex> m_newVisualNavmesh = null;


        /// <summary>
        /// Draws happy green mesh
        /// </summary>
        /*public void DebugDrawNavmesh_SAFE()
        {
            if (m_visualNavmesh != null)
            {
                Vector3D vec0 = Vector3D.Zero, vec1 = Vector3D.Zero, vec2 = Vector3D.Zero;
                // TODO: new thread and locks
                for (int i = 0; i < m_visualNavmesh.Count; )
                {
                    if(i < m_visualNavmesh.Count)
                        vec0 = m_visualNavmesh[i++];
                    if(i < m_visualNavmesh.Count)
                        vec1 = m_visualNavmesh[i++];
                    if(i < m_visualNavmesh.Count)
                        vec2 = m_visualNavmesh[i++];

                    int index = (i - 3) / 3;
                    if (index >= 0 && index < m_colorVisualNavmesh.Count)
                        VRageRender.MyRenderProxy.DebugDrawTriangle(vec0, vec1, vec2, m_colorVisualNavmesh[(i - 3) / 3], false, false);
                }
            }
        }*/

        /*public void DebugDrawNavmesh()
        {
            //DebugDrawNavmesh_theRightWay();
            //DebugDrawNavmesh_SAFE();

            //var triangles = VRageRender.MyRenderProxy.PrepareDebugDrawTriangles();

            //Vector3D vec0 = Vector3D.Zero, vec1 = Vector3D.Zero, vec2 = Vector3D.Zero;
            //for (int i = 0; i < m_visualNavmesh.Count; )
            //{

            //    if (i < m_visualNavmesh.Count)
            //        vec0 = m_visualNavmesh[i++];
            //    if (i < m_visualNavmesh.Count)
            //        vec1 = m_visualNavmesh[i++];
            //    if (i < m_visualNavmesh.Count)
            //        vec2 = m_visualNavmesh[i++];

            //    int index = (i - 3) / 3;
            //    if (index >= 0 && index < m_colorVisualNavmesh.Count)
            //    {
            //        triangles.AddTriangle(vec0, vec1, vec2);
            //    }
            //}

            //if (m_visualNavmesh.Count > 0)
            //{
            //    VRageRender.MyRenderProxy.DebugDrawTriangles(triangles, MatrixD.Identity, Color.Green, true, true, true);
            //}
        }*/

        uint m_drawNavmeshID = VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED;
        private void DrawPersistentDebugNavmesh(bool force)
        {
            if (m_newVisualNavmesh != null)
            {
                m_visualNavmesh = m_newVisualNavmesh; 
                m_newVisualNavmesh = null;
                force = true;
            }
            if (force)
            {
                if (m_visualNavmesh.Count > 0)
                {
                    MyRenderMessageDebugDrawMesh mesh = VRageRender.MyRenderProxy.PrepareDebugDrawMesh();
                    Vertex vec0 = new Vertex(), vec1 = new Vertex(), vec2 = new Vertex();
                    // TODO: new thread and locks
                    //var pos = MySector.MainCamera.Position;
                    //var left = MySector.MainCamera.ViewMatrix.Left;
                    //var down = MySector.MainCamera.ViewMatrix.Down;
                    //var forward = MySector.MainCamera.ViewMatrix.Forward;
                    //vec0 = pos + 2*forward;
                    //vec1 = pos + 2*forward + 10*left;
                    //vec2 = pos + 2*forward + 10*down;
                    //mesh.AddTriangle(ref vec0, Color.White, ref vec2,Color.White, ref vec1, Color.White);
                    for (int i = mesh.VertexCount; i < m_visualNavmesh.Count; )
                    {

                        if (i < m_visualNavmesh.Count)
                            vec0 = m_visualNavmesh[i++];
                        if (i < m_visualNavmesh.Count)
                            vec1 = m_visualNavmesh[i++];
                        if (i < m_visualNavmesh.Count)
                            vec2 = m_visualNavmesh[i++];

                        mesh.AddTriangle(ref vec0.pos, vec0.color, ref vec1.pos, vec1.color, ref vec2.pos, vec2.color);
                    }

                    /*
                    if (m_drawNavmeshID != VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED)
                        HidePersistentDebugNavmesh();

                    m_drawNavmeshID = VRageRender.MyRenderProxy.DebugDrawMesh(mesh, MatrixD.Identity, Color.Green, true, true);
                    */

                    if (m_drawNavmeshID == VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED)
                        m_drawNavmeshID = VRageRender.MyRenderProxy.DebugDrawMesh(mesh, MatrixD.Identity, Color.Green, true, true);
                    else
                    {
                        //VRageRender.MyRenderProxy.DebugDrawUpdateMesh(m_drawNavmeshID, MyRenderProxy.PrepareDebugDrawMesh(), MatrixD.Identity, Color.Green, true, true);
                        VRageRender.MyRenderProxy.DebugDrawUpdateMesh(m_drawNavmeshID, mesh, MatrixD.Identity, Color.Green, true, true);
                    }
                }
                else HidePersistentDebugNavmesh();
            }
        }

        private void HidePersistentDebugNavmesh()
        {
            if (m_drawNavmeshID != VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED)
            {
                VRageRender.MyRenderProxy.RemoveRenderObject(m_drawNavmeshID);
                m_drawNavmeshID = VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED;
            }
        }


        /*
        public void DebugDrawNavmesh_theRightWay()
        {
            MyRenderMessageDebugDrawMesh mesh = VRageRender.MyRenderProxy.PrepareDebugDrawMesh();
            if (m_visualNavmesh != null)
            {
                Vector3D vec0 = Vector3D.Zero, vec1 = Vector3D.Zero, vec2 = Vector3D.Zero;
                // TODO: new thread and locks
                //var pos = MySector.MainCamera.Position;
                //var left = MySector.MainCamera.ViewMatrix.Left;
                //var down = MySector.MainCamera.ViewMatrix.Down;
                //var forward = MySector.MainCamera.ViewMatrix.Forward;
                //vec0 = pos + 2*forward;
                //vec1 = pos + 2*forward + 10*left;
                //vec2 = pos + 2*forward + 10*down;
                //mesh.AddTriangle(ref vec0, Color.White, ref vec2,Color.White, ref vec1, Color.White);
                for (int i = mesh.VertexCount; i < m_visualNavmesh.Count; )
                {

                    if (i < m_visualNavmesh.Count)
                        vec0 = m_visualNavmesh[i++];
                    if (i < m_visualNavmesh.Count)
                        vec1 = m_visualNavmesh[i++];
                    if (i < m_visualNavmesh.Count)
                        vec2 = m_visualNavmesh[i++];

                    int index = (i - 3) / 3;
                    if (index >= 0 && index < m_colorVisualNavmesh.Count)
                    {
                        mesh.AddTriangle(ref vec0, m_colorVisualNavmesh[(i - 3) / 3], ref vec1, m_colorVisualNavmesh[(i - 3) / 3], ref vec2, m_colorVisualNavmesh[(i - 3) / 3]);
                        //VRageRender.MyRenderProxy.DebugDrawTriangle(vec0, vec1, vec2, m_colorVisualNavmesh[(i - 3) / 3], false, false);
                    }
                }
                if (m_visualNavmesh.Count > 0)
                {
                    if (m_drawNavmeshID == VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED)
                        m_drawNavmeshID = VRageRender.MyRenderProxy.DebugDrawMesh(mesh, MatrixD.Identity, Color.Green, true, true);
                    else VRageRender.MyRenderProxy.DebugDrawUpdateMesh(m_drawNavmeshID, mesh, MatrixD.Identity, Color.Green, true, true);
                }
            }
        }
        */

        /// <summary>
        /// Generates the colors for the navmesh returned from PathEngine
        /// </summary>
        /*private void GeneratePhotorealisticNavmeshColor()
        {
            int greenBase = 30;
            int greenValue = 0;
            int maxExclusiveGreenValue = 55;
            int greenStep = 10;

            for (int i = 0; i < m_visualNavmesh.Count; )
            {
                m_visualNavmesh.Add(new Color(0, greenBase + greenValue, 0));
                greenValue += greenStep;
                greenValue %= maxExclusiveGreenValue;
            }
        }*/


        /// <summary>
        /// Just for debuggggggg
        /// </summary>
        private static unsafe Vector3* GetMiddleOBBPoints(MyOrientedBoundingBoxD obb, ref Vector3* points)
        {
            Vector3 xDiff = obb.Orientation.Right * (float) obb.HalfExtent.X;
            Vector3 zDiff = obb.Orientation.Forward * (float) obb.HalfExtent.Z;

            points[0] = obb.Center - xDiff - zDiff;
            points[1] = obb.Center + xDiff - zDiff;
            points[2] = obb.Center + xDiff + zDiff;
            points[3] = obb.Center - xDiff + zDiff;

            return points;
        }

        /// <summary>
        /// Just for debuggggggg
        /// </summary>
        private static bool DrawTerrainLimits(MyPlanet planet, MyOrientedBoundingBoxD obb)
        {
            float minHeight, maxHeight;
            int pointCount = 4;
            unsafe
            {
                Vector3* points = stackalloc Vector3[4];
                GetMiddleOBBPoints(obb, ref points);
                planet.Provider.Shape.GetBounds(points, pointCount, out minHeight, out maxHeight);
            }

            if (minHeight.IsValid() && maxHeight.IsValid())
            {
                Vector3D minPoint = obb.Orientation.Up * minHeight;// +planet.PositionComp.WorldAABB.Center;
                Vector3D maxPoint = obb.Orientation.Up * maxHeight;// +planet.PositionComp.WorldAABB.Center;

                obb.Center = minPoint + (maxPoint - minPoint) * 0.5f;
                obb.HalfExtent.Y = (maxHeight - minHeight) * 0.5f;

                MyRenderProxy.DebugDrawOBB(new MyOrientedBoundingBoxD(obb.Center, obb.HalfExtent, obb.Orientation), Color.Blue, 0, true, false);

                return true;
            }

            return false;
        }

        private void DebugDrawInternal()
        {
            if (m_navmeshOBBs != null)
            {
                m_navmeshOBBs.DebugDraw();

                /*
                var pos = MySector.MainCamera.Position;
                var dir = MySector.MainCamera.ForwardVector;
                var hit = MyPhysics.CastRay(pos, pos + 500 * dir);
                if (hit.HasValue)
                {
                    ProfilerShort.Begin("NavmeshOBB GetOBB");
                    var obb = m_navmeshOBB.GetOBB(hit.Value.Position);
                    ProfilerShort.End();
                    if (obb.HasValue)
                        MyRenderProxy.DebugDrawSphere(obb.Value.Center, 10f, Color.Yellow, 0, true);
                }
                 */
            }
        

            if (DrawNavmesh)
                DrawPersistentDebugNavmesh(false);

            if (DrawPhysicalMesh)
                DebugDrawPhysicalShapes();

            var position = MySession.Static.ControlledEntity.ControllerInfo.Controller.Player.GetPosition();
            Vector3D subtitlePosition = position;
            subtitlePosition.Y += 2.4f;
            VRageRender.MyRenderProxy.DebugDrawText3D(subtitlePosition, String.Format("X: {0}\nY: {1}\nZ: {2}", Math.Round(subtitlePosition.X, 2), Math.Round(subtitlePosition.Y, 2), Math.Round(subtitlePosition.Z, 2)), Color.Red, 1, true);

            if (m_lastGroundMeshQuery.Count > 0)
            {
                MyRenderProxy.DebugDrawSphere(m_lastGroundMeshQuery[0].Center, 1f, Color.Yellow, 1, true);
                    
                foreach (var bb in m_lastGroundMeshQuery)
                    MyRenderProxy.DebugDrawOBB(bb.Matrix, Color.Yellow, 0, true, false);


                if (m_navmeshOBBs != null)
                {
                    foreach (var obbPair in m_debugDrawIntersectedOBBs)
                        MyRenderProxy.DebugDrawOBB(new MyOrientedBoundingBoxD(obbPair.OBB.Center, 
                                                                              new Vector3(obbPair.OBB.HalfExtent.X, obbPair.OBB.HalfExtent.Y / 2, obbPair.OBB.HalfExtent.Z),
                                                                              obbPair.OBB.Orientation), 
                                                   Color.White, 0, true, false);


                    MyOrientedBoundingBoxD o = m_navmeshOBBs.GetOBB(0, 0).Value;
                    float minHeight, maxHeight;

                    unsafe 
                    {
                        MyPlanet planet = GetPlanet(o.Center);
                        Vector3* points = stackalloc Vector3[4];
                        GetMiddleOBBPoints(o, ref points);
                        planet.Provider.Shape.GetBounds(points, 4, out minHeight, out maxHeight);

                        if (minHeight.IsValid() && maxHeight.IsValid())
                        {
                            Vector3D minPoint = o.Orientation.Up * minHeight;// +planet.PositionComp.WorldAABB.Center;
                            Vector3D maxPoint = o.Orientation.Up * maxHeight;// +planet.PositionComp.WorldAABB.Center;

                            MyRenderProxy.DebugDrawSphere(minPoint, 1, Color.Blue, 0, true);
                            MyRenderProxy.DebugDrawSphere(maxPoint, 1, Color.Blue, 0, true);
                        }

                        DrawTerrainLimits(planet, o);
                    }
                    /*
                    for (int i = 0; i < m_navmeshOBBs.OBBsPerLine; i++)
                        for (int j = 0; j < m_navmeshOBBs.OBBsPerLine; j++)
                        {
                            MyOrientedBoundingBoxD? obb = m_navmeshOBBs.GetOBB(j, i);
                            MyRenderProxy.DebugDrawOBB(new MyOrientedBoundingBoxD(obb.Value.Center, obb.Value.HalfExtent, obb.Value.Orientation), Color.Red, 0, true, false);
                        }
                     */
                }

                MyRenderProxy.DebugDrawSphere(m_meshCenter, 2f, Color.Red, 0, true);
            }

/*
            if (m_hasMesh && m_visualNavmesh == null)
            {
                if (m_polyMesh.ntris > 0)
                {
                    ProfilerShort.Begin("MyExternalPathfinding.BuildVisualNavmesh");
                    m_visualNavmesh = new List<Vector3D>();

                    /////////////////////////////////////
                    //MyCubeGrid targetGrid = null;
                    MatrixD transformationMatrix = MatrixD.Identity;
                    Vector3D translation = Vector3D.Zero;
                    Vector3D increaseMeshHeight = Vector3D.Zero;

                    if (MyPerGameSettings.Game == GameEnum.SE_GAME)
                    {
                        Vector3D pos;
                        MyCubeGrid targetGrid = MyCubeGrid.GetTargetGrid();

                        if (targetGrid != null)
                            pos = targetGrid.PositionComp.GetPosition();
                        else
                            pos = MySpectator.Static.Position;

                        Vector3D gravityVector = -Vector3D.Normalize(GameSystems.MyGravityProviderSystem.CalculateTotalGravityInPoint(pos));

                        //
                        increaseMeshHeight = 0.3f * Vector3D.Normalize(gravityVector);
                        //

                        translation = pos;
                                
                        var fwd = Vector3D.CalculatePerpendicularVector(gravityVector);

                        Quaternion quaternion = Quaternion.CreateFromForwardUp(fwd, gravityVector);
                        transformationMatrix = MatrixD.CreateFromQuaternion(quaternion);
                    }
                    /////////////////////////////////////////////

                    unsafe
                    {

                        for (int i = 0; i < m_polyMesh.nmeshes; ++i)
	                    {
                            uint* m = &m_polyMesh.meshes[i * 4];
		                    uint bverts = m[0];
		                    uint btris = m[2];
		                    int ntris = (int)m[3];
                            float* verts = &m_polyMesh.verts[bverts * 3];
                            byte* tris = &m_polyMesh.tris[btris * 4];

		                    for (int j = 0; j < ntris; ++j)
		                    {
                                for (int n = 0; n < 3; n++)
                                {
                                    float* v = &verts[tris[j * 4 + n] * 3];
                                    Vector3D vec = new Vector3(v[0], v[1], v[2]);

                                    if (MyPerGameSettings.Game == GameEnum.SE_GAME)
                                        vec = Vector3D.Transform(vec, transformationMatrix) + increaseMeshHeight + translation;

                                    m_visualNavmesh.Add(vec);
                                }
		                    }
	                    }
  
                    }
                    //GeneratePhotorealisticNavmeshColor();
                        
                    ProfilerShort.End();
                }
            }*/

            // Show me thy mesh!
            /*if (m_hasMesh && m_visualNavmesh != null)
            {
                ProfilerShort.Begin("MyExternalPathfinding.DrawVisualNavmesh");

                //if (DrawNavmesh)
                //    DebugDrawNavmesh();

                ProfilerShort.End();
            }
            else */if (m_polygons != null)
            {

                if (m_pathfindingDebugTarget.HasValue)
                {
                    Vector3D gravityVector = -Vector3D.Normalize(GameSystems.MyGravityProviderSystem.CalculateTotalGravityInPoint(m_pathfindingDebugTarget.Value));
                    VRageRender.MyRenderProxy.DebugDrawSphere(m_pathfindingDebugTarget.Value + 1.5f * gravityVector, 0.2f, Color.Red, 0, true);

                    // Testing World coordinates transformation and back
                    /*
                    var vec = WorldPositionToLocalNavmeshPosition(m_pathfindingDebugTarget.Value, 0);
                    vec = LocalNavmeshPositionToWorldPosition(vec, m_meshCenter, Vector3D.Zero);
                    VRageRender.MyRenderProxy.DebugDrawSphere(vec, 0.2f, Color.Orange, 0, true);
                    */

                    // Testing Planet Transformations


                }

                //if (DrawNavmesh)
                //    DebugDrawNavmesh();
            }

            
        }
        #endregion
    }
}
