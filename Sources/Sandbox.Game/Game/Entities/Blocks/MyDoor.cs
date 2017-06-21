using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Graphics.GUI;
using Sandbox.Game.GameSystems.Electricity;
using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Cube;
using Havok;
using System.Reflection;
using Sandbox.Common;
using Sandbox.Engine.Physics;
using Sandbox.Game.World;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens;
using Sandbox.Game.Screens.Terminal.Controls;
using VRage.Utils;
using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Localization;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Game.Entity;
using VRage;
using Sandbox.Engine.Multiplayer;
using VRage.Network;
using VRage.Game;
using VRage.Sync;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Door))]
    public class MyDoor : MyDoorBase, ModAPI.IMyDoor
    {
        private const float CLOSED_DISSASEMBLE_RATIO = 3.3f;

        private MySoundPair m_openSound;
        private MySoundPair m_closeSound;

        private float m_currOpening;
        private float m_currSpeed;
        private int m_lastUpdateTime;

        private MyEntitySubpart m_leftSubpart = null;
        private MyEntitySubpart m_rightSubpart = null;

        public float MaxOpen = 1.2f;

        protected override bool CheckIsWorking()
        {
            return ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) && base.CheckIsWorking();
        }

        public override float DisassembleRatio
        {
            get
            {
                //for now have the same dissasemble ratio maybe change again in the future
                return base.DisassembleRatio * CLOSED_DISSASEMBLE_RATIO/* * (Open ? 1.0f : CLOSED_DISSASEMBLE_RATIO) */;
            }
        }

        public MyDoor() : base()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_open = SyncType.CreateAndAddProp<bool>();
