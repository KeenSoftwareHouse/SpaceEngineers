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
using System.Text;
using Sandbox.Game.Localization;
using Sandbox.Engine.Multiplayer;
using VRage.Game;
using VRage.Game.Entity;

namespace Sandbox.Game.Entities.Cube
{
    public abstract class MyMotorBase : MyFunctionalBlock
    {
        protected struct State
        {
            public long? OtherEntityId;
            public MyDeltaTransform? MasterToSlave; 
        }

        private const string ROTOR_DUMMY_KEY = "electric_motor";

        private static List<HkBodyCollision> m_penetrations = new List<HkBodyCollision>();

        private Vector3 m_dummyPos;
        protected HkConstraint m_constraint;
        protected MyCubeGrid m_rotorGrid;
        protected MyMotorRotor m_rotorBlock;


        protected readonly Sync<float> m_dummyDisplacement;
        protected readonly Sync<State> m_rotorBlockId; // Null = detached, 0 = looking for anything to attach, other = attached or waiting for rotor part to be in scene
        protected readonly Sync<long?> m_weldedEntityId;

        protected bool m_welded = false;
        protected long? m_weldedRotorBlockId;
        bool m_isWelding = false;
        protected Sync<bool> m_forceWeld;
        protected Sync<float> m_weldSpeedSq; //squared

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
            get { return m_dummyDisplacement + ModelDummyDisplacement; }
            set { m_dummyDisplacement.Value = value - ModelDummyDisplacement; }
        }

        public MyCubeGrid RotorGrid { get { return m_rotorGrid; } }

        public MyMotorRotor Rotor { get { return m_rotorBlock; } }

        public float RequiredPowerInput { get { return MotorDefinition.RequiredPowerInput; } }

        protected MyMotorStatorDefinition MotorDefinition { get { return (MyMotorStatorDefinition)BlockDefinition; } }

        protected virtual float ModelDummyDisplacement { get { return 0.0f; } }

        public Vector3 RotorAngularVelocity
        {
            // TODO: Imho it would be better to read velocity from constraint
            get { return CubeGrid.Physics.RigidBody.AngularVelocity - m_rotorGrid.Physics.RigidBody.AngularVelocity; }
        }

        public float MaxRotorAngularVelocity
        {
            get { return MyGridPhysics.GetShipMaxAngularVelocity(CubeGrid.GridSizeEnum); }
        }

        protected HkConstraint SafeConstraint
        {
            get { return RefreshConstraint(); }
        }

        public MyMotorBase()
        {
            m_weldedEntityId.ValidateNever();
            m_weldedEntityId.ValueChanged += (o) => OnWeldedEntityIdChanged();
            m_rotorBlockId.ValueChanged += (o) => OnAttachTargetChanged();
            m_rotorBlockId.Validate = ValidateRotorBlockId;
        }

        bool ValidateRotorBlockId(State newState)
        {
            if (newState.OtherEntityId == null) // Detach allowed always
                return true;
            else if (newState.OtherEntityId == 0) // Try attach only valid when detached
                return m_rotorBlockId.Value.OtherEntityId == null;
            else // Attach directly now allowed by client
                return false;
        }

        public abstract bool Attach(MyMotorRotor rotor, bool updateGroup = true);
        protected abstract void UpdateText();

        public MyStringId GetAttachState()
        {
            if (m_rotorBlockId.Value.OtherEntityId == null)
                return MySpaceTexts.BlockPropertiesText_MotorDetached;
            else if (m_rotorBlockId.Value.OtherEntityId.Value == 0)
                return MySpaceTexts.BlockPropertiesText_MotorAttachingAny;
            else if (SafeConstraint != null)
                return MySpaceTexts.BlockPropertiesText_MotorAttached;
            else
                return MySpaceTexts.BlockPropertiesText_MotorAttachingSpecific;
        }

        protected override bool CheckIsWorking()
        {
            return ResourceSink.IsPowered && base.CheckIsWorking();
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;

            var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                MotorDefinition.ResourceSinkGroup,
                MotorDefinition.RequiredPowerInput,
                () => (Enabled && IsFunctional) ? sinkComp.MaxRequiredInput : 0.0f);
            sinkComp.IsPoweredChanged += Receiver_IsPoweredChanged;
            ResourceSink = sinkComp;

