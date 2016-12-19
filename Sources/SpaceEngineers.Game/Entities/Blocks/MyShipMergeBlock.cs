using System;
using System.Collections.Generic;
using System.Diagnostics;
using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using SpaceEngineers.Game.EntityComponents.DebugRenders;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Profiler;
using VRageMath;
using VRageRender;

namespace SpaceEngineers.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_MergeBlock))]
    public class MyShipMergeBlock : MyFunctionalBlock, IMyShipMergeBlock
    {
        [Flags]
        private enum UpdateBeforeFlags : byte
        {
            None = 0x0,
            EnableConstraint = 0x1,
            UpdateIsWorking = 0x2
        }

        private enum EmissivityState
        {
            UNSET,
            NONE,
            WORKING,
            CONSTRAINED,
            LOCKED
        }

        private struct MergeData
        {
            public bool PositionOk;
            public bool RotationOk;
            public bool AxisOk;

            public float Distance;
            public float RotationDelta;
            public float AxisDelta;

            public float ConstraintStrength;
            public float StrengthFactor;

            public Vector3 RelativeVelocity;
        }

        public bool InConstraint { get { return m_constraint != null; } }
        private HkConstraint m_constraint;
        private HkConstraint SafeConstraint
        {
            get
            {
                if (m_constraint != null && !m_constraint.InWorld)
                {
                    RemoveConstraintInBoth();
                }
                return m_constraint;
            }
        }

        private MyShipMergeBlock m_other;
        public MyShipMergeBlock Other { get { return m_other; } }
        private HashSet<MyCubeGrid> m_gridList = new HashSet<MyCubeGrid>();
        public int GridCount { get { return m_gridList.Count; } }
        private Vector3 m_pos;

        private ushort m_frameCounter;

        private UpdateBeforeFlags m_updateBeforeFlags = UpdateBeforeFlags.None;

        // Constraint axis directions loaded from the dummy (before transformation by this block's orientation)
        private Base6Directions.Direction m_forward;
        private Base6Directions.Direction m_right;

        // When the constraint is set, this field contains direction that should align to the other's right direction
        private Base6Directions.Direction m_otherRight;
        public Base6Directions.Direction OtherRight { get { return m_otherRight; } }
        private EmissivityState m_emissivityState = EmissivityState.UNSET;
        private bool HasConstraint = false;

        private bool IsWithinWorldLimits
        {
            get
            {
                if (!Sandbox.Game.World.MySession.Static.EnableBlockLimits) return true;
                return Sandbox.Game.World.MySession.Static.MaxGridSize == 0 || CubeGrid.BlocksCount + m_other.CubeGrid.BlocksCount <= Sandbox.Game.World.MySession.Static.MaxGridSize;
            }
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            LoadDummies();
            SlimBlock.DeformationRatio = (this.BlockDefinition as MyMergeBlockDefinition).DeformationRatio; // 3x times harder for destruction by high speed

            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            AddDebugRenderComponent(new MyDebugRenderComponentShipMergeBlock(this));
        }

        protected override bool CheckIsWorking()
        {
            var otherMergeBlock = GetOtherMergeBlock();
            return (otherMergeBlock == null || otherMergeBlock.FriendlyWithBlock(this)) && base.CheckIsWorking();
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);

            this.IsWorkingChanged += MyShipMergeBlock_IsWorkingChanged;

            CheckConnectionAllowed = !this.IsWorking;
            Physics.Enabled = this.IsWorking;
            CheckEmissivity();

            var otherMergeBlock = GetOtherMergeBlock();
            if (otherMergeBlock != null) otherMergeBlock.UpdateIsWorkingBeforeNextFrame();
        }

        public override void OnRemovedFromScene(object source)
        {
            base.OnRemovedFromScene(source);

            var otherMergeBlock = GetOtherMergeBlock();
            if (otherMergeBlock != null)
            {
                otherMergeBlock.UpdateIsWorkingBeforeNextFrame();
            }
            RemoveConstraintInBoth();
        }

        public void UpdateIsWorkingBeforeNextFrame()
        {
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            m_updateBeforeFlags |= UpdateBeforeFlags.UpdateIsWorking;
        }

        private void LoadDummies()
        {
            var finalModel = VRage.Game.Models.MyModels.GetModelOnlyDummies(BlockDefinition.Model);
            foreach (var dummy in finalModel.Dummies)
            {
                if (dummy.Key.ToLower().Contains("merge"))
                {
                    var matrix = dummy.Value.Matrix;
                    Vector3 halfExtents = matrix.Scale / 2.0f;

                    Vector3 projectedPosition = Vector3.DominantAxisProjection(matrix.Translation / halfExtents);
                    projectedPosition.Normalize();
                    m_forward = Base6Directions.GetDirection(projectedPosition);
                    m_right = Base6Directions.GetPerpendicular(m_forward);

                    var world = MatrixD.Normalize(matrix) * this.WorldMatrix;

                    var detectorShape = CreateFieldShape(halfExtents);
                    Physics = new Sandbox.Engine.Physics.MyPhysicsBody(this, RigidBodyFlag.RBF_STATIC);
                    Physics.IsPhantom = true;
                    Physics.CreateFromCollisionObject(detectorShape, matrix.Translation, world, null, MyPhysics.CollisionLayers.ObjectDetectionCollisionLayer);
                    Physics.Enabled = IsWorking;
                    Physics.RigidBody.ContactPointCallbackEnabled = true;
                    detectorShape.Base.RemoveReference();

                    break;
                }
            }
        }

        private HkBvShape CreateFieldShape(Vector3 extents)
        {
            var phantom = new HkPhantomCallbackShape(phantom_Enter, phantom_Leave);
            var detectorShape = new HkBoxShape(extents);
            return new HkBvShape(detectorShape, phantom, HkReferencePolicy.TakeOwnership);
        }

        private void phantom_Leave(HkPhantomCallbackShape shape, HkRigidBody body)
        {
            ProfilerShort.Begin("MergeLeave");
            var entities = MyPhysicsExtensions.GetAllEntities(body);
            foreach (var entity in entities)
            {
                m_gridList.Remove(entity as MyCubeGrid);
            }
            entities.Clear();
            ProfilerShort.End();
        }

        private void phantom_Enter(HkPhantomCallbackShape shape, HkRigidBody body)
        {
            ProfilerShort.Begin("MergeEnter");
            var entities = MyPhysicsExtensions.GetAllEntities(body);
            foreach (var entity in entities)
            {
                var other = entity as MyCubeGrid;
                if (other == null || other.GridSizeEnum != CubeGrid.GridSizeEnum || other == this.CubeGrid)
                    continue;
                if(other.Physics.RigidBody != body)
                    continue;
                var added = m_gridList.Add(other);
                //Debug.Assert(added, "entity already in list");
            }
            entities.Clear();
            ProfilerShort.End();
        }

        private void CalculateMergeArea(out Vector3I minI, out Vector3I maxI)
        {
            Vector3I faceNormal = Base6Directions.GetIntVector(this.Orientation.TransformDirection(m_forward));

            // Shift block maximal and minimal coords one block in the direction of the merge face normal
            minI = this.Min + faceNormal;
            maxI = this.Max + faceNormal;

            // Get maxI and minI into plane by shifting one of them to the other
            if (faceNormal.X + faceNormal.Y + faceNormal.Z == -1)
            {
                // minI is outside the block, get maxI outside too
                maxI += (maxI - minI) * faceNormal;
            }
            else
            {
                // maxI is outside the block, get minI outside too
                minI += (maxI - minI) * faceNormal;
            }
        }

        private MySlimBlock GetBlockInMergeArea()
        {
            Vector3I minI, maxI;
            CalculateMergeArea(out minI, out maxI);

            Vector3I pos = minI;
            for (Vector3I_RangeIterator it = new Vector3I_RangeIterator(ref minI, ref maxI); it.IsValid(); it.GetNext(out pos))
            {
                var block = this.CubeGrid.GetCubeBlock(pos);
                if (block != null)
                    return block;
            }

            return null;
        }

        private MyShipMergeBlock GetOtherMergeBlock()
        {
            Vector3I minI, maxI;
            CalculateMergeArea(out minI, out maxI);

            Vector3I pos = minI;
            for (Vector3I_RangeIterator it = new Vector3I_RangeIterator(ref minI, ref maxI); it.IsValid(); it.GetNext(out pos))
            {
                var block = this.CubeGrid.GetCubeBlock(pos);
                if (block != null && block.FatBlock != null)
                {
                    var mergeBlock = block.FatBlock as MyShipMergeBlock;
                    if (mergeBlock == null) continue;

                    Vector3I otherMinI, otherMaxI;
                    mergeBlock.CalculateMergeArea(out otherMinI, out otherMaxI);
                    Vector3I faceNormal = Base6Directions.GetIntVector(this.Orientation.TransformDirection(m_forward));

                    // Bounding box test of minI <-> maxI and otherMinI(shifted by faceNormal) <-> otherMaxI(shifted by faceNormal)
                    otherMinI = maxI - (otherMinI + faceNormal);
                    otherMaxI = otherMaxI + faceNormal - minI;
                    if (otherMinI.X < 0) continue;
                    if (otherMinI.Y < 0) continue;
                    if (otherMinI.Z < 0) continue;
                    if (otherMaxI.X < 0) continue;
                    if (otherMaxI.Y < 0) continue;
                    if (otherMaxI.Z < 0) continue;

                    return mergeBlock;
                }
            }

            return null;
        }

        private Vector3 GetMergeNormalWorld()
        {
            return WorldMatrix.GetDirectionVector(m_forward);
        }

        private void MyShipMergeBlock_IsWorkingChanged(MyCubeBlock obj)
        {
            Debug.Assert(Physics != null || !InScene);

            if (Physics != null)
                Physics.Enabled = this.IsWorking;

            if (!this.IsWorking)
            {
                var otherBlock = GetOtherMergeBlock();
                if (otherBlock != null)
                {
                }
                else if (InConstraint)
                    RemoveConstraintInBoth();
            }

            CheckConnectionAllowed = !this.IsWorking;
            CubeGrid.UpdateBlockNeighbours(this.SlimBlock);

            CheckEmissivity();
        }

        protected override void OnStopWorking()
        {
            CheckEmissivity();
            base.OnStopWorking();
        }

        protected override void OnStartWorking()
        {
            CheckEmissivity();
            base.OnStartWorking();
        }

        private void CheckEmissivity()
        {
            if (!InScene)
                return;

            EmissivityState state = EmissivityState.WORKING;

            var otherBlock = GetOtherMergeBlock();
            if (!IsWorking)
                state = EmissivityState.NONE;
            else if (otherBlock != null)
            {
                var dir1 = Base6Directions.GetFlippedDirection(otherBlock.Orientation.TransformDirection(otherBlock.m_forward));
                var dir2 = this.Orientation.TransformDirection(this.m_forward);
                if (dir1 == dir2)
                    state = EmissivityState.LOCKED;
            }
            else if (InConstraint)
                state = EmissivityState.CONSTRAINED;

            if (state != m_emissivityState)
                UpdateEmissivity(state);
        }

        private void UpdateEmissivity(EmissivityState state)
        {
            m_emissivityState = state;

            Color emissiveColor = Color.Black;

            switch (state)
            {
                case EmissivityState.LOCKED:
                    emissiveColor = Color.ForestGreen;
                    break;
                case EmissivityState.CONSTRAINED:
                    emissiveColor = Color.Goldenrod;
                    break;
                case EmissivityState.WORKING:
                    emissiveColor = Color.Gray;
                    break;
                case EmissivityState.NONE:
                    break;
                case EmissivityState.UNSET:
                default:
                    Debug.Assert(false, "Invalid emissivity state in ship merge block");
                    break;;
            }

            MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, emissiveColor, Color.White);
        }

        public override void OnModelChange()
        {
            base.OnModelChange();

            m_emissivityState = EmissivityState.UNSET;
            CheckEmissivity();
        }

        protected override void OnOwnershipChanged()
        {
            base.OnOwnershipChanged();

            UpdateIsWorkingBeforeNextFrame();
        }

        private void CalculateMergeData(ref MergeData data)
        {
            var mergeBlockDefinition = this.BlockDefinition as MyMergeBlockDefinition;
            float maxStrength = mergeBlockDefinition != null ? mergeBlockDefinition.Strength : 0.1f;
            data.Distance = (float)(WorldMatrix.Translation - m_other.WorldMatrix.Translation).Length() - CubeGrid.GridSize;

            data.StrengthFactor = (float)Math.Exp(-data.Distance / CubeGrid.GridSize);
            // Debug.Assert(x <= 1.0f); // This is not so important, but testers kept reporting it, so let's leave it commented out :-)
            float strength = MathHelper.Lerp(0.0f, maxStrength * (CubeGrid.GridSizeEnum == MyCubeSize.Large ? 0.005f : 0.1f), data.StrengthFactor); // 0.005 for large grid, 0.1 for small grid!?

            Vector3 thisVelocity = CubeGrid.Physics.GetVelocityAtPoint(PositionComp.GetPosition());
            Vector3 otherVelocity = m_other.CubeGrid.Physics.GetVelocityAtPoint(m_other.PositionComp.GetPosition());
            data.RelativeVelocity = otherVelocity - thisVelocity;
            float velocityFactor = 1.0f;

            // The quicker the ships move towards each other, the weaker the constraint strength
            float rvLength = data.RelativeVelocity.Length();
            velocityFactor = Math.Max(3.6f / (rvLength > 0.1f ? rvLength : 0.1f), 1.0f);
            data.ConstraintStrength = strength / velocityFactor;

            Vector3 toOther = m_other.PositionComp.GetPosition() - PositionComp.GetPosition();
            Vector3 forward = WorldMatrix.GetDirectionVector(m_forward);

            data.Distance = (toOther).Length();
            data.PositionOk = data.Distance < CubeGrid.GridSize + 0.17f; // 17 cm is tested working value. 15 cm was too few

            data.AxisDelta = (float)(forward + m_other.WorldMatrix.GetDirectionVector(m_forward)).Length();
            data.AxisOk = data.AxisDelta < 0.1f;

            data.RotationDelta = (float)(WorldMatrix.GetDirectionVector(m_right) - m_other.WorldMatrix.GetDirectionVector(m_other.m_otherRight)).Length();
            data.RotationOk = data.RotationDelta < 0.08f;
        }

        private void DebugDrawInfo(Vector2 offset)
        {
            MergeData data = new MergeData();
            CalculateMergeData(ref data);

            MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 75.0f) + offset, "x = " + data.StrengthFactor.ToString(), Color.Green, 0.8f);
            MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 0.0f) + offset, "Merge block strength: " + data.ConstraintStrength.ToString(), Color.Green, 0.8f);

            MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 15.0f) + offset, "Merge block dist: " + (data.Distance - CubeGrid.GridSize).ToString(), data.PositionOk ? Color.Green : Color.Red, 0.8f);
            MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 30.0f) + offset, "Frame counter: " + m_frameCounter.ToString(), m_frameCounter >= 6 ? Color.Green : Color.Red, 0.8f);
            MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 45.0f) + offset, "Rotation difference: " + data.RotationDelta.ToString(), data.RotationOk ? Color.Green : Color.Red, 0.8f);
            MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 60.0f) + offset, "Axis difference: " + data.AxisDelta.ToString(), data.AxisOk ? Color.Green : Color.Red, 0.8f);

             // The quicker the ships move towards each other, the weaker the constraint strength
            float rvLength = data.RelativeVelocity.Length();
            MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 90.0f) + offset, rvLength > 0.5f ? "Quick" : "Slow", rvLength > 0.5f ? Color.Red : Color.Green, 0.8f);
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            if (SafeConstraint != null)
            {
                if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CONNECTORS_AND_MERGE_BLOCKS && CustomName.ToString() == "DEBUG")
                {
                    DebugDrawInfo(new Vector2(0.0f, 0.0f));
                    m_other.DebugDrawInfo(new Vector2(0.0f, 120.0f));

                    MyRenderProxy.DebugDrawLine3D(PositionComp.GetPosition(), PositionComp.GetPosition() + WorldMatrix.GetDirectionVector(m_right) * 10.0f, Color.Red, Color.Red, false);
                    MyRenderProxy.DebugDrawLine3D(m_other.PositionComp.GetPosition(), m_other.PositionComp.GetPosition() + m_other.WorldMatrix.GetDirectionVector(m_other.m_otherRight) * 10.0f, Color.Red, Color.Red, false);

                    MyRenderProxy.DebugDrawLine3D(PositionComp.GetPosition(), PositionComp.GetPosition() + WorldMatrix.GetDirectionVector(m_otherRight) * 5.0f, Color.Yellow, Color.Yellow, false);
                    MyRenderProxy.DebugDrawLine3D(m_other.PositionComp.GetPosition(), m_other.PositionComp.GetPosition() + m_other.WorldMatrix.GetDirectionVector(m_other.m_right) * 5.0f, Color.Yellow, Color.Yellow, false);
                }

                Vector3 thisVelocity = CubeGrid.Physics.GetVelocityAtPoint(PositionComp.GetPosition());
                Vector3 otherVelocity = m_other.CubeGrid.Physics.GetVelocityAtPoint(m_other.PositionComp.GetPosition());
                Vector3 relativeVelocity = otherVelocity - thisVelocity;

                // Damping to avoid too quick approach
                if (relativeVelocity.Length() > 0.5f)
                {
                    CubeGrid.Physics.LinearVelocity += relativeVelocity * 0.05f;
                    m_other.CubeGrid.Physics.LinearVelocity -= relativeVelocity * 0.05f;
                }
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();

            if (!CheckUnobstructed())
            {
                if (SafeConstraint != null)
                {
                    RemoveConstraintInBoth();
                }
                return;
            }

            if (SafeConstraint != null)
            {
                bool staticOk = this.CubeGrid.IsStatic || !m_other.CubeGrid.IsStatic;
                if (!staticOk || !IsWorking || !m_other.IsWorking || !IsWithinWorldLimits)
                    return;

                Debug.Assert(!m_other.CubeGrid.MarkedForClose && !CubeGrid.MarkedForClose);

                var mergeBlockDefinition = this.BlockDefinition as MyMergeBlockDefinition;
                float maxStrength = mergeBlockDefinition != null ? mergeBlockDefinition.Strength : 0.1f;
                float dist = (float)(WorldMatrix.Translation - m_other.WorldMatrix.Translation).Length() - CubeGrid.GridSize;

                if (dist > CubeGrid.GridSize * 3)
                {
                    RemoveConstraintInBoth();
                    return;
                }

                MergeData data = new MergeData();
                CalculateMergeData(ref data);

                (m_constraint.ConstraintData as HkMalleableConstraintData).Strength = data.ConstraintStrength;

                if (data.PositionOk && data.AxisOk && data.RotationOk)
                {
                    if (m_frameCounter++ >= 3)
                    {
                        Vector3I gridOffset = CalculateOtherGridOffset();
                        Vector3I otherGridOffset = m_other.CalculateOtherGridOffset();

                        bool canMerge = this.CubeGrid.CanMergeCubes(m_other.CubeGrid, gridOffset);
                        if (!canMerge)
                        {
                            if (this.CubeGrid.GridSystems.ControlSystem.IsLocallyControlled || m_other.CubeGrid.GridSystems.ControlSystem.IsLocallyControlled)
                                MyHud.Notifications.Add(MyNotificationSingletons.ObstructingBlockDuringMerge);
                            return;
                        }
                        var handle = BeforeMerge;
                        if (handle != null) BeforeMerge();
                        if (Sync.IsServer)
                        {
                            foreach (var block in CubeGrid.GetBlocks())
                            {
                                var mergeBlock = block.FatBlock as MyShipMergeBlock;
                                if (mergeBlock != null && mergeBlock != this && mergeBlock.InConstraint)
                                    (block.FatBlock as MyShipMergeBlock).RemoveConstraintInBoth();
                            }

                            MyCubeGrid mergedGrid = this.CubeGrid.MergeGrid_MergeBlock(m_other.CubeGrid, gridOffset);
                            if (mergedGrid == null)
                            {
                                mergedGrid = m_other.CubeGrid.MergeGrid_MergeBlock(this.CubeGrid, otherGridOffset);
                            }
                            Debug.Assert(mergedGrid != null);

                            RemoveConstraintInBoth();
                        }
                    }
                }
                else
                {
                    m_frameCounter = 0;
                }
                return;
            }
            foreach (var other in m_gridList)
            {
                if (other.MarkedForClose)
                    continue;
                Vector3I pos = Vector3I.Zero;
                double dist = double.MaxValue;
                LineD l = new LineD(Physics.ClusterToWorld(Physics.RigidBody.Position), Physics.ClusterToWorld(Physics.RigidBody.Position) + GetMergeNormalWorld());
                if (other.GetLineIntersectionExactGrid(ref l, ref pos, ref dist))
                {
                    var block = other.GetCubeBlock(pos).FatBlock as MyShipMergeBlock;

                    if(block == null)
                    {
                        continue;
                    }
                    if (block.InConstraint || !block.IsWorking || !block.CheckUnobstructed() || block.GetMergeNormalWorld().Dot(GetMergeNormalWorld()) > 0.0f)
                        return;

                    if (!block.FriendlyWithBlock(this)) return;
                    if (other is MyCubeGrid && MyCubeGridGroups.Static.Physical.HasSameGroup(other, CubeGrid)) return;

                    CreateConstraint(other, block);

                    NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                    m_updateBeforeFlags |= UpdateBeforeFlags.EnableConstraint;
                    break;
                }
            }
        }

        private bool CheckUnobstructed()
        {
            return GetBlockInMergeArea() == null;
        }

        private Vector3I CalculateOtherGridOffset()
        {
            Debug.Assert(m_other != null);

            Vector3 myConstraint = ConstraintPositionInGridSpace() / this.CubeGrid.GridSize;
            Vector3 otherConstraint = -m_other.ConstraintPositionInGridSpace() / m_other.CubeGrid.GridSize;

            Base6Directions.Direction thisRight = Orientation.TransformDirection(m_right);     // Where does this block's right point to
            Base6Directions.Direction thisForward = Orientation.TransformDirection(m_forward); // Where does this block's forward point to

            Base6Directions.Direction otherBackward = Base6Directions.GetFlippedDirection(m_other.Orientation.TransformDirection(m_other.m_forward));
            Base6Directions.Direction otherRight = m_other.CubeGrid.WorldMatrix.GetClosestDirection(CubeGrid.WorldMatrix.GetDirectionVector(thisRight));

            Vector3 toOtherOrigin;
            MatrixI rotation = MatrixI.CreateRotation(otherRight, otherBackward, thisRight, thisForward);
            Vector3.Transform(ref otherConstraint, ref rotation, out toOtherOrigin);

            return Vector3I.Round(myConstraint + toOtherOrigin);
        }

        private Vector3 ConstraintPositionInGridSpace()
        {
            return Position * CubeGrid.GridSize + PositionComp.LocalMatrix.GetDirectionVector(m_forward) * (CubeGrid.GridSize * 0.5f + 0.06f/*6cm*/);
        }

        private void CreateConstraint(MyCubeGrid other, MyShipMergeBlock block)
        {
            var data = new HkPrismaticConstraintData();
            data.MaximumLinearLimit = 0;
            data.MinimumLinearLimit = 0;
            var posA = ConstraintPositionInGridSpace();
            var posB = block.ConstraintPositionInGridSpace();
            var axisA = PositionComp.LocalMatrix.GetDirectionVector(m_forward);
            var axisAPerp = PositionComp.LocalMatrix.GetDirectionVector(m_right);
            var axisB = -block.PositionComp.LocalMatrix.GetDirectionVector(m_forward);

            Base6Directions.Direction thisRightForOther = block.WorldMatrix.GetClosestDirection(WorldMatrix.GetDirectionVector(m_right));
            Base6Directions.Direction otherRight = WorldMatrix.GetClosestDirection(block.WorldMatrix.GetDirectionVector(block.m_right));

            var axisBPerp = block.PositionComp.LocalMatrix.GetDirectionVector(thisRightForOther);

            data.SetInBodySpace( posA,  posB,  axisA,  axisB,  axisAPerp,  axisBPerp, CubeGrid.Physics, other.Physics);
            var data2 = new HkMalleableConstraintData();
            data2.SetData(data);
            data.ClearHandle();
            data = null;
            data2.Strength = 0.00001f;

            var constraint = new HkConstraint(CubeGrid.Physics.RigidBody, other.Physics.RigidBody, data2);
            AddConstraint(constraint);

            SetConstraint(block, constraint, otherRight);
            m_other.SetConstraint(this, constraint, thisRightForOther);
        }
        private void AddConstraint(HkConstraint newConstraint)
        {
            HasConstraint = true;
            CubeGrid.Physics.AddConstraint(newConstraint);
        }
       
        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            if (m_updateBeforeFlags.HasFlag(UpdateBeforeFlags.EnableConstraint))
            {
                if (SafeConstraint != null)
                    m_constraint.Enabled = true;
            }
            else if (m_updateBeforeFlags.HasFlag(UpdateBeforeFlags.UpdateIsWorking))
            {
                UpdateIsWorking();
                CheckEmissivity();
            }

            m_updateBeforeFlags = UpdateBeforeFlags.None;
        }

       

        public override bool ConnectionAllowed(ref Vector3I otherBlockPos, ref Vector3I faceNormal, MyCubeBlockDefinition def)
        {
            return ConnectionAllowedInternal(ref faceNormal, def);
        }

        public override bool ConnectionAllowed(ref Vector3I otherBlockMinPos, ref Vector3I otherBlockMaxPos, ref Vector3I faceNormal, MyCubeBlockDefinition def)
        {
            return ConnectionAllowedInternal(ref faceNormal, def);
        }

        private bool ConnectionAllowedInternal(ref Vector3I faceNormal, MyCubeBlockDefinition def)
        {
            if (IsWorking) return true;
            if (def != this.BlockDefinition) return true;

            Base6Directions.Direction connectionDirection = Orientation.TransformDirectionInverse(Base6Directions.GetDirection(faceNormal));
            if (connectionDirection != m_forward) return true;

            return false;
        }

        protected void SetConstraint(MyShipMergeBlock otherBlock, HkConstraint constraint, Base6Directions.Direction otherRight)
        {
            Debug.Assert(m_constraint == null && m_other == null);
            if (m_constraint != null || m_other != null) return;

            m_constraint = constraint;
            m_other = otherBlock;
            m_otherRight = otherRight;
            CheckEmissivity();

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        protected void RemoveConstraint()
        {
            Debug.Assert(InConstraint);

            m_constraint = null;
            m_other = null;
            CheckEmissivity();

            NeedsUpdate &= ~MyEntityUpdateEnum.EACH_FRAME;
        }

        protected void RemoveConstraintInBoth()
        {
            if (this.HasConstraint)
            {
                m_other.RemoveConstraint();
                CubeGrid.Physics.RemoveConstraint(m_constraint);
                m_constraint.Dispose();

                RemoveConstraint();
                HasConstraint = false;
            }
            else if(m_other != null)
            {
                m_other.RemoveConstraintInBoth();
            }
        }

        protected override void Closing()
        {
            base.Closing();

            if (InConstraint)
            {
                RemoveConstraintInBoth();
            }
        }
        event Action BeforeMerge;
        event Action IMyShipMergeBlock.BeforeMerge
        {
            add { BeforeMerge += value; }
            remove { BeforeMerge += value; }
        }

        public override int GetBlockSpecificState()
        {
            //returns 2 when locked, 1 when in constraint or 0 otherwise
            return (m_emissivityState == EmissivityState.LOCKED ? 2 : (m_emissivityState == EmissivityState.CONSTRAINED ? 1 : 0));
        }

        public bool IsLocked { get { return m_emissivityState == EmissivityState.LOCKED; } }
    }
}
