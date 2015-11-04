using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.GUI;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sandbox.Game.EntityComponents;
using VRage;
using VRage.Audio;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    public abstract class MyMotorBase : MyFunctionalBlock
    {
        private static List<HkBodyCollision> m_penetrations = new List<HkBodyCollision>();
        private const string ROTOR_DUMMY_KEY = "electric_motor";

        public HkConstraint DebugConstraint { get { return m_constraint; } }
        protected HkConstraint m_constraint;
        protected MyCubeGrid m_rotorGrid;
        protected MyMotorRotor m_rotorBlock;
        protected long m_rotorBlockId;
        bool m_retryAttach = false;

        protected readonly Sync<float> m_dummyDisplacement;

        // Use the property instead of the field, because the block's transformation has to be applied
        protected Vector3 m_dummyPos;
        public Vector3 DummyPosition
        {
            get
            {
                var displacementVector = Vector3.Zero;
                if (m_dummyPos.Length() > 0)
                {
                    displacementVector = Vector3.DominantAxisProjection(m_dummyPos);
                    displacementVector.Normalize();
                    displacementVector *= m_dummyDisplacement;
                }
                else
                {
                    displacementVector = new Vector3(0, m_dummyDisplacement, 0);
                }
                return Vector3.Transform(m_dummyPos + displacementVector, this.PositionComp.LocalMatrix);
            }
        }

        public float DummyDisplacement
        {
            // 0.2f is here because of backwards compatibility: default position in old saves is m_dummyDisplacement = 0.0f, which corresponds to DummyDisplacement = 0.2f
            get { return m_dummyDisplacement + GetModelDummyDisplacement(); }
            set { m_dummyDisplacement.Value = value - GetModelDummyDisplacement(); }
        }

        public MyMotorRotor Rotor { get { return m_rotorBlock; } }

        public Vector3 RotorAngularVelocity
        {
            get { return CubeGrid.Physics.RigidBody.AngularVelocity - m_rotorGrid.Physics.RigidBody.AngularVelocity; }
        }

        public float MaxRotorAngularVelocity
        {
            get { return (CubeGrid.GridSizeEnum == MyCubeSize.Large) ? MyGridPhysics.GetLargeShipMaxAngularVelocity() : MyGridPhysics.GetSmallShipMaxAngularVelocity(); }
        }

        protected MyMotorStatorDefinition MotorDefinition
        {
            get { return (MyMotorStatorDefinition)BlockDefinition; }
        }

        protected HkConstraint SafeConstraint
        {
            get
            {
                if (m_constraint != null && !m_constraint.InWorld)
                {
                    Detach();
                }
                return m_constraint;
            }
        }

        public float RequiredPowerInput
        {
            get { return MotorDefinition.RequiredPowerInput; }
        }

        public new MySyncMotorBase SyncObject { get { return (MySyncMotorBase)base.SyncObject; } }

        protected override MySyncEntity OnCreateSync()
        {
            return new MySyncMotorBase(this);
        }

        protected override bool CheckIsWorking()
        {
			return ResourceSink.IsPowered && base.CheckIsWorking();
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;
            base.Init(objectBuilder, cubeGrid);

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

			var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                MotorDefinition.ResourceSinkGroup,
                MotorDefinition.RequiredPowerInput,
				() => (Enabled && IsFunctional) ? sinkComp.MaxRequiredInput : 0.0f);
			sinkComp.IsPoweredChanged += Receiver_IsPoweredChanged;
	        ResourceSink = sinkComp;
			ResourceSink.Update();

            m_dummyDisplacement.Value = 0.0f;
            LoadDummyPosition();

            var ob = objectBuilder as MyObjectBuilder_MotorBase;
            m_rotorBlockId = ob.RotorEntityId;
            AddDebugRenderComponent(new Components.MyDebugRenderComponentMotorBase(this));
            cubeGrid.OnPhysicsChanged += cubeGrid_OnPhysicsChanged;
        }

        protected void cubeGrid_OnPhysicsChanged(MyEntity obj)
        {
            Reattach();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = base.GetObjectBuilderCubeBlock(copy) as MyObjectBuilder_MotorBase;
            ob.RotorEntityId = m_rotorBlockId;
            return ob;
        }

        private void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
        }

        private void ComponentStack_IsFunctionalChanged()
        {
			ResourceSink.Update();
        }

        protected override void OnEnabledChanged()
        {
			ResourceSink.Update();
            base.OnEnabledChanged();
        }

        private void LoadDummyPosition()
        {
            var model = MyModels.GetModelOnlyDummies(BlockDefinition.Model);

            foreach (var dummy in model.Dummies)
            {
                if (dummy.Key.StartsWith(ROTOR_DUMMY_KEY, StringComparison.InvariantCultureIgnoreCase))
                {
                    var matrix = Matrix.Normalize(dummy.Value.Matrix);
                    m_dummyPos = matrix.Translation;
                    break;
                }
            }
        }

        public override void OnBuildSuccess(long builtBy)
        {
            Debug.Assert(m_constraint == null);

            if (Sync.IsServer)
            {
                CreateRotorGrid(out m_rotorGrid, out m_rotorBlock, builtBy);
                //var world = WorldMatrix;
                //var pos = Vector3.Transform(m_dummyPos, CubeGrid.WorldMatrix);

                if (MyFakes.REPORT_INVALID_ROTORS)
                {
                    // Simulate lag in rotor creation message sending.
                    System.Threading.Thread.Sleep(100);
                }
                Attach(m_rotorBlock, updateSync: true);
            }

            NeedsUpdate &= ~MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            base.OnBuildSuccess(builtBy);
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void OnRemovedFromScene(object source)
        {
            base.OnRemovedFromScene(source);
            Detach();
        }

        public override void OnRemovedByCubeBuilder()
        {
            if (m_rotorGrid != null && m_rotorBlock != null)
                m_rotorGrid.RemoveBlock(m_rotorBlock.SlimBlock, updatePhysics: true);
            base.OnRemovedByCubeBuilder();
        }

        protected override void Closing()
        {
            Detach();
            base.Closing();
        }

        protected virtual void CreateRotorGrid(out MyCubeGrid rotorGrid, out MyMotorRotor rotorBlock, long builtBy)
        {
            CreateRotorGrid(out rotorGrid, out rotorBlock, builtBy, MyDefinitionManager.Static.TryGetDefinitionGroup(MotorDefinition.RotorPart));
        }

        protected void CreateRotorGrid(out MyCubeGrid rotorGrid, out MyMotorRotor rotorBlock, long builtBy, MyCubeBlockDefinitionGroup rotorGroup)
        {
            if (rotorGroup == null)
            {
                CreateRotorGridFailed(builtBy, out rotorGrid, out rotorBlock);
                return;
            }

            var gridSize = CubeGrid.GridSizeEnum;

            float size = MyDefinitionManager.Static.GetCubeSize(gridSize);
            var matrix = MatrixD.CreateWorld(Vector3D.Transform(DummyPosition, CubeGrid.WorldMatrix), WorldMatrix.Forward, WorldMatrix.Up);
            var definition = rotorGroup[gridSize];
            Debug.Assert(definition != null);

            var block = MyCubeGrid.CreateBlockObjectBuilder(definition, Vector3I.Zero, MyBlockOrientation.Identity, MyEntityIdentifier.AllocateId(), OwnerId, fullyBuilt: MySession.Static.CreativeMode);

            var gridBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CubeGrid>();
            gridBuilder.GridSizeEnum = gridSize;
            gridBuilder.IsStatic = false;
            gridBuilder.PositionAndOrientation = new MyPositionAndOrientation(matrix);
            gridBuilder.CubeBlocks.Add(block);

            var grid = MyEntityFactory.CreateEntity<MyCubeGrid>(gridBuilder);
            grid.Init(gridBuilder);

            rotorGrid = grid;
            rotorBlock = (MyMotorRotor)rotorGrid.GetCubeBlock(Vector3I.Zero).FatBlock;
            rotorGrid.PositionComp.SetPosition(rotorGrid.WorldMatrix.Translation - (Vector3D.Transform(rotorBlock.DummyPosLoc, rotorGrid.WorldMatrix) - rotorGrid.WorldMatrix.Translation));

            if (!CanPlaceRotor(rotorBlock, builtBy))
            {
                CreateRotorGridFailed(builtBy, out rotorGrid, out rotorBlock);
                grid.Close();
                return;
            }

            if (Sync.IsServer)
            {
                MyEntities.Add(grid);
            }
            else
                grid.Close();
        }

        private static void CreateRotorGridFailed(long builtBy, out MyCubeGrid rotorGrid, out MyMotorRotor rotorBlock)
        {
            if (builtBy == MySession.LocalPlayerId)
                MyGuiAudio.PlaySound(MyGuiSounds.HudUnable);
            rotorGrid = null;
            rotorBlock = null;
        }

        protected virtual bool CanPlaceRotor(MyMotorRotor rotorBlock, long builtBy)
        {
            return true;
        }

        public virtual bool Detach(bool updateGroup = true, bool reattach = true)
        {
            if (m_constraint == null)
                return false;

            Debug.Assert(m_constraint != null);
            Debug.Assert(m_rotorGrid != null);
            Debug.Assert(m_rotorBlock != null);

            var tmpRotorGrid = m_rotorGrid;

            CubeGrid.Physics.RemoveConstraint(m_constraint);
            m_constraint.Dispose();
            m_constraint = null;
            m_rotorGrid = null;
            if (m_rotorBlock != null)
                m_rotorBlock.Detach();
            m_rotorBlock = null;
            // The following line is commented out on purpose! If you move the motor between grids (e.g. after splitting),
            // you have to remember the attached rotor somehow. This rotorBlockId is how it's remembered.
            //m_rotorBlockId = 0;

            if (updateGroup)
            {
                OnConstraintRemoved(GridLinkTypeEnum.Physical, tmpRotorGrid);
                OnConstraintRemoved(GridLinkTypeEnum.Logical, tmpRotorGrid);
            }

            if (reattach)
            {
                // Try to reattach, if the block will still live next frame. This fixes missing attachments when splitting grids
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }
            if (tmpRotorGrid != null)
                tmpRotorGrid.OnPhysicsChanged -= cubeGrid_OnPhysicsChanged;
            return true;
        }

        protected virtual float GetModelDummyDisplacement() { return 0.0f; }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            // CH:TODO: This will need to be done only on the server, but then, joining clients would get disconnected rotors.
            // That will have to be synchronized somewhere extra. Until then, Attach is done on both client and server and
            // this causes an assert when the stator is buil
            //if (Sync.IsServer)
            //{
            //    Attach(FindMatchingRotor(), true);
            //}
            MyMotorRotor rotor;
            if (m_rotorBlock == null)
            {
                if (m_rotorBlockId != 0 && MyEntities.TryGetEntityById<MyMotorRotor>(m_rotorBlockId, out rotor) && !rotor.MarkedForClose && rotor.CubeGrid.InScene)
                {
                    Attach(rotor, false);
                    m_retryAttach = false;
                }
                else
                {
                    if (m_retryAttach)
                    {
                        RetryAttach(m_rotorBlockId);
                    }
                    else
                    {
                        m_rotorBlockId = 0;
                        if (Sync.IsServer)
                            Attach(FindMatchingRotor(), true);
                        NeedsUpdate &= ~MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                    }
                }
                return;
            }

            if (SafeConstraint == null)
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        protected MyMotorRotor FindMatchingRotor()
        {
            Debug.Assert(CubeGrid != null);
            Debug.Assert(m_penetrations != null);
            Debug.Assert(CubeGrid.Physics != null);
            if (CubeGrid == null)
            {
                MySandboxGame.Log.WriteLine("MyMotorStator.FindMatchingRotor(): Cube grid == null!");
                return null;
            }

            if (m_penetrations == null)
            {
                MySandboxGame.Log.WriteLine("MyMotorStator.FindMatchingRotor(): penetrations cache == null!");
                return null;
            }

            if (CubeGrid.Physics == null)
            {
                MySandboxGame.Log.WriteLine("MyMotorStator.FindMatchingRotor(): Cube grid physics == null!");
                return null;
            }

            Quaternion orientation;
            Vector3D pos;
            Vector3 halfExtents;
            ComputeRotorQueryBox(out pos, out halfExtents, out orientation);
            try
            {
                MyPhysics.GetPenetrationsBox(ref halfExtents, ref pos, ref orientation, m_penetrations, MyPhysics.DefaultCollisionLayer);
                foreach (var obj in m_penetrations)
                {
                    var entity = obj.GetCollisionEntity();
                    if (entity == null || entity == CubeGrid)
                        continue;

                    var grid = entity as MyCubeGrid;
                    if (grid == null)
                        continue;

                    // Rotor should always be on position [0,0,0];
                    var pos2 = Vector3.Transform(DummyPosition, CubeGrid.WorldMatrix);
                    var blockPos = grid.RayCastBlocks(pos2, pos2 + WorldMatrix.Up);
                    if (blockPos.HasValue)
                    {
                        var slimBlock = grid.GetCubeBlock(blockPos.Value);
                        if (slimBlock == null || slimBlock.FatBlock == null)
                            continue;

                        var rotor = slimBlock.FatBlock as MyMotorRotor;
                        if (rotor != null)
                            return rotor;
                    }
                }
            }
            finally
            {
                m_penetrations.Clear();
            }
            return null;
        }

        public abstract bool Attach(MyMotorRotor rotor, bool updateSync = false, bool updateGroup = true);

        public void Reattach()
        {
            if (m_rotorBlock == null)
                return;
            var rotor = m_rotorBlock;
            bool detached = Detach(updateGroup: false);
            bool attached = Attach(rotor, updateSync: false, updateGroup: false);
            Debug.Assert(detached && attached);
            rotor.CubeGrid.Physics.ForceActivate();
        }

        public virtual void ComputeRotorQueryBox(out Vector3D pos, out Vector3 halfExtents, out Quaternion orientation)
        {
            var world = this.WorldMatrix;
            orientation = Quaternion.CreateFromRotationMatrix(world);
            halfExtents = Vector3.One * CubeGrid.GridSize * 0.35f;
            halfExtents.Y = CubeGrid.GridSize * 0.25f;
            pos = world.Translation + 0.35f * CubeGrid.GridSize * WorldMatrix.Up;
        }

        public override void UpdateBeforeSimulation100()
        {
            UpdateSoundState();
            base.UpdateBeforeSimulation100();
        }

        protected virtual void UpdateSoundState()
        {
            if (!MySandboxGame.IsGameReady)
                return;

            if (m_rotorGrid == null || m_rotorGrid.Physics == null)
            {
                m_soundEmitter.StopSound(true);
                return;
            }

            if (IsWorking && Math.Abs(m_rotorGrid.Physics.RigidBody.DeltaAngle.W) > 0.00025f)
                m_soundEmitter.PlaySingleSound(BlockDefinition.PrimarySound, true);
            else
                m_soundEmitter.StopSound(false);

            if ((m_soundEmitter.Sound != null) && (m_soundEmitter.Sound.IsPlaying))
            {
                float semitones = 4f * (Math.Abs(RotorAngularVelocity.Length()) - 0.5f * MaxRotorAngularVelocity) / MaxRotorAngularVelocity;
                m_soundEmitter.Sound.FrequencyRatio = MyAudio.Static.SemitonesToFrequencyRatio(semitones);
            }

        }
        public MyCubeGrid RotorGrid { get { return m_rotorGrid; } }

        public void RetryAttach(long entityId)
        {
            m_rotorBlockId = entityId;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            m_retryAttach = true;
        }
    }
}
