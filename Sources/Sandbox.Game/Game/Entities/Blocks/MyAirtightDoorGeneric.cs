using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using System;
using Sandbox.Game.EntityComponents;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using System.Collections.Generic;
using VRage.Game.Components;
using Sandbox.Engine.Physics;
using Havok;
using VRage.Game.Entity;
using VRage;
using VRage.Game;
using VRage.Sync;

namespace Sandbox.Game.Entities
{
    public abstract class MyAirtightDoorGeneric : MyDoorBase, ModAPI.IMyAirtightDoorBase
    {

        private MySoundPair m_sound;

        protected float m_currOpening;//  0=closed, 1=fully open
        protected float m_subpartMovementDistance=2.5f;

        protected float m_openingSpeed = 0.3f;
        protected float m_currSpeed=0;

        private int m_lastUpdateTime;

        private static readonly float EPSILON = 0.000000001f;

        protected List<MyEntitySubpart> m_subparts = new List<MyEntitySubpart>(4);

        protected static string[] m_emissiveNames;
        protected Color m_prevEmissiveColor;
        protected float m_prevEmissivity=-1;

        public float OpenRatio
        {
            get { return m_currOpening; }
        }

        public bool IsFullyClosed //closed and airtight
        {
            get
            {
                return (m_currOpening < EPSILON);
            }
        }

        protected override bool CheckIsWorking()
        {
            return ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) && base.CheckIsWorking();
        }


        #region constructors & init & save
        public MyAirtightDoorGeneric() : base()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_open = SyncType.CreateAndAddProp<bool>();
#endif // XB1
            //GR: added to base class do not use here
            //CreateTerminalControls();

