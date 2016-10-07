using RecastDetour;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Entity;
using VRage.Profiler;
using VRageMath;
using VRageRender.Messages;

namespace Sandbox.Game.AI.Pathfinding
{
    public class MyNavmeshManager
    {
        #region CoordComparer class
        public class CoordComparer : IEqualityComparer<Vector2I>
        {
            public bool Equals(Vector2I a, Vector2I b)
            {
                return a.X == b.X && a.Y == b.Y;
            }

            public int GetHashCode(Vector2I point)
            {
                return (point.X.ToString() + point.Y.ToString()).GetHashCode();
            }
        }
        #endregion

        #region OBBCoordComparer class
        public class OBBCoordComparer : IEqualityComparer<MyNavmeshOBBs.OBBCoords>
        {
            public bool Equals(MyNavmeshOBBs.OBBCoords a, MyNavmeshOBBs.OBBCoords b)
            {
                return a.Coords.X == b.Coords.X && a.Coords.Y == b.Coords.Y;
            }

            public int GetHashCode(MyNavmeshOBBs.OBBCoords point)
            {
                return (point.Coords.X.ToString() + point.Coords.Y.ToString()).GetHashCode();
            }
        }
        #endregion

        #region Vertex struct
        struct Vertex
        {
            public Vector3D pos;
            public Color color;
        }
        #endregion

        #region Fields


        const float RECAST_CELL_SIZE = 0.2f;
        /// <summary>
        /// The maximum number of tiles that, each time, can be added to navmesh generation
        /// </summary>
        const int MAX_TILES_TO_GENERATE = 7;
        // 10 seconds - 500 ticks
        const int MAX_TICKS_WITHOUT_HEARTBEAT = 5000;

        // Stores the update "ticks" after the last path request
        int m_ticksAfterLastPathRequest = 0;
        int m_tileSize, m_tileHeight, m_tileLineCount;
        float m_border;
        float m_heightCoordTransformationIncrease = 0;
        bool m_allTilesGenerated = false;
        bool m_isManagerAlive = true;
        MyNavmeshOBBs m_navmeshOBBs;
        MyRecastOptions m_recastOptions;
        MyNavigationInputMesh m_navInputMesh;
        HashSet<MyNavmeshOBBs.OBBCoords> m_obbCoordsToUpdate = new HashSet<MyNavmeshOBBs.OBBCoords>(new OBBCoordComparer());
        HashSet<Vector2I> m_coordsAlreadyGenerated = new HashSet<Vector2I>(new CoordComparer());
        Dictionary<Vector2I, List<Vertex>> m_obbCoordsPolygons = new Dictionary<Vector2I, List<Vertex>>();
        Dictionary<Vector2I, List<Vertex>> m_newObbCoordsPolygons = new Dictionary<Vector2I, List<Vertex>>();
        bool m_navmeshTileGenerationRunning = false;
        MyRDWrapper m_rdWrapper;
        // Used to obtain the the border point of the intersection of an internal point with an outside point (Coords transformations)
        MyOrientedBoundingBoxD m_extendedBaseOBB;
        List<MyVoxelMap> m_tmpTrackedVoxelMaps = new List<MyVoxelMap>();
        Dictionary<long, MyVoxelMap> m_trackedVoxelMaps = new Dictionary<long, MyVoxelMap>();

        //TODO: 2B refactored
        int?[][] m_debugTileSize;
        bool m_drawMesh;
        bool m_updateDrawMesh = false;
        List<MyRecastDetourPolygon> m_polygons = new List<MyRecastDetourPolygon>();
        List<BoundingBoxD> m_groundCaptureAABBs = new List<BoundingBoxD>();
        #endregion

        #region Properties
        public Vector3D Center { get { return m_navmeshOBBs.CenterOBB.Center; } }

        public MyOrientedBoundingBoxD CenterOBB { get { return m_navmeshOBBs.CenterOBB; } }

        public MyPlanet Planet { get; private set; }

        public bool TilesAreWaitingGeneration { get { return m_obbCoordsToUpdate.Count > 0; } }

