using Havok;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Voxels;
using VRageMath;
using VRageRender;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Character;
using VRage.Trace;
using VRage.ModAPI;
using VRage.Components;
using Sandbox.Common;

namespace Sandbox.Engine.Voxels
{
    class MyVoxelPhysicsBody : MyPhysicsBody
    {
        const bool ENABLE_AABB_PHANTOM = true;

        private static Vector3I[] m_cellsToGenerateBuffer = new Vector3I[128];

        internal HashSet<Vector3I> InvalidCells = new HashSet<Vector3I>(Vector3I.Comparer);
        internal MyPrecalcJobPhysicsBatch RunningBatchTask;

        public readonly MyVoxelBase m_voxelMap;
        private bool m_needsShapeUpdate;
        private HkpAabbPhantom m_aabbPhantom;
        private readonly HashSet<IMyEntity> m_nearbyEntities = new HashSet<IMyEntity>();

        /// <summary>
        /// Only locked in callbacks, since they can happen during multithreaded havok step.
        /// Normal update is running on single thread and it doesn't happen at the same time as stepping,
        /// so no locking is necessary there.
        /// </summary>
        private readonly FastResourceLock m_nearbyEntitiesLock = new FastResourceLock();

        private readonly MyWorkTracker<Vector3I, MyPrecalcJobPhysicsPrefetch> m_workTracker = new MyWorkTracker<Vector3I, MyPrecalcJobPhysicsPrefetch>(Vector3I.Comparer);

        private readonly Vector3I m_cellsOffset = new Vector3I(0, 0, 0);

        bool m_staticForCluster = true;

        float m_phantomExtend = 0.0f;
        float m_predictionSize = 3.0f;

        internal MyVoxelPhysicsBody(MyVoxelBase voxelMap,float phantomExtend, float predictionSize = 3.0f): base(voxelMap, RigidBodyFlag.RBF_STATIC)
        {
            m_predictionSize = predictionSize;
            m_phantomExtend = phantomExtend;
            m_voxelMap = voxelMap;
            Vector3I storageSize = m_voxelMap.Size;
            Vector3I numCels = storageSize >> MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
            m_cellsOffset = m_voxelMap.StorageMin >> MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;

            HkUniformGridShape shape;
            HkRigidBody lod1rb = null;
            if (MyFakes.USE_LOD1_VOXEL_PHYSICS)
            {
                shape = new HkUniformGridShape(
                    new HkUniformGridShapeArgs()
                    {
                        CellsCount = numCels,
                        CellSize = MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_METRES,
                        CellOffset = MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF,
                        CellExpand = MyVoxelConstants.VOXEL_SIZE_IN_METRES,
                    });
                shape.SetShapeRequestHandler(RequestShapeBlockingLod1);

                CreateFromCollisionObject(shape, -m_voxelMap.SizeInMetresHalf, m_voxelMap.WorldMatrix, collisionFilter: MyPhysics.VoxelLod1CollisionLayer);
                shape.Base.RemoveReference();
                lod1rb = RigidBody;
                RigidBody = null;
            }
            shape = new HkUniformGridShape(
                new HkUniformGridShapeArgs()
                {
                    CellsCount = numCels,
                    CellSize = MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_METRES,
                    CellOffset = MyVoxelConstants.VOXEL_SIZE_IN_METRES_HALF,
                    CellExpand = MyVoxelConstants.VOXEL_SIZE_IN_METRES,
                });
            shape.SetShapeRequestHandler(RequestShapeBlocking);

            CreateFromCollisionObject(shape, -m_voxelMap.SizeInMetresHalf, m_voxelMap.WorldMatrix, collisionFilter: MyPhysics.VoxelCollisionLayer);
            shape.Base.RemoveReference();
            if (MyFakes.USE_LOD1_VOXEL_PHYSICS)
                RigidBody2 = lod1rb;
            if (ENABLE_AABB_PHANTOM)
            {
                m_aabbPhantom = new Havok.HkpAabbPhantom(new BoundingBox(Vector3.Zero, m_voxelMap.SizeInMetres), 0);
                m_aabbPhantom.CollidableAdded = AabbPhantom_CollidableAdded;
                m_aabbPhantom.CollidableRemoved = AabbPhantom_CollidableRemoved;
            }

            if (MyFakes.ENABLE_PHYSICS_HIGH_FRICTION)
                Friction = 0.65f;

            MaterialType = MyMaterialType.ROCK;
        }

