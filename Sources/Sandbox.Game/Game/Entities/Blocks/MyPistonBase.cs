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
using VRage.Profiler;
using VRage.Sync;
using VRageRender.Import;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_PistonBase))]
    public partial class MyPistonBase : MyMechanicalConnectionBlockBase, IMyConveyorEndpointBlock
    {
        private HkConstraint m_subpartsConstraint;

        private MyPhysicsBody m_subpartPhysics;
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
            //We cannot wait 10 frames if velocity changed drasticaly because middle part physics can penetrate with top
            Velocity.ValueChanged += (o) => UpdatePhysicsShape();
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyPistonBase>())
                return;
            base.CreateTerminalControls();

            var addPistonHead = new MyTerminalControlButton<MyPistonBase>("Add Top Part", MySpaceTexts.BlockActionTitle_AddPistonHead, MySpaceTexts.BlockActionTooltip_AddPistonHead, (b) => b.RecreateTop());
            addPistonHead.Enabled = (b) => (b.TopBlock == null);
            addPistonHead.EnableAction(MyTerminalActionIcons.STATION_ON);
            MyTerminalControlFactory.AddControl(addPistonHead);

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
                () => (Enabled && IsFunctional) ? ResourceSink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId) : 0.0f);
            ResourceSink = sinkComp;

            base.Init(objectBuilder, cubeGrid);

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
      
            ResourceSink.IsPoweredChanged += Receiver_IsPoweredChanged;
            ResourceSink.Update();

            var ob = objectBuilder as MyObjectBuilder_PistonBase;
            Velocity.Value = ob.Velocity * BlockDefinition.MaxVelocity;
            MaxLimit.Value = ob.MaxLimit.HasValue ? Math.Min(DenormalizeDistance(ob.MaxLimit.Value), BlockDefinition.Maximum) : BlockDefinition.Maximum;
            MinLimit.Value = ob.MinLimit.HasValue ? Math.Max(DenormalizeDistance(ob.MinLimit.Value), BlockDefinition.Minimum) : BlockDefinition.Minimum;

            m_currentPos.Value = ob.CurrentPosition;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }


        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = (MyObjectBuilder_PistonBase)base.GetObjectBuilderCubeBlock(copy);
            ob.Velocity = Velocity / BlockDefinition.MaxVelocity;
            ob.MaxLimit = NormalizeDistance(MaxLimit);
            ob.MinLimit = NormalizeDistance(MinLimit);
            ob.CurrentPosition = m_currentPos;

            return ob;
        }

        protected override void OnEnabledChanged()
        {
            base.OnEnabledChanged();
            ResourceSink.Update();
        }

        protected override bool CheckIsWorking()
        {
            return ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) && base.CheckIsWorking();
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            DisposeSubpartsPhysics();
            LoadSubparts();
            UpdatePosition(true);
            UpdatePhysicsShape();

            if (m_subpart1 != null && m_subpartPhysics != null)
            {
                m_subpartPhysics.Enabled = false;
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

            Debug.Assert(!this.Closed);
            if (this.Closed)
            {
                // When closed, the welding callback will call this, but the model will already be unloaded
                return;
            }

            if (!Subparts.TryGetValue("PistonSubpart1", out m_subpart1))
                return;
            if (!m_subpart1.Subparts.TryGetValue("PistonSubpart2", out m_subpart2))
                return;
            if (!m_subpart2.Subparts.TryGetValue("PistonSubpart3", out Subpart3))
                return;

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
            //m_subpartPhysics = new MyPhysicsBody(this, CubeGrid.IsStatic ? RigidBodyFlag.RBF_STATIC : (CubeGrid.GridSizeEnum == MyCubeSize.Large ? RigidBodyFlag.RBF_DOUBLED_KINEMATIC : RigidBodyFlag.RBF_DEFAULT));
            m_subpartPhysics = new MyPhysicsBody(this, CubeGrid.IsStatic ? RigidBodyFlag.RBF_DEFAULT : (CubeGrid.GridSizeEnum == MyCubeSize.Large ? RigidBodyFlag.RBF_DOUBLED_KINEMATIC : RigidBodyFlag.RBF_DEFAULT));
            const float threshold = 0.11f; // Must be bigger than 2x convex radius
            HkCylinderShape shape = new HkCylinderShape(new Vector3(0, -2, 0), new Vector3(0, 2, 0), CubeGrid.GridSize / 2 - threshold, 0.05f);
            var mass = HkInertiaTensorComputer.ComputeCylinderVolumeMassProperties(new Vector3(0, -2, 0), new Vector3(0, 2, 0), CubeGrid.GridSize / 2, 40.0f * CubeGrid.GridSize);
            mass.Mass = BlockDefinition.Mass;
            m_subpartPhysics.CreateFromCollisionObject(shape, Vector3.Zero, subpart.WorldMatrix, mass);
            m_subpartPhysics.RigidBody.Layer = CubeGrid.Physics.RigidBody.Layer;
            var info = HkGroupFilter.CalcFilterInfo(m_subpartPhysics.RigidBody.Layer, CubeGrid.Physics.HavokCollisionSystemID, 1, 1);
            m_subpartPhysics.RigidBody.SetCollisionFilterInfo(info);
            shape.Base.RemoveReference();
            if (m_subpartPhysics.RigidBody2 != null)
                m_subpartPhysics.RigidBody2.Layer = MyPhysics.CollisionLayers.KinematicDoubledCollisionLayer;
            CubeGrid.OnHavokSystemIDChanged += CubeGrid_OnHavokSystemIDChanged;

            m_subpartPhysics.IsSubpart = true;

            CreateSubpartsConstraint(subpart);

            m_posChanged = true;
        }

        void CubeGrid_OnHavokSystemIDChanged(int sysId)
        {
            if (CubeGrid.Physics != null && CubeGrid.Physics.RigidBody != null && m_subpartPhysics != null && m_subpartPhysics.RigidBody != null)
            {
                var info = HkGroupFilter.CalcFilterInfo(CubeGrid.Physics.RigidBody.Layer, sysId, 1, 1);
                m_subpartPhysics.RigidBody.SetCollisionFilterInfo(info);
            }
        }

        private void DisposeSubpartsPhysics()
        {
            if (m_subpartsConstraint != null)
            {
                DisposeSubpartsConstraint();
            }
            if (m_subpart1 != null && m_subpartPhysics != null)
            {
                CubeGrid.OnHavokSystemIDChanged -= CubeGrid_OnHavokSystemIDChanged;
                m_subpartPhysics.Enabled = false;
                m_subpartPhysics.Close();
                m_subpartPhysics = null;
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
            m_subpartsConstraint = new HkConstraint(CubeGrid.Physics.RigidBody, m_subpartPhysics.RigidBody, constraintData);
            var info = CubeGrid.Physics.RigidBody.GetCollisionFilterInfo();
            info = HkGroupFilter.CalcFilterInfo(CubeGrid.Physics.RigidBody.Layer, HkGroupFilter.GetSystemGroupFromFilterInfo(info), 1, 1);
            m_subpartPhysics.RigidBody.SetCollisionFilterInfo(info);
            CubeGrid.Physics.HavokWorld.RefreshCollisionFilterOnEntity(m_subpartPhysics.RigidBody);
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
            if (!Sandbox.Engine.Physics.MyPhysicsBody.IsConstraintValid(m_subpartsConstraint))
            {
                DisposeSubpartsConstraint();
                CreateSubpartsConstraint(m_subpart1);
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
            base.OnBuildSuccess(builtBy);
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();
            if (m_welded)
            {
                UpdateSoundState();

                NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
                return;
            }
            UpdateText();

            UpdatePhysicsShape();
            UpdateSoundState();

            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        private void UpdatePhysicsShape()
        {
            var subpart = m_subpart1;
            if (!m_posChanged || subpart == null || m_subpartPhysics == null || m_subpartPhysics.RigidBody == null)
                return;
            m_posChanged = false;
            //We are updating once 10 frames so we need to account for velocity when retracting
            var speedCorrection = Math.Abs(Velocity < 0 ? Velocity / 6 : 0);
            float subpartOffset = 0.5f; //<ib.clang> Magic offset to move piston body out of base
            Vector3 vertexA = new Vector3(0, subpartOffset - m_currentPos * 0.5f + speedCorrection - 0.1f, 0);
            Vector3 vertexB = new Vector3(0, subpartOffset + m_currentPos * 0.5f - speedCorrection, 0);
            
            if (vertexB.Y - vertexA.Y > 0.1f) //larger than convex radius
            {
                var existingShape = m_subpartPhysics.RigidBody.GetShape();
                if (existingShape.ShapeType == HkShapeType.Cylinder)
                {
                    float dist = Math.Abs(vertexA.Y - vertexB.Y);
                    var cyl = (HkCylinderShape) existingShape;
                    float distExist = Math.Abs(cyl.VertexA.Y - cyl.VertexB.Y);
                    if (Math.Abs(dist - distExist) < 0.001f)
                        return;
                    cyl.VertexA = vertexA;
                    cyl.VertexB = vertexB;
                    m_subpartPhysics.RigidBody.UpdateShape();

                    if(m_subpartPhysics.RigidBody2 != null)
                        m_subpartPhysics.RigidBody2.UpdateShape();
                }
                else
                {
                    Debug.Fail("Piston subpart shape isnt cylinder. Shouldnt happen");
                }
                if (!m_subpartPhysics.Enabled)
                {
                    m_subpartPhysics.Enabled = true;
                }
                //jn:TODO hack fix
                CheckSubpartConstraint();
                UpdateSubpartFixedData();
                if (CubeGrid.Physics.IsInWorldWelded() && m_subpartPhysics.IsInWorldWelded())
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
                m_subpartPhysics.Enabled = false;
            }
        }

        public override void UpdateBeforeSimulation()
        {
            ProfilerShort.Begin("MyMechanicalConnection.UpdateBeforeSim");
            base.UpdateBeforeSimulation();
            ProfilerShort.End();

            UpdateText();

            if (m_welded)
            {
                return;
            }

            if (SafeConstraint != null && SafeConstraint.RigidBodyA == SafeConstraint.RigidBodyB) //welded
            {
                SafeConstraint.Enabled = false;
                return;
            }

            ProfilerShort.Begin("UpdatePos");
            UpdatePosition();
            ProfilerShort.End();

            if (m_subpartPhysics != null && m_subpartPhysics.RigidBody2 != null)
            {
                if (m_subpartPhysics.RigidBody.IsActive)
                {
                    m_subpartPhysics.RigidBody2.LinearVelocity = m_subpartPhysics.LinearVelocity;
                    m_subpartPhysics.RigidBody2.AngularVelocity = m_subpartPhysics.AngularVelocity;
                }
                else
                    m_subpartPhysics.RigidBody2.Deactivate();
            }

            if (m_soundEmitter != null && m_soundEmitter.IsPlaying && m_lastPosition.Equals(float.MaxValue))
            {
                m_soundEmitter.StopSound(true);
                m_lastPosition = m_currentPos;
            }
        }

        private void UpdatePosition(bool forceUpdate = false)
        {
            if (m_subpart1 == null) //GK: forceUpdate happens on Init before loading subparts when setting m_currentPos (value changed). So make sure one subpart exists
                return;
            if (!IsWorking && !forceUpdate)
                return;
            //if (!forceUpdate && !CubeGrid.SyncObject.ResponsibleForUpdate(Sync.Clients.LocalClient))
            //return;

            bool changed = false;

            float compensatedDelta = Velocity / 60;

            ProfilerShort.Begin("PosAndHandlers");
            if (!forceUpdate)
            {
                if (compensatedDelta < 0)
                {
                    if (m_currentPos > MinLimit)
                    {
                        m_currentPos.Value = Math.Max(m_currentPos.Value + compensatedDelta, MinLimit);
                        changed = true;
                        if (m_currentPos.Value <= MinLimit)
                        {
                            var handle = LimitReached;
                            if (handle != null) handle(false);
                        }
                    }
                }
                else if (m_currentPos < MaxLimit)
                {
                    m_currentPos.Value = Math.Min(m_currentPos.Value + compensatedDelta, MaxLimit);
                    changed = true;
                    if (m_currentPos.Value >= MaxLimit)
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
                if (Subpart3 == null) MySandboxGame.Log.WriteLine("Subpart is null");
                if (CubeGrid.Physics != null)
                    CubeGrid.Physics.RigidBody.Activate();
                if (TopGrid != null && TopGrid.Physics != null)
                    TopGrid.Physics.RigidBody.Activate();
                Matrix matA;
                GetTopMatrix(out matA);
                Matrix matB = Matrix.Identity;
                if(TopGrid != null)
                    matB = Matrix.CreateWorld(TopBlock.Position * TopBlock.CubeGrid.GridSize /*- TopBlock.LocalMatrix.Up * m_currentPos*/, Subpart3.PositionComp.LocalMatrix.Forward, TopBlock.PositionComp.LocalMatrix.Up);
                ProfilerShort.End();

                ProfilerShort.Begin("SetInBodySpace");
                if (m_fixedData != null)
                    m_fixedData.SetInBodySpace(matA, matB, CubeGrid.Physics, TopGrid.Physics);
                ProfilerShort.End();

                ProfilerShort.Begin("UpdateSubpartFixedData");
                UpdateSubpartFixedData();
                ProfilerShort.End();
            }
        }

        void UpdateSubpartFixedData()
        {
            Matrix matA; 
            GetTopMatrix(out matA);
            matA.Translation -= m_currentPos * PositionComp.LocalMatrix.Up * 0.5f; //half-way between top and base
            var matB = Matrix.Identity;
            if (m_subpartsFixedData != null)
                m_subpartsFixedData.SetInBodySpace(matA, matB, CubeGrid.Physics, m_subpartPhysics);
            else
                MySandboxGame.Log.WriteLine("m_subpartsFixedData is null");
        }


        private void GetTopMatrix(out Matrix m)
        {
            m = PositionComp.LocalMatrix;
            m.Translation = Vector3D.Transform(Vector3D.Transform(m_constraintBasePos, Subpart3.WorldMatrix),
                CubeGrid.PositionComp.WorldMatrixNormalizedInv);
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
            var currentPos = m_currentPos.Value;
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

        protected override bool Attach(MyAttachableTopBlockBase topBlock, bool updateGroup = true)
        {
            Debug.Assert(topBlock != null, "Top block cannot be null!");

            MyPistonTop pistonTop = topBlock as MyPistonTop;
            if (pistonTop != null && base.Attach(topBlock, updateGroup))
            {
                Debug.Assert(m_constraint == null, "Contraint already attached, call detach first!");
                Debug.Assert(m_connectionState.Value.TopBlockId.HasValue && (m_connectionState.Value.TopBlockId.Value == 0 || m_connectionState.Value.TopBlockId.Value == topBlock.EntityId), "m_topBlockId must be set prior calling Attach!");

                UpdateAnimation();

                CreateConstraint(topBlock);
                 
                if (updateGroup)
                {
                    m_conveyorEndpoint.Attach(pistonTop.ConveyorEndpoint as MyAttachableConveyorEndpoint);
                }

                UpdateText();
                return true;
            }
            return false;
        }

        protected override bool CreateConstraint(MyAttachableTopBlockBase topBlock)
        {
            if (!base.CreateConstraint(topBlock))
                return false;
            var matAD =
                MatrixD.CreateWorld(
                    Vector3D.Transform(Vector3D.Transform(m_constraintBasePos, Subpart3.WorldMatrix),
                        CubeGrid.PositionComp.WorldMatrixNormalizedInv), PositionComp.LocalMatrix.Forward,
                    PositionComp.LocalMatrix.Up);
            var matBD = MatrixD.CreateWorld(TopBlock.Position*TopBlock.CubeGrid.GridSize,
                TopBlock.PositionComp.LocalMatrix.Forward, TopBlock.PositionComp.LocalMatrix.Up);
            var matA = (Matrix) matAD;
            var matB = (Matrix) matBD;
            m_fixedData = new HkFixedConstraintData();
            m_fixedData.SetInertiaStabilizationFactor(10);
            m_fixedData.SetSolvingMethod(HkSolvingMethod.MethodStabilized);

            m_fixedData.SetInBodySpace(matA, matB, CubeGrid.Physics, TopGrid.Physics);

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
            return true;
        }

        protected override MatrixD GetTopGridMatrix()
        {
            return MatrixD.CreateWorld(Vector3D.Transform(m_constraintBasePos, Subpart3.WorldMatrix), WorldMatrix.Forward, WorldMatrix.Up);
        }

        protected override bool CanPlaceTop(MyAttachableTopBlockBase topBlock, long builtBy)
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
            LoadSubparts();

            UpdateAnimation();
            UpdatePosition(true);
            UpdatePhysicsShape();

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

            if (TopGrid == null || TopGrid.Physics == null)
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

        protected override void Detach(bool updateGroup = true)
        {
            if (TopBlock != null && updateGroup)
            {
                MyPistonTop pistonTop = TopBlock as MyPistonTop;
                if (pistonTop != null )
                {
                    m_conveyorEndpoint.Detach(pistonTop.ConveyorEndpoint as MyAttachableConveyorEndpoint);
                }
            }
            base.Detach(updateGroup);
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