#endif // XB1
            //GR: added to base class do not use here
            //CreateTerminalControls();

            m_currOpening = 0f;
            m_currSpeed = 0f;
            m_open.ValidateNever();
            m_open.ValueChanged += x => OnStateChange();
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();
            UpdateEmissivity();
        }

        private void UpdateEmissivity()
        {
            if (Enabled && ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId))
            {
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Green, Color.White);
                OnStateChange();
            }
            else
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 0.0f, Color.Red, Color.White);
        }

        public float OpenRatio
        {
            get { return m_currOpening / MaxOpen; }
        }

        protected override void OnEnabledChanged()
        {
			ResourceSink.Update();
            base.OnEnabledChanged();
        }

        public override void OnBuildSuccess(long builtBy)
        {
			ResourceSink.Update();
            base.OnBuildSuccess(builtBy);
        }

        public override void Init(MyObjectBuilder_CubeBlock builder, MyCubeGrid cubeGrid)
        {
            var doorDefinition = BlockDefinition as MyDoorDefinition;
            MyStringHash resourceSinkGroup;
            if (doorDefinition != null)
            {
                MaxOpen = doorDefinition.MaxOpen;
                m_openSound = new MySoundPair(doorDefinition.OpenSound);
                m_closeSound = new MySoundPair(doorDefinition.CloseSound);
                resourceSinkGroup = MyStringHash.GetOrCompute(doorDefinition.ResourceSinkGroup);
            }
            else
            {
                MaxOpen = 1.2f;
                m_openSound = new MySoundPair("BlockDoorSmallOpen");
                m_closeSound = new MySoundPair("BlockDoorSmallClose");
                resourceSinkGroup = MyStringHash.GetOrCompute("Doors");
            }

            var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                resourceSinkGroup,
                MyEnergyConstants.MAX_REQUIRED_POWER_DOOR,
                () => (Enabled && IsFunctional) ? sinkComp.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId) : 0f);
            sinkComp.IsPoweredChanged += Receiver_IsPoweredChanged;
         

            ResourceSink = sinkComp;

            base.Init(builder, cubeGrid);
         
            //m_subpartsSize = 0.5f * (0.5f * SlimBlock.CubeGrid.GridSize - 0.3f);
            

            var ob = (MyObjectBuilder_Door)builder;
            m_open.Value = ob.State;
            if (ob.Opening == -1)
            {
                m_currOpening = IsFunctional ? 0 : MaxOpen;
                m_open.Value = !IsFunctional;
            }
            else
                m_currOpening = ob.Opening;


            if (!Enabled || !ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId))
                UpdateSlidingDoorsPosition();

            OnStateChange();

            if (m_open)
            {
                // required when reinitializing a door after the armor beneath it is destroyed
                if (Open && (m_currOpening == MaxOpen))
                    UpdateSlidingDoorsPosition();
            }
            sinkComp.Update();
            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
        }

        private void InitSubparts()
        {
            if (!CubeGrid.CreatePhysics)
                return;

            Subparts.TryGetValue("DoorLeft", out m_leftSubpart);
            Subparts.TryGetValue("DoorRight", out m_rightSubpart);

            UpdateSlidingDoorsPosition();

            if (CubeGrid.Projector != null)
            {
                //This is a projected grid, don't add collisions for subparts
                return;
            }

            if (m_leftSubpart != null && m_leftSubpart.Physics == null)
            {
                if ((m_leftSubpart.ModelCollision.HavokCollisionShapes != null) && (m_leftSubpart.ModelCollision.HavokCollisionShapes.Length > 0))
                {
                    var shape = m_leftSubpart.ModelCollision.HavokCollisionShapes[0];
                    m_leftSubpart.Physics = new Engine.Physics.MyPhysicsBody(m_leftSubpart, RigidBodyFlag.RBF_KINEMATIC);
                    m_leftSubpart.Physics.IsPhantom = false;
                    Vector3 center = new Vector3(0.35f, 0f, 0f) + m_leftSubpart.PositionComp.LocalVolume.Center;
                    m_leftSubpart.GetPhysicsBody().CreateFromCollisionObject(shape, center, WorldMatrix, null, MyPhysics.CollisionLayers.KinematicDoubledCollisionLayer);
                    m_leftSubpart.Physics.Enabled = true;
                }
            }

            if (m_rightSubpart != null && m_rightSubpart.Physics == null)
            {
                if ((m_rightSubpart.ModelCollision.HavokCollisionShapes != null) && (m_rightSubpart.ModelCollision.HavokCollisionShapes.Length > 0))
                {
                    var shape = m_rightSubpart.ModelCollision.HavokCollisionShapes[0];
                    m_rightSubpart.Physics = new Engine.Physics.MyPhysicsBody(m_rightSubpart, RigidBodyFlag.RBF_KINEMATIC);
                    m_rightSubpart.Physics.IsPhantom = false;
                    Vector3 center = new Vector3(-0.35f, 0f, 0f) + m_rightSubpart.PositionComp.LocalVolume.Center;
                    m_rightSubpart.GetPhysicsBody().CreateFromCollisionObject(shape, center, WorldMatrix, null, MyPhysics.CollisionLayers.KinematicDoubledCollisionLayer);
                    m_rightSubpart.Physics.Enabled = true;
                }
            }
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = (MyObjectBuilder_Door)base.GetObjectBuilderCubeBlock(copy);
            ob.State = Open;
            ob.Opening = m_currOpening;
            ob.OpenSound = m_openSound.ToString();
            ob.CloseSound = m_closeSound.ToString();
            return ob;
        }

        private void OnStateChange()
        {
            float speed = ((MyDoorDefinition)BlockDefinition).OpeningSpeed;
            m_currSpeed = m_open ? speed : -speed;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            UpdateCurrentOpening();
            UpdateSlidingDoorsPosition();

            var handle = DoorStateChanged;
            if (handle != null) handle(m_open);
        }

        private void StartSound(MySoundPair cuePair)
        {
            if (m_soundEmitter == null)
                return;
            if ((m_soundEmitter.Sound != null) && (m_soundEmitter.Sound.IsPlaying) && (m_soundEmitter.SoundId == cuePair.Arcade || m_soundEmitter.SoundId == cuePair.Realistic))
                return;

            m_soundEmitter.StopSound(true);
            m_soundEmitter.PlaySingleSound(cuePair, true);
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            if (CubeGrid.Physics == null)
                return;

            // Don't need to update the door when nothing is happening
            if (m_currOpening == 0 || m_currOpening > MaxOpen)
                return;

            //Update door position because of inaccuracies in high velocities
            UpdateSlidingDoorsPosition();
        }

        public override void UpdateBeforeSimulation()
        {
            if ((Open && (m_currOpening == MaxOpen)) || (!Open && (m_currOpening == 0f)))
            {
                if (m_soundEmitter != null && m_soundEmitter.IsPlaying && m_soundEmitter.Loop && (BlockDefinition.DamagedSound == null || m_soundEmitter.SoundId != BlockDefinition.DamagedSound.SoundId))
                    m_soundEmitter.StopSound(false);

                NeedsUpdate &= ~MyEntityUpdateEnum.EACH_FRAME;
                m_currSpeed = 0;
                return;
            }

            if (m_soundEmitter != null && Enabled && ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId))
            {
                if (Open)
                    StartSound(m_openSound);
                else
                    StartSound(m_closeSound);
            }

            base.UpdateBeforeSimulation();

            UpdateCurrentOpening();

            m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
        }

        private void UpdateCurrentOpening()
        {
            if (Enabled && ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId))
            {
                float timeDelta = (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastUpdateTime) / 1000f;
                float deltaPos = m_currSpeed * timeDelta;
                m_currOpening = MathHelper.Clamp(m_currOpening + deltaPos, 0f, MaxOpen);
            }
        }

        private void UpdateSlidingDoorsPosition()
        {
            if (this.CubeGrid.Physics == null)
                return;

            float opening = m_currOpening * 0.475f; // enrico's magic numbers

            if (m_leftSubpart != null && m_leftSubpart.Physics != null)
            {
                m_leftSubpart.PositionComp.LocalMatrix = Matrix.CreateTranslation(new Vector3(-opening, 0f, 0f));
                //if (m_leftSubpart.Physics.LinearVelocity.Equals(CubeGrid.Physics.LinearVelocity, 0.01f) == false)
                //{
                //    m_leftSubpart.Physics.LinearVelocity = this.CubeGrid.Physics.LinearVelocity;
                //}

                //if (m_leftSubpart.Physics.AngularVelocity.Equals(this.CubeGrid.Physics.AngularVelocity, 0.01f) == false)
                //{
                //    m_leftSubpart.Physics.AngularVelocity = this.CubeGrid.Physics.AngularVelocity;
                //}
            }

            if (m_rightSubpart != null && m_rightSubpart.Physics != null)
            {
                m_rightSubpart.PositionComp.LocalMatrix = Matrix.CreateTranslation(new Vector3(opening, 0f, 0f));
                //if (m_rightSubpart.Physics.LinearVelocity.Equals(CubeGrid.Physics.LinearVelocity, 0.01f) == false)
                //{
                //    m_rightSubpart.Physics.LinearVelocity = this.CubeGrid.Physics.LinearVelocity;
                //}

                //if (m_rightSubpart.Physics.AngularVelocity.Equals(this.CubeGrid.Physics.AngularVelocity, 0.01f) == false)
                //{
                //    m_rightSubpart.Physics.AngularVelocity = this.CubeGrid.Physics.AngularVelocity;
                //}
            }
        }

        protected override void Closing()
        {
            if (m_soundEmitter != null)
                m_soundEmitter.StopSound(true);
            base.Closing();
        }

        public override void OnModelChange()
        {
            base.OnModelChange();
            InitSubparts();
        }

        void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
            UpdateEmissivity();
        }

        void ComponentStack_IsFunctionalChanged()
        {
			ResourceSink.Update();
        }

        event Action<bool> DoorStateChanged;
        event Action<bool> Sandbox.ModAPI.IMyDoor.DoorStateChanged
        {
            add { DoorStateChanged += value; }
            remove { DoorStateChanged -= value; }
        }
    }
}