        private void UpdateRigidBodyShape()
        {
            if (!m_needsShapeUpdate)
                return;

            m_needsShapeUpdate = false;

            ProfilerShort.Begin("MyVoxelPhysicsBody.RigidBody.UpdateShape()");
            Debug.Assert(RigidBody != null, "RigidBody in voxel physics is null! This must not happen.");
            if (RigidBody != null)
                RigidBody.UpdateShape();
            ProfilerShort.End();
        }

        private void RequestShapeBlockingLod1(int x, int y, int z, out HkBvCompressedMeshShape shape, out HkReferencePolicy refPolicy)
        {
            RequestShapeBlockingInternal(x, y, z, out shape, out refPolicy, true);
        }
        private void RequestShapeBlocking(int x, int y, int z, out HkBvCompressedMeshShape shape, out HkReferencePolicy refPolicy)
        {
            RequestShapeBlockingInternal(x, y, z, out shape, out refPolicy, false);
        }
        private void RequestShapeBlockingInternal(int x, int y, int z, out HkBvCompressedMeshShape shape, out HkReferencePolicy refPolicy, bool lod1physics)
        {
            ProfilerShort.Begin("MyVoxelPhysicsBody.RequestShapeBlocking");

            const int lod = 0;
            var cellCoord = new MyCellCoord(lod, new Vector3I(x, y, z));
            shape = (HkBvCompressedMeshShape)HkShape.Empty;
            // shape must take ownership, otherwise shapes created here will leak, since I can't remove reference
            refPolicy = HkReferencePolicy.TakeOwnership;
            MyPrecalcComponent.QueueJobCancel(m_workTracker, cellCoord.CoordInLod);

            if (m_voxelMap.MarkedForClose)
            {
                ProfilerShort.End();
                return;
            }
            //BoundingBoxD aabb;
            //MyVoxelCoordSystems.GeometryCellCoordToWorldAABB(m_voxelMap.PositionLeftBottomCorner, ref cellCoord.CoordInLod, out aabb);
            //MyRenderProxy.DebugDrawAABB(aabb, Color.Red, 1, 1, false);
            ProfilerShort.Begin("Generating geometry");
            MyIsoMesh geometryData = CreateMesh(m_voxelMap.Storage, cellCoord.CoordInLod, lod1physics);
            ProfilerShort.End();

            if (!MyIsoMesh.IsEmpty(geometryData))
            {
                ProfilerShort.Begin("Shape from geometry");
                shape = CreateShape(geometryData);
                m_needsShapeUpdate = true;
                ProfilerShort.End();
            }

            ProfilerShort.End();
        }

