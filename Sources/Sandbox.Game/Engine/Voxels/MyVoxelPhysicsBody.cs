using System;
using System.Collections.Generic;
using System.Diagnostics;
using Havok;
using Sandbox.Common;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GUI.DebugInputComponents;
using VRage;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Trace;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRage.Game;
using VRage.Profiler;
using VRage.Voxels;

namespace Sandbox.Engine.Voxels
{
    public class MyVoxelPhysicsBody : MyPhysicsBody
    {
        const bool ENABLE_AABB_PHANTOM = true;
        private const int SHAPE_DISCARD_THRESHOLD = 0;
        private const int SHAPE_DISCARD_CHECK_INTERVAL = 18; // this is in update10s

        private static Vector3I[] m_cellsToGenerateBuffer = new Vector3I[128];

        internal HashSet<Vector3I>[] InvalidCells;
        internal MyPrecalcJobPhysicsBatch[] RunningBatchTask = new MyPrecalcJobPhysicsBatch[2];

        public readonly MyVoxelBase m_voxelMap;
        private bool m_needsShapeUpdate;
        private HkpAabbPhantom m_aabbPhantom;
        private bool m_bodiesInitialized;
        private readonly HashSet<IMyEntity> m_nearbyEntities = new HashSet<IMyEntity>();

        /// <summary>
        /// Only locked in callbacks, since they can happen during multithreaded havok step.
        /// Normal update is running on single thread and it doesn't happen at the same time as stepping,
        /// so no locking is necessary there.
        /// </summary>
        private readonly FastResourceLock m_nearbyEntitiesLock = new FastResourceLock();

        private readonly MyWorkTracker<MyCellCoord, MyPrecalcJobPhysicsPrefetch> m_workTracker = new MyWorkTracker<MyCellCoord, MyPrecalcJobPhysicsPrefetch>(MyCellCoord.Comparer);

        private readonly Vector3I m_cellsOffset = new Vector3I(0, 0, 0);

        bool m_staticForCluster = true;

        float m_phantomExtend;
        float m_predictionSize = 3.0f;

        private int m_lastDiscardCheck;

        private BoundingBoxI m_queuedRange = new BoundingBoxI(-1, -1);

        private bool m_queueInvalidation;

        internal MyVoxelPhysicsBody(MyVoxelBase voxelMap, float phantomExtend, float predictionSize = 3.0f, bool lazyPhysics = false)
            : base(voxelMap, RigidBodyFlag.RBF_STATIC)
        {
            ProfilerShort.Begin("MyVoxelPhysicsBody(");

            InvalidCells = new HashSet<Vector3I>[2];

            InvalidCells[0] = new HashSet<Vector3I>();
            InvalidCells[1] = new HashSet<Vector3I>();

            m_predictionSize = predictionSize;
            m_phantomExtend = phantomExtend;
            m_voxelMap = voxelMap;
            Vector3I storageSize = m_voxelMap.Size;
            Vector3I numCels = storageSize >> MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
            m_cellsOffset = m_voxelMap.StorageMin >> MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;

            if (!MyFakes.ENABLE_LAZY_VOXEL_PHYSICS || !lazyPhysics || !ENABLE_AABB_PHANTOM)
            {
                CreateRigidBodies();
            }

            ProfilerShort.End();

            MaterialType = MyMaterialType.ROCK;
        }

        private void CreatePhantom(BoundingBox boundingBox)
        {
            m_aabbPhantom = new HkpAabbPhantom(boundingBox, 0);
            m_aabbPhantom.CollidableAdded = AabbPhantom_CollidableAdded;
            m_aabbPhantom.CollidableRemoved = AabbPhantom_CollidableRemoved;
        }

        public override bool IsStatic { get { return true; } }

        public bool QueueInvalidate
        {
            get { return m_queueInvalidation; }
            set
            {
                m_queueInvalidation = value;
                if (!value && m_queuedRange.Max.X >= 0)
                {
                    InvalidateRange(m_queuedRange.Min, m_queuedRange.Max);
                    m_queuedRange = new BoundingBoxI(-1, -1);
                }
            }
        }

        public HkRigidBody GetRigidBody(int lod)
        {
            if (UseLod1VoxelPhysics && lod == 1)
                return RigidBody2;
            return RigidBody;
        }

        public HkUniformGridShape GetShape(int lod)
        {
            return (HkUniformGridShape)GetRigidBody(lod).GetShape();
        }

