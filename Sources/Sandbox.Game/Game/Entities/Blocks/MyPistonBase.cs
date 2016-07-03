﻿using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Terminal.Controls;
using Sandbox.Game.World;
using System;
using System.Diagnostics;
using System.Text;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using VRage;
using VRage.Import;
using VRageMath;
using VRage.ModAPI;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRage.Utils;
using Sandbox.Engine.Utils;
using VRage.Game;
using VRage.Game.Entity;
using System.Collections.Generic;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Replication;
using VRage.Network;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_PistonBase))]
    public partial class MyPistonBase : MyFunctionalBlock, IMyConveyorEndpointBlock
    {
        private HkConstraint m_subpartsConstraint;
        private HkConstraint m_constraint;
        private MyCubeGrid m_topGrid;
        private MyPistonTop m_topBlock;

        private MyEntitySubpart m_subpart1;
        private MyEntitySubpart m_subpart2;
        public MyEntitySubpart Subpart3;
        private Vector3 m_subpart1LocPos;
        private Vector3 m_subpart2LocPos;
        private Vector3 m_subpart3LocPos;
        private Vector3 m_constraintBasePos;
        private HkFixedConstraintData m_fixedData;
        private HkFixedConstraintData m_subpartsFixedData;
        private bool m_resetInterpolationFlag = true;
        private bool m_subPartContraintInScene = false;
        private bool m_welded = false;
        private Sync<bool> m_forceWeld;
        private Sync<float> m_weldSpeedSq; //squared
        protected bool m_isAttached = false;
        protected readonly Sync<long?> m_weldedEntityId;
        protected long? m_weldedTopBlockId;
        protected bool m_isWelding = false;

        private static List<HkBodyCollision> m_penetrations = new List<HkBodyCollision>();
        private static HashSet<MySlimBlock> m_tmpSet = new HashSet<MySlimBlock>();

        private MyAttachableConveyorEndpoint m_conveyorEndpoint;
        private bool m_posChanged;
        private Vector3 m_subpartsConstraintPos;

        private float m_lastPosition = float.MaxValue;
        private readonly Sync<float> m_currentPos; // Necessary?
        private readonly Sync<long?> m_topBlockId;
        public readonly Sync<float> Velocity;
        public readonly Sync<float> MinLimit;
        public readonly Sync<float> MaxLimit;
        protected event Action<MyPistonBase> AttachedEntityChanged;

        private float Range { get { return BlockDefinition.Maximum - BlockDefinition.Minimum; } }

        new public MyPistonBaseDefinition BlockDefinition { get { return (MyPistonBaseDefinition)base.BlockDefinition; } }

        public float CurrentPosition { get { return m_currentPos; } }

        public PistonStatus Status
        {
            get
            {
                if (Velocity < 0)
                {
                    return m_currentPos <= MinLimit ? PistonStatus.Retracted : PistonStatus.Retracting;
                }
                else if (Velocity > 0)
                {
                    return m_currentPos >= MaxLimit ? PistonStatus.Extended : PistonStatus.Extending;
                }
                return PistonStatus.Stopped;
            }
        }

        public HkConstraint SafeConstraint { get { return RefreshConstraint(); } }

        public IMyConveyorEndpoint ConveyorEndpoint { get { return m_conveyorEndpoint; } }

        public float BreakOffTreshold { get { return CubeGrid.GridSizeEnum == MyCubeSize.Large ? 20000000 : 1000000; } }

        event Action<bool> LimitReached;

        static MyPistonBase()
        {
            var reverse = new MyTerminalControlButton<MyPistonBase>("Reverse", MySpaceTexts.BlockActionTitle_Reverse, MySpaceTexts.Blank, (x) => x.Velocity.Value = -x.Velocity);
            reverse.EnableAction(MyTerminalActionIcons.REVERSE);
            MyTerminalControlFactory.AddControl(reverse);

            var extendAction = new MyTerminalAction<MyPistonBase>("Extend", MyTexts.Get(MySpaceTexts.BlockActionTitle_Extend), OnExtendApplied, null, MyTerminalActionIcons.REVERSE);
            extendAction.Enabled = (b) => b.IsWorking == true && b.IsFunctional == true;
            MyTerminalControlFactory.AddAction(extendAction);

            var retractAction = new MyTerminalAction<MyPistonBase>("Retract", MyTexts.Get(MySpaceTexts.BlockActionTitle_Retract), OnRetractApplied, null, MyTerminalActionIcons.REVERSE);
            retractAction.Enabled = (b) => b.IsWorking == true && b.IsFunctional == true;
            MyTerminalControlFactory.AddAction(retractAction);

            var velocity = new MyTerminalControlSlider<MyPistonBase>("Velocity", MySpaceTexts.BlockPropertyTitle_Velocity, MySpaceTexts.Blank);
            velocity.SetLimits((block) => -block.BlockDefinition.MaxVelocity, (block) => block.BlockDefinition.MaxVelocity);
            velocity.DefaultValue = -0.5f;
            velocity.Getter = (x) => x.Velocity;
            velocity.Setter = (x, v) => x.Velocity.Value = v;
            velocity.Writer = (x, res) => res.AppendDecimal(x.Velocity, 1).Append("m/s");
            velocity.EnableActionsWithReset();
            MyTerminalControlFactory.AddControl(velocity);

            var maxDist = new MyTerminalControlSlider<MyPistonBase>("UpperLimit", MySpaceTexts.BlockPropertyTitle_MaximalDistance, MySpaceTexts.Blank);
            maxDist.SetLimits((block) => block.BlockDefinition.Minimum, (block) => block.BlockDefinition.Maximum);
            maxDist.DefaultValueGetter = (block) => block.BlockDefinition.Maximum;
            maxDist.Getter = (x) => x.MaxLimit;
            maxDist.Setter = (x, v) => x.MaxLimit.Value = v;
            maxDist.Writer = (x, res) => res.AppendDecimal(x.MaxLimit, 1).Append("m");
            maxDist.EnableActions();
            MyTerminalControlFactory.AddControl(maxDist);

            var minDist = new MyTerminalControlSlider<MyPistonBase>("LowerLimit", MySpaceTexts.BlockPropertyTitle_MinimalDistance, MySpaceTexts.Blank);
            minDist.SetLimits((block) => block.BlockDefinition.Minimum, (block) => block.BlockDefinition.Maximum);
            minDist.DefaultValueGetter = (block) => block.BlockDefinition.Minimum;
            minDist.Getter = (x) => x.MinLimit;
            minDist.Setter = (x, v) => x.MinLimit.Value = v;
            minDist.Writer = (x, res) => res.AppendDecimal(x.MinLimit, 1).Append("m");
            minDist.EnableActions();
            MyTerminalControlFactory.AddControl(minDist);

            var weldSpeed = new MyTerminalControlSlider<MyPistonBase>("Weld speed", MySpaceTexts.BlockPropertyTitle_WeldSpeed, MySpaceTexts.Blank);
            weldSpeed.SetLimits((block) => 0f, (block) => MyGridPhysics.SmallShipMaxLinearVelocity());
            weldSpeed.DefaultValueGetter = (block) => MyGridPhysics.LargeShipMaxLinearVelocity() - 5f;
            weldSpeed.Getter = (x) => (float)Math.Sqrt(x.m_weldSpeedSq);
            weldSpeed.Setter = (x, v) => x.m_weldSpeedSq.Value = v * v;
            weldSpeed.Writer = (x, res) => res.AppendDecimal((float)Math.Sqrt(x.m_weldSpeedSq), 1).Append("m/s");
            weldSpeed.EnableActions();
            MyTerminalControlFactory.AddControl(weldSpeed);

            var weldForce = new MyTerminalControlCheckbox<MyPistonBase>("Force weld", MySpaceTexts.BlockPropertyTitle_WeldForce, MySpaceTexts.Blank);
            weldForce.Getter = (x) => x.m_forceWeld;
            weldForce.Setter = (x, v) => x.m_forceWeld.Value = v;
            weldForce.EnableAction();
            MyTerminalControlFactory.AddControl(weldForce);

            var addPistonHead = new MyTerminalControlButton<MyPistonBase>("Add Piston Head", MySpaceTexts.BlockActionTitle_AddPistonHead, MySpaceTexts.BlockActionTooltip_AddPistonHead, (b) => b.RecreateTop());
            addPistonHead.Enabled = (b) => (b.m_topBlock == null);
            addPistonHead.EnableAction(MyTerminalActionIcons.STATION_ON);
            MyTerminalControlFactory.AddControl(addPistonHead);
        }

        public MyPistonBase()
        {
            m_currentPos.ValueChanged += (o) => UpdatePosition(true);
            m_topBlockId.ValueChanged += (o) => OnAttachTargetChanged();
            m_topBlockId.Validate = ValidatePistonBlockId;
        }

        private bool ValidatePistonBlockId(long? newValue)
        {
            if (newValue == null)           // Always allow detach
                return true;
            else if (newValue.Value == 0)   // Only allow attach if already detached
                return m_topBlockId.Value == null;
            else
                return false;
        }

        private static void OnExtendApplied(MyPistonBase piston)
        {
            if (piston.Velocity < 0)
                piston.Velocity.Value = -piston.Velocity;
        }

        private static void OnRetractApplied(MyPistonBase piston)
        {
            if (piston.Velocity > 0)
                piston.Velocity.Value = -piston.Velocity;
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;

            var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                BlockDefinition.ResourceSinkGroup,
                BlockDefinition.RequiredPowerInput,
                () => (Enabled && IsFunctional) ? ResourceSink.MaxRequiredInput : 0.0f);
            ResourceSink = sinkComp;

            base.Init(objectBuilder, cubeGrid);

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
      
            ResourceSink.IsPoweredChanged += Receiver_IsPoweredChanged;
            ResourceSink.Update();

            var ob = objectBuilder as MyObjectBuilder_PistonBase;
            Velocity.Value = ob.Velocity * BlockDefinition.MaxVelocity;
            MaxLimit.Value = ob.MaxLimit.HasValue ? Math.Min(DenormalizeDistance(ob.MaxLimit.Value), BlockDefinition.Maximum) : BlockDefinition.Maximum;
            MinLimit.Value = ob.MinLimit.HasValue ? Math.Max(DenormalizeDistance(ob.MinLimit.Value), BlockDefinition.Minimum) : BlockDefinition.Minimum;
            m_topBlockId.Value = ob.TopBlockId;
            m_currentPos.Value = ob.CurrentPosition;

            CubeGrid.OnPhysicsChanged += CubeGrid_OnPhysicsChanged;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            float defaultWeldSpeed = ob.weldSpeed; //weld before reaching the max speed
            defaultWeldSpeed *= defaultWeldSpeed;
            m_weldSpeedSq.Value = defaultWeldSpeed;
            m_forceWeld.Value = ob.forceWeld;
        }

        void CubeGrid_OnPhysicsChanged(MyEntity obj)
        {
            if (m_welded && m_topBlock != null && (m_topBlock.Closed || m_topBlock.MarkedForClose))
            {
                UnweldGroup();
                Detach();
                m_topBlock = null;
                m_topGrid = null;
            }
            DisposeSubpartsPhysics();

            // If the physics isn't being changed because we are being closed, reload the subparts, and update physics
            if (!Closed)
            {
                LoadSubparts();
                UpdatePosition(true);
                UpdatePhysicsShape();
                if (SafeConstraint == null)
                    Reattach();
            }
        }

        void OnAttachTargetChanged()
        {
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            UpdateText();
        }
        
        private bool IsBroken()
        {
            if (m_constraint != null)
            {
                var breakable = m_constraint.ConstraintData as HkBreakableConstraintData;
                return breakable != null && breakable.getIsBroken(m_constraint);
            }
            return false;
        }

        private HkConstraint RefreshConstraint()
        {
            if (m_constraint != null)
            {
                if (!m_constraint.InWorld)
                {
                    Detach();
                }
                else if (IsBroken())
                {
                    Detach();
                    if(Sync.IsServer)
                    {
                        m_topBlockId.Value = null;
                    }
                }
            }
            return m_constraint;
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = (MyObjectBuilder_PistonBase)base.GetObjectBuilderCubeBlock(copy);
            ob.Velocity = Velocity / BlockDefinition.MaxVelocity;
            ob.MaxLimit = NormalizeDistance(MaxLimit);
            ob.MinLimit = NormalizeDistance(MinLimit);
            ob.TopBlockId = m_topBlockId.Value;
            ob.CurrentPosition = m_currentPos;
            ob.weldSpeed = (float)Math.Sqrt(m_weldSpeedSq);
            ob.forceWeld = m_forceWeld;
            return ob;
        }

        protected override void OnEnabledChanged()
        {
            base.OnEnabledChanged();
            ResourceSink.Update();
        }

        protected override bool CheckIsWorking()
        {
            return ResourceSink.IsPowered && base.CheckIsWorking();
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            if (m_subpart1 != null && m_subpart1.Physics != null)
            {
                m_subpart1.Physics.Enabled = false;
            }

            if (m_welded || m_isWelding)
            {
                UnweldGroup();
            }
        }

        //Cubegrid physics changed is also called so 
        public override void OnModelChange()
        {
            base.OnModelChange();
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        private float NormalizeDistance(float value)
        {
            return (value + BlockDefinition.Minimum) / Range;
        }

        private float DenormalizeDistance(float value)
        {
            return (value * Range) + BlockDefinition.Minimum;
        }

        private void LoadSubparts()
        {
            DisposeSubpartsPhysics();

            if (!Subparts.TryGetValue("PistonSubpart1", out m_subpart1))
                return;
            if (!m_subpart1.Subparts.TryGetValue("PistonSubpart2", out m_subpart2))
                return;
            if (!m_subpart2.Subparts.TryGetValue("PistonSubpart3", out Subpart3))
                return;

            Debug.Assert(!this.Closed);
            if( this.Closed )
            {
                // When closed, the welding callback will call this, but the model will already be unloaded
                return;
            }
            MyModelDummy dummy;
            if (Subpart3.Model.Dummies.TryGetValue("TopBlock", out dummy))
                m_constraintBasePos = dummy.Matrix.Translation;
            if (Model.Dummies.TryGetValue("subpart_PistonSubpart1", out dummy))
            {
                m_subpartsConstraintPos = dummy.Matrix.Translation;
                m_subpart1LocPos = m_subpartsConstraintPos;
            }
            if (m_subpart1.Model.Dummies.TryGetValue("subpart_PistonSubpart2", out dummy))
                m_subpart2LocPos = dummy.Matrix.Translation;
            if (m_subpart2.Model.Dummies.TryGetValue("subpart_PistonSubpart3", out dummy))
                m_subpart3LocPos = dummy.Matrix.Translation;


            if (!CubeGrid.CreatePhysics)
                return;

            InitSubpartsPhysics();
        }

        private void InitSubpartsPhysics()
        {
            var subpart = m_subpart1;
            if (subpart == null || CubeGrid.Physics == null)
                return;
            subpart.Physics = new MyPhysicsBody(subpart, CubeGrid.IsStatic ? RigidBodyFlag.RBF_STATIC : (CubeGrid.GridSizeEnum == MyCubeSize.Large ? RigidBodyFlag.RBF_DOUBLED_KINEMATIC : RigidBodyFlag.RBF_DEFAULT));
            HkCylinderShape shape = new HkCylinderShape(Vector3.Zero, new Vector3(0, 0, 2), CubeGrid.GridSize / 2);
            var mass = HkInertiaTensorComputer.ComputeCylinderVolumeMassProperties(Vector3.Zero, Vector3.One, 1, 40.0f * CubeGrid.GridSize);
            subpart.GetPhysicsBody().CreateFromCollisionObject(shape, Vector3.Zero, subpart.WorldMatrix, mass);
            shape.Base.RemoveReference();
            subpart.Physics.RigidBody.Layer = CubeGrid.Physics.RigidBody.Layer;
            if (subpart.Physics.RigidBody2 != null)
                subpart.Physics.RigidBody2.Layer = MyPhysics.CollisionLayers.KinematicDoubledCollisionLayer;

            CreateSubpartsConstraint(subpart);

            m_posChanged = true;
        }

        private void DisposeSubpartsPhysics()
        {
            if (m_subpartsConstraint != null)
            {
                DisposeSubpartsConstraint();
            }
            if (m_subpart1 != null && m_subpart1.Physics != null)
            {
                m_subpart1.Physics.Enabled = false;
                m_subpart1.Physics.Close();
                m_subpart1.Physics = null;
            }
        }

        private void CreateSubpartsConstraint(MyEntitySubpart subpart)
        {
            m_subpartsFixedData = new HkFixedConstraintData();
            m_subpartsFixedData.SetSolvingMethod(HkSolvingMethod.MethodStabilized);
            m_subpartsFixedData.SetInertiaStabilizationFactor(1);
            var matAD = MatrixD.CreateWorld(Position * CubeGrid.GridSize + Vector3D.Transform(Vector3D.Transform(m_subpartsConstraintPos, WorldMatrix), CubeGrid.PositionComp.LocalMatrix), PositionComp.LocalMatrix.Forward, PositionComp.LocalMatrix.Up);
            matAD.Translation = CubeGrid.Physics.WorldToCluster(matAD.Translation);
            var matA = (Matrix)matAD;
            var matB = subpart.PositionComp.LocalMatrix;
            m_subpartsFixedData.SetInWorldSpace(ref matA, ref matB, ref matB);
            //Dont dispose the fixed data or we wont have access to them

            HkConstraintData constraintData = m_subpartsFixedData;
            if (Sync.IsServer) // Breakable only on server
            {
                constraintData = new HkBreakableConstraintData(m_subpartsFixedData) { Threshold = BreakOffTreshold };
            }
            m_subpartsConstraint = new HkConstraint(CubeGrid.Physics.RigidBody, subpart.Physics.RigidBody, constraintData);
            Debug.Assert(m_subpartsConstraint.RigidBodyA != null);
            m_subpartsConstraint.WantRuntime = true;
        }
        
        private void DisposeSubpartsConstraint()
        {
            if (m_subPartContraintInScene)
            {
                m_subPartContraintInScene = false;
                CubeGrid.Physics.RemoveConstraint(m_subpartsConstraint);
            }
            m_subpartsConstraint.Dispose();
            m_subpartsConstraint = null;
            if (Sync.IsServer)
            {
                // On server, contraint contains breakable data, fixed data must be disposed manually
                // On client, contraint contains fixed data, which are disposed with constraint
                m_subpartsFixedData.Dispose();
            }
            m_subpartsFixedData = null;
        }

        private void CheckSubpartConstraint()
        {
            if (m_subpartsConstraint != null && (m_subpartsConstraint.RigidBodyA == null))
            {
                DisposeSubpartsConstraint();
                CreateSubpartsConstraint(m_subpart1);
            }
        }

        public void RecreateTop(long? builderId = null)
        {
            if (this.m_topBlock != null || this.m_welded == true)
            {
                long builder = builderId.HasValue ? builderId.Value : MySession.Static.LocalPlayerId;
                if (builder == MySession.Static.LocalPlayerId)
                {
                    MyHud.Notifications.Add(MyNotificationSingletons.HeadAlreadyExists);
                }
                return;
            }

            if (builderId.HasValue)
            {
                MyMultiplayer.RaiseEvent(this, x => x.DoRecreateTop, builderId.Value);
            }
            else
            {
                MyMultiplayer.RaiseEvent(this, x => x.DoRecreateTop, MySession.Static.LocalPlayerId);
            }
        }

        [Event, Reliable, Server]
        private void DoRecreateTop(long builderId)
        {
            if (m_topBlock != null) return;

            CreateTopGrid(out m_topGrid, out m_topBlock, builderId);
            if (m_topBlock != null) // No place for top part
            {
                m_topBlockId.Value = m_topBlock.EntityId;
                Attach(m_topBlock);
            }
        }

        private void ComponentStack_IsFunctionalChanged()
        {
            ResourceSink.Update();
        }

        private void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
        }

        public override void OnBuildSuccess(long builtBy)
        {
            LoadSubparts();
            Debug.Assert(m_constraint == null);
            if (Sync.IsServer)
            {
                Sandbox.Engine.Multiplayer.MyMultiplayer.ReplicateImmediatelly(this, this.CubeGrid);
                CreateTopGrid(out m_topGrid, out m_topBlock, builtBy);
                if(m_topBlock != null) // No place for top part
                    Attach(m_topBlock);
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();
            if (CheckVelocities())
            {
                UpdateSoundState();
                return;
            }

            TryAttach();
            UpdatePhysicsShape();
            UpdateSoundState();
        }

        private void UpdatePhysicsShape()
        {
            var subpart = m_subpart1;
            if (!m_posChanged || subpart == null || subpart.Physics == null || subpart.Physics.RigidBody == null)
                return;
            m_posChanged = false;
            var speedCorrection = Velocity < 0 ? Velocity / 6 : 0;
            if (m_currentPos - 0.4f + speedCorrection > 0.02f)
            {
                Vector3 vec = new Vector3(0, subpart.Model.BoundingBoxSize.Y + 0.3f, 0);
                if (m_currentPos > 2 * Range / 3)
                    vec.Y -= m_currentPos - 2 * Range / 3;
                Vector3 vecB = vec + new Vector3(0, m_currentPos - 0.4f + speedCorrection, 0);

                var existingShape = subpart.Physics.RigidBody.GetShape();
                if (existingShape.ShapeType == HkShapeType.Cylinder)
                {
                    float dist = Math.Abs(vec.Y - vecB.Y);
                    var cyl = (HkCylinderShape)existingShape;
                    float distExist = Math.Abs(cyl.VertexA.Y - cyl.VertexB.Y);
                    if (Math.Abs(dist - distExist) < 0.1f)
                        return;
                }

                var threshold = 0.11f; // Must be bigger than 2x convex radius
                HkCylinderShape shape = new HkCylinderShape(vec, vecB, CubeGrid.GridSize / 2 - threshold, 0.001f);
                subpart.Physics.RigidBody.SetShape(shape);
                if (subpart.Physics.RigidBody2 != null)
                    subpart.Physics.RigidBody2.SetShape(shape);
                shape.Base.RemoveReference();
                subpart.Physics.Enabled = true;
                //jn:TODO hack fix
                CheckSubpartConstraint();
                UpdateSubpartFixedData();
                if (CubeGrid.Physics.IsInWorldWelded() && subpart.Physics.IsInWorldWelded())
                {
                    if (m_subpartsConstraint != null && !m_subpartsConstraint.InWorld && !m_subPartContraintInScene)
                    {
                        m_subPartContraintInScene = true;
                        CubeGrid.Physics.AddConstraint(m_subpartsConstraint);
                    }
                }
                if (m_subpartsConstraint != null && !m_subpartsConstraint.Enabled)
                {
                    m_subPartContraintInScene = true;
                    m_subpartsConstraint.Enabled = true;
                }
            }
            else
            {
                if (m_subpartsConstraint.Enabled && m_subpartsConstraint.InWorld)
                {
                    m_subPartContraintInScene = false;
                    m_subpartsConstraint.Enabled = false;
                    CubeGrid.Physics.RemoveConstraint(m_subpartsConstraint);
                }
                subpart.Physics.Enabled = false;
            }
        }

        public override void UpdateBeforeSimulation()
        {
            ProfilerShort.Begin("MyTerminalBlock.UpdateBeforeSim");
            base.UpdateBeforeSimulation();
            ProfilerShort.End();

            if (CheckVelocities())
                return;

            if (m_topGrid == null || SafeConstraint == null)
                return;

            if (SafeConstraint.RigidBodyA == SafeConstraint.RigidBodyB) //welded
            {
                SafeConstraint.Enabled = false;
                return;
            }

            ProfilerShort.Begin("UpdatePos");
            UpdatePosition();
            if (Sync.IsServer == false && m_topBlockId.Value.HasValue && m_topBlock == null)
            {
                TryAttach();
            }
            ProfilerShort.End();
            if (m_soundEmitter != null && m_soundEmitter.IsPlaying && m_lastPosition.Equals(float.MaxValue))
            {
                m_soundEmitter.StopSound(true);
                m_lastPosition = m_currentPos;
            }
        }

        protected bool CheckVelocities()
        {
            if (!MyFakes.WELD_PISTONS)
                return false;
            if (CubeGrid.Physics == null)
                return false;

            var velSq = CubeGrid.Physics.LinearVelocity.LengthSquared();
            if (m_forceWeld || velSq > m_weldSpeedSq)
            {
                if (m_welded)
                    return true;
                else
                {
                    if (m_topGrid == null && m_topBlockId.Value.HasValue && m_topBlockId.Value.Value != 0)
                    {
                        if (MyEntities.TryGetEntityById<MyPistonTop>(m_topBlockId.Value.Value, out m_topBlock))
                            m_topGrid = m_topBlock.CubeGrid;
                    }
                }
                if (m_topGrid != null && MyWeldingGroups.Static.GetGroup(CubeGrid) != MyWeldingGroups.Static.GetGroup(m_topGrid))
                {
                    WeldGroup();
                    return true;
                }
                return false;
            }

            const float safeUnweldSpeedDif = 2*2;
            if (!m_forceWeld && velSq < m_weldSpeedSq - safeUnweldSpeedDif && m_welded)
            {
                UnweldGroup();
            }
            return m_welded;
        }

        private void WeldGroup()
        {
            var topGrid = m_topGrid;
            var topBlock = m_topBlock;

            m_isWelding = true;

            m_weldedTopBlockId = m_topBlock.EntityId;

            if (Sync.IsServer)
            {
                MatrixD masterToSlave = topBlock.CubeGrid.WorldMatrix * MatrixD.Invert(WorldMatrix);

                m_topBlockId.Value = m_weldedTopBlockId;
                m_weldedEntityId.Value = m_weldedTopBlockId;
            }

            Detach(false);
            MyWeldingGroups.Static.CreateLink(EntityId, CubeGrid, topGrid);
            m_topGrid = topGrid;
            m_topBlock = topBlock;
            m_welded = true;

            m_isWelding = false;
            RaisePropertiesChanged();
        }

        private void UnweldGroup()
        {
            if (m_welded)
            {
                m_isWelding = true;

                if (Sync.IsServer)
                {
                    m_weldedEntityId.Value = null;
                }

                m_weldedTopBlockId = null;

                MyWeldingGroups.Static.BreakLink(EntityId, CubeGrid, m_topGrid);

                if (m_topBlock != null && m_topBlock.Closed == false && m_topBlock.MarkedForClose == false)
                {
                    Attach(m_topBlock, false);
                    m_welded = false;            
                    RaisePropertiesChanged();
                }
                m_isWelding = false;
            }
        }

        private void UpdatePosition(bool forceUpdate = false)
        {
            if (SafeConstraint == null)
                return;
            if (!IsWorking && !forceUpdate)
                return;
            //if (!forceUpdate && !CubeGrid.SyncObject.ResponsibleForUpdate(Sync.Clients.LocalClient))
            //return;

            bool changed = false;

            float compensatedDelta = Velocity / 60 * Sync.RelativeSimulationRatio;

            ProfilerShort.Begin("PosAndHandlers");
            if (!forceUpdate)
            {
                if (compensatedDelta < 0)
                {
                    if (m_currentPos > MinLimit)
                    {
                        m_currentPos.Value = Math.Max(m_currentPos + compensatedDelta, MinLimit);
                        changed = true;
                        if (m_currentPos == MinLimit)
                        {
                            var handle = LimitReached;
                            if (handle != null) handle(false);
                        }
                    }
                }
                else if (m_currentPos < MaxLimit)
                {
                    m_currentPos.Value = Math.Min(m_currentPos + compensatedDelta, MaxLimit);
                    changed = true;
                    if (m_currentPos == MaxLimit)
                    {
                        var handle = LimitReached;
                        if (handle != null) handle(true);
                    }
                }
            }
            ProfilerShort.End();

            if (changed || forceUpdate)
            {
                ProfilerShort.Begin("UpdateText");
                UpdateText();
                ProfilerShort.End();

                ProfilerShort.Begin("UpdateAnimation");
                UpdateAnimation();
                ProfilerShort.End();

                ProfilerShort.Begin("Calculations");
                m_posChanged = true;
                if (CubeGrid == null) MySandboxGame.Log.WriteLine("CubeGrid is null");
                if (m_topGrid == null) MySandboxGame.Log.WriteLine("TopGrid is null");
                if (Subpart3 == null) MySandboxGame.Log.WriteLine("Subpart is null");
                if (CubeGrid.Physics != null)
                    CubeGrid.Physics.RigidBody.Activate();
                if (m_topGrid != null && m_topGrid.Physics != null)
                    m_topGrid.Physics.RigidBody.Activate();
                var matAD = MatrixD.CreateWorld(Vector3D.Transform(Vector3D.Transform(m_constraintBasePos, Subpart3.WorldMatrix), CubeGrid.PositionComp.WorldMatrixNormalizedInv), PositionComp.LocalMatrix.Forward, PositionComp.LocalMatrix.Up);
                var matA = (Matrix)matAD;
                var matB = Matrix.CreateWorld(m_topBlock.Position * m_topBlock.CubeGrid.GridSize /*- m_topBlock.LocalMatrix.Up * m_currentPos*/, m_topBlock.PositionComp.LocalMatrix.Forward, m_topBlock.PositionComp.LocalMatrix.Up);
                ProfilerShort.End();

                ProfilerShort.Begin("SetInBodySpace");
                m_fixedData.SetInBodySpace(matA, matB, CubeGrid.Physics, m_topGrid.Physics);
                ProfilerShort.End();

                ProfilerShort.Begin("UpdateSubpartFixedData");
                UpdateSubpartFixedData();
                ProfilerShort.End();
            }
        }

        void UpdateSubpartFixedData()
        {
            var matA = Matrix.CreateWorld(Vector3.Transform(Vector3.Transform(m_subpartsConstraintPos, WorldMatrix), CubeGrid.PositionComp.WorldMatrixNormalizedInv), Vector3D.TransformNormal(WorldMatrix.Forward, CubeGrid.PositionComp.WorldMatrixNormalizedInv), Vector3D.TransformNormal(WorldMatrix.Up, CubeGrid.PositionComp.WorldMatrixNormalizedInv));
            var offset = Vector3.Zero;
            if (m_currentPos > 2 * Range / 3)
                offset.Y -= m_currentPos - 2 * Range / 3;
            var matB = Matrix.CreateWorld(offset, Vector3.Forward, Vector3.Up);
            if (m_subpartsFixedData != null)
                m_subpartsFixedData.SetInBodySpace(matA, matB, CubeGrid.Physics, m_subpart1.Physics as MyPhysicsBody);
            else
                MySandboxGame.Log.WriteLine("m_subpartsFixedData is null");
            //if (CubeGrid.SyncObject.ResponsibleForUpdate(Sync.Clients.LocalClient))
            //SyncObject.SetCurrentPosition(m_currentPos);
        }

        private void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(GetAttachState())).AppendLine();

            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_PistonCurrentPosition)).AppendDecimal(m_currentPos, 1).Append("m");
            RaisePropertiesChanged();
        }

        public MyStringId GetAttachState()
        {
            if ((m_welded || m_isWelding) && SafeConstraint == null)
            {
                return MySpaceTexts.BlockPropertiesText_MotorLocked;
            }
            else
            {
                if (m_topBlockId.Value == null)
                    return MySpaceTexts.BlockPropertiesText_MotorDetached;
                else if (m_topBlockId.Value == 0)
                    return MySpaceTexts.BlockPropertiesText_HeadAttachingAny;
                else if (SafeConstraint != null)
                    return MySpaceTexts.BlockPropertiesText_MotorAttached;
                else
                    return MySpaceTexts.BlockPropertiesText_HeadAttachingSpecific;
            }
        }

        private void UpdateAnimation()
        {
            Debug.Assert(m_subpart1 != null && m_subpart2 != null && Subpart3 != null);

            // ugly but what can we do
            if (m_resetInterpolationFlag == true)
            {
                VRageRender.MyRenderProxy.UpdateRenderObjectVisibility((uint)m_subpart1.Render.GetRenderObjectID(), true, false);
                VRageRender.MyRenderProxy.UpdateRenderObjectVisibility((uint)m_subpart2.Render.GetRenderObjectID(), true, false);
                VRageRender.MyRenderProxy.UpdateRenderObjectVisibility((uint)Subpart3.Render.GetRenderObjectID(), true, false);

                m_resetInterpolationFlag = false;
            }

            //The position is relative to subpart dummy
            var currentPos = m_currentPos;
            //Subpart 1
            var offset = Math.Min((currentPos - 2 * Range / 3), Range / 3);
            offset = Math.Max(0, offset);

            if (m_subpart1 != null)
                m_subpart1.PositionComp.SetLocalMatrix(Matrix.CreateWorld(m_subpart1LocPos + Vector3.Up * offset, Vector3.Forward, Vector3.Up));
            //Subpart 2
            offset = Math.Min((currentPos - Range / 3), Range / 3);
            offset = Math.Max(0, offset);
            if (m_subpart2 != null)
                m_subpart2.PositionComp.SetLocalMatrix(Matrix.CreateWorld(m_subpart2LocPos + Vector3.Up * offset, Vector3.Forward, Vector3.Up));
            //Subpart 3
            offset = Math.Min(currentPos, Range / 3);
            offset = Math.Max(0, offset);
            if (Subpart3 != null)
                Subpart3.PositionComp.SetLocalMatrix(Matrix.CreateWorld(m_subpart3LocPos + Vector3.Up * offset, Vector3.Forward, Vector3.Up));
        }

        private void CreateTopGrid(out MyCubeGrid topGrid, out MyPistonTop topBlock, long ownerId)
        {
            CreateTopGrid(out topGrid, out topBlock, ownerId, MyDefinitionManager.Static.TryGetDefinitionGroup(BlockDefinition.TopPart));
        }

        public bool Attach(MyPistonTop topBlock, bool updateGroup = true)
        {
            Debug.Assert(topBlock != null, "Top block cannot be null!");

            if (topBlock == null)
            {
                return false;
            }

            if (CubeGrid == topBlock.CubeGrid)
                return false;

            Debug.Assert(m_constraint == null, "Contraint already attached, call detach first!");
            Debug.Assert(m_topBlockId.Value.HasValue && (m_topBlockId.Value.Value == 0 || m_topBlockId.Value.Value == topBlock.EntityId), "m_topBlockId must be set prior calling Attach!");

            UpdateAnimation();

            if (CubeGrid.Physics != null && CubeGrid.Physics.Enabled && topBlock.CubeGrid.Physics != null)
            {
                m_topBlock = topBlock;
                m_topGrid = m_topBlock.CubeGrid;
                var topBody = m_topGrid.Physics.RigidBody;

                if (m_topGrid.Physics == null)
                    return false;
                if (CubeGrid.Physics.RigidBody == m_topGrid.Physics.RigidBody)
                {
                    if (updateGroup && m_welded)
                    {
                        OnConstraintAdded(GridLinkTypeEnum.Physical, m_topGrid);
                        OnConstraintAdded(GridLinkTypeEnum.Logical, m_topGrid);
                    }
                    m_isAttached = true;
                    return true;
                }

                var matAD = MatrixD.CreateWorld(Vector3D.Transform(Vector3D.Transform(m_constraintBasePos, Subpart3.WorldMatrix), CubeGrid.PositionComp.WorldMatrixNormalizedInv), PositionComp.LocalMatrix.Forward, PositionComp.LocalMatrix.Up);
                var matBD = MatrixD.CreateWorld(m_topBlock.Position * m_topBlock.CubeGrid.GridSize, m_topBlock.PositionComp.LocalMatrix.Forward, m_topBlock.PositionComp.LocalMatrix.Up);
                var matA = (Matrix)matAD;
                var matB = (Matrix)matBD;
                m_fixedData = new HkFixedConstraintData();
                m_fixedData.SetInertiaStabilizationFactor(10);
                m_fixedData.SetSolvingMethod(HkSolvingMethod.MethodStabilized);
                m_fixedData.SetInBodySpace(matA, matB, CubeGrid.Physics, m_topGrid.Physics);
                var breakData = new HkBreakableConstraintData(m_fixedData);
                //Dont dispose the fixed data or we wont have access to them

                breakData.Threshold = BreakOffTreshold;
                breakData.ReapplyVelocityOnBreak = true;
                breakData.RemoveFromWorldOnBrake = false;
                m_constraint = new HkConstraint(CubeGrid.Physics.RigidBody, topBody, breakData);
                m_constraint.WantRuntime = true;

                CubeGrid.Physics.AddConstraint(m_constraint);
                if(!m_constraint.InWorld)
                {
                    Debug.Fail("Constraint was not added to world");
                    CubeGrid.Physics.RemoveConstraint(m_constraint);
                    m_constraint.Dispose();
                    m_constraint = null;
                    m_fixedData.Dispose();
                    m_fixedData = null;
                    return false;
                }
                m_constraint.Enabled = true;

                m_topBlock.Attach(this);
                m_isAttached = true;

                if (updateGroup)
                {
                    OnConstraintAdded(GridLinkTypeEnum.Physical, m_topGrid);
                    OnConstraintAdded(GridLinkTypeEnum.Logical, m_topGrid);
                    m_topGrid.OnPhysicsChanged += CubeGrid_OnPhysicsChanged;
                }
                m_conveyorEndpoint.Attach(topBlock.ConveyorEndpoint as MyAttachableConveyorEndpoint);
                UpdateText();
                return true;
            }
            return false;
        }

        private void CreateTopGrid(out MyCubeGrid topGrid, out MyPistonTop topBlock, long builtBy, MyCubeBlockDefinitionGroup topGroup)
        {
            if (topGroup == null)
            {
                topGrid = null;
                topBlock = null;
                return;
            }

            var gridSize = CubeGrid.GridSizeEnum;

            float size = MyDefinitionManager.Static.GetCubeSize(gridSize);
            var matrix = MatrixD.CreateWorld(Vector3D.Transform(m_constraintBasePos, Subpart3.WorldMatrix), WorldMatrix.Forward, WorldMatrix.Up);

            var definition = topGroup[gridSize];
            Debug.Assert(definition != null);

            var block = MyCubeGrid.CreateBlockObjectBuilder(definition, Vector3I.Zero, MyBlockOrientation.Identity, MyEntityIdentifier.AllocateId(), OwnerId, fullyBuilt: MySession.Static.CreativeMode);

            var gridBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CubeGrid>();
            gridBuilder.GridSizeEnum = gridSize;
            gridBuilder.IsStatic = false;
            gridBuilder.PositionAndOrientation = new MyPositionAndOrientation(matrix);
            gridBuilder.CubeBlocks.Add(block);

            var grid = MyEntityFactory.CreateEntity<MyCubeGrid>(gridBuilder);
            grid.Init(gridBuilder);

            topGrid = grid;
            topBlock = (MyPistonTop)topGrid.GetCubeBlock(Vector3I.Zero).FatBlock;

            if (!CanPlaceTop(topBlock, builtBy))
            {
                topGrid = null;
                topBlock = null;
                grid.Close();
                return;
            }
            //topGrid.SetPosition(topGrid.WorldMatrix.Translation - (topBlock.WorldMatrix.Translation/*Vector3.Transform(topBlock.DummyPosLoc, topGrid.WorldMatrix) - topGrid.WorldMatrix.Translation*/));
      
            MyEntities.Add(grid);
            if (MyFakes.ENABLE_SENT_GROUP_AT_ONCE)
            {
                MyMultiplayer.ReplicateImmediatelly(MyExternalReplicable.FindByObject(grid), MyExternalReplicable.FindByObject(CubeGrid));
            }
            m_topBlockId.Value = topBlock.EntityId;
        }

        protected virtual bool CanPlaceTop(MyPistonTop topBlock, long builtBy)
        {
            // Compute the rough actual position for the head, this improves the detection if it can be placed
            float topDistance = (Subpart3.Model.BoundingBoxSize.Y);
            Vector3D topPosition = this.Subpart3.WorldMatrix.Translation + this.WorldMatrix.Up * topDistance;
            float topRadius = topBlock.ModelCollision.HavokCollisionShapes[0].ConvexRadius * 0.9f;

            // First test if we intersect any blocks of our own grid
            BoundingSphereD sphere = topBlock.Model.BoundingSphere;
            sphere.Center = topPosition;
            sphere.Radius = topRadius;
            CubeGrid.GetBlocksInsideSphere(ref sphere, m_tmpSet);

            // If we intersect more than 1 block (because top sometimes intersects piston), don't add top
            if (m_tmpSet.Count > 1)
            {
                m_tmpSet.Clear();
                if (builtBy == MySession.Static.LocalPlayerId)
                    MyHud.Notifications.Add(MyNotificationSingletons.HeadNotPlaced);
                return false;
            }

            m_tmpSet.Clear();

            // Next test if we intersect any physics objects
            HkSphereShape spShape = new HkSphereShape(topRadius);
            Quaternion q = Quaternion.Identity;
            MyPhysics.GetPenetrationsShape(topBlock.ModelCollision.HavokCollisionShapes[0], ref topPosition, ref q, m_penetrations, MyPhysics.CollisionLayers.DefaultCollisionLayer);

            // If we have any collisions with anything other than our own grid, don't add the head
            // We already checked for inner-grid collisions in the previous case
            for (int i = 0; i < m_penetrations.Count; i++)
            {
                MyCubeGrid grid = m_penetrations[i].GetCollisionEntity().GetTopMostParent() as MyCubeGrid;
                if (grid == null || grid != CubeGrid)
                {
                    m_penetrations.Clear();
                    if (builtBy == MySession.Static.LocalPlayerId)
                        MyHud.Notifications.Add(MyNotificationSingletons.HeadNotPlaced);
                    return false;
                }
            }

            m_penetrations.Clear();

            return true;
        }

        public bool Detach(bool updateGroup = true)
        {
            if (m_isAttached == false)
            {
                return false;
            }

            m_isAttached = false;

            if (m_constraint == null)
                return m_welded;

            if (m_isWelding == false)
            {
                UnweldGroup();
            }

            Debug.Assert(m_constraint != null);
            Debug.Assert(m_topGrid != null);
            Debug.Assert(m_topBlock != null);

            var tmpTopGrid = m_topGrid;
            if (updateGroup)
            {
                tmpTopGrid.OnPhysicsChanged -= CubeGrid_OnPhysicsChanged;
                OnConstraintRemoved(GridLinkTypeEnum.Physical, tmpTopGrid);
                OnConstraintRemoved(GridLinkTypeEnum.Logical, tmpTopGrid);
            }

            m_fixedData.Dispose();
            m_fixedData = null;
            CubeGrid.Physics.RemoveConstraint(m_constraint);
            m_constraint.Dispose();
            m_constraint = null;
            m_topGrid = null;
            if (m_topBlock != null)
                m_topBlock.Detach(m_welded || m_isWelding);
            m_conveyorEndpoint.Detach(m_topBlock.ConveyorEndpoint as MyAttachableConveyorEndpoint);

            m_topBlock = null;

            UpdateText();
            return true;
        }

        public void SyncDetach()
        {
            m_topBlockId.Value = 0; // Look for anything to attach (to make grid split work correctly)
        }

        public override void UpdateOnceBeforeFrame()
        {
            TryAttach();
            TryWeld();

            LoadSubparts();

            UpdateAnimation();
            UpdatePosition(true);
            UpdatePhysicsShape();

            if (AttachedEntityChanged != null)
            {
                AttachedEntityChanged(this);
            }
            base.UpdateOnceBeforeFrame();
        }

        void TryAttach()
        {
            if (!CubeGrid.InScene)
                return;

            var updateFlags = NeedsUpdate;
            updateFlags &= ~MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            if (m_topBlockId.Value.HasValue == false) // Detached
            {
                if (m_constraint != null)
                {
                    Detach();
                }
            }
            else if (m_topBlockId.Value.Value == 0) // Find anything to attach (only on server)
            {
                if (Sync.IsServer)
                {
                    var top = FindMatchingTop();
                    if (top != null)
                    {
                        if (m_constraint != null)
                        {
                            Detach();
                        }

                        m_topBlockId.Value = top.EntityId;
                        Attach(top);
                    }
                }
            }
            else if (m_topBlockId.Value.HasValue && ((false == m_welded && (m_topBlock == null || m_topBlock.EntityId != m_topBlockId.Value)) ||
                     (m_welded && m_weldedTopBlockId != m_topBlockId.Value))) // Attached to something else or nothing
            {
                Detach();
                MyPistonTop top;
                bool attached = false;
                if (MyEntities.TryGetEntityById<MyPistonTop>(m_topBlockId.Value.Value, out top) && !top.MarkedForClose && top.CubeGrid.InScene)
                {
                    if (Sync.IsServer)
                    {
                        m_topBlockId.Value = top.EntityId;
                    }
                    attached = Attach(top);
                }
            }
            NeedsUpdate = updateFlags;
            RefreshConstraint();
        }

        protected void TryWeld()
        {
            if (m_weldedEntityId.Value == null)
            {
                if (m_welded)
                {
                    UnweldGroup();
                }
            }
            else if (m_weldedEntityId.Value != m_weldedTopBlockId && m_topBlock != null)
            {
                WeldGroup();
            }

        }

        protected MyPistonTop FindMatchingTop()
        {
            Debug.Assert(CubeGrid != null);
            Debug.Assert(m_penetrations != null);
            Debug.Assert(CubeGrid.Physics != null);
            if (CubeGrid == null)
            {
                MySandboxGame.Log.WriteLine("MyPistonBase.FindMatchingTop(): Cube grid == null!");
                return null;
            }

            if (m_penetrations == null)
            {
                MySandboxGame.Log.WriteLine("MyPistonBase.FindMatchingTop(): penetrations cache == null!");
                return null;
            }

            if (CubeGrid.Physics == null)
            {
                MySandboxGame.Log.WriteLine("MyPistonBase.FindMatchingTop(): Cube grid physics == null!");
                return null;
            }

            Quaternion orientation;
            Vector3D pos;
            Vector3 halfExtents;
            ComputeTopQueryBox(out pos, out halfExtents, out orientation);
            try
            {
                MyPhysics.GetPenetrationsBox(ref halfExtents, ref pos, ref orientation, m_penetrations, MyPhysics.CollisionLayers.DefaultCollisionLayer);
                foreach (var obj in m_penetrations)
                {
                    var entity = obj.GetCollisionEntity();
                    if (entity == null || entity == CubeGrid)
                        continue;

                    var grid = entity as MyCubeGrid;
                    if (grid == null)
                        continue;

                    // Tops should always be on position [0,0,0];
                    var blockPos = grid.RayCastBlocks(pos, pos + WorldMatrix.Up);
                    if (blockPos.HasValue)
                    {
                        var slimBlock = grid.GetCubeBlock(blockPos.Value);
                        if (slimBlock == null || slimBlock.FatBlock == null)
                            continue;

                        var top = slimBlock.FatBlock as MyPistonTop;
                        if (top != null)
                            return top;
                    }
                }
            }
            finally
            {
                m_penetrations.Clear();
            }
            return null;
        }

        public void Reattach(bool force = false)
        {
            if (m_topBlock == null || m_topBlock.Closed)
            {
                return;
            }

            if (force == false && (m_welded || m_isWelding))
            {
                return;
            }

            var head = m_topBlock;
            bool detached = Detach(force);
            if (MarkedForClose || Closed || head.MarkedForClose || head.Closed || CubeGrid.MarkedForClose || CubeGrid.Closed)
            {
                return;
            }
            bool attached = Attach(head, force);
            //Debug.Assert(detached && attached);
            if (!head.MarkedForClose && head.CubeGrid.Physics != null)
                head.CubeGrid.Physics.ForceActivate();
        }

        public virtual void ComputeTopQueryBox(out Vector3D pos, out Vector3 halfExtents, out Quaternion orientation)
        {
            var world = this.WorldMatrix;
            orientation = Quaternion.CreateFromRotationMatrix(world);
            halfExtents = Vector3.One * CubeGrid.GridSize * 0.35f;
            halfExtents.Y = CubeGrid.GridSize * 0.25f;
            pos = Subpart3.WorldMatrix.Translation + Subpart3.PositionComp.WorldVolume.Radius * WorldMatrix.Up + 0.5f * CubeGrid.GridSize * WorldMatrix.Up;
        }

        public override void OnRemovedFromScene(object source)
        {
            base.OnRemovedFromScene(source);
            CubeGrid.OnPhysicsChanged -= CubeGrid_OnPhysicsChanged;
            Detach();
            DisposeSubpartsPhysics();
        }

        protected override void Closing()
        {
            CubeGrid.OnPhysicsChanged -= CubeGrid_OnPhysicsChanged;
            Detach();
            base.Closing();
        }

        protected override void BeforeDelete()
        {
            DisposeSubpartsPhysics();
            base.BeforeDelete();
        }

        public void InitializeConveyorEndpoint()
        {
            m_conveyorEndpoint = new MyAttachableConveyorEndpoint(this);
            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_conveyorEndpoint));
        }

        private void UpdateSoundState()
        {
            if (!MySandboxGame.IsGameReady || m_soundEmitter == null || IsWorking == false)
                return;

            if (m_topGrid == null || m_topGrid.Physics == null)
            {
                m_soundEmitter.StopSound(true);
                return;
            }

            if (IsWorking && m_lastPosition.Equals(m_currentPos.Value) == false)
                m_soundEmitter.PlaySingleSound(BlockDefinition.PrimarySound, true);
            else
                m_soundEmitter.StopSound(false);
            m_lastPosition = m_currentPos.Value;
        }

        #region IMyConveyorEndpointBlock implementation

        public Sandbox.Game.GameSystems.Conveyors.PullInformation GetPullInformation()
        {
            return null;
        }

        public Sandbox.Game.GameSystems.Conveyors.PullInformation GetPushInformation()
        {
            return null;
        }

        #endregion
    }
}