        /// <param name="minVoxelChanged">Inclusive min.</param>
        /// <param name="maxVoxelChanged">Inclusive max.</param>
        internal void InvalidateRange(Vector3I minVoxelChanged, Vector3I maxVoxelChanged)
        {
            MyPrecalcComponent.AssertUpdateThread();
            ProfilerShort.Begin("MyVoxelPhysicsBody.InvalidateRange");

            minVoxelChanged -= MyPrecalcComponent.InvalidatedRangeInflate;
            maxVoxelChanged += MyPrecalcComponent.InvalidatedRangeInflate;
            m_voxelMap.Storage.ClampVoxelCoord(ref minVoxelChanged);
            m_voxelMap.Storage.ClampVoxelCoord(ref maxVoxelChanged);

            Vector3I minCellChanged, maxCellChanged;
            MyVoxelCoordSystems.VoxelCoordToGeometryCellCoord(ref minVoxelChanged, out minCellChanged);
            MyVoxelCoordSystems.VoxelCoordToGeometryCellCoord(ref maxVoxelChanged, out maxCellChanged);

            Vector3I minCellChangedVoxelMap, maxCellChangedVoxelMap;
            minCellChangedVoxelMap = minCellChanged - m_cellsOffset;
            maxCellChangedVoxelMap = maxCellChanged - m_cellsOffset;
            var maxCell = m_voxelMap.Size - 1;
            MyVoxelCoordSystems.VoxelCoordToGeometryCellCoord(ref maxCell, out maxCell);
            Vector3I.Min(ref maxCellChangedVoxelMap, ref maxCell, out maxCellChangedVoxelMap);

            Debug.Assert(RigidBody != null, "RigidBody in voxel physics is null! This must not happen.");
            if (RigidBody != null)
            {
                var shape = (HkUniformGridShape)GetShape();//RigidBody.GetShape();
                Debug.Assert(shape.Base.IsValid);
                var tmpBuffer = m_cellsToGenerateBuffer;
                int invalidCount = shape.InvalidateRange(ref minCellChangedVoxelMap, ref maxCellChangedVoxelMap, tmpBuffer);
                if (invalidCount > tmpBuffer.Length)
                {
                    // Not storing this new buffer in static variable since this is just temporary and potentially large.
                    // Static variable could be potentially the same as leak.
                    tmpBuffer = new Vector3I[invalidCount];
                    int invalidCount2 = shape.InvalidateRange(ref minCellChangedVoxelMap, ref maxCellChangedVoxelMap, tmpBuffer);
                    Debug.Assert(invalidCount == invalidCount2);
                    invalidCount = invalidCount2;
                }

                Debug.Assert(invalidCount <= tmpBuffer.Length);
                for (int i = 0; i < invalidCount; i++)
                {
                    InvalidCells.Add(tmpBuffer[i]);
                }
                if (RunningBatchTask == null && InvalidCells.Count != 0)
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
                for (var it = new Vector3I.RangeIterator(ref minCellChanged, ref maxCellChanged);
                    it.IsValid(); it.GetNext(out cell))
                {
                    m_workTracker.Cancel(cell);
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
            UpdateRigidBodyShape();

            // Apply prediction based on movement of nearby entities.
            foreach (var entity in m_nearbyEntities)
            {
                if (!(entity is MyCubeGrid)) //jn:TODO prediction for lod0
                    continue;

                if (entity.MarkedForClose)
                    continue;

                if (entity.Physics.LinearVelocity.Length() < 2f)
                    continue;

                var predictionOffset = ComputePredictionOffset(entity);
                var aabb = entity.WorldAABB;
                aabb.Inflate(MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_METRES * 3);
                aabb.Translate(predictionOffset);
                if (!aabb.Intersects(m_voxelMap.PositionComp.WorldAABB))
                    continue;
                Vector3I min, max;
                Vector3D localPositionMin, localPositionMax;

                MyVoxelCoordSystems.WorldPositionToLocalPosition(aabb.Min, m_voxelMap.PositionComp.WorldMatrix, m_voxelMap.PositionComp.WorldMatrixInvScaled, m_voxelMap.SizeInMetresHalf, out localPositionMin);
                MyVoxelCoordSystems.WorldPositionToLocalPosition(aabb.Max, m_voxelMap.PositionComp.WorldMatrix, m_voxelMap.PositionComp.WorldMatrixInvScaled, m_voxelMap.SizeInMetresHalf, out localPositionMax);


                MyVoxelCoordSystems.LocalPositionToVoxelCoord(ref localPositionMin, out min);
                MyVoxelCoordSystems.LocalPositionToVoxelCoord(ref localPositionMax, out max);
                m_voxelMap.Storage.ClampVoxelCoord(ref min);
                m_voxelMap.Storage.ClampVoxelCoord(ref max);
                MyVoxelCoordSystems.VoxelCoordToGeometryCellCoord(ref min, out min);
                MyVoxelCoordSystems.VoxelCoordToGeometryCellCoord(ref max, out max);
                {
                    var size = (max - min + 1).Size;
                    if (size >= m_cellsToGenerateBuffer.Length)
                    {
                        m_cellsToGenerateBuffer = new Vector3I[MathHelper.GetNearestBiggerPowerOfTwo(size)];
                    }
                }
                var shape = (HkUniformGridShape)GetShape();// RigidBody.GetShape();
                Debug.Assert(shape.Base.IsValid);
                int requiredCellsCount = shape.GetMissingCellsInRange(ref min, ref max, m_cellsToGenerateBuffer);

                for (int i = 0; i < requiredCellsCount; ++i)
                {
                    if (m_workTracker.Exists(m_cellsToGenerateBuffer[i]))
                        continue;

                    MyPrecalcJobPhysicsPrefetch.Start(new MyPrecalcJobPhysicsPrefetch.Args()
                    {
                        TargetPhysics = this,
                        Tracker = m_workTracker,
                        GeometryCell = new MyCellCoord(0, m_cellsToGenerateBuffer[i]),
                        Storage = m_voxelMap.Storage,
                    });
                }
            }
            var voxelShape = (HkUniformGridShape)GetShape();
            if (m_nearbyEntities.Count == 0 && RigidBody != null && MyFakes.ENABLE_VOXEL_PHYSICS_SHAPE_DISCARDING && voxelShape.ShapeCount > 0)
            {
                // RigidBody.GetShape();
                Debug.Assert(voxelShape.Base.IsValid);
                voxelShape.DiscardLargeData();
                if(RigidBody2 != null)
                {
                    voxelShape = (HkUniformGridShape)RigidBody2.GetShape();
                    voxelShape.DiscardLargeData();
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
            if (MyDebugDrawSettings.DEBUG_DRAW_VOXEL_PHYSICS_PREDICTION)
            {
                foreach (var entity in m_nearbyEntities)
                {
                    if (entity.MarkedForClose)
                        continue;
                    var worldAabb = entity.WorldAABB;
                    VRageRender.MyRenderProxy.DebugDrawAABB(worldAabb, Color.Bisque, 1f, 1f, true);
                    VRageRender.MyRenderProxy.DebugDrawLine3D(GetWorldMatrix().Translation, worldAabb.Center, Color.Bisque, Color.BlanchedAlmond, true);
                    var prediction = ComputePredictionOffset(entity);
                    worldAabb.Translate(entity.Physics.LinearVelocity * 2.0f);
                    VRageRender.MyRenderProxy.DebugDrawAABB(worldAabb, Color.Crimson, 1f, 1f, true);
                }

                using (var batch = VRageRender.MyRenderProxy.DebugDrawBatchAABB(MatrixD.Identity, Color.Cyan, true, false))
                {
                    foreach (var entry in m_workTracker)
                    {
                        BoundingBoxD worldAabb;
                        var localCell = entry.Key;
                        MyVoxelCoordSystems.GeometryCellCoordToWorldAABB(m_voxelMap.PositionLeftBottomCorner, ref localCell, out worldAabb);
                        batch.Add(ref worldAabb);
                    }
                }
            }
        }

        internal void OnTaskComplete(Vector3I coord, HkBvCompressedMeshShape childShape)
        {
            Debug.Assert(RigidBody != null, "RigidBody in voxel physics is null! This must not happen.");
            if (RigidBody != null)
            {
                var shape = (HkUniformGridShape)GetShape();//RigidBody.GetShape();
                Debug.Assert(shape.Base.IsValid);
                shape.SetChild(coord.X, coord.Y, coord.Z, childShape, HkReferencePolicy.None);
                //BoundingBoxD worldAabb;
                //MyVoxelCoordSystems.GeometryCellCoordToWorldAABB(m_voxelMap.PositionLeftBottomCorner, ref coord, out worldAabb);
                //VRageRender.MyRenderProxy.DebugDrawAABB(worldAabb, Color.Green, 1f, 1f, true);
                m_needsShapeUpdate = true;
            }
        }

        internal void OnBatchTaskComplete(Dictionary<Vector3I, HkBvCompressedMeshShape> newShapes)
        {
            Debug.Assert(RigidBody != null, "RigidBody in voxel physics is null! This must not happen.");
            if (RigidBody != null)
            {
                var shape = (HkUniformGridShape)GetShape();//RigidBody.GetShape();
                Debug.Assert(shape.Base.IsValid);
                foreach (var entry in newShapes)
                {
                    var coord = entry.Key;
                    var childShape = entry.Value;
                    shape.SetChild(coord.X, coord.Y, coord.Z, childShape, HkReferencePolicy.None);
                }
                m_needsShapeUpdate = true;
                if (InvalidCells.Count != 0)
                    MyPrecalcComponent.PhysicsWithInvalidCells.Add(this);
            }
        }

        internal MyIsoMesh CreateMesh(IMyStorage storage, Vector3I coord, bool lod1Physics = false)
        {
            // mk:NOTE This method must be thread safe. Called from worker threads.

            coord += m_cellsOffset;
            var min = coord << MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
            var max = min + MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS;
            // overlap to neighbor; introduces extra data but it makes logic for raycasts and collisions simpler (no need to check neighbor cells)
            if (!lod1Physics)
            {
                min -= 1;
                max += 2;
                return MyPrecalcComponent.IsoMesher.Precalc(storage, 0, min, max, false, false);
            }
            min >>= 1;
            max >>= 1;
            min -= 1;
            max += 1;
            return MyPrecalcComponent.IsoMesher.Precalc(storage, 1, min, max, false, false);
        }

        internal HkBvCompressedMeshShape CreateShape(MyIsoMesh mesh)
        {
            // mk:NOTE This method must be thread safe. Called from worker threads.

            if (mesh == null || mesh.TrianglesCount == 0 || mesh.VerticesCount == 0)
                return (HkBvCompressedMeshShape)HkShape.Empty;

            List<int> indexList = new List<int>(mesh.TrianglesCount * 3);
            List<Vector3> vertexList = new List<Vector3>(mesh.VerticesCount);

            for (int i = 0; i < mesh.TrianglesCount; i++)
            {
                indexList.Add(mesh.Triangles[i].VertexIndex0);
                indexList.Add(mesh.Triangles[i].VertexIndex2);
                indexList.Add(mesh.Triangles[i].VertexIndex1);
            }

            // mk:TODO Unify denormalizing of positions with what is in MyIsoMesh.
            var positionOffset = mesh.PositionOffset - m_voxelMap.StorageMin * MyVoxelConstants.VOXEL_SIZE_IN_METRES;
            var positions = mesh.Positions.GetInternalArray();
            for (int i = 0; i < mesh.VerticesCount; i++)
            {
                vertexList.Add(positions[i] * mesh.PositionScale + positionOffset);
            }
            using (var cellGeometry = new HkGeometry(vertexList, indexList))
            {
                var result = new HkBvCompressedMeshShape(cellGeometry, null, null, HkWeldingType.None, MyPerGameSettings.PhysicsConvexRadius);
                Debug.Assert(result.Base.ReferenceCount == 1);
                return result;
            }
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
            if (RunningBatchTask != null)
            {
                RunningBatchTask.Cancel();
                RunningBatchTask = null;
            }

            if (ENABLE_AABB_PHANTOM)
            {
                m_aabbPhantom.Dispose();
                m_aabbPhantom = null;
            }
        }

        private void ActivatePhantom()
        {
            if (ENABLE_AABB_PHANTOM)
            {
                var center = GetRigidBodyMatrix().Translation + m_voxelMap.SizeInMetresHalf;
                var size = m_voxelMap.SizeInMetres;
                size *= m_phantomExtend;
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
                HavokWorld.RemovePhantom(m_aabbPhantom);
                MyTrace.Send(TraceWindow.Analytics, "RemovePhantom-after");
                Debug.Assert(m_nearbyEntities.Count == 0, "Inconsistent entities management");
                m_nearbyEntities.Clear();
            }
        }

        private void AabbPhantom_CollidableAdded(ref Havok.HkpCollidableAddedEvent eventData)
        {
            var rb = eventData.RigidBody;
            if (rb == null) // ignore phantoms
                return;
            var entities = rb.GetAllEntities();

            foreach (var entity in entities)
            {
                var grid = entity.Physics as MyGridPhysics;
                var character = entity.Physics as MyPhysicsBody;
                // I get both rigid bodies reported but they don't match, I will only track RB 1
                if (IsDynamicGrid(rb, grid) ||
                    IsCharacter(rb, character))
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

        private void AabbPhantom_CollidableRemoved(ref Havok.HkpCollidableRemovedEvent eventData)
        {
            var rb = eventData.RigidBody;
            if (rb == null) // ignore phantoms
                return;
            var entities = rb.GetAllEntities();
            foreach (var entity in entities)
            {
                var grid = entity.Physics as MyGridPhysics;
                var character = entity.Physics as MyPhysicsBody;
                if (IsDynamicGrid(rb, grid) ||
                    IsCharacter(rb, character))
                {
                    using (m_nearbyEntitiesLock.AcquireExclusiveUsing())
                    {
                        if (character != null)
                        {
                            MyTrace.Send(TraceWindow.Analytics, string.Format("{0} Removed character", character.Entity.EntityId));
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
            var min = Vector3I.Zero;

            Vector3I storageSize = m_voxelMap.Size;
            Vector3I max = new Vector3I(0, 0, 0);
            max.X = storageSize.X >> MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
            max.Y = storageSize.Y >> MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
            max.Z = storageSize.Z >> MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;

            max += min;

            var args = new MyPrecalcJobPhysicsPrefetch.Args
            {
                GeometryCell = new MyCellCoord(0, min),
                Storage = m_voxelMap.Storage,
                TargetPhysics = this,
                Tracker = m_workTracker,
            };
            for (var it = new Vector3I.RangeIterator(ref min, ref max);
                it.IsValid();
                it.GetNext(out args.GeometryCell.CoordInLod))
            {
                MyPrecalcJobPhysicsPrefetch.Start(args);
            }
        }
    }
}