            base.Init(objectBuilder, cubeGrid);

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
         
            ResourceSink.Update();

            m_dummyDisplacement.Value = 0.0f;
            LoadDummyPosition();

            var ob = objectBuilder as MyObjectBuilder_MotorBase;

            if (ob.RotorEntityId.HasValue && ob.RotorEntityId.Value != 0)
            {
                MyDeltaTransform? deltaTransform = ob.MasterToSlaveTransform.HasValue ? ob.MasterToSlaveTransform.Value : (MyDeltaTransform?)null;
                m_rotorBlockId.Value = new State() { OtherEntityId = ob.RotorEntityId, MasterToSlave = deltaTransform };
            }
          
            m_weldedEntityId.Value = ob.WeldedEntityId;

            AddDebugRenderComponent(new Components.MyDebugRenderComponentMotorBase(this));
            cubeGrid.OnPhysicsChanged += cubeGrid_OnPhysicsChanged;

            float defaultWeldSpeed = ob.weldSpeed; //weld before reaching the max speed
            defaultWeldSpeed *= defaultWeldSpeed;
            m_weldSpeedSq.Value = defaultWeldSpeed;
            m_forceWeld.Value = ob.forceWeld;
        }

        protected void cubeGrid_OnPhysicsChanged(MyEntity obj)
        {
            if (Sync.IsServer)
            {
                Reattach();
            }
        }

        void OnWeldedEntityIdChanged()
        {
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        void OnAttachTargetChanged()
        {
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            UpdateText();
        }

        private HkConstraint RefreshConstraint()
        {
            if (m_constraint != null && !m_constraint.InWorld)
            {
                Detach();
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }
            return m_constraint;
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var state = m_rotorBlockId.Value;
            var ob = base.GetObjectBuilderCubeBlock(copy) as MyObjectBuilder_MotorBase;
            ob.RotorEntityId = state.OtherEntityId;
            ob.WeldedEntityId = m_weldedEntityId;
            ob.MasterToSlaveTransform = state.MasterToSlave.HasValue ? state.MasterToSlave.Value : (MyPositionAndOrientation?)null;
            ob.weldSpeed = (float)Math.Sqrt(m_weldSpeedSq);
            ob.forceWeld = m_forceWeld;
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
            var model = VRage.Game.Models.MyModels.GetModelOnlyDummies(BlockDefinition.Model);

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
                MyMultiplayer.ReplicateImmediatelly(this, this.CubeGrid);
                CreateRotorGrid(out m_rotorGrid, out m_rotorBlock, builtBy);
                if (m_rotorBlock != null) // No place for top part
                {
                    MatrixD masterToSlave = m_rotorBlock.CubeGrid.WorldMatrix * MatrixD.Invert(WorldMatrix);
                    m_rotorBlockId.Value = new State() { OtherEntityId = m_rotorBlock.EntityId, MasterToSlave = masterToSlave };
                    Attach(m_rotorBlock);
                }
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
            Detach(true);
        }

        protected override void Closing()
        {
            Detach(true);
            base.Closing();
        }

        protected virtual void CreateRotorGrid(out MyCubeGrid rotorGrid, out MyMotorRotor rotorBlock, long builtBy)
        {
            CreateRotorGrid(out rotorGrid, out rotorBlock, builtBy, MyDefinitionManager.Static.TryGetDefinitionGroup(MotorDefinition.RotorPart));
        }

        protected void CreateRotorGrid(out MyCubeGrid rotorGrid, out MyMotorRotor rotorBlock, long builtBy, MyCubeBlockDefinitionGroup rotorGroup)
        {
            Debug.Assert(Sync.IsServer, "Rotor grid can be created only on server");
            if (rotorGroup == null)
            {
                rotorGrid = null;
                rotorBlock = null;
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
                rotorGrid = null;
                rotorBlock = null;
                grid.Close();
                return;
            }

            MyEntities.Add(grid);

            MatrixD masterToSlave = rotorBlock.CubeGrid.WorldMatrix * MatrixD.Invert(WorldMatrix);
            m_rotorBlockId.Value = new State() { OtherEntityId = rotorBlock.EntityId, MasterToSlave = masterToSlave};
        }

        protected virtual bool CanPlaceRotor(MyMotorRotor rotorBlock, long builtBy)
        {
            return true;
        }

        public virtual bool Detach(bool updateGroup = true)
        {
            if (m_isWelding == false)
            {
                UnweldGroup();
            }

            var tmpRotorGrid = m_rotorGrid;
            if (updateGroup)
            {
                OnConstraintRemoved(GridLinkTypeEnum.Physical, tmpRotorGrid);
                OnConstraintRemoved(GridLinkTypeEnum.Logical, tmpRotorGrid);
            }

            if (m_constraint == null)
                return false;

            Debug.Assert(m_constraint != null);
            Debug.Assert(m_rotorGrid != null);
            Debug.Assert(m_rotorBlock != null);


            CubeGrid.Physics.RemoveConstraint(m_constraint);
            m_constraint.Dispose();
            m_constraint = null;
            m_rotorGrid = null;
            if (m_rotorBlock != null)
                m_rotorBlock.Detach(m_welded || m_isWelding);
            m_rotorBlock = null;

            UpdateText();
            return true;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            TryAttach();
            TryWeld();
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();
            TryAttach();
            TryWeld();
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();
            UpdateSoundState();
        }

        protected void TryWeld()
        { 
            if(m_weldedEntityId.Value == null)
            {
                if(m_welded)
                {
                    UnweldGroup();
                }
            }
            else if(m_weldedEntityId.Value != m_weldedRotorBlockId && m_rotorBlock != null)
            {
                WeldGroup();
            }

        }

        protected void TryAttach()
        {
            if (!CubeGrid.InScene)
                return;
            var updateFlags = NeedsUpdate;
            updateFlags &= ~MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            updateFlags &= ~MyEntityUpdateEnum.EACH_10TH_FRAME;

            if (m_rotorBlockId.Value.OtherEntityId == null || m_rotorBlockId.Value.OtherEntityId.HasValue == false) // Detached
            {
                if (m_constraint != null)
                {
                    Detach();
                }
            }
            else if (m_rotorBlockId.Value.OtherEntityId == 0) // Find anything to attach (only on server)
            {
                if (Sync.IsServer)
                {
                    var rotor = FindMatchingRotor();
                    if (rotor != null)
                    {
                        if (m_constraint != null)
                        {
                            Detach();
                        }

                        MatrixD masterToSlave = rotor.CubeGrid.WorldMatrix * MatrixD.Invert(WorldMatrix);
                        m_rotorBlockId.Value = new State() { OtherEntityId = rotor.EntityId, MasterToSlave = masterToSlave };
                        Attach(rotor);
                    }
                }
            }
            else if ((false == m_welded &&(m_rotorBlock == null || m_rotorBlock.EntityId != m_rotorBlockId.Value.OtherEntityId)) ||
                     (m_welded && m_weldedRotorBlockId != m_rotorBlockId.Value.OtherEntityId)) // Attached to something else or nothing
            {
                Detach();
                MyMotorRotor rotor;
                bool attached = false;
                if (MyEntities.TryGetEntityById<MyMotorRotor>(m_rotorBlockId.Value.OtherEntityId.Value, out rotor) && !rotor.MarkedForClose && rotor.CubeGrid.InScene)
                {
                    if (Sync.IsServer == false)
                    {
                        rotor.CubeGrid.WorldMatrix = MatrixD.Multiply(m_rotorBlockId.Value.MasterToSlave.Value, this.WorldMatrix);
                    }
                    else
                    {
                        MatrixD masterToSlave = rotor.CubeGrid.WorldMatrix * MatrixD.Invert(WorldMatrix);
                        m_rotorBlockId.Value = new State() { OtherEntityId = rotor.EntityId, MasterToSlave = masterToSlave };
                    }
                    attached = Attach(rotor);
                }

                if (!attached)
                {
                    // Rotor not found by EntityId or not in scene, try again in 10 frames
                    updateFlags |= MyEntityUpdateEnum.EACH_10TH_FRAME;
                }
            }
            NeedsUpdate = updateFlags;
            RefreshConstraint();
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
                MyPhysics.GetPenetrationsBox(ref halfExtents, ref pos, ref orientation, m_penetrations, MyPhysics.CollisionLayers.DefaultCollisionLayer);
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

        public void Reattach(bool force = false)
        {
            if (m_rotorBlock == null || m_rotorBlock.Closed)
            {
                return;
            }

            if(force == false && (m_welded || m_isWelding))
            {
                return;
            }

            var rotor = m_rotorBlock;
            bool detached = Detach(force);
            if (MarkedForClose || Closed || rotor.MarkedForClose || rotor.Closed || CubeGrid.MarkedForClose || CubeGrid.Closed)
            {
                return;
            }
            bool attached = Attach(rotor, force);
            //Debug.Assert(detached && attached);
            if(!rotor.MarkedForClose && rotor.CubeGrid.Physics != null)
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

        protected virtual void UpdateSoundState()
        {
            if (!MySandboxGame.IsGameReady || m_soundEmitter == null || IsWorking == false)
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

        public void SyncDetach()
        {
            m_rotorBlockId.Value = new State() { OtherEntityId = 0,MasterToSlave = null}; // Look for anything to attach (to make grid split work correctly)
        }

        protected bool CheckVelocities()
        {
            if (!MyFakes.WELD_ROTORS || Sync.IsServer == false)
                return false;

            if (m_rotorBlock == null || m_rotorGrid == null || m_rotorGrid.Physics == null)
                return false;

            var velSq = CubeGrid.Physics.LinearVelocity.LengthSquared();
            if (m_forceWeld || velSq > m_weldSpeedSq)
            {
                if (m_welded)
                {
                    return true;
                }

                if (m_rotorBlock != null && m_rotorGrid != null && MyWeldingGroups.Static.GetGroup(CubeGrid) != MyWeldingGroups.Static.GetGroup(m_rotorGrid))
                {
                    WeldGroup();
                    return true;
                }
                return false;
            }

            const float safeUnweldSpeedDif = 2 * 2;
            if (!m_forceWeld && velSq < m_weldSpeedSq - safeUnweldSpeedDif && m_welded)
            {
                UnweldGroup();
            }
            if (m_welded)
                return true;
            return false;
        }

        private void WeldGroup()
        {
            var topGrid = m_rotorGrid;
            var topBlock = m_rotorBlock;

            m_isWelding = true;

            m_weldedRotorBlockId = m_rotorBlock.EntityId;

            if (Sync.IsServer)
            {
                MatrixD masterToSlave = topBlock.CubeGrid.WorldMatrix * MatrixD.Invert(WorldMatrix);

                m_rotorBlockId.Value = new State() { OtherEntityId = m_weldedRotorBlockId, MasterToSlave = masterToSlave };
                m_weldedEntityId.Value = m_weldedRotorBlockId;
            }
      
            Detach(false);
            MyWeldingGroups.Static.CreateLink(EntityId, CubeGrid, topGrid);
            m_rotorGrid = topGrid;
            m_rotorBlock = topBlock;
            m_welded = true;

            m_isWelding = false;
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

                m_weldedRotorBlockId = null;
              
                MyWeldingGroups.Static.BreakLink(EntityId, CubeGrid, m_rotorGrid);
        
                Attach(m_rotorBlock,false);
                m_welded = false;
                m_isWelding = false;
            }
        }

        protected void CubeGrid_OnGridSplit(MyCubeGrid grid1, MyCubeGrid grid2)
        {
            OnGridSplit();
        }

        public void OnGridSplit()
        {
            if (m_welded && m_isWelding == false)
            {
                UnweldGroup();
            }
            else
            {
                Reattach(true);
            }
        }

        public override void OnUnregisteredFromGridSystems()
        {
            base.OnUnregisteredFromGridSystems();

            if (Sync.IsServer)
            {
                CubeGrid.OnGridSplit -= CubeGrid_OnGridSplit;
            }
        }


        public override void OnRegisteredToGridSystems()
        {
            base.OnRegisteredToGridSystems();

            if (Sync.IsServer)
            {
                CubeGrid.OnGridSplit += CubeGrid_OnGridSplit;
            }
        }
    }
}
