using Havok;
using Sandbox.Common;

using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;
using VRageRender;
using Sandbox.Engine.Multiplayer;
using VRage.Network;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game;
using VRage.Game;
using VRage.Game.Entity;

namespace Sandbox.Game.Components
{
    public struct MyRopeData
    {
        public float MaxRopeLength;
        public float MinRopeLength; // Minimal rope length - equal to MinRopeLengthFromDummies for dynamic grids or distance between dummies if any of grids is static.
        public float MinRopeLengthFromDummySizes; // Minimal rope length from dummy sizes.
        public float? MinRopeLengthStatic; // Minimal rope length when one of grids is static.
        public float CurrentRopeLength;
        public long HookEntityIdA;
        public long HookEntityIdB;
        public MyRopeDefinition Definition;
    }

    // component dependencies: MySector -> MyRopeComponent -> MyMedievalDefinitions
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation, 600), StaticEventOwner]
    public sealed class MyRopeComponent : MySessionComponentBase
    {
        sealed class InternalRopeData
        { // Id like this to be struct, but due to delegates, I can't do it that way.
            public MyRopeData Public;
            public float TargetRopeLength;
            public HkRopeConstraintData ConstraintData;
            public HkConstraint Constraint;
            public long RopeId;
            public MyCubeGrid GridA;
            public MyCubeGrid GridB;
            public float ImpulseApplied;

            public readonly Action<MyEntity> HandlePhysicsChanged;

            public InternalRopeData()
            {
                HandlePhysicsChanged = (a) => HandleChange();
            }

            private void HandleChange()
            {
                if (GridA != null && GridB != null)
                {
                    MyRopeComponent.RemoveConstraint(this);
                    GridA.OnPhysicsChanged -= HandlePhysicsChanged;
                    if (GridB != GridA)
                    {
                        GridB.OnPhysicsChanged -= HandlePhysicsChanged;
                    }
                }

                GridA = null;
                GridB = null;
                MyEntity hookEntity;
                // Could the parent be removed before it's child? Or is the handler registered on incorrect grid?
                Debug.Assert(MyEntities.EntityExists(Public.HookEntityIdA), "Entity closed but physics changed handler from its parent was not removed.");
                Debug.Assert(MyEntities.EntityExists(Public.HookEntityIdB), "Entity closed but physics changed handler from its parent was not removed.");
                if (MyEntities.TryGetEntityById(Public.HookEntityIdA, out hookEntity))
                    GridA = (MyCubeGrid)hookEntity.Parent;
                if (MyEntities.TryGetEntityById(Public.HookEntityIdB, out hookEntity))
                    GridB = (MyCubeGrid)hookEntity.Parent;

                if (GridA != null && GridB != null)
                {
                    // Handle static to dynamic grid change
                    if (Public.MinRopeLengthStatic != null && !IsStaticConnection() && Public.MinRopeLength == Public.MinRopeLengthStatic.Value)
                        Public.MinRopeLength = Public.MinRopeLengthFromDummySizes;

                    GridA.OnPhysicsChanged += HandlePhysicsChanged;
                    if (GridB != GridA)
                    {
                        GridB.OnPhysicsChanged += HandlePhysicsChanged;
                    }
                    MyRopeComponent.CreateConstraint(this);
                }
            }

            // Static connection is when one grid is static and the other is part of static group.
            public bool IsStaticConnection() 
            {
                if (GridA == null || GridB == null)
                    return false;

                return (GridA.IsStatic && IsGridGroupStatic(GridB)) || (GridB.IsStatic && IsGridGroupStatic(GridA));
            }

            private static bool IsGridGroupStatic(MyCubeGrid grid)
            {
                // Take logical group, because physical contains also rope connections
                var groupNodes = MyCubeGridGroups.Static.GetGroups(GridLinkTypeEnum.Logical).GetGroupNodes(grid);
                if (groupNodes == null)
                    return grid.IsStatic;

                foreach (var node in groupNodes)
                {
                    if (node.IsStatic)
                        return true;
                }

                return false;
            }

        }

        sealed class HookData
        {
            public readonly float Size;
            public readonly Vector3 LocalPivot;

            public HookData(float size, Vector3 localPivot)
            {
                Size = size;
                LocalPivot = localPivot;
            }
        }

        sealed class RopeDrumLimits
        {
            public float MinLength;
            public float MaxLength;
        }

        sealed class WindingData
        {
            public readonly float Radius;
            public readonly Matrix LocalDummy;

            public MatrixD LastDummyWorld;
            public MatrixD CurrentDummyWorld;
            public bool IsUnlocked;

            public float AngleDelta;

            public MySoundPair Sound;
            public MyEntity3DSoundEmitter Emitter;

            public WindingData(float radius, ref Matrix localDummy)
            {
                Radius = radius;
                LocalDummy = localDummy;
            }
        }

        sealed class ReleaseData
        {
            public readonly Vector3 LocalBaseAxis;

            /// <summary>
            /// Given as 2 angles of rotation around X and Y axes.
            /// </summary>
            public Vector2 Orientation;
            public float ThresholdAngleCos;

            public Vector3 LocalAxis;

            public ReleaseData(Vector3 localBaseAxis, float thresholdAngleCos)
            {
                LocalAxis = LocalBaseAxis = localBaseAxis;
                ThresholdAngleCos = thresholdAngleCos;
            }
        }

        sealed class UnlockedWindingData
        {
            public MyEntitySubpart LeftLock;
            public MyEntitySubpart RightLock;
            public MyEntitySubpart Drum;
            public float AngularVelocity;
        }

        public const float DEFAULT_RELEASE_THRESHOLD = (float)(Math.PI / 18.0);
        private static float UNLOCK_OFFSET = 0.1125f;

        public static MyRopeComponent Static;

        private static readonly List<MyPhysics.HitInfo> m_hitInfoBuffer = new List<MyPhysics.HitInfo>();

        private static readonly List<long> m_tmpRopesInitialized = new List<long>();
        private static readonly HashSet<long> m_ropesToRemove = new HashSet<long>();

        private static readonly Dictionary<long, InternalRopeData> m_ropeIdToRope = new Dictionary<long, InternalRopeData>();
        private static readonly Dictionary<long, InternalRopeData> m_ropeIdToRayCastRelease = new Dictionary<long, InternalRopeData>();

        private static readonly Dictionary<long, long> m_hookIdToRopeId = new Dictionary<long, long>();
        private static readonly Dictionary<long, HookData> m_hookIdToHook = new Dictionary<long, HookData>();
        private static readonly Dictionary<long, WindingData> m_hookIdToWinding = new Dictionary<long, WindingData>();
        private static readonly Dictionary<long, ReleaseData> m_hookIdToRelease = new Dictionary<long, ReleaseData>();
        private static readonly Dictionary<long, WindingData> m_hookIdToActiveWinding = new Dictionary<long, WindingData>();
        private static readonly Dictionary<long, ReleaseData> m_hookIdToActiveRelease = new Dictionary<long, ReleaseData>();
        private static readonly Dictionary<long, RopeDrumLimits> m_hookIdToRopeLimits = new Dictionary<long, RopeDrumLimits>();
        private static readonly Dictionary<long, UnlockedWindingData> m_hookIdToUnlockedWinding = new Dictionary<long, UnlockedWindingData>();

        private static readonly HashSet<long> m_ropeIdToInit = new HashSet<long>();

        /// <summary>
        /// Default rope attaching mechanism in creative mode.
        /// </summary>
        private static MyRopeAttacher m_ropeAttacher;

        public static MyRopeAttacher RopeAttacher
        {
            get { return m_ropeAttacher; }
        }

        public override bool IsRequiredByGame
        {
            get { return MyPerGameSettings.Game == GameEnum.ME_GAME; }
        }

        public override void LoadData()
        {
            Static = this;

            if (MySession.Static.CreativeMode)
            {
                MyRopeDefinition defaultRope = null;
                foreach (var rope in MyDefinitionManager.Static.GetRopeDefinitions())
                {
                    if (rope.IsDefaultCreativeRope)
                    {
                        defaultRope = rope;
                        break;
                    }
                }

                if (defaultRope != null)
                    m_ropeAttacher = new MyRopeAttacher(defaultRope);
            }

            base.LoadData();
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            // Finalize any rope initialization dependent on other objects.
            // Note that entities might be available at some later time (replication).
            foreach (var ropeId in m_ropeIdToInit)
            {
                InternalRopeData ropeData;
                if (m_ropeIdToRope.TryGetValue(ropeId, out ropeData) && ropeData != null)
                {
                    HookData hookA, hookB;
                    if (!m_hookIdToHook.TryGetValue(ropeData.Public.HookEntityIdA, out hookA) || hookA == null ||
                        !m_hookIdToHook.TryGetValue(ropeData.Public.HookEntityIdB, out hookB) || hookB == null)
                    {
                        continue;
                    }

                    MyEntity entityA;
                    if (!MyEntities.TryGetEntityById(ropeData.Public.HookEntityIdA, out entityA) || entityA == null)
                        continue;

                    MyEntity entityB;
                    if (!MyEntities.TryGetEntityById(ropeData.Public.HookEntityIdB, out entityB) || entityB == null)
                        continue;

                    ropeData.GridA = (MyCubeGrid)entityA.Parent;
                    ropeData.GridB = (MyCubeGrid)entityB.Parent;

                    if (ropeData.Public.MinRopeLength == 0f)
                    {
                        // Rope shouldn't cause collision of objects, so using a minimum length based on dummies.
                        ropeData.Public.MinRopeLength = (hookA.Size + hookB.Size) * 0.85f;
                        ropeData.Public.MinRopeLengthFromDummySizes = ropeData.Public.MinRopeLength;
                        ropeData.Public.MinRopeLengthStatic = null;

                        // If rope is connected with static grid(s) then use current dummy distance as min rope length.
                        if (ropeData.IsStaticConnection())
                        {
                            Vector3D worldPivotA = Vector3D.Transform(hookA.LocalPivot, entityA.WorldMatrix);
                            Vector3D worldPivotB = Vector3D.Transform(hookB.LocalPivot, entityB.WorldMatrix);
                            float dst = (float)Vector3D.Distance(worldPivotA, worldPivotB);
                            ropeData.Public.MinRopeLength = Math.Max(ropeData.Public.MinRopeLength, dst);
                            ropeData.Public.MinRopeLengthStatic = ropeData.Public.MinRopeLength;
                        }
                    }

                    ApplyRopeLimits(ropeData);

                    ropeData.GridA.OnPhysicsChanged += ropeData.HandlePhysicsChanged;
                    if (ropeData.GridB != ropeData.GridA)
                        ropeData.GridB.OnPhysicsChanged += ropeData.HandlePhysicsChanged;

                    entityA.OnClosing += hookEntity_OnClosing;
                    entityB.OnClosing += hookEntity_OnClosing;

                    if (Sync.IsServer)
                    {
                        MyRope rope;
                        if (!MyEntities.TryGetEntityById(ropeId, out rope))
                        {
                            rope = new MyRope();
                            rope.EntityId = ropeId;
                            MyEntities.RaiseEntityCreated(rope);
                            MyEntities.Add(rope);

                        }
                    }
                    else
                    {
                        if (!MyEntities.EntityExists(ropeId))
                            continue;
                    }

                    CreateConstraint(ropeData);

                    WindingData winding;
                    if (m_hookIdToWinding.TryGetValue(ropeData.Public.HookEntityIdA, out winding) && !winding.IsUnlocked)
                        ActivateWinding(ropeData.Public.HookEntityIdA, winding, ropeData.Public.Definition);
                    if (m_hookIdToWinding.TryGetValue(ropeData.Public.HookEntityIdB, out winding) && !winding.IsUnlocked)
                        ActivateWinding(ropeData.Public.HookEntityIdB, winding, ropeData.Public.Definition);
                    ActivateRelease(ropeData.Public.HookEntityIdA);
                    ActivateRelease(ropeData.Public.HookEntityIdB);

                    m_tmpRopesInitialized.Add(ropeId);
                }
            }

            foreach (var ropeId in m_tmpRopesInitialized)
                m_ropeIdToInit.Remove(ropeId);

            m_tmpRopesInitialized.Clear();

            // Commented out - data can be in incosistent state due to replication
            // TODO: try to fix check even for replication
            //AssertWindingLocksConsistent();

            // Update winding components which have ropes attached to them
            foreach (var entry in m_hookIdToActiveWinding)
            {
                var entity = MyEntities.GetEntityById(entry.Key);
                Debug.Assert(entity != null);
                if (entity == null)
                    continue;

                UpdateDummyWorld(entity.PositionComp, entry.Value);
                var winding = entry.Value;
                Debug.Assert(!winding.IsUnlocked);
                UpdateWindingAngleDelta(winding);
                m_ropeIdToRope[m_hookIdToRopeId[entry.Key]].TargetRopeLength += winding.AngleDelta * winding.Radius;
                if (winding.Sound != null)
                {
                    const float threshold = 0.0001f;
                    bool shouldPlaySound = winding.AngleDelta < -threshold || threshold < winding.AngleDelta;
                    if (shouldPlaySound != winding.Emitter.IsPlaying)
                    {
                        if (shouldPlaySound)
                            winding.Emitter.PlaySound(winding.Sound);
                        else
                            winding.Emitter.StopSound(false);
                    }
                }
            }

            if (Sync.IsServer)
            {
                // Update release components which have ropes attached to them
                foreach (var entry in m_hookIdToActiveRelease)
                {
                    var hookEntity = MyEntities.GetEntityById(entry.Key);
                    Debug.Assert(hookEntity != null);
                    if (hookEntity == null)
                        continue;

                    var ropeId = m_hookIdToRopeId[entry.Key];
                    var ropeData = m_ropeIdToRope[ropeId];

                    MyRope rope;
                    if (MyEntities.TryGetEntityById(ropeId, out rope) && rope != null)
                    {
                        var ropeRender = rope.Render as MyRenderComponentRope;
                        var ropeDirection = Vector3.Normalize(ropeRender.WorldPivotB - ropeRender.WorldPivotA);
                        if (entry.Key == ropeData.Public.HookEntityIdB)
                            ropeDirection *= -1; // rope direction must be switched for dot product with one of the releases
                        var worldAxis = Vector3.TransformNormal(entry.Value.LocalAxis, hookEntity.WorldMatrix);
                        if (Vector3.Dot(worldAxis, ropeDirection) > entry.Value.ThresholdAngleCos)
                            m_ropesToRemove.Add(ropeId);
                    }
                }

                // Do raycasts for tearable ropes and see which one has been crossed by something
                foreach (var entry in m_ropeIdToRayCastRelease)
                {
                    var ropeData = entry.Value;
                    MyRope rope;
                    if (MyEntities.TryGetEntityById(ropeData.RopeId, out rope) && rope != null)
                    {
                        var render = (MyRenderComponentRope)rope.Render;

                        using (m_hitInfoBuffer.GetClearToken())
                        {
                            MyPhysics.CastRay(render.WorldPivotA, render.WorldPivotB, m_hitInfoBuffer);
                            foreach (var hitInfo in m_hitInfoBuffer)
                            {
                                var entity = hitInfo.HkHitInfo.GetHitEntity();
                                if (entity != ropeData.GridA && entity.EntityId != ropeData.Public.HookEntityIdA &&
                                    entity != ropeData.GridB && entity.EntityId != ropeData.Public.HookEntityIdB)
                                {
                                    m_ropesToRemove.Add(ropeData.RopeId);
                                }
                            }
                        }
                    }
                }

                // Remove ropes released by some mechanism (release hooks, raycasts, etc.)
                foreach (var ropeId in m_ropesToRemove)
                {
                    CloseRopeRequest(ropeId);
                }
                m_ropesToRemove.Clear();
            }

            // Update ropes (length and constraint axis)
            foreach (var ropeData in m_ropeIdToRope.Values)
            {
                if (ropeData.Constraint == null)
                    continue; // Ropes with constraint (connecting different RB) could be stored separately to avoid this test.

                var constraint = ropeData.Constraint;

                if (constraint.RigidBodyA == null || constraint.RigidBodyB == null)
                { //Rigidbody changed (destruction?)
                    RemoveConstraint(ropeData);
                    CreateConstraint(ropeData);
                    constraint = ropeData.Constraint;
                }

                const float maxDelta = 0.1f;
                const float threshold = 1e-3f;

                MyRope rope;
                if (MyEntities.TryGetEntityById(ropeData.RopeId, out rope) && rope != null)
                {
                    var render = rope.Render as MyRenderComponentRope;
                    float lengthDelta = ropeData.TargetRopeLength - ropeData.Public.CurrentRopeLength;
                    if ((lengthDelta < -threshold && ropeData.Public.CurrentRopeLength > ropeData.Public.MinRopeLength) ||
                        (lengthDelta > threshold && ropeData.Public.CurrentRopeLength < ropeData.Public.MaxRopeLength))
                    {
                        float currentDistance = (float)(render.WorldPivotA - render.WorldPivotB).Length();
                        float newTargetLength = MathHelper.Clamp(ropeData.Public.CurrentRopeLength + lengthDelta,
                            ropeData.Public.MinRopeLength,
                            ropeData.Public.MaxRopeLength);
                        // ensure that constraint limit is never much shorter than distance of objects
                        // prevents problems after load
                        lengthDelta = Math.Max(newTargetLength - currentDistance, -maxDelta);
                        ropeData.Public.CurrentRopeLength = currentDistance + lengthDelta;
                        ropeData.TargetRopeLength = ropeData.Public.CurrentRopeLength;
                        ropeData.ConstraintData.LinearLimit = ropeData.Public.CurrentRopeLength;
                        constraint.RigidBodyA.Activate();
                        constraint.RigidBodyB.Activate();
                    }
                    else
                    {
                        ropeData.TargetRopeLength = ropeData.Public.CurrentRopeLength;
                    }
                }

                // mk:TODO Constraints getting removed from world when collision/destruction happens.
                // mk:TODO Remove once constraints are stored along with both bodies that they connect.
                // Or once the problem with inconsistent constraints after disabling physics is solved.
                // Assert commented out - replaced with remove/add constraint again.
                //Debug.Assert(constraint.InWorld);
                if (!constraint.InWorld)
                {
                    RemoveConstraint(ropeData);
                    constraint = null;

                    CreateConstraint(ropeData);
                    constraint = ropeData.Constraint;

                    if (constraint == null)
                        continue;

                    Debug.Assert(constraint.InWorld);
                }

                // Update constraint axis.
                // mk:TODO Move impulse reading until after simulation? Update of constraint axis should still happen here. Possibly rename to UpdateConstraintAxis if it doesn't read impulse.
                if (constraint.InWorld)
                    ropeData.ImpulseApplied = ropeData.ConstraintData.Update(constraint);
            }

            if (MyFakes.ENABLE_ROPE_UNWINDING_TORQUE)
            {
                foreach (var entry in m_hookIdToActiveWinding)
                {
                    var windingData = entry.Value;
                    var ropeData = m_ropeIdToRope[m_hookIdToRopeId[entry.Key]];

                    MyRope rope;
                    if (MyEntities.TryGetEntityById(ropeData.RopeId, out rope) && rope != null)
                    {
                        var render = rope.Render as MyRenderComponentRope;
                        MyPhysicsBody physics;
                        float torqueMultiplier;
                        if (ropeData.Public.HookEntityIdA == entry.Key)
                        {
                            physics = ropeData.GridA.Physics;
                        }
                        else
                        {
                            physics = ropeData.GridB.Physics;
                        }
                        torqueMultiplier = -0.75f;
                        var tau = ComputeTorqueFromRopeImpulse(windingData, ropeData,
                            (Vector3)(render.WorldPivotB - render.WorldPivotA),
                            (Vector3)(windingData.CurrentDummyWorld.Translation - physics.CenterOfMassWorld));
                        // mk:NOTE Internally applies impulse. Might be more stable/smooth if it was applying torque over time inside solver.
                        physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, null, null, tau * torqueMultiplier);
                    }
                }
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            // Commented out - data can be in incosistent state due to replication
            // TODO: try to fix check even for replication
            //AssertWindingLocksConsistent();

            // Update world positions of ropes and their render components
            foreach (var entry in m_ropeIdToRope)
            {
                var ropeData = entry.Value;
                MyEntity entityA, entityB;
                if (MyEntities.TryGetEntityById(ropeData.Public.HookEntityIdA, out entityA) && entityA != null &&
                    MyEntities.TryGetEntityById(ropeData.Public.HookEntityIdB, out entityB) && entityB != null)
                {
                    var hookA = m_hookIdToHook[ropeData.Public.HookEntityIdA];
                    var hookB = m_hookIdToHook[ropeData.Public.HookEntityIdB];

                    Vector3D worldPivotA = Vector3D.Transform(hookA.LocalPivot, entityA.WorldMatrix);
                    Vector3D worldPivotB = Vector3D.Transform(hookB.LocalPivot, entityB.WorldMatrix);

                    MyRope rope;
                    if (!MyEntities.TryGetEntityById(ropeData.RopeId, out rope))
                        continue;

                    var render = rope.Render as MyRenderComponentRope;
                    if (worldPivotA != render.WorldPivotA || worldPivotB != render.WorldPivotB)
                    {
                        render.WorldPivotA = worldPivotA;
                        render.WorldPivotB = worldPivotB;
                        var worldCenter = (worldPivotA + worldPivotB) * 0.5;
                        var localPivotA = (Vector3)(worldPivotA - worldCenter);
                        var localPivotB = (Vector3)(worldPivotB - worldCenter);
                        var localAabb = BoundingBox.CreateInvalid();
                        localAabb.Include(ref localPivotA);
                        localAabb.Include(ref localPivotB);
                        localAabb.Inflate(0.25f);
                        rope.PositionComp.LocalAABB = localAabb;
                        var worldMatrix = rope.PositionComp.WorldMatrix;
                        worldMatrix.Translation = worldCenter;
                        rope.PositionComp.SetWorldMatrix(worldMatrix, forceUpdate: true);
                    }
                }
            }

            foreach (var entry in m_hookIdToUnlockedWinding)
            {
                var hookId = entry.Key;
                var unlockedData = entry.Value;
                if (unlockedData.Drum != null && MyEntities.EntityExists(hookId))
                {
                    var drumPositionComponent = unlockedData.Drum.PositionComp;
                    Debug.Assert(!unlockedData.Drum.Closed);
                    if (unlockedData.AngularVelocity > 0)
                    {
                        drumPositionComponent.LocalMatrix *= Matrix.CreateRotationX(unlockedData.AngularVelocity * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
                        unlockedData.AngularVelocity -= 0.1f + unlockedData.AngularVelocity * 0.1f;
                        if (unlockedData.AngularVelocity < 0f)
                            unlockedData.AngularVelocity = 0f;
                    }
                    var winding = m_hookIdToWinding[hookId];
                    UpdateDummyWorld(MyEntities.GetEntityById(hookId).PositionComp, winding);
                    Debug.Assert(winding.IsUnlocked);
                    UpdateWindingAngleDelta(winding);
                    drumPositionComponent.LocalMatrix *= Matrix.CreateRotationX(-winding.AngleDelta);
                }
            }

            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_ROPES)
            {
                foreach (var entry in m_hookIdToWinding)
                {
                    var winding = entry.Value;
                    var currentDummyWorld = Matrix.Multiply(winding.LocalDummy, MyEntities.GetEntityById(entry.Key).WorldMatrix);
                    MyRenderProxy.DebugDrawCylinder(currentDummyWorld, -0.5 * Vector3D.UnitZ, 0.5 * Vector3D.UnitZ, winding.Radius, Color.White, 1f, true, false);
                    MyRenderProxy.DebugDrawAxis((MatrixD)currentDummyWorld, 1f, false);
                }

                foreach (var entry in m_hookIdToRelease)
                {
                    var hookEntity = MyEntities.GetEntityById(entry.Key);
                    var worldPosition = Vector3.Transform(m_hookIdToHook[entry.Key].LocalPivot, hookEntity.WorldMatrix);
                    var worldAxis = Vector3.TransformNormal(entry.Value.LocalAxis, hookEntity.WorldMatrix);
                    var baseVector = Vector3D.CalculatePerpendicularVector(worldAxis) * Math.Sin(Math.Acos(entry.Value.ThresholdAngleCos));
                    worldAxis *= entry.Value.ThresholdAngleCos;
                    MyRenderProxy.DebugDrawCone(worldPosition + worldAxis, -worldAxis, baseVector, Color.White, true);
                }

                foreach (var entry in m_ropeIdToRope)
                {
                    MyRope rope;
                    if (MyEntities.TryGetEntityById(entry.Value.RopeId, out rope))
                    {
                        var render = rope.Render as MyRenderComponentRope;
                        var direction = Vector3D.Normalize(render.WorldPivotA - render.WorldPivotB);
                        var center = rope.PositionComp.GetPosition();
                        MyRenderProxy.DebugDrawLine3D(
                            center - 0.5f * direction * entry.Value.Public.CurrentRopeLength,
                            center + 0.5f * direction * entry.Value.Public.CurrentRopeLength,
                            Color.Red, Color.Green, false);
                        MyRenderProxy.DebugDrawText3D(center,
                            string.Format("Impulse: {0}, Min: {1}, Max: {2}, Current: {3}",
                                entry.Value.ImpulseApplied.ToString("#.00"),
                                entry.Value.Public.MinRopeLength.ToString("#.00"),
                                entry.Value.Public.MaxRopeLength.ToString("#.00"),
                                entry.Value.Public.CurrentRopeLength.ToString("#.00")),
                            Color.White, 1f, true, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
                    }
                }
            }

        }

        protected override void UnloadData()
        {
            Debug.Assert(m_ropeIdToRope.Count == 0, "All ropes should have been removed by now.");
            Debug.Assert(m_hookIdToHook.Count == 0, "All hooks should have been removed by now.");
            m_ropeIdToRope.Clear();
            m_hookIdToHook.Clear();
            m_hookIdToWinding.Clear();
            m_hookIdToRelease.Clear();
            m_hookIdToRopeId.Clear();
            m_ropesToRemove.Clear();
            foreach (var winding in m_hookIdToActiveWinding.Values)
            {
                if (winding.Emitter != null)
                    winding.Emitter.StopSound(true);
            }
            m_hookIdToActiveWinding.Clear();
            m_hookIdToActiveRelease.Clear();
            m_ropeIdToRayCastRelease.Clear();
            m_hookIdToRopeLimits.Clear();
            m_hookIdToUnlockedWinding.Clear();
            m_ropeIdToInit.Clear();

            if (m_ropeAttacher != null)
            {
                m_ropeAttacher.Clear();
                m_ropeAttacher = null;
            }

            Static = null;

            base.UnloadData();
        }

        public static void AddHook(long hookEntityId, float size, Vector3 localPivot)
        {
            m_hookIdToHook.Add(hookEntityId, new HookData(size, localPivot));
        }

        public static void AddHookWinding(long hookEntityId, float radius, ref Matrix localDummy)
        {
            m_hookIdToWinding.Add(hookEntityId, new WindingData(radius, ref localDummy));
        }

        public static void AddHookRelease(long hookEntityId, Vector3 localAxis, float thresholdAngleCos)
        {
            m_hookIdToRelease.Add(hookEntityId, new ReleaseData(localAxis, thresholdAngleCos));
        }

        public static long AddRopeData(long hookEntityIdA, long hookEntityIdB, MyRopeDefinition ropeDefinition, long ropeId)
        {
            MyEntity entityA, entityB;
            if (!MyEntities.TryGetEntityById(hookEntityIdA, out entityA))
                return 0;

            if (!MyEntities.TryGetEntityById(hookEntityIdB, out entityB))
                return 0;

            var hookA = m_hookIdToHook[hookEntityIdA];
            var hookB = m_hookIdToHook[hookEntityIdB];
            var posA = Vector3D.Transform(hookA.LocalPivot, entityA.WorldMatrix);
            var posB = Vector3D.Transform(hookB.LocalPivot, entityB.WorldMatrix);
            var maxRopeLength = (float)(posA - posB).Length();
            var ropeData = new MyRopeData
            {
                HookEntityIdA = hookEntityIdA,
                HookEntityIdB = hookEntityIdB,
                MaxRopeLength = maxRopeLength,
                CurrentRopeLength = maxRopeLength,
                Definition = ropeDefinition,
            };
            return AddRopeData(ropeData, ropeId);
        }

        public static long AddRopeData(MyRopeData publicData, long ropeId)
        {
            if (ropeId == 0)
                ropeId = MyEntityIdentifier.AllocateId();

            // Avoid referencing external objects during addition. They might not have been created yet.
            var ropeData = new InternalRopeData
            {
                Public = publicData,
                RopeId = ropeId,
            };

            Debug.Assert(!m_hookIdToRopeId.ContainsKey(ropeData.Public.HookEntityIdA), "Hook already has rope attached!");
            Debug.Assert(!m_hookIdToRopeId.ContainsKey(ropeData.Public.HookEntityIdB), "Hook already has rope attached!");
            Debug.Assert(!m_ropeIdToRope.ContainsKey(ropeData.RopeId), "Rope with given ID already exists!");
            m_ropeIdToRope[ropeData.RopeId] = ropeData;
            m_hookIdToRopeId[ropeData.Public.HookEntityIdA] = ropeId;
            m_hookIdToRopeId[ropeData.Public.HookEntityIdB] = ropeId;

            if (ropeData.Public.Definition.EnableRayCastRelease && Sync.IsServer)
            {
                m_ropeIdToRayCastRelease.Add(ropeId, ropeData);
            }

            m_ropeIdToInit.Add(ropeId);

            return ropeId;
        }

        public static void RemoveHook(long hookEntityId)
        {
            RemoveRopeOnHookInternal(hookEntityId);
            m_hookIdToHook.Remove(hookEntityId);
            m_hookIdToRelease.Remove(hookEntityId);
            m_hookIdToWinding.Remove(hookEntityId);
            m_hookIdToUnlockedWinding.Remove(hookEntityId);
        }

        static void RemoveRopeOnHookInternal(long hookEntityId)
        {
            long ropeId;
            if (m_hookIdToRopeId.TryGetValue(hookEntityId, out ropeId))
            {
                if (!Sync.IsServer)
                {
                    if (MyEntities.CloseAllowed)
                        RemoveRopeData(ropeId);
                }
                else
                    CloseRopeRequest(ropeId);
            }
        }

        public static void TryRemoveRopeOnHook(long hookEntityId)
        {
            long ropeId;
            if (m_hookIdToRopeId.TryGetValue(hookEntityId, out ropeId))
            {
                CloseRopeRequest(ropeId);
            }
        }

        public static void RemoveRopeData(long ropeId)
        {
            if (!m_ropeIdToRope.ContainsKey(ropeId))
                return;

            var ropeData = m_ropeIdToRope[ropeId];

            m_ropeIdToInit.Remove(ropeId);
            m_ropeIdToRope.Remove(ropeId);
            m_ropeIdToRayCastRelease.Remove(ropeId);
            m_hookIdToRopeId.Remove(ropeData.Public.HookEntityIdA);
            m_hookIdToRopeId.Remove(ropeData.Public.HookEntityIdB);
            DeactivateWinding(ropeData.Public.HookEntityIdA);
            DeactivateWinding(ropeData.Public.HookEntityIdB);
            m_hookIdToActiveRelease.Remove(ropeData.Public.HookEntityIdA);
            m_hookIdToActiveRelease.Remove(ropeData.Public.HookEntityIdB);

            RemoveConstraint(ropeData);

            MyEntity entity;
            if (MyEntities.TryGetEntityById(ropeData.Public.HookEntityIdA, out entity))
            {
                entity.OnClosing -= hookEntity_OnClosing;
            }
            if (MyEntities.TryGetEntityById(ropeData.Public.HookEntityIdB, out entity))
            {
                entity.OnClosing -= hookEntity_OnClosing;
            }

            if (ropeData.GridA != null && ropeData.GridB != null)
            {
                ropeData.GridA.OnPhysicsChanged -= ropeData.HandlePhysicsChanged;
                if (ropeData.GridB != ropeData.GridA)
                    ropeData.GridB.OnPhysicsChanged -= ropeData.HandlePhysicsChanged;
            }
        }

        public static bool HasRope(long hookEntityId)
        {
            return m_hookIdToRopeId.ContainsKey(hookEntityId);
        }

        public static bool AreGridsConnected(MyCubeGrid grid1, MyCubeGrid grid2)
        {
            foreach (var ropeData in m_ropeIdToRope.Values)
            {
                if ((ropeData.GridA == grid1 && ropeData.GridB == grid2) || (ropeData.GridA == grid2 && ropeData.GridB == grid1))
                    return true;
            }

            return false;
        }

        public static bool HasRelease(long hookEntityId)
        {
            return m_hookIdToRelease.ContainsKey(hookEntityId);
        }

        public static void GetRopeData(long ropeEntityId, out MyRopeData ropeData)
        {
            ropeData = m_ropeIdToRope[ropeEntityId].Public;
        }

        public static bool TryGetRopeData(long ropeEntityId, out MyRopeData ropeData)
        {
            ropeData = default(MyRopeData);
            InternalRopeData internalRopeData;
            if (m_ropeIdToRope.TryGetValue(ropeEntityId, out internalRopeData))
            {
                ropeData = internalRopeData.Public;
                return true;
            }

            return false;
        }

        public static void SetRopeData(long ropeEntityId, float minRopeLength, float maxRopeLength)
        {
            var ropeData = m_ropeIdToRope[ropeEntityId];
            ropeData.Public.MinRopeLength = minRopeLength;
            ropeData.Public.MaxRopeLength = maxRopeLength;
            ApplyRopeLimits(ropeData);
        }

        public static bool TryGetRopeForHook(long hookEntityId, out long ropeEntityId)
        {
            return m_hookIdToRopeId.TryGetValue(hookEntityId, out ropeEntityId);
        }

        public static void GetHookData(long hookEntityId, out Vector3 localPivot)
        {
            localPivot = m_hookIdToHook[hookEntityId].LocalPivot;
        }

        public static void GetReleaseData(long hookEntityId, out Vector3 localBaseAxis, out Vector2 orientation, out float thresholdAngleCos)
        {
            var release = m_hookIdToRelease[hookEntityId];
            localBaseAxis = release.LocalBaseAxis;
            orientation = release.Orientation;
            thresholdAngleCos = release.ThresholdAngleCos;
        }

        public static void SetReleaseData(long hookEntityId, Vector2 orientation, float thresholdAngleCos)
        {
            ReleaseData release;
            if (m_hookIdToRelease.TryGetValue(hookEntityId, out release))
            {
                release.Orientation = orientation;
                release.ThresholdAngleCos = thresholdAngleCos;
                ComputeLocalReleaseAxis(release.LocalBaseAxis, release.Orientation, out release.LocalAxis);
            }
        }

        public static void ComputeLocalReleaseAxis(Vector3 localBaseAxis, Vector2 orientation, out Vector3 localAxis)
        {
            localAxis = Vector3.TransformNormal(localBaseAxis, Matrix.CreateRotationY(orientation.X) * Matrix.CreateRotationX(orientation.Y));
        }

        public static bool IsWindingUnlocked(long hookEntityId)
        {
            return m_hookIdToWinding[hookEntityId].IsUnlocked;
        }

        public static void AddOrSetDrumRopeLimits(long hookEntityId, float minRopeLength, float maxRopeLength)
        {
            RopeDrumLimits limits;
            if (!m_hookIdToRopeLimits.TryGetValue(hookEntityId, out limits))
            {
                limits = new RopeDrumLimits();
                m_hookIdToRopeLimits[hookEntityId] = limits;
            }
            limits.MinLength = minRopeLength;
            limits.MaxLength = maxRopeLength;

            long ropeId;
            if (m_hookIdToRopeId.TryGetValue(hookEntityId, out ropeId))
            {
                var ropeData = m_ropeIdToRope[ropeId];
                ApplyRopeLimits(ropeData, limits);
            }
        }

        public static void GetDrumRopeLimits(long hookEntityId, out float minRopeLength, out float maxRopeLength)
        {
            RopeDrumLimits limits = m_hookIdToRopeLimits[hookEntityId];
            minRopeLength = limits.MinLength;
            maxRopeLength = limits.MaxLength;
        }

        public static void RemoveDrumRopeLimits(long hookEntityId)
        {
            m_hookIdToRopeLimits.Remove(hookEntityId);
        }

        public static bool CanConnectHooks(long hookIdFrom, long hookIdTo, MyRopeDefinition ropeDefinition)
        {
            bool result = true;
            RopeDrumLimits limitsA, limitsB;
            m_hookIdToRopeLimits.TryGetValue(hookIdFrom, out limitsA);
            m_hookIdToRopeLimits.TryGetValue(hookIdTo, out limitsB);
            if (limitsA != null && limitsB != null)
            {
                result = false;
            }
            else if (limitsA != null || limitsB != null)
            {
                var from = MyEntities.GetEntityById(hookIdFrom);
                var to = MyEntities.GetEntityById(hookIdTo);
                var distanceSqr = (to.PositionComp.GetPosition() - from.PositionComp.GetPosition()).LengthSquared();
                if (limitsA != null) result = result && (distanceSqr < (limitsA.MaxLength * limitsA.MaxLength));
                if (limitsB != null) result = result && (distanceSqr < (limitsB.MaxLength * limitsB.MaxLength));
            }

            return result;
        }

        public static bool HasEntityWinding(long hookEntityId)
        {
            return m_hookIdToWinding.ContainsKey(hookEntityId);
        }

        public static void UnlockWinding(long hookEntityId)
        {
            var winding = m_hookIdToWinding[hookEntityId];
            if (!winding.IsUnlocked)
            {
                var unlockedWindingData = new UnlockedWindingData();

                long ropeId;
                // locking and unlocking doesn't require rope to be attached, but when there is rope we reset it's length
                if (m_hookIdToRopeId.TryGetValue(hookEntityId, out ropeId))
                {
                    var rope = m_ropeIdToRope[ropeId];
                    rope.TargetRopeLength = rope.Public.CurrentRopeLength = rope.Public.MaxRopeLength;
                    if (rope.Constraint != null)
                    {
                        rope.ConstraintData.LinearLimit = rope.Public.CurrentRopeLength;
                        if (rope.Constraint.RigidBodyA != null) rope.Constraint.RigidBodyA.Activate();
                        if (rope.Constraint.RigidBodyB != null) rope.Constraint.RigidBodyB.Activate();
                    }

                    MyRope ropeEntity;
                    if (MyEntities.TryGetEntityById(ropeId, out ropeEntity))
                    {
                        var render = ropeEntity.Render as MyRenderComponentRope;
                        MyCubeGrid grid;
                        if (rope.Public.HookEntityIdA == hookEntityId)
                        {
                            grid = rope.GridA;
                        }
                        else
                        {
                            grid = rope.GridB;
                        }
                        MyPhysicsBody physics = (grid != null) ? grid.Physics : null;

                        if (physics != null)
                        {
                            var torque = ComputeTorqueFromRopeImpulse(winding, rope,
                                (Vector3)(render.WorldPivotB - render.WorldPivotA),
                                (Vector3)(winding.CurrentDummyWorld.Translation - physics.CenterOfMassWorld));
                            // Should be (torque/mass) * dt, but but it looks better this way. :)
                            // Even easier hack might be to just take last impulse applied instead of bothering with these computations.
                            unlockedWindingData.AngularVelocity = torque.Length();
                        }
                    }
                }

                DeactivateWinding(hookEntityId, winding);

                var subparts = MyEntities.GetEntityById(hookEntityId).Subparts;
                subparts.TryGetValue("LeftLock", out unlockedWindingData.LeftLock);
                subparts.TryGetValue("RightLock", out unlockedWindingData.RightLock);
                subparts.TryGetValue("Drum", out unlockedWindingData.Drum);
                m_hookIdToUnlockedWinding.Add(hookEntityId, unlockedWindingData);
                MoveSubpart(unlockedWindingData.LeftLock, new Vector3(-UNLOCK_OFFSET, 0f, 0f));
                MoveSubpart(unlockedWindingData.RightLock, new Vector3(+UNLOCK_OFFSET, 0f, 0f));

                winding.IsUnlocked = true;
            }
        }

        public static void LockWinding(long hookEntityId)
        {
            var winding = m_hookIdToWinding[hookEntityId];
            if (winding.IsUnlocked)
            {
                var unlockedWindingData = m_hookIdToUnlockedWinding[hookEntityId];
                m_hookIdToUnlockedWinding.Remove(hookEntityId);
                MoveSubpart(unlockedWindingData.LeftLock, new Vector3(+UNLOCK_OFFSET, 0f, 0f));
                MoveSubpart(unlockedWindingData.RightLock, new Vector3(-UNLOCK_OFFSET, 0f, 0f));

                // only when there is a rope do we activate winding updates
                long ropeId;
                if (m_hookIdToRopeId.TryGetValue(hookEntityId, out ropeId))
                {
                    var rope = m_ropeIdToRope[ropeId];
                    ActivateWinding(hookEntityId, winding, rope.Public.Definition);
                    float unwoundRopeLength = rope.Public.MaxRopeLength;
                    MyRope ropeEntity;
                    if (MyEntities.TryGetEntityById(ropeId, out ropeEntity))
                    {
                        var render = ropeEntity.Render as MyRenderComponentRope;
                        unwoundRopeLength = (float)(render.WorldPivotB - render.WorldPivotA).Length();
                    }
                    rope.Public.CurrentRopeLength = MathHelper.Clamp(unwoundRopeLength, rope.Public.MinRopeLength, rope.Public.MaxRopeLength);
                    rope.TargetRopeLength = rope.Public.CurrentRopeLength;
                }

                winding.IsUnlocked = false;
            }
        }

        private static void ActivateWinding(long hookEntityId, WindingData winding, MyRopeDefinition attachedRope)
        {
            Debug.Assert(winding != null);
            Debug.Assert(!m_hookIdToActiveWinding.ContainsKey(hookEntityId), "Winding already added");
            Debug.Assert(m_hookIdToRopeId.ContainsKey(hookEntityId));

            m_hookIdToActiveWinding[hookEntityId] = winding;
            var hookEntity = MyEntities.GetEntityById(hookEntityId);
            UpdateDummyWorld(hookEntity.PositionComp, winding);
            if (attachedRope.WindingSound != null)
            {
                winding.Sound = attachedRope.WindingSound;
                winding.Emitter = new MyEntity3DSoundEmitter(hookEntity);
            }
        }

        private static void DeactivateWinding(long hookEntityId, WindingData winding = null)
        {
            if (winding == null)
            {
                m_hookIdToActiveWinding.TryGetValue(hookEntityId, out winding);
            }

            if (winding != null && winding.Emitter != null)
            {
                winding.Emitter.StopSound(false);
                winding.Emitter = null;
                winding.Sound = null;
            }

            m_hookIdToActiveWinding.Remove(hookEntityId);
        }

        private static void ActivateRelease(long hookEntityId)
        {
            ReleaseData release;
            if (!m_hookIdToRelease.TryGetValue(hookEntityId, out release))
                return;

            if (Sync.IsServer)
                m_hookIdToActiveRelease.Add(hookEntityId, release);
        }

        private static void CreateConstraint(InternalRopeData ropeData)
        {
            CreateConstraint(ropeData,
                m_hookIdToHook[ropeData.Public.HookEntityIdA],
                m_hookIdToHook[ropeData.Public.HookEntityIdB],
                (MyCubeBlock)MyEntities.GetEntityById(ropeData.Public.HookEntityIdA),
                (MyCubeBlock)MyEntities.GetEntityById(ropeData.Public.HookEntityIdB));
        }

        private static void CreateConstraint(InternalRopeData ropeData, HookData hookA, HookData hookB, MyCubeBlock blockA, MyCubeBlock blockB)
        {
            if (ropeData.GridA == ropeData.GridB)
                return;

            var physicsA = blockA.CubeGrid.Physics;
            var physicsB = blockB.CubeGrid.Physics;
            if (physicsA == null || physicsB == null || !physicsA.RigidBody.InWorld || !physicsB.RigidBody.InWorld)
                return;

            Vector3D posA, posB;
            ComputeLocalPosition(blockA, hookA, out posA);
            ComputeLocalPosition(blockB, hookB, out posB);

            ropeData.ConstraintData = new HkRopeConstraintData();
            {
                Vector3 posAf = (Vector3)posA;
                Vector3 posBf = (Vector3)posB;
                ropeData.ConstraintData.SetInBodySpace( posAf,  posBf, physicsA, physicsB);
            }
            posA = Vector3D.Transform(posA, physicsA.GetWorldMatrix());
            posB = Vector3D.Transform(posB, physicsB.GetWorldMatrix());
            ropeData.ConstraintData.LinearLimit = ropeData.Public.CurrentRopeLength;
            ropeData.ConstraintData.Strength = 0.6f;
            ropeData.Constraint = new HkConstraint(physicsA.RigidBody, physicsB.RigidBody, ropeData.ConstraintData);
            physicsA.AddConstraint(ropeData.Constraint);
            ropeData.Constraint.Enabled = true;
            ropeData.ConstraintData.Update(ropeData.Constraint);

            MyCubeGrid.CreateGridGroupLink(GridLinkTypeEnum.Physical, ropeData.RopeId, blockA.CubeGrid, blockB.CubeGrid);

            physicsA.RigidBody.Activate();
            physicsB.RigidBody.Activate();
        }

        private static void RemoveConstraint(InternalRopeData ropeData)
        {
            if (ropeData.Constraint == null)
                return;

            if (ropeData.Constraint.RigidBodyA != null)
                ropeData.Constraint.RigidBodyA.Activate();

            if (ropeData.Constraint.RigidBodyB != null)
                ropeData.Constraint.RigidBodyB.Activate();

            var physics = ropeData.GridA.Physics;
            if (physics != null)
                physics.RemoveConstraint(ropeData.Constraint);

            if (!ropeData.Constraint.IsDisposed)
                ropeData.Constraint.Dispose();
            ropeData.Constraint = null;
            // HkConstraint deletes constraint data as well. Is this correct? (prevents sharing
            // constraint data, as each instance reduces reference count on data by 2 on dispose!)
            ropeData.ConstraintData = null;

            MyCubeGrid.BreakGridGroupLink(GridLinkTypeEnum.Physical, ropeData.RopeId, ropeData.GridA, ropeData.GridB);
        }

        private static void hookEntity_OnClosing(MyEntity hookEntity)
        {
            RemoveRopeOnHookInternal(hookEntity.EntityId);
        }

        private static void ComputeLocalPosition(MyCubeBlock block, HookData hook, out Vector3D localPosition)
        {
            Vector3 instanceLocalPivot = hook.LocalPivot;
            Vector3.TransformNormal(ref instanceLocalPivot, block.Orientation, out instanceLocalPivot);
            Vector3D instanceCenter = (Vector3D)(block.Min + block.Max) / 2.0;
            localPosition = instanceLocalPivot + instanceCenter * block.CubeGrid.GridSize;
        }

        private static void UpdateDummyWorld(MyPositionComponentBase windingEntityPositionComponent, WindingData winding)
        {
            winding.LastDummyWorld = winding.CurrentDummyWorld;
            winding.CurrentDummyWorld = MatrixD.Multiply(winding.LocalDummy, windingEntityPositionComponent.WorldMatrix);
        }

        private static void ApplyRopeLimits(InternalRopeData ropeData, RopeDrumLimits limits = null)
        {
            if (limits == null) m_hookIdToRopeLimits.TryGetValue(ropeData.Public.HookEntityIdA, out limits);
            if (limits == null) m_hookIdToRopeLimits.TryGetValue(ropeData.Public.HookEntityIdB, out limits);

            if (limits != null)
            {
                Debug.Assert(
                    m_hookIdToRopeLimits.ContainsKey(ropeData.Public.HookEntityIdA) ^
                    m_hookIdToRopeLimits.ContainsKey(ropeData.Public.HookEntityIdB),
                    "Rope should only be connected to one drum with rope limits.");
                ropeData.Public.MinRopeLength = limits.MinLength;
                ropeData.Public.MaxRopeLength = limits.MaxLength;
                WindingData winding;
                float newLength;
                if (m_hookIdToWinding.TryGetValue(ropeData.Public.HookEntityIdA, out winding) && winding.IsUnlocked)
                    newLength = limits.MaxLength;
                else if (m_hookIdToWinding.TryGetValue(ropeData.Public.HookEntityIdB, out winding) && winding.IsUnlocked)
                    newLength = limits.MaxLength;
                else
                    newLength = MathHelper.Clamp(ropeData.Public.CurrentRopeLength, limits.MinLength, limits.MaxLength);

                ropeData.Public.CurrentRopeLength = newLength;
                if (ropeData.ConstraintData != null)
                {
                    // Would be nice if I could leave this up to the normal update by changing only target rope length.
                    // But target rope length is limited to only certain delta in actual length per update.
                    ropeData.ConstraintData.LinearLimit = newLength;
                    ropeData.Constraint.RigidBodyA.Activate();
                    ropeData.Constraint.RigidBodyB.Activate();
                }
            }

            ropeData.TargetRopeLength = ropeData.Public.CurrentRopeLength;
        }

        private static void MoveSubpart(MyEntitySubpart subpart, Vector3 offset)
        {
            if (subpart != null)
            {
                var positionComponent = subpart.PositionComp;
                var localMatrix = positionComponent.LocalMatrix;
                localMatrix.Translation += offset;
                positionComponent.LocalMatrix = localMatrix;
            }
        }

        private static Vector3 ComputeTorqueFromRopeImpulse(WindingData windingData, InternalRopeData ropeData, Vector3 ropeDirectionVector, Vector3 centerDelta)
        {
            /*
             * /R   = rope direction vector (unit length, world space)
             * /A   = drum axis (unit length, world space)
             * /r   = displacement vector for torque application (world space) from center of mass
             * /tau = torque applied
             * /C_d = center delta (center of winding (world space) - center of mass (world space))
             * /F   = force
             * r    = drum radius
             * I    = impulse applied
             * 
             * /r = |/R x /A| * r + C_d
             * /F = /R * -I
             * /tau = /r x /F
             */

            Vector3 A, r, F, tau;
            Vector3.Normalize(ref ropeDirectionVector, out ropeDirectionVector);
            A = windingData.CurrentDummyWorld.Backward;
            Vector3.Cross(ref ropeDirectionVector, ref A, out r);
            r = r * windingData.Radius + centerDelta;
            F = ropeDirectionVector * ropeData.ImpulseApplied;
            Vector3.Cross(ref r, ref F, out tau);
            return tau;
        }

        private static void UpdateWindingAngleDelta(WindingData winding)
        {
            var sin = winding.CurrentDummyWorld.Right.Dot(winding.LastDummyWorld.Up);
            var cos = winding.CurrentDummyWorld.Right.Dot(winding.LastDummyWorld.Right);

            // Circumference is C=2πr for whole circle (2π) but I'm only interested in part of it (atan(y,x)*π).
            winding.AngleDelta = (float)Math.Atan2(sin, cos);
        }

        /// <summary>
        /// Writes rope entity IDs (to outRopes) associated with given grids.
        /// </summary>
        public void GetRopesForGrids(HashSet<MyCubeGrid> grids, HashSet<MyRope> outRopes)
        {
            foreach (var entry in m_ropeIdToRope)
            {
                if (grids.Contains(entry.Value.GridA) || grids.Contains(entry.Value.GridB))
                {
                    if (entry.Value.Constraint == null || !entry.Value.Constraint.InWorld)
                        continue;

                    MyRope rope = MyEntities.GetEntityById(entry.Key) as MyRope;
                    Debug.Assert(rope != null);
                    if (rope != null)
                        outRopes.Add(rope);
                }
            }
        }

        public bool HasGridAttachedRope(MyCubeGrid grid)
        {
            foreach (var entry in m_ropeIdToRope)
            {
                if (grid == entry.Value.GridA || grid == entry.Value.GridB)
                    return true;
            }
            return false;
        }

        public void SetRopeLengthSynced(long ropeId, float currentLength)
        {
            Debug.Assert(!Sync.IsServer);

            InternalRopeData ropeData;
            if (m_ropeIdToRope.TryGetValue(ropeId, out ropeData))
            {
                ropeData.Public.CurrentRopeLength = currentLength;
                ropeData.TargetRopeLength = currentLength;
                if (ropeData.ConstraintData != null)
                    ropeData.ConstraintData.LinearLimit = ropeData.Public.CurrentRopeLength;

                if (ropeData.Constraint != null)
                {
                    ropeData.Constraint.RigidBodyA.Activate();
                    ropeData.Constraint.RigidBodyB.Activate();
                }
            }
        }

        [Conditional("DEBUG")]
        private static void AssertWindingLocksConsistent()
        {
            foreach (var entry in m_hookIdToWinding)
            {
                var hookId = entry.Key;
                var winding = entry.Value;
                bool isUnlocked = winding.IsUnlocked;
                bool hasRope = m_hookIdToRopeId.ContainsKey(hookId);
                bool hasUnlockedData = m_hookIdToUnlockedWinding.ContainsKey(hookId);
                bool isActive = m_hookIdToActiveWinding.ContainsKey(hookId);
                Debug.Assert(
                    (isUnlocked && hasUnlockedData) ||
                    (!isUnlocked && (!hasRope || isActive)));
            }
        }

        public static void AddRopeRequest(long entityId1, long entityId2, MyDefinitionId ropeDefinitionId)
        {
            MyMultiplayer.RaiseStaticEvent(x => AddRopeRequest_Implementation, entityId1, entityId2, (DefinitionIdBlit)ropeDefinitionId);
        }

        [Event, Reliable, Server]
        private static void AddRopeRequest_Implementation(long entityId1, long entityId2, DefinitionIdBlit ropeDefinitionId)
        {
            var definition = (MyRopeDefinition)MyDefinitionManager.Static.GetDefinition(ropeDefinitionId);
            if (CanConnectHooks(entityId1, entityId2, definition))
                AddRopeData(entityId1, entityId2, definition, 0);
        }

        public static void CloseRopeRequest(long ropeId)
        {
            MyMultiplayer.RaiseStaticEvent(x => CloseRopeRequest_Implementation, ropeId);
        }

        [Event, Reliable, Server]
        private static void CloseRopeRequest_Implementation(long ropeId)
        {
            RemoveRopeData(ropeId);

            MyRope rope;
            if (MyEntities.TryGetEntityById(ropeId, out rope))
            {
                rope.Close();
            }
        }

        public static void SetReleaseDataRequest(long hookEntityId, Vector2 orientation, float thresholdCos)
        {
            MyMultiplayer.RaiseStaticEvent(x => SetReleaseDataRequest_Implementation, hookEntityId, orientation, thresholdCos);
        }

        [Event, Reliable, Server]
        private static void SetReleaseDataRequest_Implementation(long hookEntityId, Vector2 orientation, float thresholdCos)
        {
            MyMultiplayer.RaiseStaticEvent(x => SetReleaseData_Implementation, hookEntityId, orientation, thresholdCos);
            SetReleaseData(hookEntityId, orientation, thresholdCos);
        }

        [Event, Reliable, Broadcast]
        public static void SetReleaseData_Implementation(long hookEntityId, Vector2 orientation, float thresholdCos)
        {
            SetReleaseData(hookEntityId, orientation, thresholdCos);
        }

        public static void SetDrumRopeLimitsRequest(long hookEntityId, float lengthMin, float lengthMax)
        {
            MyMultiplayer.RaiseStaticEvent(x => SetDrumRopeLimitsRequest_Implementation, hookEntityId, lengthMin, lengthMax);
        }

        [Event, Reliable, Server]
        private static void SetDrumRopeLimitsRequest_Implementation(long hookEntityId, float lengthMin, float lengthMax)
        {
            MyMultiplayer.RaiseStaticEvent(x => SetDrumRopeLimits_Implementation, hookEntityId, lengthMin, lengthMax);
            AddOrSetDrumRopeLimits(hookEntityId, lengthMin, lengthMax);
        }

        [Event, Reliable, Broadcast]
        public static void SetDrumRopeLimits_Implementation(long hookEntityId, float lengthMin, float lengthMax)
        {
            AddOrSetDrumRopeLimits(hookEntityId, lengthMin, lengthMax);
        }
    }

    public class MyRopeAttacher
    {
        private long m_hookIdFrom;
        private readonly Action<MyEntity> m_selectedHook_OnClosing;
        private MyRopeDefinition m_ropeDefinition;

        public MyRopeAttacher(MyRopeDefinition ropeDefinition)
        {
            m_ropeDefinition = ropeDefinition;
            m_selectedHook_OnClosing = delegate(MyEntity entity)
            {
                Debug.Assert(m_hookIdFrom == entity.EntityId);
                m_hookIdFrom = 0;
            };
        }

        public void OnUse(long hookIdTarget)
        {
            Debug.Assert(!MyRopeComponent.HasRope(hookIdTarget));
            bool playAttachSound = false;
            MyEntity hookEntity = null;
            if (!MyEntities.TryGetEntityById(hookIdTarget, out hookEntity))
                return;

            if (hookIdTarget != m_hookIdFrom)
            { // not yet selected for attaching
                if (m_hookIdFrom == 0)
                {
                    m_hookIdFrom = hookIdTarget;
                    hookEntity.OnClosing += m_selectedHook_OnClosing;
                    playAttachSound = true;
                }
                else if (MyRopeComponent.HasRope(m_hookIdFrom))
                { // Rope attached using some other attacher while this one was active. Just update selection.
                    m_hookIdFrom = hookIdTarget;
                    playAttachSound = true;
                }
                else
                {
                    if (MyRopeComponent.CanConnectHooks(m_hookIdFrom, hookIdTarget, m_ropeDefinition))
                    {
                        MyRopeComponent.AddRopeRequest(m_hookIdFrom, hookIdTarget, m_ropeDefinition.Id);
                        playAttachSound = true;
                    }
                    Clear();
                }
            }

            MySoundPair soundToPlay = playAttachSound ? m_ropeDefinition.AttachSound : null;
            if (soundToPlay != null)
            {
                var soundEmitter = MyAudioComponent.TryGetSoundEmitter();
                if (soundEmitter != null)
                {
                    soundEmitter.SetPosition((Vector3)hookEntity.PositionComp.GetPosition());
                    soundEmitter.PlaySound(soundToPlay);
                }
            }
        }

        public void Clear()
        {
            if (m_hookIdFrom != 0)
            {
                MyEntity entity;
                if (MyEntities.TryGetEntityById(m_hookIdFrom, out entity))
                {
                    MyEntities.GetEntityById(m_hookIdFrom).OnClosing -= m_selectedHook_OnClosing;
                    m_hookIdFrom = 0;
                }
            }
        }
    }
}
