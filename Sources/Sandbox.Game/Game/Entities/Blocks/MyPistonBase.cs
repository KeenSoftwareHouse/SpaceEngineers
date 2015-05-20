using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Terminal.Controls;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;
using VRage;
using VRage;
using VRage.Import;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_PistonBase))]
    class MyPistonBase : MyFunctionalBlock, IMyPowerConsumer, IMyConveyorEndpointBlock, Sandbox.ModAPI.IMyPistonBase
    {
        private HkConstraint m_subpartsConstraint;
        private HkConstraint m_constraint;
        private MyCubeGrid m_topGrid;
        private MyPistonTop m_topBlock;
        private long m_topBlockId;

        private MyEntitySubpart m_subpart1;
        private MyEntitySubpart m_subpart2;
        public MyEntitySubpart Subpart3;
        private Vector3 m_subpart1LocPos;
        private Vector3 m_subpart2LocPos;
        private Vector3 m_subpart3LocPos;
        private Vector3 m_constraintBasePos;
        private float m_currentPos;
        private HkFixedConstraintData m_fixedData;
        private HkFixedConstraintData m_subpartsFixedData;
        private bool m_resetInterpolationFlag = true;
        private bool m_subPartContraintInScene = false;

        private HkFixedConstraintData ConstraintData { get { return m_fixedData; } }

        private float Range { get { return BlockDefinition.Maximum - BlockDefinition.Minimum; } }

        new public MyPistonBaseDefinition BlockDefinition { get { return (MyPistonBaseDefinition)base.BlockDefinition; } }
        new public MySyncPistonBase SyncObject { get { return (MySyncPistonBase)base.SyncObject; } }

        public float CurrentPosition { get { return m_currentPos; } }

        public PistonStatus Status
        {
            get
            {
                var minLimit = MinLimit;
                var maxLimit = MaxLimit;
                
                if (m_velocity < 0)
                {
                    if (m_currentPos <= minLimit)
                        return PistonStatus.Retracted;
                    return PistonStatus.Retracting;
                }
                
                if (m_velocity > 0)
                {
                    if (m_currentPos >= maxLimit)
                        return PistonStatus.Extended;
                    return PistonStatus.Extending;
                }

                return PistonStatus.Stopped;
            }
        }
        
        public HkConstraint SafeConstraint
        {
            get
            {
                if (m_constraint != null && (!m_constraint.InWorld || (m_constraint.ConstraintData as HkBreakableConstraintData).getIsBroken(m_constraint)))
                {
                    Detach();
                }
                return m_constraint;
            }
        }

        private float m_velocity;
        public float Velocity
        {
            get { return m_velocity; }
            set
            {
                if (m_velocity != value)
                {
                    m_velocity = value;
                    RaisePropertiesChanged();
                }
            }
        }

        public float MinLimit { get; set; }
        public float MaxLimit { get; set; }

        protected virtual MyPistonBaseDefinition PistonDefinition
        {
            get { return (MyPistonBaseDefinition)BlockDefinition; }
        }

        public float BreakOffTreshold { get { return CubeGrid.GridSizeEnum == MyCubeSize.Large ? 20000000 : 1000000; } }

        static MyPistonBase()
        {
            var reverse = new MyTerminalControlButton<MyPistonBase>("Reverse", MySpaceTexts.BlockActionTitle_Reverse, MySpaceTexts.Blank, (x) => x.SyncObject.SetVelocity(-x.Velocity));
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
            velocity.Setter = (x, v) => x.SyncObject.SetVelocity(v);
            velocity.Writer = (x, res) => res.AppendDecimal(x.Velocity, 1).Append("m/s");
            velocity.EnableActionsWithReset();
            MyTerminalControlFactory.AddControl(velocity);

            var maxDist = new MyTerminalControlSlider<MyPistonBase>("UpperLimit", MySpaceTexts.BlockPropertyTitle_MaximalDistance, MySpaceTexts.Blank);
            maxDist.SetLimits((block) => block.BlockDefinition.Minimum, (block) => block.BlockDefinition.Maximum);
            maxDist.DefaultValueGetter = (block) => block.BlockDefinition.Maximum;
            maxDist.Getter = (x) => x.MaxLimit;
            maxDist.Setter = (x, v) => x.SyncObject.SetMax(v);
            maxDist.Writer = (x, res) => res.AppendDecimal(x.MaxLimit, 1).Append("m");
            maxDist.EnableActions();
            MyTerminalControlFactory.AddControl(maxDist);

            var minDist = new MyTerminalControlSlider<MyPistonBase>("LowerLimit", MySpaceTexts.BlockPropertyTitle_MinimalDistance, MySpaceTexts.Blank);
            minDist.SetLimits((block) => block.BlockDefinition.Minimum, (block) => block.BlockDefinition.Maximum);
            minDist.DefaultValueGetter = (block) => block.BlockDefinition.Minimum;
            minDist.Getter = (x) => x.MinLimit;
            minDist.Setter = (x, v) => x.SyncObject.SetMin(v);
            minDist.Writer = (x, res) => res.AppendDecimal(x.MinLimit, 1).Append("m");
            minDist.EnableActions();
            MyTerminalControlFactory.AddControl(minDist);
        }

        private static void OnExtendApplied(MyPistonBase piston)
        {
            if (piston.Velocity > 0)
                return;
            piston.SyncObject.SetVelocity(-piston.Velocity);
        }

        private static void OnRetractApplied(MyPistonBase piston)
        {
            if (piston.Velocity < 0)
                return;
            piston.SyncObject.SetVelocity(-piston.Velocity);
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;
            base.Init(objectBuilder, cubeGrid);

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            PowerReceiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Utility,
                false,
                PistonDefinition.RequiredPowerInput,
                () => (Enabled && IsFunctional) ? PowerReceiver.MaxRequiredInput : 0.0f);
            PowerReceiver.IsPoweredChanged += Receiver_IsPoweredChanged;
            PowerReceiver.Update();

            var ob = objectBuilder as MyObjectBuilder_PistonBase;
            Velocity = ob.Velocity * BlockDefinition.MaxVelocity;
            MaxLimit = ob.MaxLimit.HasValue ? Math.Min(DenormalizeDistance(ob.MaxLimit.Value), BlockDefinition.Maximum) : BlockDefinition.Maximum;
            MinLimit = ob.MinLimit.HasValue ? Math.Max(DenormalizeDistance(ob.MinLimit.Value), BlockDefinition.Minimum) : BlockDefinition.Minimum;
            m_topBlockId = ob.TopBlockId;
            m_currentPos = ob.CurrentPosition;

            CubeGrid.OnPhysicsChanged += CubeGrid_OnPhysicsChanged;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        void CubeGrid_OnPhysicsChanged(MyEntity obj)
        {
            DisposeSubpartsPhysics();
            LoadSubparts();
            UpdatePosition(true);
            UpdatePhysicsShape();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = (MyObjectBuilder_PistonBase)base.GetObjectBuilderCubeBlock(copy);
            ob.Velocity = Velocity / BlockDefinition.MaxVelocity;
            ob.MaxLimit = NormalizeDistance(MaxLimit);
            ob.MinLimit = NormalizeDistance(MinLimit);
            ob.TopBlockId = m_topBlockId;
            ob.CurrentPosition = m_currentPos;
            return ob;
        }

        protected override void OnEnabledChanged()
        {
            base.OnEnabledChanged();
            PowerReceiver.Update();
        }

        protected override bool CheckIsWorking()
        {
            return PowerReceiver.IsPowered && base.CheckIsWorking();
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
            var mass = HkInertiaTensorComputer.ComputeCylinderVolumeMassProperties(Vector3.Zero, Vector3.One, 1, 40.0f*CubeGrid.GridSize);
            subpart.Physics.CreateFromCollisionObject(shape, Vector3.Zero, subpart.WorldMatrix, mass);
            shape.Base.RemoveReference();
            subpart.Physics.RigidBody.Layer = CubeGrid.Physics.RigidBody.Layer;
            if (subpart.Physics.RigidBody2 != null)
                subpart.Physics.RigidBody2.Layer = MyPhysics.KinematicDoubledCollisionLayer;

            m_subpartsFixedData = new HkFixedConstraintData();
            m_subpartsFixedData.SetSolvingMethod(HkSolvingMethod.MethodStabilized);
            float res;
            var x = m_subpartsFixedData.GetInertiaStabilizationFactor(out res);
            m_subpartsFixedData.SetInertiaStabilizationFactor(1);
            var matAD = MatrixD.CreateWorld(Position * CubeGrid.GridSize + Vector3D.Transform(Vector3D.Transform(m_subpartsConstraintPos, WorldMatrix), CubeGrid.PositionComp.LocalMatrix), PositionComp.LocalMatrix.Forward, PositionComp.LocalMatrix.Up);
            matAD.Translation = CubeGrid.Physics.WorldToCluster(matAD.Translation);
            var matA = (Matrix)matAD;
            var matB = subpart.PositionComp.LocalMatrix;
            m_subpartsFixedData.SetInWorldSpace(ref matA, ref matB, ref matB);
            var breakableData = new HkBreakableConstraintData(m_subpartsFixedData);
            //Dont dispose the fixed data or we wont have access to them
            breakableData.Threshold = BreakOffTreshold;
            m_subpartsConstraint = new HkConstraint(CubeGrid.Physics.RigidBody, subpart.Physics.RigidBody, breakableData);
            m_subpartsConstraint.WantRuntime = true;

            m_posChanged = true;
        }

        private void DisposeSubpartsPhysics()
        {
            if (m_subpartsConstraint != null)
            {
                m_subpartsFixedData.Dispose();
                m_subpartsFixedData = null;
                if (m_subPartContraintInScene)
                {
                    m_subPartContraintInScene = false;
                    CubeGrid.Physics.RemoveConstraint(m_subpartsConstraint);
                }
                m_subpartsConstraint.Dispose();
                m_subpartsConstraint = null;
            }
            if (m_subpart1 != null && m_subpart1.Physics != null)
            {
                m_subpart1.Physics.Enabled = false;
                m_subpart1.Physics.Close();
                m_subpart1.Physics = null;
            }
        }

        private void ComponentStack_IsFunctionalChanged()
        {
            PowerReceiver.Update();
        }

        private void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
        }

        public MyPowerReceiver PowerReceiver
        {
            get;
            private set;
        }

        public override void OnBuildSuccess(long builtBy)
        {
            LoadSubparts();
            Debug.Assert(m_constraint == null);
            if (Sync.IsServer)
            {
                CreateTopGrid(out m_topGrid, out m_topBlock);
                Attach(m_topBlock, updateSync: true);
            }
        }

        protected override MySyncEntity OnCreateSync()
        {
            var sync = new MySyncPistonBase(this);
            sync.SyncPosition = OnSyncPosition;
            return sync;
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();
            UpdatePhysicsShape();
        }

        private void UpdatePhysicsShape()
        {
            var subpart = m_subpart1;
            if (!m_posChanged || subpart == null || subpart.Physics == null || subpart.Physics.RigidBody == null)
                return;
            m_posChanged = false;
            if (m_currentPos > 0.4f)
            {
                Vector3 vec = new Vector3(0, subpart.Model.BoundingBoxSize.Y + 0.3f, 0);
                if (m_currentPos > 2 * Range / 3)
                    vec.Y -= m_currentPos - 2 * Range / 3;
                var speedCorrection = Velocity < 0 ? Velocity / 6 : 0;
                Vector3 vecB = vec + new Vector3(0, m_currentPos - 0.4f + speedCorrection, 0);

                var existingShape = subpart.Physics.RigidBody.GetShape();
                if (existingShape.ShapeType == HkShapeType.Cylinder)
                {
                    float dist = Math.Abs(vec.Y - vecB.Y);
                    var cyl = (HkCylinderShape)existingShape;
                    float distExist = Math.Abs(cyl.VertexA.Y - cyl.VertexB.Y);
                    if(Math.Abs(dist - distExist) < 0.1f)
                        return;
                }

                var threshold = 0.11f; // Must be bigger than 2x convex radius
                HkCylinderShape shape = new HkCylinderShape(vec, vecB, CubeGrid.GridSize / 2 - threshold, 0.001f);
                subpart.Physics.RigidBody.SetShape(shape);
                if (subpart.Physics.RigidBody2 != null)
                    subpart.Physics.RigidBody2.SetShape(shape);
                shape.Base.RemoveReference();
                subpart.Physics.Enabled = true;
                UpdateSubpartFixedData();
                if (CubeGrid.Physics.IsInWorld)
                {
                    if (m_subpartsConstraint != null &&!m_subpartsConstraint.InWorld)
                    {
                        m_subPartContraintInScene = true;
                        CubeGrid.Physics.AddConstraint(m_subpartsConstraint);
                    }
                }
                if (m_subpartsConstraint != null&&!m_subpartsConstraint.Enabled)
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

            ProfilerShort.Begin("TopPartCheck");
            if (m_topBlock == null && m_topBlockId != 0)
            {
                MyEntity topBlock;
                if (!MyEntities.TryGetEntityById(m_topBlockId, out topBlock))
                {
                    if (Sync.IsServer)
                        m_topBlockId = 0;
                }
                else
                {
                    Debug.Assert(topBlock is MyPistonTop);
                    if ((topBlock is MyPistonTop))
                        Attach(topBlock as MyPistonTop, false);
                }
            }
            ProfilerShort.End();

            ProfilerShort.Begin("UpdatePos");
            UpdatePosition();
            ProfilerShort.End();
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
                        m_currentPos += compensatedDelta;
                        m_currentPos = Math.Max(m_currentPos, MinLimit);
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
                    m_currentPos += compensatedDelta;
                    m_currentPos = Math.Min(m_currentPos, MaxLimit);
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
                ConstraintData.SetInBodySpace(ref matA, ref matB);
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
                m_subpartsFixedData.SetInBodySpace(ref matA, ref matB);
            else
                MySandboxGame.Log.WriteLine("m_subpartsFixedData is null");
            //if (CubeGrid.SyncObject.ResponsibleForUpdate(Sync.Clients.LocalClient))
            //SyncObject.SetCurrentPosition(m_currentPos);
        }

        private void UpdateText()
        {
            DetailedInfo.Clear();
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

        private void CreateTopGrid(out MyCubeGrid topGrid, out MyPistonTop topBlock)
        {
            CreateTopGrid(out topGrid, out topBlock, MyDefinitionManager.Static.TryGetDefinitionGroup(BlockDefinition.TopPart));
        }

        public void Attach(MyPistonTop topBlock, bool updateSync = false)
        {
            if (m_topBlock != null)
                Detach();
            if (CubeGrid.Physics == null || SafeConstraint != null)
                return;
            UpdateAnimation();
            Debug.Assert(SafeConstraint == null);

            if (CubeGrid.Physics.Enabled && topBlock != null)
            {
                m_topBlock = topBlock;
                m_topBlockId = topBlock.EntityId;

                if (updateSync)
                    SyncObject.AttachTop(m_topBlock);

                m_topGrid = m_topBlock.CubeGrid;
                var rotorBody = m_topGrid.Physics.RigidBody;

                var matAD = MatrixD.CreateWorld(Vector3D.Transform(Vector3D.Transform(m_constraintBasePos, Subpart3.WorldMatrix), CubeGrid.PositionComp.WorldMatrixNormalizedInv), PositionComp.LocalMatrix.Forward, PositionComp.LocalMatrix.Up);
                var matBD = MatrixD.CreateWorld(m_topBlock.Position * m_topBlock.CubeGrid.GridSize, m_topBlock.PositionComp.LocalMatrix.Forward, m_topBlock.PositionComp.LocalMatrix.Up);
                var matA = (Matrix)matAD;
                var matB = (Matrix)matBD;
                m_fixedData = new HkFixedConstraintData();
                m_fixedData.SetInertiaStabilizationFactor(10);
                m_fixedData.SetSolvingMethod(HkSolvingMethod.MethodStabilized);
                m_fixedData.SetInBodySpace(ref matA, ref matB);
                var breakData = new HkBreakableConstraintData(m_fixedData);
                //Dont dispose the fixed data or we wont have access to them

                breakData.Threshold = BreakOffTreshold;
                breakData.ReapplyVelocityOnBreak = true;
                breakData.RemoveFromWorldOnBrake = false;
                m_constraint = new HkConstraint(CubeGrid.Physics.RigidBody, rotorBody, breakData);
                m_constraint.WantRuntime = true;

                CubeGrid.Physics.AddConstraint(m_constraint);
                m_constraint.Enabled = true;

                m_topBlock.Attach(this);

                OnConstraintAdded(GridLinkTypeEnum.Physical, m_topGrid);
                OnConstraintAdded(GridLinkTypeEnum.Logical, m_topGrid);

                m_conveyorEndpoint.Attach(topBlock.ConveyorEndpoint as MyAttachableConveyorEndpoint);

            }
        }

        private void CreateTopGrid(out MyCubeGrid topGrid, out MyPistonTop topBlock, MyCubeBlockDefinitionGroup topGroup)
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

            var gridBuilder = Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CubeGrid>();
            gridBuilder.GridSizeEnum = gridSize;
            gridBuilder.IsStatic = false;
            gridBuilder.PositionAndOrientation = new MyPositionAndOrientation(matrix);
            gridBuilder.CubeBlocks.Add(block);

            var grid = MyEntityFactory.CreateEntity<MyCubeGrid>(gridBuilder);
            grid.Init(gridBuilder);

            topGrid = grid;
            topBlock = (MyPistonTop)topGrid.GetCubeBlock(Vector3I.Zero).FatBlock;
            //topGrid.SetPosition(topGrid.WorldMatrix.Translation - (topBlock.WorldMatrix.Translation/*Vector3.Transform(topBlock.DummyPosLoc, topGrid.WorldMatrix) - topGrid.WorldMatrix.Translation*/));

            MyEntities.Add(grid);

            MySyncCreate.SendEntityCreated(grid.GetObjectBuilder());
            m_topBlockId = topBlock.EntityId;
        }

        internal void Detach(bool updateGroups = true)
        {
            if (m_constraint == null)
                return;

            Debug.Assert(m_constraint != null);
            Debug.Assert(m_topGrid != null);
            Debug.Assert(m_topBlock != null);

            var tmpTopGrid = m_topGrid;
            if (m_constraint.InWorld && (m_constraint.ConstraintData as HkBreakableConstraintData).getIsBroken(m_constraint))
                m_topBlockId = 0;
            m_fixedData.Dispose();
            m_fixedData = null;
            CubeGrid.Physics.RemoveConstraint(m_constraint);
            m_constraint.Dispose();
            m_constraint = null;
            m_topGrid = null;
            if (m_topBlock != null)
                m_topBlock.Detach();

            m_conveyorEndpoint.Detach(m_topBlock.ConveyorEndpoint as MyAttachableConveyorEndpoint);

            m_topBlock = null;

            if (updateGroups)
            {
                OnConstraintRemoved(GridLinkTypeEnum.Physical, tmpTopGrid);
                OnConstraintRemoved(GridLinkTypeEnum.Logical, tmpTopGrid);
            }


            // Try to reattach, if the block will still live next frame. This fixes missing attachments when splitting grids
        }

        private void OnSyncPosition(float currentPosition)
        {
            m_currentPos = currentPosition;
            UpdatePosition(true);
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (m_topBlockId != 0 && m_topBlock == null)
            {
                MyPistonTop top;
                if (MyEntities.TryGetEntityById<MyPistonTop>(m_topBlockId, out top))
                {
                    m_topBlock = top;
                    Attach(m_topBlock, true);
                }
                else
                    if (Sync.IsServer)
                        m_topBlockId = 0;
            }

            LoadSubparts();

            UpdateAnimation();
            UpdatePosition(true);
            UpdatePhysicsShape();
            base.UpdateOnceBeforeFrame();
        }

        public override void OnRemovedFromScene(object source)
        {
            base.OnRemovedFromScene(source);
            CubeGrid.OnPhysicsChanged -= CubeGrid_OnPhysicsChanged;
            Detach();
            DisposeSubpartsPhysics();
        }

        public override void OnRemovedByCubeBuilder()
        {
            if (m_topGrid != null && m_topBlock != null)
                m_topGrid.RemoveBlock(m_topBlock.SlimBlock, updatePhysics: true);
            base.OnRemovedByCubeBuilder();
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
        private MyAttachableConveyorEndpoint m_conveyorEndpoint;
        private bool m_posChanged;
        private Vector3 m_subpartsConstraintPos;
        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get { return m_conveyorEndpoint; }
        }

        public void InitializeConveyorEndpoint()
        {
            m_conveyorEndpoint = new MyAttachableConveyorEndpoint(this);
            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_conveyorEndpoint));
        }

        event Action<bool> LimitReached;
        event Action<bool> Sandbox.ModAPI.IMyPistonBase.LimitReached
        {
            add { LimitReached += value; }
            remove { LimitReached -= value; }
        }
    }

}