        private void CreateRigidBodies()
        {
            if (Entity.MarkedForClose) return;

            ProfilerShort.Begin("MyVoxelPhysicsBody::CreateRigidBodies()");
            try
            {
                if (m_bodiesInitialized)
                {
                    Debug.Fail("Double rigid body intialization for voxel map!");
                    return;
                }

                Vector3I numCels = m_voxelMap.Size >> MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;

                HkUniformGridShape shape;
                HkRigidBody lod1rb = null;
                if (UseLod1VoxelPhysics)
                {
                    shape = new HkUniformGridShape(
                        new HkUniformGridShapeArgs
                        {
                            CellsCount = numCels>>1,
                            CellSize = MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_METRES * 2,
                            CellOffset = MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF,
                            CellExpand = MyVoxelConstants.VOXEL_SIZE_IN_METRES
                        });
                    shape.SetShapeRequestHandler(RequestShapeBlockingLod1, QueryEmptyOrFull);

                    CreateFromCollisionObject(shape, -m_voxelMap.SizeInMetresHalf, m_voxelMap.WorldMatrix, collisionFilter: MyPhysics.CollisionLayers.VoxelLod1CollisionLayer);
                    shape.Base.RemoveReference();
                    lod1rb = RigidBody;
                    RigidBody = null;
                }

                shape = new HkUniformGridShape(
                    new HkUniformGridShapeArgs
                    {
                        CellsCount = numCels,
                        CellSize = MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_METRES,
                        CellOffset = MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF,
                        CellExpand = MyVoxelConstants.VOXEL_SIZE_IN_METRES
                    });
                shape.SetShapeRequestHandler(RequestShapeBlocking, QueryEmptyOrFull);

                CreateFromCollisionObject(shape, -m_voxelMap.SizeInMetresHalf, m_voxelMap.WorldMatrix, collisionFilter: MyPhysics.CollisionLayers.VoxelCollisionLayer);
                shape.Base.RemoveReference();
                if (UseLod1VoxelPhysics)
                    RigidBody2 = lod1rb;

                if (MyFakes.ENABLE_PHYSICS_HIGH_FRICTION)
                    Friction = 0.65f;

                m_bodiesInitialized = true;

                // When doing the enable disable roundtrip we can mess up the ClusterTree because Activate() can end up calling this
                // if the phantom is immediatelly intersecting something.
                if (Enabled)
                {
                    GetRigidBodyMatrix(out m_bodyMatrix);

                    RigidBody.SetWorldMatrix(m_bodyMatrix);
                    m_world.AddRigidBody(RigidBody);

                    if (UseLod1VoxelPhysics)
                    {
                        RigidBody2.SetWorldMatrix(m_bodyMatrix);
                        m_world.AddRigidBody(RigidBody2);
                    }
                }

            }
            finally
            {
                ProfilerShort.End();
            }
        }

        private void UpdateRigidBodyShape()
        {
            if (!m_needsShapeUpdate)
                return;

            m_needsShapeUpdate = false;

            if (!m_bodiesInitialized) CreateRigidBodies();

            ProfilerShort.Begin("MyVoxelPhysicsBody.RigidBody.UpdateShape()");
            Debug.Assert(RigidBody != null, "RigidBody in voxel physics is null! This must not happen.");
            if (RigidBody != null)
                RigidBody.UpdateShape();
            if (RigidBody2 != null)
                RigidBody2.UpdateShape();
            ProfilerShort.End();
        }

