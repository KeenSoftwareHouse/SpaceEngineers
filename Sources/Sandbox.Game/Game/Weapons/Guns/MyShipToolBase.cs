using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sandbox.Game.EntityComponents;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRage.ModAPI;
using VRage.Game.Components;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Interfaces;
using Sandbox.Game.Audio;
using Sandbox.Game.World;
using VRage.Profiler;
using VRage.Sync;
using IMyEntity = VRage.ModAPI.IMyEntity;

namespace Sandbox.Game.Weapons
{
    public abstract class MyShipToolBase : MyFunctionalBlock, IMyGunObject<MyToolBase>, IMyInventoryOwner, IMyConveyorEndpointBlock, IMyShipToolBase
    {
        /// <summary>
        /// Default reach distance of a tool;
        /// </summary>
        protected float DEFAULT_REACH_DISTANCE = 4.5f;

        private MyMultilineConveyorEndpoint m_endpoint;
        private MyDefinitionId m_defId;

        // State variables
        private bool m_wantsToActivate;
        protected bool WantsToActivate { get { return m_wantsToActivate; } set { m_wantsToActivate = value; UpdateActivationState(); } }

        private bool m_isActivated;
        private bool m_isActivatedOnSomething;
        protected int m_lastTimeActivate;

        private int m_shootHeatup;
        public bool IsHeatingUp { get { return (m_shootHeatup > 0); } }

        private bool m_effectsSet;

        //Debugging variable
        private int m_activateCounter;

        private HashSet<MyEntity> m_entitiesInContact;
        protected BoundingSphere m_detectorSphere;

        private HashSet<MySlimBlock> m_blocksToActivateOn;
        private HashSet<MySlimBlock> m_tempBlocksBuffer;

        private Sync<bool> m_useConveyorSystem;

        protected MyCharacter controller = null;
        public int HeatUpFrames { get; protected set; }

        public MyShipToolBase()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_useConveyorSystem = SyncType.CreateAndAddProp<bool>();
#endif // XB1
            CreateTerminalControls();
        }

