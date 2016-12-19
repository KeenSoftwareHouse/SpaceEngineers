using System;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.World;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.Gui;
using System.Collections.Generic;
using Havok;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Common;
using VRage;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.Components;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.Game.Localization;
using VRage.ModAPI;
using VRage.Game.Components;
using System.Diagnostics;
using VRage.Game.Entity;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Interfaces;
using VRage.Profiler;
using VRage.Sync;

namespace Sandbox.Game.Weapons
{
    [MyCubeBlockType(typeof(MyObjectBuilder_SmallGatlingGun))]
    public class MySmallGatlingGun : MyUserControllableGun, IMyGunObject<MyGunBase>, IMyInventoryOwner, IMyConveyorEndpointBlock, IMyGunBaseUser, IMySmallGatlingGun
    {
        float m_rotationAngle;                          //  Actual rotation angle (not rotation speed) around Z axis
        int m_lastTimeShoot;                            //  When was this gun last time shooting
        public int LastTimeShoot { get { return m_lastTimeShoot; } }

        float m_rotationTimeout;

        bool m_cannonMotorEndPlayed;

        //  Muzzle flash parameters, with random values at each shot
        float m_muzzleFlashLength;
        public float MuzzleFlashLength { get { return  m_muzzleFlashLength;}}
        float m_muzzleFlashRadius;
        public float MuzzleFlashRadius{ get { return m_muzzleFlashRadius; } }
		
        //  When gun fires too much, we start generating smokes at the muzzle
        int m_smokeLastTime;
        int m_smokesToGenerate;
        MyEntity3DSoundEmitter m_soundEmitterRotor;

        MyEntity m_barrel;

        MyParticleEffect m_smokeEffect;

        MyGunBase m_gunBase;

        List<HkWorld.HitInfo> m_hits = new List<HkWorld.HitInfo>();
        bool m_isShooting = false;

        protected override bool CheckIsWorking()
        {
            return ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) && base.CheckIsWorking();
        }