        private bool QueryEmptyOrFull(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
        {
            ////return false;
            var bb = new BoundingBox(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
            if (bb.Volume() < 100)
                return false;

            //bb.Translate(m_voxelMap.StorageMin);
            var result = m_voxelMap.Storage.Intersect(ref bb, false) != ContainmentType.Intersects;
            {
                var bbd = new BoundingBoxD(new Vector3(minX, minY, minZ) * 8, new Vector3(maxX, maxY, maxZ) * 8);
                bbd.TransformFast(Entity.WorldMatrix);
                var obb = new MyOrientedBoundingBoxD(bbd, Entity.WorldMatrix);
                MyRenderProxy.DebugDrawAABB(bbd, result ? Color.Green : Color.Red, 1, 1, false);
            }
            return result;
        }

        private void RequestShapeBlockingLod1(int x, int y, int z, out HkShape shape, out HkReferencePolicy refPolicy)
        {
            RequestShapeBlockingInternal(x, y, z, out shape, out refPolicy, true);
        }
        private void RequestShapeBlocking(int x, int y, int z, out HkShape shape, out HkReferencePolicy refPolicy)
        {
            RequestShapeBlockingInternal(x, y, z, out shape, out refPolicy, false);
        }

        private void RequestShapeBlockingInternal(int x, int y, int z, out HkShape shape, out HkReferencePolicy refPolicy, bool lod1physics)
        {
            ProfilerShort.Begin("MyVoxelPhysicsBody.RequestShapeBlocking");

            if (!m_bodiesInitialized) CreateRigidBodies();

            int lod = lod1physics ? 1 : 0;
            var cellCoord = new MyCellCoord(lod, new Vector3I(x, y, z));
            shape = HkShape.Empty;
            // shape must take ownership, otherwise shapes created here will leak, since I can't remove reference
            refPolicy = HkReferencePolicy.TakeOwnership;
            //MyPrecalcComponent.QueueJobCancel(m_workTracker, cellCoord);
            if (m_voxelMap.MarkedForClose)
            {
                ProfilerShort.End();
                return;
            }
            if (MyDebugDrawSettings.DEBUG_DRAW_REQUEST_SHAPE_BLOCKING)
            {
                BoundingBoxD aabb;
                MyVoxelCoordSystems.GeometryCellCoordToWorldAABB(m_voxelMap.PositionLeftBottomCorner, ref cellCoord, out aabb);
                MyRenderProxy.DebugDrawAABB(aabb, lod1physics ? Color.Yellow : Color.Red, 1, 1, true);
            }
            ProfilerShort.Begin("Generating geometry");
            MyIsoMesh geometryData = CreateMesh(m_voxelMap.Storage, cellCoord);
            ProfilerShort.End();

            if (!MyIsoMesh.IsEmpty(geometryData))
            {
                ProfilerShort.Begin("Shape from geometry");
                shape = CreateShape(geometryData, true);
                shape.AddReference();
                var args = new MyPrecalcJobPhysicsPrefetch.Args() {GeometryCell = cellCoord, TargetPhysics = this, Tracker = m_workTracker, SimpleShape = shape};
                MyPrecalcJobPhysicsPrefetch.Start(args);
                m_needsShapeUpdate = true;
                ProfilerShort.End();
            }

            ProfilerShort.End();
        }

        /// <param name="minVoxelChanged">Inclusive min.</param>
        /// <param name="maxVoxelChanged">Inclusive max.</param>
        internal void InvalidateRange(Vector3I minVoxelChanged, Vector3I maxVoxelChanged)
        {
            InvalidateRange(minVoxelChanged, maxVoxelChanged, 0);
            if(UseLod1VoxelPhysics)
                InvalidateRange(minVoxelChanged, maxVoxelChanged, 1);
        }

        private void GetPrediction(IMyEntity entity, out BoundingBoxD box)
        {
            var predictionOffset = ComputePredictionOffset(entity);
            box = entity.WorldAABB;

            if (entity.Physics.AngularVelocity.Sum > 0.03f)
            {
                var extents = entity.LocalAABB.HalfExtents.Length();
                box = new BoundingBoxD(box.Center - extents, box.Center + extents);
            }

            if (box.Extents.Max() > MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_METRES)
                box.Inflate(MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_METRES);
            else
                box.InflateToMinimum(new Vector3(MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_METRES));

            box.Translate(predictionOffset);
        }

        internal void InvalidateRange(Vector3I minVoxelChanged, Vector3I maxVoxelChanged, int lod)
        {
            MyPrecalcComponent.AssertUpdateThread();

            // No physics there ever was so we don't care.
            if (!m_bodiesInitialized) return;

            if (m_queueInvalidation)
            {
                if (m_queuedRange.Max.X < 0)
                {
                    m_queuedRange = new BoundingBoxI(minVoxelChanged, maxVoxelChanged);
                }
                else
                {
                    var bb = new BoundingBoxI(minVoxelChanged, maxVoxelChanged);
                    m_queuedRange.Include(ref bb);
                }
                return;
            }

            ProfilerShort.Begin("MyVoxelPhysicsBody.InvalidateRange");
            minVoxelChanged -= 1;// MyPrecalcComponent.InvalidatedRangeInflate;
            maxVoxelChanged += 1;//MyPrecalcComponent.InvalidatedRangeInflate;
            m_voxelMap.Storage.ClampVoxelCoord(ref minVoxelChanged);
            m_voxelMap.Storage.ClampVoxelCoord(ref maxVoxelChanged);

            Vector3I minCellChanged, maxCellChanged;
            MyVoxelCoordSystems.VoxelCoordToGeometryCellCoord(ref minVoxelChanged, out minCellChanged);
            MyVoxelCoordSystems.VoxelCoordToGeometryCellCoord(ref maxVoxelChanged, out maxCellChanged);

            Vector3I minCellChangedVoxelMap, maxCellChangedVoxelMap;
            minCellChangedVoxelMap = (minCellChanged - m_cellsOffset) >> lod;
            maxCellChangedVoxelMap = (maxCellChanged - m_cellsOffset) >> lod;

            var maxCell = m_voxelMap.Size - 1;
            MyVoxelCoordSystems.VoxelCoordToGeometryCellCoord(ref maxCell, out maxCell);
            maxCell >>= lod;
            Vector3I.Min(ref maxCellChangedVoxelMap, ref maxCell, out maxCellChangedVoxelMap);

            Debug.Assert(RigidBody != null, "RigidBody in voxel physics is null! This must not happen.");

            var rb = GetRigidBody(lod);
            Debug.Assert(rb != null, "RigidBody in voxel physics is null! This must not happen.");

            if (rb != null)
            {
                HkUniformGridShape shape = (HkUniformGridShape)rb.GetShape();
                Debug.Assert(shape.Base.IsValid);


                var numCells = (maxCellChangedVoxelMap - minCellChangedVoxelMap + 1).Size;
                if (numCells >= m_cellsToGenerateBuffer.Length)
                {
                    m_cellsToGenerateBuffer = new Vector3I[MathHelper.GetNearestBiggerPowerOfTwo(numCells)];
                }

                var tmpBuffer = m_cellsToGenerateBuffer;
                int invalidCount = shape.InvalidateRange(ref minCellChangedVoxelMap, ref maxCellChangedVoxelMap, tmpBuffer);
                Debug.Assert(invalidCount <= tmpBuffer.Length);

                //if (numCells <= 8)
                //shape.InvalidateRangeImmediate(ref minCellChangedVoxelMap, ref maxCellChangedVoxelMap);

                Debug.Assert(invalidCount <= tmpBuffer.Length);
                for (int i = 0; i < invalidCount; i++)
                {
                    InvalidCells[lod].Add(tmpBuffer[i]);
                }

                if (RunningBatchTask[lod] == null && InvalidCells[lod].Count != 0)
                {
                    MyPrecalcComponent.PhysicsWithInvalidCells.Add(this);
                }
            }

            if (minCellChangedVoxelMap == Vector3I.Zero && maxCellChangedVoxelMap == maxCell)
            {
                m_workTracker.CancelAll();
            }
            else
            {
                var cell = minCellChanged;
                for (var it = new Vector3I_RangeIterator(ref minCellChanged, ref maxCellChanged);
                    it.IsValid(); it.GetNext(out cell))
                {
                    m_workTracker.Cancel(new MyCellCoord(lod, cell));
                }
            }

            m_needsShapeUpdate = true;

            ProfilerShort.End();

            m_voxelMap.RaisePhysicsChanged();
        }

        internal void UpdateBeforeSimulation10()
        {
            UpdateRigidBodyShape();
        }

        internal void UpdateAfterSimulation10()
        {
            ProfilerShort.Begin("Voxel Physics Prediction");
            UpdateRigidBodyShape();

            // Apply prediction based on movement of nearby entities.
            foreach (var entity in m_nearbyEntities)
            {
                Debug.Assert(m_bodiesInitialized, "Voxel map does not have physics!");

                bool lod0 = entity is MyCharacter;

                if (!lod0)
                {
                    var body = entity.Physics as MyPhysicsBody;

                    if (body != null && body.RigidBody != null &&
                        (body.RigidBody.Layer == MyPhysics.CollisionLayers.FloatingObjectCollisionLayer || body.RigidBody.Layer == MyPhysics.CollisionLayers.LightFloatingObjectCollisionLayer))
                        lod0 = true;
                }

                if (!(entity is MyCubeGrid) && !lod0)
                    continue;

                if (entity.MarkedForClose)
                    continue;

                if (entity.Physics == null || entity.Physics.LinearVelocity.Length() < 2f)
                    continue;

                BoundingBoxD aabb;
                GetPrediction(entity, out aabb);
                if (!aabb.Intersects(m_voxelMap.PositionComp.WorldAABB))
                    continue;

                int lod = lod0 ? 0 : 1;
                float lodSize = 1 << lod;

                Vector3I min, max;
                Vector3D localPositionMin, localPositionMax;

                aabb = aabb.TransformFast(m_voxelMap.PositionComp.WorldMatrixInvScaled);

                aabb.Translate(m_voxelMap.SizeInMetresHalf);

                localPositionMax = aabb.Max;
                localPositionMin = aabb.Min;



                MyVoxelCoordSystems.LocalPositionToVoxelCoord(ref localPositionMin, out min);
                MyVoxelCoordSystems.LocalPositionToVoxelCoord(ref localPositionMax, out max);
                m_voxelMap.Storage.ClampVoxelCoord(ref min);
                m_voxelMap.Storage.ClampVoxelCoord(ref max);
                MyVoxelCoordSystems.VoxelCoordToGeometryCellCoord(ref min, out min);
                MyVoxelCoordSystems.VoxelCoordToGeometryCellCoord(ref max, out max);
                min >>= lod;
                max >>= lod;

                {
                    var size = (max - min + 1).Size;
                    if (size >= m_cellsToGenerateBuffer.Length)
                    {
                        m_cellsToGenerateBuffer = new Vector3I[MathHelper.GetNearestBiggerPowerOfTwo(size)];
                    }
                }
                var shape = GetShape(lod);

                Debug.Assert(shape.Base.IsValid);
                int requiredCellsCount = shape.GetMissingCellsInRange(ref min, ref max, m_cellsToGenerateBuffer);

                if (requiredCellsCount == 0)
                {
                    continue;
                }

                var bb = new BoundingBox(min * MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_METRES * lodSize, (max + 1) * MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_METRES * lodSize);
                bb.Translate(m_voxelMap.StorageMin);
                ProfilerShort.Begin("Storage Intersect");
                if (requiredCellsCount > 0)
                {
                    if (m_voxelMap.Storage.Intersect(ref bb, false) == ContainmentType.Intersects)
                    {
                        ProfilerShort.BeginNextBlock("Set Empty Shapes");
                        for (int i = 0; i < requiredCellsCount; ++i)
                        {
                            var cell = m_cellsToGenerateBuffer[i];
                            m_workTracker.Cancel(new MyCellCoord(lod, cell));
                            shape.SetChild(cell.X, cell.Y, cell.Z, (HkBvCompressedMeshShape)HkShape.Empty, HkReferencePolicy.TakeOwnership);
                        }
                    }
                    else
                    {
                        /*if (MyVoxelDebugInputComponent.PhysicsComponent.Static != null)
                            MyVoxelDebugInputComponent.PhysicsComponent.Static.Add(m_voxelMap.WorldMatrix, bb, new Vector4I(m_cellsToGenerateBuffer[0], lod), m_voxelMap);*/

                        ProfilerShort.End();
                        continue;
                    }
                }

                ProfilerShort.BeginNextBlock("Start Jobs");
                for (int i = 0; i < requiredCellsCount; ++i)
                {
                    if (m_workTracker.Exists(new MyCellCoord(lod, m_cellsToGenerateBuffer[i])))
                        continue;

                    MyPrecalcJobPhysicsPrefetch.Start(new MyPrecalcJobPhysicsPrefetch.Args
                    {
                        TargetPhysics = this,
                        Tracker = m_workTracker,
                        GeometryCell = new MyCellCoord(lod, m_cellsToGenerateBuffer[i]),
                        Storage = m_voxelMap.Storage
                    });
                }
                ProfilerShort.End();
            }

            if (m_bodiesInitialized)
            {
                CheckAndDiscardShapes();
            }
            ProfilerShort.End();
        }

        private void CheckAndDiscardShapes()
        {
            m_lastDiscardCheck++;
            if (m_lastDiscardCheck > SHAPE_DISCARD_CHECK_INTERVAL)
            {
                m_lastDiscardCheck = 0;
                var voxelShape = (HkUniformGridShape)GetShape();
                int hits = voxelShape.GetHitsAndClear();
                if (m_nearbyEntities.Count == 0 && RigidBody != null && MyFakes.ENABLE_VOXEL_PHYSICS_SHAPE_DISCARDING && voxelShape.ShapeCount > 0 && hits <= SHAPE_DISCARD_THRESHOLD)
                {
                    // RigidBody.GetShape();
                    Debug.Assert(voxelShape.Base.IsValid);
                    voxelShape.DiscardLargeData();
                    if (RigidBody2 != null)
                    {
                        voxelShape = (HkUniformGridShape)RigidBody2.GetShape();
                        hits = voxelShape.GetHitsAndClear();
                        if (hits <= SHAPE_DISCARD_THRESHOLD)
                            voxelShape.DiscardLargeData();
                    }
                }
            }
        }

        private Vector3 ComputePredictionOffset(IMyEntity entity)
        {
            return entity.Physics.LinearVelocity; //*m_predictionSize;
        }

        public override void DebugDraw()
        {
            base.DebugDraw();

            if (m_aabbPhantom != null && MyDebugDrawSettings.DEBUG_DRAW_VOXEL_MAP_AABB && IsInWorld)
            {
                var offset = ClusterToWorld(Vector3D.Zero);

                BoundingBoxD phantom = (BoundingBoxD) m_aabbPhantom.Aabb;
                phantom.Translate(offset);

                MyRenderProxy.DebugDrawAABB(phantom, Color.Orange, 1.0f, 1.0f, true);
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_VOXEL_PHYSICS_PREDICTION)
            {
                foreach (var entity in m_nearbyEntities)
                {
                    if (entity.MarkedForClose)
                        continue;

                    var worldAabb = entity.WorldAABB;
                    MyRenderProxy.DebugDrawAABB(worldAabb, Color.Bisque, 1f, 1f, true);
                    MyRenderProxy.DebugDrawLine3D(GetWorldMatrix().Translation, worldAabb.Center, Color.Bisque, Color.BlanchedAlmond, true);

                    BoundingBoxD predAabb;
                    GetPrediction(entity, out predAabb);
                    MyRenderProxy.DebugDrawAABB(predAabb, Color.Crimson, 1f, 1f, true);
                }

                using (var batch = MyRenderProxy.DebugDrawBatchAABB(GetWorldMatrix(), new Color(Color.Cyan, 0.2f), true, false))
                {
                    int i = 0;
                    foreach (var entry in m_workTracker)
                    {
                        i++;
                        BoundingBoxD localAabb;
                        var localCell = entry.Key;

                        localAabb.Min = localCell.CoordInLod << localCell.Lod;
                        localAabb.Min *= MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_METRES;
                        localAabb.Min -= m_voxelMap.SizeInMetresHalf;
                        localAabb.Max = localAabb.Min + MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_METRES;
                        
                        batch.Add(ref localAabb);
                        if (i > 250)
                            break;
                    }
                }
            }
        }

        internal void OnTaskComplete(MyCellCoord coord, HkShape childShape)
        {
            Debug.Assert(RigidBody != null, "RigidBody in voxel physics is null! This must not happen.");
            if (RigidBody != null)
            {
                HkUniformGridShape shape = GetShape(coord.Lod);
                Debug.Assert(shape.Base.IsValid);
                shape.SetChild(coord.CoordInLod.X, coord.CoordInLod.Y, coord.CoordInLod.Z, childShape, HkReferencePolicy.None);
                //BoundingBoxD worldAabb;
                //MyVoxelCoordSystems.GeometryCellCoordToWorldAABB(m_voxelMap.PositionLeftBottomCorner, ref coord, out worldAabb);
                //VRageRender.MyRenderProxy.DebugDrawAABB(worldAabb, Color.Green, 1f, 1f, true);
                m_needsShapeUpdate = true;
            }
        }

        internal void OnBatchTaskComplete(Dictionary<Vector3I, HkShape> newShapes, int lod)
        {
            Debug.Assert(RigidBody != null, "RigidBody in voxel physics is null! This must not happen.");
            if (RigidBody != null)
            {
                HkUniformGridShape shape = GetShape(lod);
                Debug.Assert(shape.Base.IsValid);
                foreach (var entry in newShapes)
                {
                    var coord = entry.Key;
                    var childShape = entry.Value;
                    shape.SetChild(coord.X, coord.Y, coord.Z, childShape, HkReferencePolicy.None);
                }
                m_needsShapeUpdate = true;
                /*if (InvalidCells.Count != 0)
                    MyPrecalcComponent.PhysicsWithInvalidCells.Add(this);*/
            }
        }

        internal MyIsoMesh CreateMesh(IMyStorage storage, MyCellCoord coord)
        {
            // mk:NOTE This method must be thread safe. Called from worker threads.

            coord.CoordInLod += m_cellsOffset >> coord.Lod;

            var min = coord.CoordInLod << MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
            var max = min + MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS;
            // overlap to neighbor; introduces extra data but it makes logic for raycasts and collisions simpler (no need to check neighbor cells)
            min -= 1;
            max += 2;
            var mesh = MyPrecalcComponent.IsoMesher.Precalc(storage, coord.Lod, min, max, false, false);

            if (mesh == null)
            {
                if (MyVoxelDebugInputComponent.PhysicsComponent.Static != null)
                    MyVoxelDebugInputComponent.PhysicsComponent.Static.Add(m_voxelMap.WorldMatrix, new BoundingBox(min+1, max-2), new Vector4I(min, coord.Lod), m_voxelMap);
            }

            return mesh;
        }

        [ThreadStatic]
        private static List<int> s_indexListMember;

        private static List<int> s_indexList { get { return MyUtils.Init(ref s_indexListMember); } }

        [ThreadStatic]
        private static HkGeometry s_cellGeometryMember;

        private static HkGeometry s_cellGeometry { get { return MyUtils.Init(ref s_cellGeometryMember); } }

        [ThreadStatic]
        private static List<Vector3> s_vertexListMember;
        private static List<Vector3> s_vertexList { get { return MyUtils.Init(ref s_vertexListMember); } }

        internal unsafe HkShape CreateShape(MyIsoMesh mesh, bool simple = false)
        {
            // mk:NOTE This method must be thread safe. Called from worker threads.

            if (mesh == null || mesh.TrianglesCount == 0 || mesh.VerticesCount == 0)
                return HkShape.Empty;

            if (s_indexList.Capacity < mesh.TrianglesCount * 3)
                s_indexList.Capacity = mesh.TrianglesCount * 3;
            if (s_vertexList.Capacity < mesh.VerticesCount)
                s_vertexList.Capacity = mesh.VerticesCount;

            for (int i = 0; i < mesh.TrianglesCount; i++)
            {
                s_indexList.Add(mesh.Triangles[i].VertexIndex0);
                s_indexList.Add(mesh.Triangles[i].VertexIndex2);
                s_indexList.Add(mesh.Triangles[i].VertexIndex1);
            }

            // mk:TODO Unify denormalizing of positions with what is in MyIsoMesh.
            var positionOffset = mesh.PositionOffset - m_voxelMap.StorageMin * MyVoxelConstants.VOXEL_SIZE_IN_METRES;
            var positions = mesh.Positions.GetInternalArray();
            for (int i = 0; i < mesh.VerticesCount; i++)
            {
                s_vertexList.Add(positions[i] * mesh.PositionScale + positionOffset);
            }
            {
                HkShape result;
                if (simple)
                {
                    result = new HkSimpleMeshShape(s_vertexList, s_indexList);
                }
                else
                {
                    s_cellGeometry.SetGeometry(s_vertexList, s_indexList);
                    result = new HkBvCompressedMeshShape(s_cellGeometry, null, null, HkWeldingType.None,
                        MyPerGameSettings.PhysicsConvexRadius);

                }
                Debug.Assert(result.ReferenceCount == 1);
                s_vertexList.Clear();
                s_indexList.Clear();
                return result;
            }
        }

        internal HkShape BakeCompressedMeshShape(HkSimpleMeshShape simpleMesh)
        {
            return new HkBvCompressedMeshShape(simpleMesh);
        }

        public override bool IsStaticForCluster
        {
            get { return m_staticForCluster; }
            set { m_staticForCluster = value; }
        }

        public override void Activate(object world, ulong clusterObjectID)
        {
            base.Activate(world, clusterObjectID);
            ActivatePhantom();
        }

        public override void ActivateBatch(object world, ulong clusterObjectID)
        {
            base.ActivateBatch(world, clusterObjectID);
            ActivatePhantom();
        }

        public override void Deactivate(object world)
        {
            DeactivatePhantom();
            base.Deactivate(world);
        }

        public override void DeactivateBatch(object world)
        {
            DeactivatePhantom();
            base.DeactivateBatch(world);
        }

        public override void Close()
        {
            base.Close();
            m_workTracker.CancelAll();

            for (int lod = 0; lod < RunningBatchTask.Length; ++lod)
            {
                if (RunningBatchTask[lod] != null)
                {
                    RunningBatchTask[lod].Cancel();
                    RunningBatchTask[lod] = null;
                }
            }

            if (ENABLE_AABB_PHANTOM && m_aabbPhantom != null)
            {
                m_aabbPhantom.Dispose();
                m_aabbPhantom = null;
            }
        }

        private void ActivatePhantom()
        {
            if (ENABLE_AABB_PHANTOM)
            {
                GetRigidBodyMatrix(out m_bodyMatrix);
                var center = m_bodyMatrix.Translation + m_voxelMap.SizeInMetresHalf;
                var size = m_voxelMap.SizeInMetres;
                size *= m_phantomExtend;

                if (m_aabbPhantom == null)
                    CreatePhantom(new BoundingBox(center - 0.5f * size, center + 0.5f * size));
                else
                    m_aabbPhantom.Aabb = new BoundingBox(center - 0.5f * size, center + 0.5f * size);

                MyTrace.Send(TraceWindow.Analytics, "AddPhantom-before");
                HavokWorld.AddPhantom(m_aabbPhantom);
                MyTrace.Send(TraceWindow.Analytics, "AddPhantom-after");
            }
        }

        private void DeactivatePhantom()
        {
            if (ENABLE_AABB_PHANTOM)
            {
                MyTrace.Send(TraceWindow.Analytics, "RemovePhantom-before");
                Debug.Assert(m_aabbPhantom != null);
                HavokWorld.RemovePhantom(m_aabbPhantom);
                MyTrace.Send(TraceWindow.Analytics, "RemovePhantom-after");
                // TODO(DI): Check if this assert is really not valid anymore
                //Debug.Assert(m_nearbyEntities.Count == 0, "Inconsistent entities management");
                m_nearbyEntities.Clear();
            }
        }

        private void AabbPhantom_CollidableAdded(ref HkpCollidableAddedEvent eventData)
        {
            var rb = eventData.RigidBody;
            if (rb == null) // ignore phantoms
                return;

            var entities = rb.GetAllEntities();

            if (rb.IsFixedOrKeyframed)
            {
                entities.Clear();
                return;
            }

            if (!m_bodiesInitialized) CreateRigidBodies();

            foreach (var entity in entities)
            {
                var grid = entity.Physics as MyGridPhysics;
                var character = entity.Physics as MyPhysicsBody;
                // I get both rigid bodies reported but they don't match, I will only track RB 1
                //if (IsDynamicGrid(rb, grid) ||
                //    IsCharacter(rb, character))
                if(!rb.IsFixedOrKeyframed && rb.Layer != MyPhysics.CollisionLayers.DebrisCollisionLayer)
                {
                    using (m_nearbyEntitiesLock.AcquireExclusiveUsing())
                    {
                        //unreliable
                        //Debug.Assert(!m_nearbyEntities.Contains(entity), "Entity added twice");
                        m_nearbyEntities.Add(entity);
                    }
                }
            }
            entities.Clear();
        }

        private static bool IsCharacter(HkRigidBody rb, MyPhysicsBody character)
        {
            if (character == null)
                return false;

            var c = character.Entity as MyCharacter;
            if (c == null) return false;
            if (c.Physics == null) return false;    // Physics may have been already released / Entity removed from the world?
            if (c.Physics.CharacterProxy != null)
                return c.Physics.CharacterProxy.GetHitRigidBody() == rb;
            return (c.Physics.RigidBody == rb);     // Otherwise we do proper check
        }

        private static bool IsDynamicGrid(HkRigidBody rb, MyGridPhysics grid)
        {
            return (grid != null && grid.RigidBody == rb && !grid.IsStatic);
        }

        private void AabbPhantom_CollidableRemoved(ref HkpCollidableRemovedEvent eventData)
        {
            var rb = eventData.RigidBody;
            if (rb == null) // ignore phantoms
                return;
            var entities = rb.GetAllEntities();
            foreach (var entity in entities)
            {
                var grid = entity.Physics as MyGridPhysics;
                var character = entity.Physics as MyPhysicsBody;
                // RK: IsDynamicGrid(rb, grid) commented out because body can be changed to static after added to m_nearbyEntities, see method MyGridShape.UpdateShape, 
                // before CreateConnectionToWorld(destructionBody) it can be static = false but after true!
                if ((grid != null && grid.RigidBody == rb)/*IsDynamicGrid(rb, grid)*/ ||
                    IsCharacter(rb, character))
                {
                    using (m_nearbyEntitiesLock.AcquireExclusiveUsing())
                    {
                        if (character != null)
                        {
                            MyTrace.Send(TraceWindow.Analytics, String.Format("{0} Removed character", character.Entity.EntityId));
                        }
                        //unreliable
                        //Debug.Assert(m_nearbyEntities.Contains(entity), "Removing entity which was not added");
                        m_nearbyEntities.Remove(entity);
                    }
                }
            }
            entities.Clear();
        }

        internal void GenerateAllShapes()
        {
            if (!m_bodiesInitialized) CreateRigidBodies();

            var min = Vector3I.Zero;

            Vector3I storageSize = m_voxelMap.Size;
            Vector3I max = new Vector3I(0, 0, 0);
            max.X = storageSize.X >> MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
            max.Y = storageSize.Y >> MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
            max.Z = storageSize.Z >> MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;

            max += min;

            var args = new MyPrecalcJobPhysicsPrefetch.Args
            {
                GeometryCell = new MyCellCoord(1, min),
                Storage = m_voxelMap.Storage,
                TargetPhysics = this,
                Tracker = m_workTracker
            };
            for (var it = new Vector3I_RangeIterator(ref min, ref max);
                it.IsValid();
                it.GetNext(out args.GeometryCell.CoordInLod))
            {
                MyPrecalcJobPhysicsPrefetch.Start(args);
            }
        }

        public override MyStringHash GetMaterialAt(Vector3D worldPos)
        {
            var material = m_voxelMap.GetMaterialAt(ref worldPos);
            //Debug.Assert(material != null);
            return material != null ? MyStringHash.GetOrCompute(material.MaterialTypeName) : MyStringHash.NullOrEmpty;
        }

        public void PrefetchShapeOnRay(ref LineD ray)
        {
            int lod = 1;
            Vector3D localStart;
            MyVoxelCoordSystems.WorldPositionToLocalPosition(m_voxelMap.PositionLeftBottomCorner, ref ray.From, out localStart);
            
            Vector3D localEnd;
            MyVoxelCoordSystems.WorldPositionToLocalPosition(m_voxelMap.PositionLeftBottomCorner, ref ray.To, out localEnd);

            var shape = GetShape(lod);
            Debug.Assert(shape.Base.IsValid);

            if (m_cellsToGenerateBuffer.Length < 64)
            {
                m_cellsToGenerateBuffer = new Vector3I[64];
            }

            int requiredCellsCount = shape.GetHitCellsInRange(localStart, localEnd, m_cellsToGenerateBuffer);
      
            if (requiredCellsCount == 0)
            {
                return;
            }

            ProfilerShort.Begin("Start Jobs");
            for (int i = 0; i < requiredCellsCount; ++i)
            {
                if (m_workTracker.Exists(new MyCellCoord(lod, m_cellsToGenerateBuffer[i])))
                    continue;

                MyPrecalcJobPhysicsPrefetch.Start(new MyPrecalcJobPhysicsPrefetch.Args
                {
                    TargetPhysics = this,
                    Tracker = m_workTracker,
                    GeometryCell = new MyCellCoord(lod, m_cellsToGenerateBuffer[i]),
                    Storage = m_voxelMap.Storage
                });
            }
            ProfilerShort.End();

        }

        public static bool UseLod1VoxelPhysics = false;
    }
}
