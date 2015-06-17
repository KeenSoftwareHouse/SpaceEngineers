using System;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Graphics.TransparentGeometry.Particles;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.World;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.Gui;
using System.Collections.Generic;
using Havok;
using Sandbox.Graphics.TransparentGeometry;
using VRageRender;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;

using Sandbox.Graphics.GUI;
using System.Diagnostics;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Conveyors;
using System.Reflection;
using Sandbox.Common;
using MyGuiConstants = Sandbox.Graphics.GUI.MyGuiConstants;
using VRage;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.Components;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.Localization;
using VRage.ModAPI;
using VRage.Components;

namespace Sandbox.Game.Weapons
{
    [MyCubeBlockType(typeof(MyObjectBuilder_SmallGatlingGun))]
    class MySmallGatlingGun : MyUserControllableGun, IMyGunObject<MyGunBase>, IMyPowerConsumer, IMyInventoryOwner, IMyConveyorEndpointBlock, IMyGunBaseUser, IMySmallGatlingGun
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
		public bool IsDeconstructor { get { return false; } }

        //  When gun fires too much, we start generating smokes at the muzzle
        int m_smokeLastTime;
        int m_smokesToGenerate;

        MyEntity m_barrel;
        //  Matrix m_barrelMatrix;

        MyParticleEffect m_smokeEffect;

        MyGunBase m_gunBase;

        List<HkWorld.HitInfo> m_hits = new List<HkWorld.HitInfo>();
        bool m_isShooting = false;

        private MyInventory m_ammoInventory;

        public MyPowerReceiver PowerReceiver
        {
            get;
            private set;
        }

        protected override bool CheckIsWorking()
        {
            return PowerReceiver.IsPowered && base.CheckIsWorking();
        }