        private MyMultilineConveyorEndpoint m_conveyorEndpoint;
        private readonly Sync<bool> m_useConveyorSystem;
        private MyEntity[] m_shootIgnoreEntities;   // for projectiles to know which entities to ignore

        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get { return m_conveyorEndpoint; }
        }

        public void InitializeConveyorEndpoint()
        {
            m_conveyorEndpoint = new MyMultilineConveyorEndpoint(this);
            AddDebugRenderComponent(new MyDebugRenderComponentDrawConveyorEndpoint(m_conveyorEndpoint));
        }

        public override bool IsStationary()
        {
            return true;
        }

        public MySmallGatlingGun()
        {
            m_shootIgnoreEntities = new MyEntity[] { this };

#if XB1 // XB1_SYNC_NOREFLECTION
            m_useConveyorSystem = SyncType.CreateAndAddProp<bool>();
#endif // XB1
            CreateTerminalControls();

            m_rotationAngle = MyUtils.GetRandomRadian();
            m_lastTimeShoot = MyConstants.FAREST_TIME_IN_PAST;
            m_smokeLastTime = MyConstants.FAREST_TIME_IN_PAST;
            m_smokesToGenerate = 0;
            m_cannonMotorEndPlayed = true;
            m_rotationTimeout = (float)MyGatlingConstants.ROTATION_TIMEOUT + MyUtils.GetRandomFloat(-500, +500);

            m_soundEmitter = new MyEntity3DSoundEmitter(this, true);

#if XB1// XB1_SYNC_NOREFLECTION
            m_gunBase = new MyGunBase(SyncType);
#else // !XB1
            m_gunBase = new MyGunBase();         
#endif // !XB1

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;
            Render.NeedsDrawFromParent = true;

            Render = new MyRenderComponentSmallGatlingGun();
            AddDebugRenderComponent(new MyDebugRenderComponentSmallGatlingGun(this));

#if !XB1 // !XB1_SYNC_NOREFLECTION
            SyncType.Append(m_gunBase);
#endif // !XB1
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MySmallGatlingGun>())
                return;
            base.CreateTerminalControls();
            var useConvSystem = new MyTerminalControlOnOffSwitch<MySmallGatlingGun>("UseConveyor", MySpaceTexts.Terminal_UseConveyorSystem);
            useConvSystem.Getter = (x) => (x).UseConveyorSystem;
            useConvSystem.Setter = (x, v) => (x).UseConveyorSystem = v;
            useConvSystem.EnableToggleAction();
            MyTerminalControlFactory.AddControl(useConvSystem);
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            MyObjectBuilder_SmallGatlingGun weaponBuilder = (MyObjectBuilder_SmallGatlingGun)base.GetObjectBuilderCubeBlock(copy);
            weaponBuilder.Inventory = this.GetInventory().GetObjectBuilder();
            weaponBuilder.GunBase = m_gunBase.GetObjectBuilder();
            weaponBuilder.UseConveyorSystem = this.m_useConveyorSystem;
            return weaponBuilder;
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {         
            SyncFlag = true;
            var ob = objectBuilder as MyObjectBuilder_SmallGatlingGun;

            var weaponBlockDefinition = BlockDefinition as MyWeaponBlockDefinition;
            
            if (MyFakes.ENABLE_INVENTORY_FIX)
            {
                FixSingleInventory();
            }
            m_soundEmitterRotor = new MyEntity3DSoundEmitter(this);

            if (this.GetInventory() == null)
            {
                MyInventory inventory = null;
                if (weaponBlockDefinition != null)
                    inventory = new MyInventory(weaponBlockDefinition.InventoryMaxVolume, new Vector3(0.4f, 0.4f, 0.4f), MyInventoryFlags.CanReceive);
                else
                    inventory = new MyInventory(64.0f / 1000, new Vector3(0.4f, 0.4f, 0.4f), MyInventoryFlags.CanReceive);

                Components.Add<MyInventoryBase>(inventory);

                this.GetInventory().Init(ob.Inventory);
            }
            
            Debug.Assert(this.GetInventory().Owner == this, "Ownership was not set!");

            var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                weaponBlockDefinition.ResourceSinkGroup,
                MyEnergyConstants.MAX_REQUIRED_POWER_SHIP_GUN,
                () => ResourceSink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId));
            sinkComp.IsPoweredChanged += Receiver_IsPoweredChanged;
            ResourceSink = sinkComp;

            base.Init(objectBuilder, cubeGrid);
            
            m_gunBase.Init(ob.GunBase, BlockDefinition, this);

            GetBarrelAndMuzzle();
            //if (m_ammoPerShotConsumption == 0)
            //    m_ammoPerShotConsumption = (MyFixedPoint)((45.0f / (1000.0f / MyGatlingConstants.SHOT_INTERVAL_IN_MILISECONDS)) / m_gunBase.WeaponProperties.AmmoMagazineDefinition.Capacity);

		
			ResourceSink.Update();
			AddDebugRenderComponent(new MyDebugRenderComponentDrawPowerReciever(ResourceSink, this));

            m_useConveyorSystem.Value = ob.UseConveyorSystem;
        }

        protected override void OnInventoryComponentAdded(MyInventoryBase inventory)
        {
            base.OnInventoryComponentAdded(inventory);
            Debug.Assert(this.GetInventory() != null, "Added inventory to collector, but different type than MyInventory?! Check this.");
            if (this.GetInventory() != null)
            {
                this.GetInventory().ContentsChanged += AmmoInventory_ContentsChanged;
            }
        }

        protected override void OnInventoryComponentRemoved(MyInventoryBase inventory)
        {
            base.OnInventoryComponentRemoved(inventory);
            var removedInventory = inventory as MyInventory;
            Debug.Assert(removedInventory != null, "Removed inventory is not MyInventory type? Check this.");
            if (removedInventory != null)
            {
                removedInventory.ContentsChanged -= AmmoInventory_ContentsChanged;
            }
        }

        private void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
        }

        void AmmoInventory_ContentsChanged(MyInventoryBase obj)
        {
            m_gunBase.RefreshAmmunitionAmount();
        }

        protected override void Closing()
        {
            if (m_soundEmitter != null)
                m_soundEmitter.StopSound(true);
            if (m_soundEmitterRotor != null)
                m_soundEmitterRotor.StopSound(true);

            if (m_smokeEffect != null)
            {
                m_smokeEffect.Stop();
                m_smokeEffect = null;
            }

            base.Closing();
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

        protected override void WorldPositionChanged(object source)
        {
            base.WorldPositionChanged(source);
 
            m_gunBase.WorldMatrix = PositionComp.WorldMatrix;
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();
            MyEntitySubpart barrel;
            if (Subparts.TryGetValue("Barrel", out barrel))
            {
                m_barrel = barrel;
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
         
            Debug.Assert(PositionComp != null, "MySmallGatlingGun Cubegrid is null");
            if (PositionComp == null)
                return;

            //  Cannon is rotating while shoting. After that, it will slow-down.
            float normalizedRotationSpeed = 1.0f - MathHelper.Clamp((float)(MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimeShoot) / m_rotationTimeout, 0, 1);
            normalizedRotationSpeed = MathHelper.SmoothStep(0, 1, normalizedRotationSpeed);
            float rotationAngle = normalizedRotationSpeed * MyGatlingConstants.ROTATION_SPEED_PER_SECOND * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            if (rotationAngle != 0 && m_barrel != null)
            {
                Debug.Assert(m_barrel.PositionComp != null, "MySmallGatlingGun barrel PositionComp is null");
                if (m_barrel.PositionComp != null)
                m_barrel.PositionComp.LocalMatrix = Matrix.CreateRotationY(rotationAngle) * m_barrel.PositionComp.LocalMatrix;
            }

            //  Handle 'motor loop and motor end' cues
            if (m_cannonMotorEndPlayed == false && m_gunBase != null)
            {
                if (MySandboxGame.TotalGamePlayTimeInMilliseconds > m_lastTimeShoot + m_gunBase.ReleaseTimeAfterFire)
                {
                    //  Stop 'shooting loop' cue
                    StopLoopSound();

                    m_cannonMotorEndPlayed = true;
                }
            }

            //  If gun fires too much, we start generating smokes at the muzzle
            /*
            if ((MySandboxGame.TotalGamePlayTimeInMilliseconds - m_smokeLastTime) >= (MyGatlingConstants.SMOKES_INTERVAL_IN_MILISECONDS))
            {
                m_smokeLastTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

                SmokesToGenerateDecrease();

                if (m_smokesToGenerate > 0 && m_smokeEffect == null)
                {
                    if (MySector.MainCamera.GetDistanceFromPoint(PositionComp.GetPosition()) < 150)
                    {
                        if (MyParticlesManager.TryCreateParticleEffect((int)MyParticleEffectsIDEnum.Smoke_Autocannon, out m_smokeEffect))
                        {
                            m_smokeEffect.WorldMatrix = PositionComp.WorldMatrix;
                            m_smokeEffect.OnDelete += new EventHandler(m_smokeEffect_OnDelete);
                        }
                    }
                }
            }*/

            if (m_smokeEffect != null)
            {
                m_smokeEffect.WorldMatrix = MatrixD.CreateTranslation(m_gunBase.GetMuzzleWorldPosition());
                m_smokeEffect.UserBirthMultiplier = m_smokesToGenerate;
            }
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();

            if (MySession.Static.SurvivalMode && Sync.IsServer && IsWorking && m_useConveyorSystem && this.GetInventory().VolumeFillFactor < 0.6f)
            {
                var definition = m_gunBase.CurrentAmmoMagazineDefinition; //MyDefinitionManager.Static.GetPhysicalItemDefinition(m_currentAmmoMagazineId);
                if (definition != null)
                {
                    var maxNum = MyFixedPoint.Floor((this.GetInventory().MaxVolume - this.GetInventory().CurrentVolume) * (1.0f / definition.Volume));
                    if (maxNum == 0) 
                        return;
                    MyGridConveyorSystem.ItemPullRequest(this, this.GetInventory(), OwnerId, m_gunBase.CurrentAmmoMagazineId, maxNum);
                }
            }
        }
   
        void m_smokeEffect_OnDelete(object sender, EventArgs e)
        {
            m_smokeEffect = null;
        }

        void ClampSmokesToGenerate()
        {
            m_smokesToGenerate = MyUtils.GetClampInt(m_smokesToGenerate, 0, MyGatlingConstants.SMOKES_MAX);
        }

        void SmokesToGenerateIncrease()
        {
            m_smokesToGenerate += MyGatlingConstants.SMOKE_INCREASE_PER_SHOT;
            ClampSmokesToGenerate();
        }

        void SmokesToGenerateDecrease()
        {
            m_smokesToGenerate -= MyGatlingConstants.SMOKE_DECREASE;
            ClampSmokesToGenerate();
        }

        #region IMyGunObject

        public float BackkickForcePerSecond
        {
            get 
            {
                return m_gunBase.BackkickForcePerSecond;
            }
        }

        public float ShakeAmount { get; protected set; }

        public float ProjectileCountMultiplier
        {
            get
            {
                return 0; //MyGatlingConstants.REAL_SHOTS_PER_SECOND / (1000.0f / MyGatlingConstants.SHOT_INTERVAL_IN_MILISECONDS);
            }
        }

        public bool EnabledInWorldRules { get { return MySession.Static.WeaponsEnabled; } }

        public MyDefinitionId DefinitionId
        {   
            get { return BlockDefinition.Id; }
        }

        public Vector3 DirectionToTarget(Vector3D target)
        {
            return PositionComp.WorldMatrix.Forward;
        }

        public override bool CanShoot(MyShootActionEnum action, long shooter, out MyGunStatusEnum status)
        {
            status = MyGunStatusEnum.OK;

            if (action != MyShootActionEnum.PrimaryAction)
            {
                status = MyGunStatusEnum.Failed;
                return false;
            }

            if (Parent.Physics == null)
            {
                status = MyGunStatusEnum.Failed;
                return false;
            }

            if (!m_gunBase.HasAmmoMagazines)
            {
                status = MyGunStatusEnum.Failed;
                return false;
            }

            if ((MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimeShoot) < m_gunBase.ShootIntervalInMiliseconds)
            {
                status = MyGunStatusEnum.Cooldown;
                return false;
            }

            if (!HasPlayerAccess(shooter))
            {
                status = MyGunStatusEnum.AccessDenied;
                return false;
            }

            if (!ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId))
            {
                status = MyGunStatusEnum.OutOfPower;
                return false;
            }

            if (!IsFunctional)
            {
                status = MyGunStatusEnum.NotFunctional;
                return false;
            }

            if (!Enabled)
            {
                status = MyGunStatusEnum.Disabled;
                return false;
            }

            if (!MySession.Static.CreativeMode &&  !m_gunBase.HasEnoughAmmunition())//Inventory.GetItemAmount(m_currentAmmoMagazineId) < m_ammoPerShotConsumption) 
            {
                status = MyGunStatusEnum.OutOfAmmo;
                return false;
            }
            return true;
        }

        public void Shoot(MyShootActionEnum action, Vector3 direction, Vector3D? overrideWeaponPos, string gunAction)
        {            
            // Don't shoot when the grid doesn't have physics.
            if (Parent.Physics == null)
                return;

            //  Angle of muzzle flash particle
            m_muzzleFlashLength = MyUtils.GetRandomFloat(3, 4) * CubeGrid.GridSize ;// *m_barrel.GetMuzzleSize();
            m_muzzleFlashRadius = MyUtils.GetRandomFloat(0.9f, 1.5f) * CubeGrid.GridSize;// *m_barrel.GetMuzzleSize();

            //  Increase count of smokes to draw
            SmokesToGenerateIncrease();

            // Plays sound
            StartLoopSound();
         
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyAutocannonGun.Shot add projectile");

            m_gunBase.Shoot(Parent.Physics.LinearVelocity);
            m_gunBase.ConsumeAmmo();        
            //VRageRender.MyRenderProxy.DebugDrawSphere(GetPosition(), 0.1f, Vector3.One, 1, false);

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            if (BackkickForcePerSecond > 0)
            {
                CubeGrid.Physics.AddForce(
                    MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE,
                    -direction * BackkickForcePerSecond,
                    PositionComp.GetPosition(),
                    null);
            }

            m_cannonMotorEndPlayed = false;
            m_lastTimeShoot = MySandboxGame.TotalGamePlayTimeInMilliseconds;
        }

        public void EndShoot(MyShootActionEnum action)
        { }

        public bool IsShooting
        {
            get { return MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimeShoot < m_gunBase.ShootIntervalInMiliseconds * 2; }
        }

        public int ShootDirectionUpdateTime
        {
            get { return 0; }
        }

        public void BeginFailReaction(MyShootActionEnum action, MyGunStatusEnum status)
        {
            if (status == MyGunStatusEnum.OutOfAmmo && !MySession.Static.CreativeMode)
            {
                MyFixedPoint newAmount = this.GetInventory().GetItemAmount(m_gunBase.CurrentAmmoMagazineId);

                if (newAmount < MyGunBase.AMMO_PER_SHOOT)
                    StartNoAmmoSound();
            }
        }

        public void BeginFailReactionLocal(MyShootActionEnum action, MyGunStatusEnum status)
        { }

        public void ShootFailReactionLocal(MyShootActionEnum action, MyGunStatusEnum status)
        { }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
        }

        public void OnControlAcquired(MyCharacter owner)
        {

        }

        public void OnControlReleased()
        {

        }

        public void DrawHud(IMyCameraController camera, long playerId)
        {
            ProfilerShort.Begin("Can shoot");
            MyGunStatusEnum status;
            CanShoot(MyShootActionEnum.PrimaryAction, playerId, out status);
            ProfilerShort.End();

            if (status == MyGunStatusEnum.OK || status == MyGunStatusEnum.Cooldown)
            {
                var from = PositionComp.GetPosition() + PositionComp.WorldMatrix.Forward;
                var to = PositionComp.GetPosition() + 50 * PositionComp.WorldMatrix.Forward;

                Vector3D target = Vector3D.Zero;
                if (MyHudCrosshair.GetTarget(from, to, ref target))
                {
                    float distance = (float)Vector3D.Distance(MySector.MainCamera.Position, target);

                    MyTransparentGeometry.AddBillboardOriented(
                        "RedDot",
                        Vector4.One,
                        target,
                        MySector.MainCamera.LeftVector,
                        MySector.MainCamera.UpVector,
                          distance / 300.0f);
                }
            }
        }

        #endregion

        private void UpdatePower()
        {
			ResourceSink.Update();
            if (!ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId))
                StopLoopSound();
        }

        public void StartNoAmmoSound()
        {
            m_gunBase.StartNoAmmoSound(m_soundEmitter);
        }

        private void StopLoopSound()
        {
            if (m_soundEmitter != null && m_soundEmitter.IsPlaying && m_soundEmitter.Loop)
                m_soundEmitter.StopSound(true);
            if (m_soundEmitterRotor != null && m_soundEmitterRotor.IsPlaying && m_soundEmitterRotor.Loop)
            {
                m_soundEmitterRotor.StopSound(true);
                m_soundEmitterRotor.PlaySound(m_gunBase.SecondarySound, skipToEnd: true);
            }
        }

        private void StartLoopSound()
        {
            m_gunBase.StartShootSound(m_soundEmitter);
            if (m_soundEmitterRotor != null && m_gunBase.SecondarySound != MySoundPair.Empty && (m_soundEmitterRotor.IsPlaying == false || m_soundEmitterRotor.Loop == false))
            {
                if (m_soundEmitterRotor.IsPlaying)
                    m_soundEmitterRotor.StopSound(true);
                m_soundEmitterRotor.PlaySound(m_gunBase.SecondarySound, true);
            }
        }

        #region Inventory
                
        bool UseConveyorSystem
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

        #endregion

        public int GetAmmunitionAmount()
        {
            return m_gunBase.GetTotalAmmunitionAmount();
        }

        public MyGunBase GunBase
        {
            get { return m_gunBase; }
        }

        private void GetBarrelAndMuzzle()
        {
            MyEntitySubpart barrel;
            if (Subparts.TryGetValue("Barrel", out barrel))
            {
                m_barrel = barrel;
            }

            var model = VRage.Game.Models.MyModels.GetModelOnlyDummies(BlockDefinition.Model);
            m_gunBase.LoadDummies(model.Dummies);

            // backward compatibility for models without dummies or old dummies
            if (!m_gunBase.HasDummies)
            {
                if (model.Dummies.ContainsKey("Muzzle"))
                {
                    m_gunBase.AddMuzzleMatrix(MyAmmoType.HighSpeed, model.Dummies["Muzzle"].Matrix);
                }
                else
                {
                    Matrix muzzleMatrix = Matrix.CreateTranslation(new Vector3(0, 0, -1));
                    m_gunBase.AddMuzzleMatrix(MyAmmoType.HighSpeed, muzzleMatrix);
                }
            }
        }

        #region IMyGunBaseUser

        MyEntity[] IMyGunBaseUser.IgnoreEntities
        {
            get { return m_shootIgnoreEntities; }
        }

        MyEntity IMyGunBaseUser.Weapon
        {
            get { return Parent; }
        }

        MyEntity IMyGunBaseUser.Owner
        {
            get { return Parent; }
        }

        IMyMissileGunObject IMyGunBaseUser.Launcher
        {
            get { return null; }
        }

        MyInventory IMyGunBaseUser.AmmoInventory
        {
            get { return this.GetInventory(); }
        }

        MyDefinitionId IMyGunBaseUser.PhysicalItemId
        {
            get { return new MyDefinitionId(); }
        }

        MyInventory IMyGunBaseUser.WeaponInventory
        {
            get { return null; }
        }

        long IMyGunBaseUser.OwnerId
        {
            get { return this.OwnerId; }
        }

        string IMyGunBaseUser.ConstraintDisplayName
        {
            get { return BlockDefinition.DisplayNameText; }
        }

        #endregion

        bool Sandbox.ModAPI.Ingame.IMySmallGatlingGun.UseConveyorSystem
        {
            get
            {
                return m_useConveyorSystem;
            }
        }

        public override bool CanOperate()
        {
            return CheckIsWorking();
        }

        public override void ShootFromTerminal(Vector3 direction)
        {
            Shoot(MyShootActionEnum.PrimaryAction, direction, null, null);
        }

        public void UpdateSoundEmitter()
        {
            if (m_soundEmitter != null)
                m_soundEmitter.Update();
        }

        #region IMyInventoryOwner

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

        public Sandbox.Game.GameSystems.Conveyors.PullInformation GetPullInformation()
        {
            Sandbox.Game.GameSystems.Conveyors.PullInformation pullInformation = new Sandbox.Game.GameSystems.Conveyors.PullInformation();
            pullInformation.Inventory = this.GetInventory();
            pullInformation.OwnerID = OwnerId;
            pullInformation.Constraint = pullInformation.Inventory.Constraint;
            return pullInformation;
    }

        public Sandbox.Game.GameSystems.Conveyors.PullInformation GetPushInformation()
        {
            return null;
        }

        #endregion
    }
}