        protected override bool CheckIsWorking()
        {
            return ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) && base.CheckIsWorking();
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyShipToolBase>())
                return;
            base.CreateTerminalControls();
            var useConvSystem = new MyTerminalControlOnOffSwitch<MyShipToolBase>("UseConveyor", MySpaceTexts.Terminal_UseConveyorSystem);
            useConvSystem.Getter = (x) => (x).UseConveyorSystem;
            useConvSystem.Setter = (x, v) => (x).UseConveyorSystem =  v;
            useConvSystem.EnableToggleAction();
            MyTerminalControlFactory.AddControl(useConvSystem);
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                MyStringHash.GetOrCompute("Defense"),
                MyEnergyConstants.MAX_REQUIRED_POWER_SHIP_GRINDER,
                ComputeRequiredPower);
            sinkComp.IsPoweredChanged += Receiver_IsPoweredChanged;
            ResourceSink = sinkComp;

            base.Init(objectBuilder, cubeGrid);

            m_entitiesInContact = new HashSet<MyEntity>();
            m_blocksToActivateOn = new HashSet<MySlimBlock>();
            m_tempBlocksBuffer = new HashSet<MySlimBlock>();

            m_isActivated = false;
            m_isActivatedOnSomething = false;
            m_wantsToActivate = false;
            m_effectsSet = false;

            m_shootHeatup = 0;
            m_activateCounter = 0;

            m_defId = objectBuilder.GetId();
            var def = MyDefinitionManager.Static.GetCubeBlockDefinition(m_defId);

            var typedBuilder = objectBuilder as MyObjectBuilder_ShipToolBase;

            //each dimension of size needs to be scaled by grid size not only one 
            float inventoryVolume = def.Size.X * cubeGrid.GridSize*def.Size.Y *cubeGrid.GridSize* def.Size.Z * cubeGrid.GridSize * 0.5f;
            Vector3 inventorySize = new Vector3(def.Size.X, def.Size.Y, def.Size.Z * 0.5f);

            if (this.GetInventory() == null) // could be already initialized as component
            {
                MyInventory inventory = new MyInventory(inventoryVolume, inventorySize, MyInventoryFlags.CanSend);
                Components.Add<MyInventoryBase>(inventory);
                inventory.Init(typedBuilder.Inventory);
            }
            Debug.Assert(this.GetInventory().Owner == this, "Ownership was not set!");

            SlimBlock.UsesDeformation = false;
            SlimBlock.DeformationRatio = typedBuilder.DeformationRatio; // 3x times harder for destruction by high speed

            Enabled = typedBuilder.Enabled;
            UseConveyorSystem = typedBuilder.UseConveyorSystem;

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            LoadDummies();

            UpdateActivationState();

            IsWorkingChanged += MyShipToolBase_IsWorkingChanged;
			ResourceSink.Update();

            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_FRAME;
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = base.GetObjectBuilderCubeBlock(copy);

            MyObjectBuilder_ShipToolBase obShipToolBase = (MyObjectBuilder_ShipToolBase)ob;
            obShipToolBase.Inventory = this.GetInventory().GetObjectBuilder();
            obShipToolBase.UseConveyorSystem = UseConveyorSystem;

            return obShipToolBase;
        }

        public override void OnRemovedByCubeBuilder()
        {
            ReleaseInventory(this.GetInventory());
            base.OnRemovedByCubeBuilder();
        }

        public override void OnDestroy()
        {
            ReleaseInventory(this.GetInventory(), true);
            base.OnDestroy();
        }

        private void LoadDummies()
        {
            var finalModel = VRage.Game.Models.MyModels.GetModelOnlyDummies(BlockDefinition.Model);
            foreach (var dummy in finalModel.Dummies)
            {
                if (dummy.Key.ToLower().Contains("detector_shiptool"))
                {
                    var matrix = dummy.Value.Matrix;
                    float radius = matrix.Scale.AbsMin();

                    Matrix blockMatrix = this.PositionComp.LocalMatrix;
                    Vector3 gridDetectorPosition = Vector3.Transform(matrix.Translation, blockMatrix);
                    m_detectorSphere = new BoundingSphere(gridDetectorPosition, radius);
                    break;
                }
            }
        }

        protected void SetBuildingMusic(int amount)
        {
            if (MySession.Static != null && controller == MySession.Static.LocalCharacter && MyMusicController.Static != null)
                MyMusicController.Static.Building(amount);
        }

        protected virtual bool CanInteractWithSelf
        {
            get
            {
                return false;
            }
        }

        private bool CanInteractWith(IMyEntity entity)
        {
            if (entity == null)
                return false;
            if ((entity == this.CubeGrid && !CanInteractWithSelf))
                return false;
            if (!(entity is MyCubeGrid) && !(entity is MyCharacter))
                return false;
            return true;
        }

        protected override void OnEnabledChanged()
        {
            WantsToActivate = Enabled;

            base.OnEnabledChanged();
        }

        private void ComponentStack_IsFunctionalChanged()
        {
			ResourceSink.Update();
        }

        private void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
            UpdateActivationState();
        }

        void MyShipToolBase_IsWorkingChanged(MyCubeBlock obj)
        {
            UpdateActivationState();
        }

        public override void OnModelChange()
        {
            base.OnModelChange();

            UpdateActivationState();
        }

        private void UpdateActivationState()
        {
            if (ResourceSink != null)
                ResourceSink.Update();
            if ((Enabled || WantsToActivate) && IsFunctional && ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId))
            {
                StartShooting();
            }
            else
            {
                StopShooting();
            }
        }

        private float ComputeRequiredPower()
        {
            return (IsFunctional && (Enabled || WantsToActivate)) ? ResourceSink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId) : 0f;
        }

        public override void UpdateAfterSimulation()
        {
            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_SHIP_TOOLS)
            {
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 0.0f), m_isActivated ? "Activated" : "Not activated", Color.Red, 1.0f);
                MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 90.0f), "Activation counter: " + m_activateCounter, Color.Red, 1.0f);
            }

            base.UpdateAfterSimulation();

            if (IsFunctional)
                UpdateAnimationCommon();
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();

            if (m_isActivated)
            {
                ActivateCommon();
            }
        }

        protected abstract bool Activate(HashSet<MySlimBlock> targets);

        protected abstract void UpdateAnimation(bool activated);

        private void UpdateAnimationCommon()
        {
            UpdateAnimation(m_isActivated || IsHeatingUp);

            if (m_isActivatedOnSomething && m_effectsSet == false)
            {
                StartEffects();
                m_effectsSet = true;
            }
            else if (m_isActivatedOnSomething)
                UpdateEffects();
            else if (!m_isActivatedOnSomething && m_effectsSet == true)
            {
                StopEffects();
                m_effectsSet = false;
            }
        }

        protected abstract void StartEffects();

        protected abstract void StopEffects();

        protected abstract void UpdateEffects();

        private void ActivateCommon()
        {
            BoundingSphereD globalSphere = new BoundingSphereD(Vector3D.Transform(m_detectorSphere.Center, CubeGrid.WorldMatrix), m_detectorSphere.Radius);

            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW)
            {
                MyRenderProxy.DebugDrawSphere(globalSphere.Center, (float)globalSphere.Radius, Color.Red.ToVector3(), 1.0f, false);
            }

            m_isActivatedOnSomething = false;
            ProfilerShort.Begin("Query");
            var res = MyEntities.GetTopMostEntitiesInSphere(ref globalSphere);
            m_entitiesInContact.Clear();
            foreach (MyEntity entity in res)
            {
                var rootEntity = entity.GetTopMostParent();
                if (CanInteractWith(rootEntity))
                    m_entitiesInContact.Add(rootEntity);
            }
            res.Clear();
            ProfilerShort.End();

            //TODO:cache blocks on this grid
            foreach (var entry in m_entitiesInContact)
            {
                MyCubeGrid grid = entry as MyCubeGrid;
                MyCharacter character = entry as MyCharacter;

                if (grid != null)
                {
                    m_tempBlocksBuffer.Clear();
                    grid.GetBlocksInsideSphere(ref globalSphere, m_tempBlocksBuffer);
                    m_blocksToActivateOn.UnionWith(m_tempBlocksBuffer);
                }
                if (character != null && Sync.IsServer)
                {
                    MyStringHash damageType = MyDamageType.Drill;
                    if (this is IMyShipGrinder)
                        damageType = MyDamageType.Grind;
                    else if (this is IMyShipWelder)
                        damageType = MyDamageType.Weld;
                    if (globalSphere.Intersects(character.PositionComp.WorldAABB))
                        character.DoDamage(20, damageType, true, attackerId: EntityId);
                }
            }

            m_isActivatedOnSomething |= Activate(m_blocksToActivateOn);

            m_activateCounter++;
            m_lastTimeActivate = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            PlayLoopSound(m_isActivatedOnSomething);

            m_blocksToActivateOn.Clear();
        }

        public override void OnRemovedFromScene(object source)
        {
            base.OnRemovedFromScene(source);

            StopEffects();
            StopLoopSound();
        }

        public override void OnAddedToScene(object source)
        {
            //Reload dummies in order to update local position of detector component (else the targeting position may differ!!!)
            LoadDummies();
            base.OnAddedToScene(source);
        }

        protected override void Closing()
        {
            base.Closing();

            StopEffects();
            StopLoopSound();
        }

        public float BackkickForcePerSecond
        {
            get { return 0; }
        }

        public float ShakeAmount
        {
            get;
            protected set;
        }

        public MyDefinitionId DefinitionId
        {
            get { return m_defId; }
        }

        public bool EnabledInWorldRules
        {
            get { return true; }
        }

        protected virtual void StartShooting()
        {
            m_isActivated = true;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        protected virtual void StopShooting()
        {
            m_wantsToActivate = false;
            m_isActivated = false;
            m_isActivatedOnSomething = false;

            if (Physics != null)
                Physics.Enabled = false;
			if (ResourceSink != null)
				ResourceSink.Update();

            m_shootHeatup = 0;

            StopEffects();
            StopLoopSound();
        }

        public int GetAmmunitionAmount()
        {
            throw new NotImplementedException();
        }

        public virtual void OnControlAcquired(Entities.Character.MyCharacter owner)
        { }

        public virtual void OnControlReleased()
        {
            if (!Enabled && !Closed)
                StopShooting();
        }

        public void DrawHud(IMyCameraController camera, long playerId)
        { }

        public void SetInventory(MyInventory inventory, int index)
        {
            Components.Add<MyInventoryBase>( inventory);
        }

        public bool UseConveyorSystem
        {
            get
            {
                return m_useConveyorSystem;
            }
            set
            {
                m_useConveyorSystem.Value = value;
            }
        }

        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get { return m_endpoint; }
        }

        public void InitializeConveyorEndpoint()
        {
            m_endpoint = new MyMultilineConveyorEndpoint(this);
            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_endpoint));
        }

        protected abstract void StopLoopSound();
        protected abstract void PlayLoopSound(bool activated);

        public Vector3 DirectionToTarget(Vector3D target)
        {
            throw new NotImplementedException();
        }

        public bool CanShoot(MyShootActionEnum action, long shooter, out MyGunStatusEnum status)
        {
            status = MyGunStatusEnum.OK;

            if (action != MyShootActionEnum.PrimaryAction)
            {
                status = MyGunStatusEnum.Failed;
                return false;
            }
            if (!IsFunctional)
            {
                status = MyGunStatusEnum.NotFunctional;
                return false;
            }
            if (Enabled)
            {
                status = MyGunStatusEnum.Disabled;
                return false;
            }
            if (!HasPlayerAccess(shooter))
            {
                status = MyGunStatusEnum.AccessDenied;
                return false;
            }
            if (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimeActivate < MyShipGrinderConstants.GRINDER_COOLDOWN_IN_MILISECONDS)
            {
                status = MyGunStatusEnum.Cooldown;
                return false;
            }

            return true;
        }

        public void Shoot(MyShootActionEnum action, Vector3 direction, Vector3D? overrideWeaponPos, string gunAction)
        {
            if (action != MyShootActionEnum.PrimaryAction) return;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;

            if (m_shootHeatup < HeatUpFrames)
            {
                m_shootHeatup++;
                return;
            }

            WantsToActivate = true;
			ResourceSink.Update();
        }

        public void EndShoot(MyShootActionEnum action)
        {
            if (action != MyShootActionEnum.PrimaryAction) return;

            if (!Enabled)
                StopShooting();
        }

        public bool IsShooting
        {
            get { return m_isActivated; }
        }

        public int ShootDirectionUpdateTime
        {
            get { return 0; }
        }

        public void BeginFailReaction(MyShootActionEnum action, MyGunStatusEnum status)
        { }

        public void BeginFailReactionLocal(MyShootActionEnum action, MyGunStatusEnum status)
        { }

        public void ShootFailReactionLocal(MyShootActionEnum action, MyGunStatusEnum status)
        { }

        public MyToolBase GunBase
        {
            get { return null; }
        }
        bool ModAPI.Ingame.IMyShipToolBase.UseConveyorSystem { get { return UseConveyorSystem; } }

        public void UpdateSoundEmitter()
        {
            if (m_soundEmitter != null)
                m_soundEmitter.Update();
        }

        #region IMyInventoryOwner implementation

        int IMyInventoryOwner.InventoryCount
        {
            get { return InventoryCount; }
        }

        long IMyInventoryOwner.EntityId
        {
            get { return EntityId; }
        }

        bool IMyInventoryOwner.HasInventory
        {
            get { return HasInventory; }
        }

        bool IMyInventoryOwner.UseConveyorSystem
        {
            get
            {
                return UseConveyorSystem;
            }
            set
            {
                UseConveyorSystem = value;
            }
        }

        IMyInventory IMyInventoryOwner.GetInventory(int index)
        {
            return MyEntityExtensions.GetInventory(this, index);
        }

        #endregion

        #region IMyConveyorEndpointBlock implementation

        public virtual Sandbox.Game.GameSystems.Conveyors.PullInformation GetPullInformation()
        {
            return null;
        }

        public virtual Sandbox.Game.GameSystems.Conveyors.PullInformation GetPushInformation()
        {
            return null;
        }

        #endregion
    }
}