        private MyMultilineConveyorEndpoint m_conveyorEndpoint;
        private bool m_useConveyorSystem;
        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get { return m_conveyorEndpoint; }
        }

        public void InitializeConveyorEndpoint()
        {
            m_conveyorEndpoint = new MyMultilineConveyorEndpoint(this);
            AddDebugRenderComponent(new MyDebugRenderComponentDrawConveyorEndpoint(m_conveyorEndpoint));
        }

        static MySmallGatlingGun()
        {
            var useConvSystem = new MyTerminalControlOnOffSwitch<MySmallGatlingGun>("UseConveyor", MySpaceTexts.Terminal_UseConveyorSystem);
            useConvSystem.Getter = (x) => (x as IMyInventoryOwner).UseConveyorSystem;
            useConvSystem.Setter = (x, v) => MySyncConveyors.SendChangeUseConveyorSystemRequest(x.EntityId, v);
            useConvSystem.EnableToggleAction();
            MyTerminalControlFactory.AddControl(useConvSystem);     
        }

        public MySmallGatlingGun()
        {
            m_rotationAngle = MyUtils.GetRandomRadian();
            m_lastTimeShoot = MyConstants.FAREST_TIME_IN_PAST;
            m_smokeLastTime = MyConstants.FAREST_TIME_IN_PAST;
            m_smokesToGenerate = 0;
            m_cannonMotorEndPlayed = true;
            m_rotationTimeout = (float)MyGatlingConstants.ROTATION_TIMEOUT + MyUtils.GetRandomFloat(-500, +500);

            m_soundEmitter = new MyEntity3DSoundEmitter(this);

            m_gunBase = new MyGunBase();         

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;
            Render.NeedsDrawFromParent = true;

            Render = new MyRenderComponentSmallGatlingGun();
            AddDebugRenderComponent(new MyDebugRenderComponentSmallGatlingGun(this));
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            MyObjectBuilder_SmallGatlingGun weaponBuilder = (MyObjectBuilder_SmallGatlingGun)base.GetObjectBuilderCubeBlock(copy);
            weaponBuilder.Inventory = m_ammoInventory.GetObjectBuilder();
            weaponBuilder.GunBase = m_gunBase.GetObjectBuilder();
            return weaponBuilder;
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {         
            SyncFlag = true;
            var ob = objectBuilder as MyObjectBuilder_SmallGatlingGun;

            var weaponBlockDefinition = BlockDefinition as MyWeaponBlockDefinition;
            if (weaponBlockDefinition != null)
                m_ammoInventory = new MyInventory(weaponBlockDefinition.InventoryMaxVolume, new Vector3(0.4f, 0.4f, 0.4f), MyInventoryFlags.CanReceive, this);
            else
                m_ammoInventory = new MyInventory(64.0f / 1000, new Vector3(0.4f, 0.4f, 0.4f), MyInventoryFlags.CanReceive, this);

            base.Init(objectBuilder, cubeGrid);

            m_ammoInventory.Init(ob.Inventory);
            m_gunBase.Init(ob.GunBase, BlockDefinition, this);

            m_ammoInventory.ContentsChanged += AmmoInventory_ContentsChanged;

            GetBarrelAndMuzzle();
            //if (m_ammoPerShotConsumption == 0)
            //    m_ammoPerShotConsumption = (MyFixedPoint)((45.0f / (1000.0f / MyGatlingConstants.SHOT_INTERVAL_IN_MILISECONDS)) / m_gunBase.WeaponProperties.AmmoMagazineDefinition.Capacity);

            m_useConveyorSystem = ob.UseConveyorSystem;
            PowerReceiver = new MyPowerReceiver(MyConsumerGroupEnum.Defense, false, MyEnergyConstants.MAX_REQUIRED_POWER_SHIP_GUN, () => PowerReceiver.MaxRequiredInput);
            PowerReceiver.IsPoweredChanged += Receiver_IsPoweredChanged;
            PowerReceiver.Update();
            AddDebugRenderComponent(new MyDebugRenderComponentDrawPowerReciever(PowerReceiver, this));
        }

        private void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
        }

        void AmmoInventory_ContentsChanged(MyInventory obj)
        {
            m_gunBase.RefreshAmmunitionAmount();
        }

        protected override void Closing()
        {
            m_soundEmitter.StopSound(true);

            if (m_smokeEffect != null)
            {
                m_smokeEffect.Stop();
                m_smokeEffect = null;
            }

            base.Closing();
        }

        public override void OnRemovedByCubeBuilder()
        {
            ReleaseInventory(m_ammoInventory);
            base.OnRemovedByCubeBuilder();
        }

        public override void OnDestroy()
        {
            ReleaseInventory(m_ammoInventory, true);
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
         
            //  Cannon is rotating while shoting. After that, it will slow-down.
            float normalizedRotationSpeed = 1.0f - MathHelper.Clamp((float)(MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimeShoot) / m_rotationTimeout, 0, 1);
            normalizedRotationSpeed = MathHelper.SmoothStep(0, 1, normalizedRotationSpeed);
            float rotationAngle = normalizedRotationSpeed * MyGatlingConstants.ROTATION_SPEED_PER_SECOND * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            Matrix worldMatrix = this.PositionComp.WorldMatrix;

            if (rotationAngle != 0 && m_barrel != null)
                m_barrel.PositionComp.LocalMatrix = Matrix.CreateRotationY(rotationAngle) * m_barrel.PositionComp.LocalMatrix;

            //  Handle 'motor loop and motor end' cues
            if (m_cannonMotorEndPlayed == false)
            {
                if (MySandboxGame.TotalGamePlayTimeInMilliseconds > m_lastTimeShoot + m_gunBase.ReleaseTimeAfterFire)
                {
                    //  Stop 'shooting loop' cue
                    StopLoopSound();

                    m_cannonMotorEndPlayed = true;
                }
            }

            //  If gun fires too much, we start generating smokes at the muzzle
            if ((MySandboxGame.TotalGamePlayTimeInMilliseconds - m_smokeLastTime) >= (MyGatlingConstants.SMOKES_INTERVAL_IN_MILISECONDS))
            {
                m_smokeLastTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

                SmokesToGenerateDecrease();

                if (m_smokesToGenerate > 0 && m_smokeEffect == null)
                {
                    if (MySector.MainCamera.GetDistanceWithFOV(PositionComp.GetPosition()) < 150)
                    {
                        if (MyParticlesManager.TryCreateParticleEffect((int)MyParticleEffectsIDEnum.Smoke_Autocannon, out m_smokeEffect))
                        {
                            m_smokeEffect.WorldMatrix = PositionComp.WorldMatrix;
                            m_smokeEffect.OnDelete += new EventHandler(m_smokeEffect_OnDelete);
                        }
                    }
                }
            }

            if (m_smokeEffect != null)
            {
                m_smokeEffect.WorldMatrix = MatrixD.CreateTranslation(m_gunBase.GetMuzzleWorldPosition());
                m_smokeEffect.UserBirthMultiplier = m_smokesToGenerate;
            }
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();

            if (MySession.Static.SurvivalMode && Sync.IsServer && IsWorking && m_useConveyorSystem && m_ammoInventory.VolumeFillFactor < 0.6f)
            {
                var definition = m_gunBase.CurrentAmmoMagazineDefinition; //MyDefinitionManager.Static.GetPhysicalItemDefinition(m_currentAmmoMagazineId);
                if (definition != null)
                {
                    var maxNum = MyFixedPoint.Floor((m_ammoInventory.MaxVolume - m_ammoInventory.CurrentVolume) * (1.0f / definition.Volume));
                    if (maxNum == 0) 
                        return;
                    MyGridConveyorSystem.ItemPullRequest(this, m_ammoInventory, OwnerId, m_gunBase.CurrentAmmoMagazineId, maxNum);
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

            if (!PowerReceiver.IsPowered)
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

            if (!MySession.Static.CreativeMode &&  !m_gunBase.HasEnoughAmmunition())//m_ammoInventory.GetItemAmount(m_currentAmmoMagazineId) < m_ammoPerShotConsumption) 
            {
                status = MyGunStatusEnum.OutOfAmmo;
                return false;
            }
            return true;
        }

        public void Shoot(MyShootActionEnum action, Vector3 direction)
        {            
            //  Angle of muzzle flash particle
            m_muzzleFlashLength = MyUtils.GetRandomFloat(3, 4);// *m_barrel.GetMuzzleSize();
            m_muzzleFlashRadius = MyUtils.GetRandomFloat(0.9f, 1.5f);// *m_barrel.GetMuzzleSize();

            //  Increase count of smokes to draw
            SmokesToGenerateIncrease();

            // Plays sound
            StartLoopSound();
         
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyAutocannonGun.Shot add projectile");

            m_gunBase.Shoot(Parent.Physics.LinearVelocity);

            if (!MySession.Static.CreativeMode)
            {
                m_gunBase.ConsumeAmmo();        
            }
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
                MyFixedPoint newAmount = m_ammoInventory.GetItemAmount(m_gunBase.CurrentAmmoMagazineId);

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
                var to = PositionComp.GetPosition() + 1000 * PositionComp.WorldMatrix.Forward;

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
            PowerReceiver.Update();
            if (!PowerReceiver.IsPowered)
                StopLoopSound();
        }

        public void StartNoAmmoSound()
        {
            m_gunBase.StartNoAmmoSound(m_soundEmitter);
        }

        private void StopLoopSound()
        {
            m_soundEmitter.StopSound(true);
        }

        private void StartLoopSound()
        {
            m_gunBase.StartShootSound(m_soundEmitter);
        }

        #region IMyInventoryOwner

        public int InventoryCount
        {
            get { return 1; }
        }

        String IMyInventoryOwner.DisplayNameText
        {
            get { return CustomName.ToString(); }
        }

        public MyInventoryOwnerTypeEnum InventoryOwnerType
        {
            get { return MyInventoryOwnerTypeEnum.System; }
        }

        public MyInventory GetInventory(int id)
        {
            return m_ammoInventory;
        }

        bool IMyInventoryOwner.UseConveyorSystem
        {
            get
            {
                return m_useConveyorSystem;
            }
            set
            {
                m_useConveyorSystem = value;
            }
        }

        bool ModAPI.Interfaces.IMyInventoryOwner.UseConveyorSystem
        {
            get
            {
                return (this as IMyInventoryOwner).UseConveyorSystem;
            }
            set
            {
                (this as IMyInventoryOwner).UseConveyorSystem = value;
            }
        }

        Sandbox.ModAPI.Interfaces.IMyInventory Sandbox.ModAPI.Interfaces.IMyInventoryOwner.GetInventory(int index)
        {
            return GetInventory(index);
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

            var model = Engine.Models.MyModels.GetModelOnlyDummies(BlockDefinition.Model);
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

        MyEntity IMyGunBaseUser.IgnoreEntity
        {
            get { return this; }
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
            get { return m_ammoInventory; }
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
                return (this as IMyInventoryOwner).UseConveyorSystem;
            }
        }

        public override bool CanOperate()
        {
            return CheckIsWorking();
        }

        public override void ShootFromTerminal(Vector3 direction)
        {
            Shoot(MyShootActionEnum.PrimaryAction, direction);
        }
    }
}