            m_open.Value = false;
            m_currOpening = 0f;
            m_currSpeed = 0f;
            m_open.ValueChanged += (x) => DoChangeOpenClose();
        }

        private new MyAirtightDoorGenericDefinition BlockDefinition
        {
            get { return (MyAirtightDoorGenericDefinition)base.BlockDefinition; }
        }

        public override void Init(MyObjectBuilder_CubeBlock builder, MyCubeGrid cubeGrid)
        {
            ResourceSink = new MyResourceSinkComponent();
            ResourceSink.Init(
                MyStringHash.GetOrCompute(BlockDefinition.ResourceSinkGroup),
                BlockDefinition.PowerConsumptionMoving,
                UpdatePowerInput);

            base.Init(builder, cubeGrid);

            var ob = (MyObjectBuilder_AirtightDoorGeneric)builder;
            m_open.Value = ob.Open;
            m_currOpening = ob.CurrOpening;

            m_openingSpeed = BlockDefinition.OpeningSpeed;
            m_sound = new MySoundPair(BlockDefinition.Sound);
            m_subpartMovementDistance = BlockDefinition.SubpartMovementDistance;
	
			if (!Enabled || !ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId))
                UpdateDoorPosition();

            OnStateChange();

            ResourceSink.IsPoweredChanged += Receiver_IsPoweredChanged;
            ResourceSink.Update();
            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
			ResourceSink.Update();
        }

        protected virtual void FillSubparts(){}//fills m_subparts based on model naming etc.
        private void InitSubparts()
        {
            if (!CubeGrid.CreatePhysics)
                return;

            FillSubparts();

            UpdateDoorPosition();
            UpdateEmissivity(true);

            if (CubeGrid.Projector != null)
            {
                //This is a projected grid, don't add collisions for subparts
                return;
            }
            foreach (var subpart in m_subparts)
            {
                if (subpart != null && subpart.Physics == null)
                {
                    if ((subpart.ModelCollision.HavokCollisionShapes != null) && (subpart.ModelCollision.HavokCollisionShapes.Length > 0))
                    {
                        var shape = subpart.ModelCollision.HavokCollisionShapes[0];
                        subpart.Physics = new Engine.Physics.MyPhysicsBody(subpart, RigidBodyFlag.RBF_DOUBLED_KINEMATIC | RigidBodyFlag.RBF_KINEMATIC);
                        subpart.Physics.IsPhantom = false;
                        Vector3 center = subpart.PositionComp.LocalVolume.Center;
                        subpart.GetPhysicsBody().CreateFromCollisionObject(shape, center, WorldMatrix, null, MyPhysics.CollisionLayers.KinematicDoubledCollisionLayer);
                        subpart.Physics.Enabled = true;
                    }
                }
            }

            CubeGrid.OnHavokSystemIDChanged -= CubeGrid_OnHavokSystemIDChanged;
            CubeGrid.OnHavokSystemIDChanged += CubeGrid_OnHavokSystemIDChanged;
            CubeGrid.OnPhysicsChanged -= CubeGrid_OnPhysicsChanged;
            CubeGrid.OnPhysicsChanged += CubeGrid_OnPhysicsChanged;
            if (CubeGrid.Physics != null)
                UpdateHavokCollisionSystemID(CubeGrid.GetPhysicsBody().HavokCollisionSystemID);
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = (MyObjectBuilder_AirtightDoorGeneric)base.GetObjectBuilderCubeBlock(copy);
            ob.Open = m_open;
            ob.CurrOpening = m_currOpening;
            return ob;
        }
        #endregion


        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            if (CubeGrid.Physics == null)
                return;
            if (m_currSpeed != 0 && Enabled && ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId))
                UpdateDoorPosition();
        }

        #region update

        bool m_updated = false;
        bool m_stateChange = false;
        public override void UpdateBeforeSimulation()
        {
            if (!m_updated)
            {
                MatrixD tmp = PositionComp.WorldMatrix;
                foreach (var subpart in m_subparts)
                {
                    subpart.PositionComp.UpdateWorldMatrix(ref tmp);
                }
                m_updated = true;
            }
            if (m_stateChange && ((m_open && 1f - m_currOpening < EPSILON) || (!m_open && m_currOpening < EPSILON)))
            {
                //END OF MOVEMENT
                if (m_soundEmitter != null && m_soundEmitter.Loop)
                    m_soundEmitter.StopSound(false);
                m_currSpeed = 0;
                NeedsUpdate &= ~(MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME);
                ResourceSink.Update();
                RaisePropertiesChanged();
                if (!m_open)
                {   //finished closing - they are airtight now
                    var handle = DoorStateChanged;
                    if (handle != null) handle(m_open);
                }
                m_stateChange = false;
            }
            if (m_soundEmitter != null)
            {
                if (Enabled && ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) && m_currSpeed != 0)
                {
                    StartSound(m_sound);
                }
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
                m_currOpening = MathHelper.Clamp(m_currOpening + deltaPos, 0f, 1f);
            }
        }

        protected abstract void UpdateDoorPosition();

        public override void UpdateVisual()
        {
            base.UpdateVisual();
            UpdateEmissivity();
        }
        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            UpdateEmissivity();
        }
        protected virtual void UpdateEmissivity(bool force=false)
        {
        }

        protected void SetEmissive(Color color, float emissivity=1, bool force=false)
        {
            if (Render.RenderObjectIDs[0] != VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED
                && (force || color != m_prevEmissiveColor || m_prevEmissivity!=emissivity))
            {
                foreach (var name in m_emissiveNames)
                    UpdateNamedEmissiveParts(Render.RenderObjectIDs[0], name, color, emissivity);

                m_prevEmissiveColor = color;
                m_prevEmissivity = emissivity;
            }
        }


        #endregion

        #region open/close
        public void ChangeOpenClose(bool open)
        {
            if (open == m_open)
                return;
             m_open.Value = open;
        }

        internal void DoChangeOpenClose()
        {
            if (!Enabled || !ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId))
                return;

            if (m_soundEmitter != null)
                m_soundEmitter.StopSound(true);
            OnStateChange();
            RaisePropertiesChanged();
        }
        #endregion

        #region On...
        private void OnStateChange()
        {//BEGIN OF MOVEMENT
            if (m_open)
                m_currSpeed = m_openingSpeed;
            else
                m_currSpeed = -m_openingSpeed;
            ResourceSink.Update();

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
            m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds - 1;//-1 because we need to have doors already moving at time of DoorStateChanged
            UpdateCurrentOpening();
            UpdateDoorPosition();
            if (m_open)
            {   //starting to open, not airtight any more
                var handle = DoorStateChanged;
                if (handle != null) handle(m_open);
            }
            m_stateChange = true;
        }

        void CubeGrid_OnPhysicsChanged(MyEntity obj)
        {
            if (m_subparts == null || m_subparts.Count == 0)
            {
                return;
            }

            if (m_subparts[0].Physics == null)
            {
                return;
            }
            if (obj.Physics != null && obj.GetPhysicsBody().HavokCollisionSystemID != m_subparts[0].GetPhysicsBody().HavokCollisionSystemID)
                UpdateHavokCollisionSystemID(obj.GetPhysicsBody().HavokCollisionSystemID);
            UpdateDoorPosition();
        }

        protected override void OnEnabledChanged()
        {
            ResourceSink.Update();
            base.OnEnabledChanged();
        }

        public override void OnBuildSuccess(long builtBy)
        {
            ResourceSink.Update();
            if (CubeGrid.Physics != null)
                UpdateHavokCollisionSystemID(CubeGrid.GetPhysicsBody().HavokCollisionSystemID);
            base.OnBuildSuccess(builtBy);
        }

        public override void OnCubeGridChanged(MyCubeGrid oldGrid)
        {
            oldGrid.OnHavokSystemIDChanged -= CubeGrid_OnHavokSystemIDChanged;
            CubeGrid.OnHavokSystemIDChanged += CubeGrid_OnHavokSystemIDChanged;
            oldGrid.OnPhysicsChanged -= CubeGrid_OnPhysicsChanged;
            CubeGrid.OnPhysicsChanged += CubeGrid_OnPhysicsChanged;
            if (CubeGrid.Physics != null)//when splitting blocks or creating new, then this is null and IDs are set from grit activation
                //when merging blocks, however, no grid is activated, so IDs must be changed from here
                UpdateHavokCollisionSystemID(CubeGrid.GetPhysicsBody().HavokCollisionSystemID);
            base.OnCubeGridChanged(oldGrid);
        }

        void CubeGrid_OnHavokSystemIDChanged(int id)
        {
            if (CubeGrid.Physics != null)
            {
                UpdateHavokCollisionSystemID(CubeGrid.GetPhysicsBody().HavokCollisionSystemID);
            }
        }

        public override void OnModelChange()
        {
            base.OnModelChange();
            InitSubparts();
            UpdateDoorPosition();
        }

        #endregion

        #region misc
        internal void UpdateHavokCollisionSystemID(int HavokCollisionSystemID)
        {
            foreach (var subpart in m_subparts)
            {
                if (subpart != null && subpart.Physics != null)
                {
                    if ((subpart.ModelCollision.HavokCollisionShapes != null) && (subpart.ModelCollision.HavokCollisionShapes.Length > 0))
                    {
                        var info = HkGroupFilter.CalcFilterInfo(MyPhysics.CollisionLayers.KinematicDoubledCollisionLayer, HavokCollisionSystemID, 1, 1);
                        subpart.Physics.RigidBody.SetCollisionFilterInfo(info);
                        if (subpart.GetPhysicsBody().HavokWorld != null)
                            subpart.GetPhysicsBody().HavokWorld.RefreshCollisionFilterOnEntity(subpart.Physics.RigidBody);
                        if (subpart.Physics.RigidBody2 != null)
                        {
                            info = HkGroupFilter.CalcFilterInfo(MyPhysics.CollisionLayers.DynamicDoubledCollisionLayer, HavokCollisionSystemID, 1, 1);
                            subpart.Physics.RigidBody2.SetCollisionFilterInfo(info);
                            if (subpart.GetPhysicsBody().HavokWorld != null)
                                subpart.GetPhysicsBody().HavokWorld.RefreshCollisionFilterOnEntity(subpart.Physics.RigidBody2);
                        }

                        /*if (this.CubeGrid.Physics != null && this.CubeGrid.GetPhysicsBody().HavokWorld != null)
                        {
                            this.CubeGrid.GetPhysicsBody().HavokWorld.RefreshCollisionFilterOnEntity(m_subpartDoor1.Physics.RigidBody);
                            this.CubeGrid.GetPhysicsBody().HavokWorld.RefreshCollisionFilterOnEntity(m_subpartDoor1.Physics.RigidBody2);
                        }*/
                    }
                }
            }
        }

        protected float UpdatePowerInput()
        {
            if (!(Enabled && IsFunctional))
                return 0;
            if (m_currSpeed == 0)
                return BlockDefinition.PowerConsumptionIdle;
            return BlockDefinition.PowerConsumptionMoving;
        }
        protected bool IsEnoughPower()
        {
            if (this.ResourceSink != null)
                return ResourceSink.IsPowerAvailable(MyResourceDistributorComponent.ElectricityId,
                        BlockDefinition.PowerConsumptionMoving);
            return false;

        }

        private void StartSound(MySoundPair cuePair)
        {
            if ((m_soundEmitter.Sound != null) && (m_soundEmitter.Sound.IsPlaying) && (m_soundEmitter.SoundId == cuePair.Arcade || m_soundEmitter.SoundId == cuePair.Realistic))
                return;

            m_soundEmitter.StopSound(true);
            m_soundEmitter.PlaySingleSound(cuePair, true);
        }



        protected override void Closing()
        {
            CubeGrid.OnHavokSystemIDChanged -= CubeGrid_OnHavokSystemIDChanged;
            if (m_soundEmitter != null)
                m_soundEmitter.StopSound(true);
            base.Closing();
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
        #endregion

        protected override void WorldPositionChanged(object source)
        {
            base.WorldPositionChanged(source);
            UpdateDoorPosition();
        }

        #region modapi
        public event Action<bool> DoorStateChanged;
        event Action<bool> Sandbox.ModAPI.IMyDoor.DoorStateChanged
        {
            add { DoorStateChanged += value; }
            remove { DoorStateChanged -= value; }
        }
        #endregion
    }
}