        public bool DrawNavmesh
        {
            get { return m_drawMesh; }
            set
            {
                m_drawMesh = value;
                if (m_drawMesh)
                    DrawPersistentDebugNavmesh();
                else
                    HidePersistentDebugNavmesh();
            }
        }
        #endregion

        #region Constructor
        public MyNavmeshManager(MyRDPathfinding rdPathfinding, Vector3D center, Vector3D forwardDirection, int tileSize, int tileHeight, int tileLineCount, MyRecastOptions recastOptions)
        {
            m_tileSize = tileSize;
            m_tileHeight = tileHeight;
            m_tileLineCount = tileLineCount;
            Planet = GetPlanet(center);

            m_heightCoordTransformationIncrease = 0.5f;

            float cellSize = RECAST_CELL_SIZE;
            m_recastOptions = recastOptions;

            float horizontalOrigin = (m_tileSize * 0.5f + m_tileSize * (float)Math.Floor(m_tileLineCount * 0.5f));
            var verticalOrigin = m_tileHeight * 0.5f;
            m_border = m_recastOptions.agentRadius + 3 * cellSize;

            float[] bmin = new float[3] { -horizontalOrigin, -verticalOrigin, -horizontalOrigin };
            float[] bmax = new float[3] { horizontalOrigin, verticalOrigin, horizontalOrigin };

            m_rdWrapper = new MyRDWrapper();
            m_rdWrapper.Init(cellSize, m_tileSize, bmin, bmax);

            Vector3D gravityVector = -Vector3D.Normalize(GameSystems.MyGravityProviderSystem.CalculateTotalGravityInPoint(center));
            var direction = Vector3D.CalculatePerpendicularVector(gravityVector);

            m_navmeshOBBs = new MyNavmeshOBBs(Planet, center, direction, m_tileLineCount, m_tileSize, m_tileHeight);
            m_debugTileSize = new int?[m_tileLineCount][];
            for (int i = 0; i < m_tileLineCount; i++)
                m_debugTileSize[i] = new int?[m_tileLineCount];

                m_extendedBaseOBB = new MyOrientedBoundingBoxD(m_navmeshOBBs.BaseOBB.Center,
                                                              new Vector3D(m_navmeshOBBs.BaseOBB.HalfExtent.X, m_tileHeight, m_navmeshOBBs.BaseOBB.HalfExtent.Z),
                                                              m_navmeshOBBs.BaseOBB.Orientation);

            m_navInputMesh = new MyNavigationInputMesh(rdPathfinding, Planet, center);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Checks if the manager intersects the given OBB.
        /// </summary>
        /// <param name="areaAABB"></param>
        /// <returns></returns>
        public bool InvalidateArea(BoundingBoxD areaAABB)
        {
            bool areaInvalidated = false;

            if (!Intersects(areaAABB))
                return areaInvalidated;

            // Does a search kinda optimized - search each line until it finds an intersected obb and if 
            // any of the following does not intersect, go to next line same thing for column
            bool iIntersectionFound = false;
            for (int i = 0; i < m_tileLineCount; i++)
            {
                bool iLocalIntersection = false;
                bool jIntersectionFound = false;
                for (int j = 0; j < m_tileLineCount; j++)
                {
                    var obb = m_navmeshOBBs.GetOBB(i, j);
                    if (obb.Value.Intersects(ref areaAABB))
                    {
                        var coords = new Vector2I(i, j);
                        iLocalIntersection = jIntersectionFound = true;
                        if (m_coordsAlreadyGenerated.Remove(coords))
                        {
                            areaInvalidated = true;
                            m_allTilesGenerated = false;

                            var tileNavmeshCoords = WorldPositionToLocalNavmeshPosition(obb.Value.Center, m_heightCoordTransformationIncrease);

                            // Removes the debug navmesh polygons
                            //m_obbCoordsPolygons.Remove(coords);
                            m_newObbCoordsPolygons[coords] = null;

                            // Don't remove the tile, it will be regenerated when needed...
                            ///////m_rdWrapper.RemoveTile(tileNavmeshCoords, 0);

                            m_navInputMesh.InvalidateCache(areaAABB);
                        }
                    }
                    else if (jIntersectionFound)
                        break;
                }

                if (iLocalIntersection)
                    iIntersectionFound = true;
                else if (iIntersectionFound)
                    break;
            }

            if (areaInvalidated)
                m_updateDrawMesh = true;

            return areaInvalidated;
        }

        /// <summary>
        /// Checks if the given point is within the bounds of the navmesh
        /// </summary>
        /// <param name="position"></param>
        /// <returns>Returns true if the point is within bounds</returns>
        public bool ContainsPosition(Vector3D position)
        {
            LineD planetCenterToPositionLine = new LineD(Planet.PositionComp.WorldAABB.Center, position);
            return m_navmeshOBBs.BaseOBB.Intersects(ref planetCenterToPositionLine).HasValue;
        }

        /// <summary>
        /// Saves the tiles that need to be generated
        /// </summary>
        /// <param name="initialPosition"></param>
        /// <param name="targetPosition"></param>
        /// <returns></returns>
        public void TilesToGenerate(Vector3D initialPosition, Vector3D targetPosition)
        {
            int tilesAddedToGeneration;
            TilesToGenerateInternal(initialPosition, targetPosition, out tilesAddedToGeneration);
        }

        /// <summary>
        /// Delivers the path and returns true if the path contains the target position
        /// </summary>
        /// <param name="initialPosition"></param>
        /// <param name="targetPosition"></param>
        /// <param name="path"></param>
        /// <param name="finalPath">The returned path is final - "targetPosition was reached" OR "cannot be reached"</param>
        /// <returns></returns>
        public bool GetPathPoints(Vector3D initialPosition, Vector3D targetPosition, out List<Vector3D> path, out bool noTilesToGenerate)
        {
            Heartbeat();

            bool pathContainsTarget = false;
            bool fullManagerInternalPath = false;
            noTilesToGenerate = true;
            path = new List<Vector3D>();

            // Generate tiles along line
            if (!m_allTilesGenerated)
            {
                int tilesAddedToGeneration;
                ProfilerShort.Begin("MyNavmeshManager.TilesToGenerate");
                List<MyNavmeshOBBs.OBBCoords> intersectedOBBs = TilesToGenerateInternal(initialPosition, targetPosition, out tilesAddedToGeneration);
                ProfilerShort.End();

                noTilesToGenerate = tilesAddedToGeneration == 0;
            }

            #region Get Path
            Vector3D iniPos = WorldPositionToLocalNavmeshPosition(initialPosition, m_heightCoordTransformationIncrease);
            Vector3D worldEndPosition = targetPosition;
            bool targetPositionOutsideManager = !ContainsPosition(targetPosition);
            if (targetPositionOutsideManager)
            {
                worldEndPosition = GetBorderPoint(initialPosition, targetPosition);
                worldEndPosition = GetPositionAtDistanceFromPlanetCenter(worldEndPosition, (initialPosition - Planet.PositionComp.WorldAABB.Center).Length());
            }
            Vector3D endPos = WorldPositionToLocalNavmeshPosition(worldEndPosition, m_heightCoordTransformationIncrease);

            var localPath = m_rdWrapper.GetPath(iniPos, endPos);
            #endregion

            if (localPath.Count > 0)
            {
                foreach (var point in localPath)
                    path.Add(LocalPositionToWorldPosition(point));

                Vector3D lastPosition = path.Last();
                double endOfPathTargetDistance = (worldEndPosition - lastPosition).Length();
                // The max acceptable distance between the last point of the path and the target position 
                //TODO: define this where??
                double maxAcceptableDistance = 0.25;
                fullManagerInternalPath = endOfPathTargetDistance <= maxAcceptableDistance;
                pathContainsTarget = fullManagerInternalPath && !targetPositionOutsideManager;
                
                if (fullManagerInternalPath)
                {
                    // If the target position is outside the manager and the path contains the last point -> substitute the last point by the original one
                    if (targetPositionOutsideManager)
                    {
                        path.RemoveAt(path.Count - 1);
                        path.Add(targetPosition);
                    }
                    else if (noTilesToGenerate)
                    // If the path is too long, try generate tiles around the initial position
                    {
                        double pathDistance = GetPathDistance(path);
                        double shortestDistance = Vector3D.Distance(initialPosition, targetPosition);

                        if (pathDistance > 3 * shortestDistance)
                            noTilesToGenerate = !TryGenerateTilesAroundPosition(initialPosition);
                    }
                }

                // Generate tiles around position
                if (!fullManagerInternalPath &&
                     !m_allTilesGenerated &&
                     noTilesToGenerate)
                {
                    noTilesToGenerate = !TryGenerateTilesAroundPosition(lastPosition);
                }
            }

            return pathContainsTarget;
        }
        
        /// <summary>
        /// Updates the navmesh manager by generating the next necessary tile and updates the debug mesh.
        /// Returns false if the manager is no longer valid - it was unloaded.
        /// </summary>
        /// <returns></returns>
        public bool Update()
        {
            if (!CheckManagerHeartbeat())
                return false;

            GenerateNextQueuedTile();

            if(m_updateDrawMesh)
            {
                m_updateDrawMesh = false;
                UpdatePersistentDebugNavmesh();
            }

            return true;
        }

        /// <summary>
        /// Clears the data
        /// </summary>
        public void UnloadData()
        {
            m_isManagerAlive = false;

            foreach(var map in m_trackedVoxelMaps)
                map.Value.RangeChanged -= VoxelMapRangeChanged;
            m_trackedVoxelMaps.Clear();

            m_rdWrapper.Clear();
            m_rdWrapper = null;

            m_navInputMesh.Clear();
            m_navInputMesh = null;

            m_navmeshOBBs.Clear();
            m_navmeshOBBs = null;

            m_obbCoordsToUpdate.Clear();
            m_obbCoordsToUpdate = null;

            m_coordsAlreadyGenerated.Clear();
            m_coordsAlreadyGenerated = null;

            m_obbCoordsPolygons.Clear();
            m_obbCoordsPolygons = null;

            m_newObbCoordsPolygons.Clear();
            m_newObbCoordsPolygons = null;

            m_polygons.Clear();
            m_polygons = null;
        }   

        public void DebugDraw()
        {
            m_navmeshOBBs.DebugDraw();
            m_navInputMesh.DebugDraw();
            VRageRender.MyRenderProxy.DebugDrawOBB(m_extendedBaseOBB, Color.White, 0, true, false);

            foreach (var box in m_groundCaptureAABBs)
                VRageRender.MyRenderProxy.DebugDrawAABB(box, Color.Yellow);

            // Draw tile byte size
            /*
            for (int i = 0; i < m_debugTileSize.Length; i++)
                for (int j = 0; j < m_debugTileSize.Length; j++)
                {
                    int? val = m_debugTileSize[i][j];
                    if(val.HasValue)
                    {
                        var obb = m_navmeshOBBs.GetOBB(i, j);
                        if (obb.HasValue)
                            VRageRender.MyRenderProxy.DebugDrawText3D(obb.Value.Center, val.Value.ToString(), Color.Yellow, 1, true);
                    }
                }
             * */
        }
        #endregion

        #region Private Methods
        #region Coords transformation
        private Vector3D LocalPositionToWorldPosition(Vector3D position)
        {
            //TODO: review this
            Vector3D center = position;
            if (m_navmeshOBBs != null)
                center = Center;
            //

            Vector3D gravityVector = -Vector3D.Normalize(GameSystems.MyGravityProviderSystem.CalculateTotalGravityInPoint(center));
            return LocalNavmeshPositionToWorldPosition(m_navmeshOBBs.CenterOBB, position, center, -m_heightCoordTransformationIncrease * gravityVector);//Vector3D.Zero);
        }

        private MatrixD LocalNavmeshPositionToWorldPositionTransform(MyOrientedBoundingBoxD obb, Vector3D center)
        {
            // TODO: should use the obb?

            Vector3D gravityVector = -Vector3D.Normalize(GameSystems.MyGravityProviderSystem.CalculateTotalGravityInPoint(center));
            var fwd = Vector3D.CalculatePerpendicularVector(gravityVector);
            Quaternion quaternion = Quaternion.CreateFromForwardUp(fwd, gravityVector);

            return MatrixD.CreateFromQuaternion(quaternion);
        }

        private Vector3D LocalNavmeshPositionToWorldPosition(MyOrientedBoundingBoxD obb, Vector3D position, Vector3D center, Vector3D heightIncrease)
        {
            var transformationMatrix = LocalNavmeshPositionToWorldPositionTransform(obb, center);

            Vector3D transformedPosition = Vector3D.Transform(position, transformationMatrix) + Center + heightIncrease;// center;
            return transformedPosition;
        }

        private Vector3D WorldPositionToLocalNavmeshPosition(Vector3D position, float heightIncrease)
        {
            //TODO: review this
            /*
            Vector3D center;
            var obb = m_navmeshOBBs.GetOBB(position);
            if (obb != null)
                center = obb.Value.Center;
            else
                center = Center;
              //
            */
            Vector3D gravityVector = -Vector3D.Normalize(GameSystems.MyGravityProviderSystem.CalculateTotalGravityInPoint(Center));
            Vector3D forwardVector = Vector3D.CalculatePerpendicularVector(gravityVector);
            Quaternion quaternion = Quaternion.CreateFromForwardUp(forwardVector, gravityVector);
            MatrixD rotationMatrix = MatrixD.CreateFromQuaternion(Quaternion.Inverse(quaternion));

            Vector3D transPosition = (position - Center) + heightIncrease * gravityVector;
            transPosition = Vector3D.Transform(transPosition, rotationMatrix);
            return transPosition;
        }

        private Vector3D GetBorderPoint(Vector3D startingPoint, Vector3D outsidePoint)
        {
            LineD line = new LineD(startingPoint, outsidePoint);
            double? intersection = m_extendedBaseOBB.Intersects(ref line);

            if (!intersection.HasValue)
                return outsidePoint;

            //intersection is less 1m than the border
            line.Length = intersection.Value - 1;
            line.To = startingPoint + line.Direction * intersection.Value;

            return line.To;
        }
        #endregion

        /// <summary>
        /// Updates the heartbeat in order to keep the manager alive
        /// </summary>
        private void Heartbeat()
        {
            m_ticksAfterLastPathRequest = 0;
        }

        /// <summary>
        /// Returns true if the manager is still alive 
        /// </summary>
        /// <returns></returns>
        private bool CheckManagerHeartbeat()
        {
            if (!m_isManagerAlive)
                return false;

            m_ticksAfterLastPathRequest++;
            m_isManagerAlive = m_ticksAfterLastPathRequest < MAX_TICKS_WITHOUT_HEARTBEAT;

            if (!m_isManagerAlive)
                UnloadData();

            return m_isManagerAlive;
        }

        /// <summary>
        /// Returns the distance of the given path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private double GetPathDistance(List<Vector3D> path)
        {
            double distance = 0;
            for (int i = 0; i < path.Count - 1; i++)
                distance += Vector3D.Distance(path[i], path[i + 1]);

            return distance;
        }

        /// <summary>
        /// Checks if the manager intersects the given OBB.
        /// </summary>
        /// <param name="obb"></param>
        /// <returns></returns>
        private bool Intersects(BoundingBoxD obb)
        {
            //TODO: is this good enough?
            return m_extendedBaseOBB.Intersects(ref obb);
        }

        /// <summary>
        /// Generated tiles around position. EPIC COMMENT. I LIKE BANANAS (Who wrote this?!)
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private bool TryGenerateTilesAroundPosition(Vector3D position)
        {
            MyNavmeshOBBs.OBBCoords? obbCoord = m_navmeshOBBs.GetOBBCoord(position);
            if (obbCoord.HasValue)
                return TryGenerateNeighbourTiles(obbCoord.Value);

            return false;
        }

        /// <summary>
        /// Generate tiles around the obbCoord. Each time, a bigger "circle" around it.
        /// </summary>
        /// <param name="obbCoord"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        private bool TryGenerateNeighbourTiles(MyNavmeshOBBs.OBBCoords obbCoord, int radius = 1)
        {
            int tilesAddedToGeneration = 0;
            bool validTiles = false;
            Vector2I newCoord;
            for (int i = -radius; i <= radius; i++)
            {
                // Get the neigbour tiles
                int jump = i == -radius || i == radius ? 1 : 2 * radius;
                for (int j = -radius; j <= radius; j += jump)
                {
                    newCoord.X = obbCoord.Coords.X + j;
                    newCoord.Y = obbCoord.Coords.Y + i;
                    var surroundingOBBCoord = m_navmeshOBBs.GetOBBCoord(newCoord.X, newCoord.Y);
                    if (surroundingOBBCoord.HasValue)
                    {
                        validTiles = true;
                        if (AddTileToGeneration(surroundingOBBCoord.Value))
                        {
                            tilesAddedToGeneration++;
                            if (tilesAddedToGeneration >= MAX_TILES_TO_GENERATE)
                                return true;
                        }
                    }
                }
            }

            if (tilesAddedToGeneration > 0)
                return true;

            m_allTilesGenerated = !validTiles;
            if (m_allTilesGenerated)
                return false;
            else
                return TryGenerateNeighbourTiles(obbCoord, radius + 1);
        }

        /// <summary>
        /// Saves the tiles that need to be generated and returns the full intersected OBB list
        /// </summary>
        /// <param name="initialPosition"></param>
        /// <param name="targetPosition"></param>
        /// /// <param name="tilesAddedToGeneration"></param>
        /// <returns></returns>
        private List<MyNavmeshOBBs.OBBCoords> TilesToGenerateInternal(Vector3D initialPosition, Vector3D targetPosition, out int tilesAddedToGeneration)
        {
            tilesAddedToGeneration = 0;
            var intersectedOBBs = m_navmeshOBBs.GetIntersectedOBB(new LineD(initialPosition, targetPosition));
            foreach (var obbCoord in intersectedOBBs)
                if (AddTileToGeneration(obbCoord))
                {
                    tilesAddedToGeneration++;
                    // Warning: it will only add a max of MAX_TILES_TO_GENERATE tile each time! (it's better for parallel path requests)
                    if (tilesAddedToGeneration == MAX_TILES_TO_GENERATE)
                        break;
                }
            return intersectedOBBs;
        }

        /// <summary>
        /// Adds the file the be generation, if it wasn't generated before.
        /// </summary>
        /// <param name="obbCoord"></param>
        /// <returns>True if it was added to the list of the tiles to be generated</returns>
        private bool AddTileToGeneration(MyNavmeshOBBs.OBBCoords obbCoord)
        {
            if (!m_coordsAlreadyGenerated.Contains(obbCoord.Coords))
                return m_obbCoordsToUpdate.Add(obbCoord);
            return false;
        }

        /// <summary>
        /// Returns a new position moved along the gravity vector by distance amount
        /// </summary>
        /// <param name="position"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        private Vector3D GetPositionAtDistanceFromPlanetCenter(Vector3D position, double distance)
        {
            //TODO: PROBLEM - it may be too low or too high!
            var positionDistance = (position - Planet.PositionComp.WorldAABB.Center).Length();
            var gravityVector = -Vector3D.Normalize(GameSystems.MyGravityProviderSystem.CalculateTotalGravityInPoint(position));
            return gravityVector * distance + Planet.PositionComp.WorldAABB.Center;
        }

        /// <summary>
        /// Returns the planet closest to the given position
        /// </summary>
        /// <param name="position">3D Point from where the search is started</param>
        /// <returns>The closest planet</returns>
        private MyPlanet GetPlanet(Vector3D position)
        {
            int voxelDistance = 200;
            BoundingBoxD box = new BoundingBoxD(position - voxelDistance * 0.5f, position + voxelDistance * 0.5f);
            return MyGamePruningStructure.GetClosestPlanet(ref box);
        }

        /// <summary>
        /// Generates the next tile in the queue
        /// </summary>
        private void GenerateNextQueuedTile()
        {
            if (!m_navmeshTileGenerationRunning && TilesAreWaitingGeneration)
            {
                m_navmeshTileGenerationRunning = true;

                var obb = m_obbCoordsToUpdate.First();
                m_obbCoordsToUpdate.Remove(obb);
                m_coordsAlreadyGenerated.Add(obb.Coords);

                ParallelTasks.Parallel.Start(() => { GenerateTile(obb); });
            }
        }

        /// <summary>
        /// Generates a navmesh tile
        /// </summary>
        /// <param name="obbCoord"></param>
        private void GenerateTile(MyNavmeshOBBs.OBBCoords obbCoord)
        {
            ProfilerShort.Begin("LET THERE BE TILE!");

            var obb = obbCoord.OBB;
            Vector3 localCenter = WorldPositionToLocalNavmeshPosition(obb.Center, 0);

            ProfilerShort.Begin("GetWorldVertices!!!");
            List<BoundingBoxD> bbs = new List<BoundingBoxD>();

            
            var worldVertices = m_navInputMesh.GetWorldVertices(m_border, Center, obb, bbs, m_tmpTrackedVoxelMaps);
            m_groundCaptureAABBs = bbs;

            foreach (var map in m_tmpTrackedVoxelMaps)
                if (!m_trackedVoxelMaps.ContainsKey(map.EntityId))
                {
                    map.RangeChanged += VoxelMapRangeChanged;
                    m_trackedVoxelMaps.Add(map.EntityId, map);
                }

            m_tmpTrackedVoxelMaps.Clear();

            ProfilerShort.End();

            if (worldVertices.Triangles.Count > 0)
            {
                unsafe
                {
                    fixed (Vector3* vertices = worldVertices.Vertices.GetInternalArray())
                    {
                        float* verticesPointer = (float*)vertices;
                        fixed(int* trianglesPointer = worldVertices.Triangles.GetInternalArray())
                        {
                            ProfilerShort.Begin("GrdWrapper.CreateNavmeshTile CALL");
                            m_rdWrapper.CreateNavmeshTile(localCenter, ref m_recastOptions, ref m_polygons, obbCoord.Coords.X, obbCoord.Coords.Y, 0, verticesPointer, worldVertices.Vertices.Count, trianglesPointer, worldVertices.Triangles.Count / 3);
                            //Gets the triangles mesh that is sent to Recast
                            //m_rdWrapper.DebugGetPolygonsFromInputTriangles(verticesPointer, worldVertices.Vertices.Count, trianglesPointer, worldVertices.Triangles.Count / 3, m_polygons);
                            
                            ProfilerShort.End();
                        }    
                    }
                }

                ProfilerShort.Begin("GenerateDebugDrawPolygonNavmesh");
                GenerateDebugDrawPolygonNavmesh(Planet, m_polygons, m_navmeshOBBs.CenterOBB, obbCoord.Coords);           
                ProfilerShort.End();

                GenerateDebugTileDataSize(localCenter, obbCoord.Coords.X, obbCoord.Coords.Y); 

                if (m_polygons != null)
                {
                    m_polygons.Clear();
                    m_updateDrawMesh = true;
                }
            }
            else
                m_newObbCoordsPolygons[obbCoord.Coords] = null;

            m_navmeshTileGenerationRunning = false;
            ProfilerShort.End();
        }

        void VoxelMapRangeChanged(MyVoxelBase storage, Vector3I minVoxelChanged, Vector3I maxVoxelChanged, VRage.Voxels.MyStorageDataTypeFlags changedData)
        {
            var aabb = MyRDPathfinding.GetVoxelAreaAABB(storage, minVoxelChanged, maxVoxelChanged);
            InvalidateArea(aabb);
        }

        #region Debug Draw methods
        private void GenerateDebugTileDataSize(Vector3 center, int xCoord, int yCoord)
        {
            int dataSize = m_rdWrapper.GetTileDataSize(center, 0);
            m_debugTileSize[xCoord][yCoord] = dataSize;
        }

        private void GenerateDebugDrawPolygonNavmesh(MyPlanet planet, List<MyRecastDetourPolygon> polygons, MyOrientedBoundingBoxD centerOBB, Vector2I coords /*List<Vertex> navmesh, int xCoord, int yCoord*/)
        {
            if (polygons == null)
                return;

            List<Vertex> navmesh = new List<Vertex>();

            int greenBase = 10;
            int greenValue = 0;
            int maxExclusiveGreenValue = 95;
            int greenStep = 10;

            foreach (var polygon in polygons)
            {
                foreach (var vertice in polygon.Vertices)
                {
                    var v = new Vertex()
                    {
                        pos = LocalNavmeshPositionToWorldPosition(centerOBB, vertice, Center, Vector3D.Zero),
                        color = new Color(0, greenBase + greenValue, 0)
                    };
                    navmesh.Add(v);
                }
                greenValue += greenStep;
                greenValue %= maxExclusiveGreenValue;
            }

            if (navmesh != null && navmesh.Count > 0)
                m_newObbCoordsPolygons[coords] = navmesh;
        }

        uint m_drawNavmeshID = VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED;
        private void DrawPersistentDebugNavmesh()
        {
            foreach (var coordPoly in m_newObbCoordsPolygons)
                if (m_newObbCoordsPolygons[coordPoly.Key] == null)
                    m_obbCoordsPolygons.Remove(coordPoly.Key);
                else
                    m_obbCoordsPolygons[coordPoly.Key] = coordPoly.Value;

            m_newObbCoordsPolygons.Clear();

            if (m_obbCoordsPolygons.Count > 0)
            {
                MyRenderMessageDebugDrawMesh mesh = VRageRender.MyRenderProxy.PrepareDebugDrawMesh();
                Vertex vec0 = new Vertex(), vec1 = new Vertex(), vec2 = new Vertex();

                foreach (var vertexList in m_obbCoordsPolygons.Values)
                    for (int i = 0; i < vertexList.Count; )
                    {
                        vec0 = vertexList[i++];
                        vec1 = vertexList[i++];
                        vec2 = vertexList[i++];
                        mesh.AddTriangle(ref vec0.pos, vec0.color, ref vec1.pos, vec1.color, ref vec2.pos, vec2.color);
                    }

                if (m_drawNavmeshID == VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED)
                    m_drawNavmeshID = VRageRender.MyRenderProxy.DebugDrawMesh(mesh, MatrixD.Identity, Color.Green, true, true);
                else
                    VRageRender.MyRenderProxy.DebugDrawUpdateMesh(m_drawNavmeshID, mesh, MatrixD.Identity, Color.Green, true, true);
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

        private void UpdatePersistentDebugNavmesh()
        {
            // Updates the navmesh... the naughty way
            DrawNavmesh = DrawNavmesh;
        }
        #endregion

        #endregion

        #region CLI performance test
        private void TestCliPerformance()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            long level0, level1, level2;
            int n = 10000000;

            // Level0
            sw.Start();
            long sum = 0;
            for (int i = 0; i < n; i++)
                sum += Test.SimpleTest(i);
            sw.Stop();
            level0 = sw.ElapsedMilliseconds;

            // Level1
            sw.Start();
            sum = 0;
            for (int i = 0; i < n; i++)
                sum += MyRDWrapper.SimpleTestShallow(i);
            sw.Stop();
            level1 = sw.ElapsedMilliseconds;

            // Level2
            sw.Start();
            sum = 0;
            for (int i = 0; i < n; i++)
                sum += MyRDWrapper.SimpleTestDeep(i);
            sw.Stop();
            level2 = sw.ElapsedMilliseconds;
        }

        private class Test
        {
            static public int SimpleTest(int i)
            {
                i++;
                return i;
            }
        }
        #endregion
    }
}
