using Havok;
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
    public partial class MyPistonBase : MyMechanicalConnectionBlockBase, IMyConveyorEndpointBlock
    {
        private HkConstraint m_subpartsConstraint;

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


        private MyAttachableConveyorEndpoint m_conveyorEndpoint;
        private bool m_posChanged;
        private Vector3 m_subpartsConstraintPos;

        private float m_lastPosition = float.MaxValue;
        private readonly Sync<float> m_currentPos; // Necessary?
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

        public IMyConveyorEndpoint ConveyorEndpoint { get { return m_conveyorEndpoint; } }

        public float BreakOffTreshold { get { return CubeGrid.GridSizeEnum == MyCubeSize.Large ? 20000000 : 1000000; } }

        event Action<bool> LimitReached;

        public MyPistonBase()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_currentPos = SyncType.CreateAndAddProp<float>();
            Velocity = SyncType.CreateAndAddProp<float>();
            MinLimit = SyncType.CreateAndAddProp<float>();
            MaxLimit = SyncType.CreateAndAddProp<float>();
#endif // XB1
            CreateTerminalControls();

            m_currentPos.ValueChanged += (o) => UpdatePosition(true);
        }

        static void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyPistonBase>())
                return;

            var reverse = new MyTerminalControlButton<MyPistonBase>("Reverse", MySpaceTexts.BlockActionTitle_Reverse, MySpaceTexts.Blank, (x) => x.Velocity.Value = -x.Velocity);
            reverse.EnableAction(MyTerminalActionIcons.REVERSE);
            MyTerminalControlFactory.AddControl(reverse);

            var extendAction = new MyTerminalAction<MyPistonBase>("Extend", MyTexts.Get(MySpaceTexts.BlockActionTitle_Extend), OnExtendApplied, null, MyTerminalActionIcons.REVERSE);
            extendAction.Enabled = (b) => b.IsFunctional == true;
            MyTerminalControlFactory.AddAction(extendAction);

            var retractAction = new MyTerminalAction<MyPistonBase>("Retract", MyTexts.Get(MySpaceTexts.BlockActionTitle_Retract), OnRetractApplied, null, MyTerminalActionIcons.REVERSE);
            retractAction.Enabled = (b) => b.IsFunctional == true;
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

           
            var addPistonHead = new MyTerminalControlButton<MyPistonBase>("Add Piston Head", MySpaceTexts.BlockActionTitle_AddPistonHead, MySpaceTexts.BlockActionTooltip_AddPistonHead, (b) => b.RecreateTop());
            addPistonHead.Enabled = (b) => (b.m_topBlock == null);
            addPistonHead.EnableAction(MyTerminalActionIcons.STATION_ON);
            MyTerminalControlFactory.AddControl(addPistonHead);
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

            if (ob.TopBlockId.HasValue && ob.TopBlockId.Value != 0)
            {
                MyDeltaTransform? deltaTransform = ob.MasterToSlaveTransform.HasValue ? ob.MasterToSlaveTransform.Value : (MyDeltaTransform?)null;
                m_connectionState.Value = new State() { TopBlockId = ob.TopBlockId, MasterToSlave = deltaTransform, Welded = ob.IsWelded || ob.ForceWeld};
            }

            m_currentPos.Value = ob.CurrentPosition;

            CubeGrid.OnPhysicsChanged += cubeGrid_OnPhysicsChanged;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            float defaultWeldSpeed = ob.WeldSpeed; //weld before reaching the max speed
            defaultWeldSpeed *= defaultWeldSpeed;
            m_weldSpeedSq.Value = defaultWeldSpeed;
            m_forceWeld.Value = ob.ForceWeld;
        }

        protected override void cubeGrid_OnPhysicsChanged()
        {
            DisposeSubpartsPhysics();

            // If the physics isn't being changed because we are being closed, reload the subparts, and update physics
            if (!Closed)
            {
                LoadSubparts();
                UpdatePosition(true);
                UpdatePhysicsShape();;
            }
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = (MyObjectBuilder_PistonBase)base.GetObjectBuilderCubeBlock(copy);
            ob.Velocity = Velocity / BlockDefinition.MaxVelocity;
            ob.MaxLimit = NormalizeDistance(MaxLimit);
            ob.MinLimit = NormalizeDistance(MinLimit);
            ob.TopBlockId = m_connectionState.Value.TopBlockId;
            ob.CurrentPosition = m_currentPos;
            ob.WeldSpeed = (float)Math.Sqrt(m_weldSpeedSq);
            ob.ForceWeld = m_forceWeld;

            ob.IsWelded = m_connectionState.Value.Welded;

            ob.MasterToSlaveTransform = m_connectionState.Value.MasterToSlave.HasValue ? m_connectionState.Value.MasterToSlave.Value : (MyPositionAndOrientation?)null;

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
             CreateTopGrid(builderId);
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
                CreateTopGrid(builtBy);
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();
            if (CheckVelocities())
            {
                UpdateSoundState();

                NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
                return;
            }
            UpdateText();

            TryWeld();
            TryAttach();

            UpdatePhysicsShape();
            UpdateSoundState();

            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
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

            UpdateText();
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
            if (Sync.IsServer == false && m_connectionState.Value.TopBlockId.HasValue && m_topBlock == null)
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
                        if (m_currentPos.Value == MinLimit)
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
                    if (m_currentPos.Value == MaxLimit)
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

        protected override void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(GetAttachState())).AppendLine();

            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_PistonCurrentPosition)).AppendDecimal(m_currentPos, 1).Append("m");
            RaisePropertiesChanged();
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

        protected override void CreateTopGrid(out MyCubeGrid topGrid, out MyAttachableTopBlockBase topBlock, long ownerId)
        {
            CreateTopGrid(out topGrid, out topBlock, ownerId, MyDefinitionManager.Static.TryGetDefinitionGroup(BlockDefinition.TopPart));
        }

        protected override bool Attach(MyAttachableTopBlockBase topBlock, bool updateGroup = true)
        {
            Debug.Assert(topBlock != null, "Top block cannot be null!");

            if (CubeGrid == topBlock.CubeGrid)
                return false;

            MyPistonTop pistonTop = topBlock as MyPistonTop;
            if (pistonTop != null && base.Attach(topBlock, updateGroup))
            {
                Debug.Assert(m_constraint == null, "Contraint already attached, call detach first!");
                Debug.Assert(m_connectionState.Value.TopBlockId.HasValue && (m_connectionState.Value.TopBlockId.Value == 0 || m_connectionState.Value.TopBlockId.Value == topBlock.EntityId), "m_topBlockId must be set prior calling Attach!");

                UpdateAnimation();

                var matAD = MatrixD.CreateWorld(Vector3D.Transform(Vector3D.Transform(m_constraintBasePos, Subpart3.WorldMatrix), CubeGrid.PositionComp.WorldMatrixNormalizedInv), PositionComp.LocalMatrix.Forward, PositionComp.LocalMatrix.Up);
                var matBD = MatrixD.CreateWorld(m_topBlock.Position * m_topBlock.CubeGrid.GridSize, m_topBlock.PositionComp.LocalMatrix.Forward, m_topBlock.PositionComp.LocalMatrix.Up);
                var matA = (Matrix)matAD;
                var matB = (Matrix)matBD;
                m_fixedData = new HkFixedConstraintData();
                m_fixedData.SetInertiaStabilizationFactor(10);
                m_fixedData.SetSolvingMethod(HkSolvingMethod.MethodStabilized);
                m_fixedData.SetInBodySpace(matA, matB, CubeGrid.Physics, m_topGrid.Physics);

                //Dont dispose the fixed data or we wont have access to them

                m_constraint = new HkConstraint(CubeGrid.Physics.RigidBody, topBlock.CubeGrid.Physics.RigidBody, m_fixedData);
                m_constraint.WantRuntime = true;

                CubeGrid.Physics.AddConstraint(m_constraint);
                if (!m_constraint.InWorld)
                {
                    Debug.Fail("Constraint was not added to world");
                    CubeGrid.Physics.RemoveConstraint(m_constraint);
                    m_constraint.Dispose();
                    m_constraint = null;
                    m_fixedData = null;
                    return false;
                }
                m_constraint.Enabled = true;

                m_topBlock = topBlock;
                m_topGrid = topBlock.CubeGrid;
                topBlock.Attach(this);
                m_isAttached = true;

                if (updateGroup)
                {
                    m_conveyorEndpoint.Attach(pistonTop.ConveyorEndpoint as MyAttachableConveyorEndpoint);
                }

                UpdateText();
                return true;
            }
            return false;
        }

        private void CreateTopGrid(out MyCubeGrid topGrid, out MyAttachableTopBlockBase topBlock, long builtBy, MyCubeBlockDefinitionGroup topGroup)
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
        }

        protected virtual bool CanPlaceTop(MyAttachableTopBlockBase topBlock, long builtBy)
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



        public override void UpdateOnceBeforeFrame()
        {
            TryWeld();
            TryAttach();

            LoadSubparts();

            UpdateAnimation();
            UpdatePosition(true);
            UpdatePhysicsShape();

            if (AttachedEntityChanged != null)
            {
                AttachedEntityChanged(this);
            }
            base.UpdateOnceBeforeFrame();

            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        public override void OnRemovedFromScene(object source)
        {
            DisposeSubpartsPhysics();
            base.OnRemovedFromScene(source);
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

        public override void ComputeTopQueryBox(out Vector3D pos, out Vector3 halfExtents, out Quaternion orientation)
        {
            var world = this.WorldMatrix;
            orientation = Quaternion.CreateFromRotationMatrix(world);
            halfExtents = Vector3.One * CubeGrid.GridSize * 0.35f;
            halfExtents.Y = CubeGrid.GridSize * 0.25f;
            pos = Subpart3.WorldMatrix.Translation + Subpart3.PositionComp.WorldVolume.Radius * WorldMatrix.Up + 0.5f * CubeGrid.GridSize * WorldMatrix.Up;
        }

        protected override void DisposeConstraint()
        {
           if(m_constraint != null)
           {
               m_fixedData = null;
               CubeGrid.Physics.RemoveConstraint(m_constraint);
               m_constraint.Dispose();
               m_constraint = null;
           }
        }

        public override bool Detach(bool updateGroup = true)
        {
            if (m_topBlock != null && updateGroup)
            {
                MyPistonTop pistonTop = m_topBlock as MyPistonTop;
                if (pistonTop != null )
                {
                    m_conveyorEndpoint.Detach(pistonTop.ConveyorEndpoint as MyAttachableConveyorEndpoint);
                }
            }
            var ret = base.Detach(updateGroup);
            return ret;
        }

        protected override void CustomUnweld()
        {
            base.CustomUnweld();

            MyPistonTop pistonTop = m_topBlock as MyPistonTop;
            if (pistonTop != null)
            {
                m_conveyorEndpoint.Detach(pistonTop.ConveyorEndpoint as MyAttachableConveyorEndpoint);
            }
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
