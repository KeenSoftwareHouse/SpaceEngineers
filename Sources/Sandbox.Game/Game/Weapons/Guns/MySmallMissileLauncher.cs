using System;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.Electricity;
using VRageMath;
using Sandbox.Game.Multiplayer;
using Havok;
using System.Collections.Generic;
using Sandbox.Engine.Physics;
using Sandbox.Game.World;
using Sandbox.Engine.Platform.VideoMode;
using Sandbox.Graphics.GUI;
using Sandbox.Game.Gui;
using VRageRender;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;

using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.GameSystems;
using System.Reflection;
using Sandbox.Common;
using System.Text;
using MyGuiConstants = Sandbox.Graphics.GUI.MyGuiConstants;
using Sandbox.ModAPI.Interfaces;
using System.Diagnostics;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.Localization;
using VRage.ModAPI;
using VRage.Utils;
using VRage;
using VRage.Game.Entity;
using VRage.Game;
using VRage.ModAPI.Ingame;

namespace Sandbox.Game.Weapons
{
    [MyCubeBlockType(typeof(MyObjectBuilder_SmallMissileLauncher))]
    class MySmallMissileLauncher : MyUserControllableGun, IMyMissileGunObject, IMyInventoryOwner, IMyConveyorEndpointBlock, IMyGunBaseUser, IMySmallMissileLauncher
    {
        protected int m_lastTimeShoot;        //  When was this gun last time shooting

        MyGunBase m_gunBase;

		bool m_shoot = false;
        Vector3 m_shootDirection;
        private int m_currentBarrel;

        protected override bool CheckIsWorking()
        {
			return ResourceSink.IsPowered && base.CheckIsWorking();
        }

        private MyMultilineConveyorEndpoint m_endpoint;
        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get { return m_endpoint; }
        }

        public void InitializeConveyorEndpoint()
        {
            m_endpoint = new MyMultilineConveyorEndpoint(this);
            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_endpoint));
        }

        static MySmallMissileLauncher()
        {
            var useConveyor = new MyTerminalControlOnOffSwitch<MySmallMissileLauncher>("UseConveyor", MySpaceTexts.Terminal_UseConveyorSystem);
            useConveyor.Getter = (x) => (x).UseConveyorSystem;
            useConveyor.Setter = (x, v) => (x).UseConveyorSystem = v;
            useConveyor.Visible = (x) => x.CubeGrid.GridSizeEnum == MyCubeSize.Large; // Only large missile launchers can use conveyor system
            useConveyor.EnableToggleAction();
            MyTerminalControlFactory.AddControl(useConveyor);
        }

        public MySmallMissileLauncher()
        {
            m_gunBase = new MyGunBase();
            m_soundEmitter = new MyEntity3DSoundEmitter(this);
            m_useConveyorSystem.Value = true;
            SyncType.Append(m_gunBase);
        }

        //[TerminalValues(MySpaceTexts.SwitchText_On, MySpaceTexts.SwitchText_Off)]
        //[Terminal(2, MyPropertyDisplayEnum.Switch, MySpaceTexts.Terminal_UseConveyorSystem, MySpaceTexts.Blank)]
        //[Obfuscation(Feature = Obfuscator.NoRename, Exclude = true)]
        //public MyStringId UseConveyorSystemGui
        //{
        //    get { return m_useConveyorSystem ? MySpaceTexts.SwitchText_On : MySpaceTexts.SwitchText_Off; }
        //    set
        //    {
        //        if (m_useConveyorSystem != (value == MySpaceTexts.SwitchText_On))
        //        {
        //            m_useConveyorSystem = (value == MySpaceTexts.SwitchText_On);
        //            OnPropertiesChanged();
        //        }
        //    }
        //}
        //[TerminalValueSetter(2)]
        //public void RequestUseConveyorSystemChange(MyStringId newVal)
        //{
        //    MySyncConveyors.SendChangeUseConveyorSystemRequest(EntityId, newVal == MySpaceTexts.SwitchText_On);
        //}

        public override void Init(MyObjectBuilder_CubeBlock builder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;
            var ob = builder as MyObjectBuilder_SmallMissileLauncher;
           
 
            MyStringHash resourceSinkGroup;
            var weaponBlockDefinition = BlockDefinition as MyWeaponBlockDefinition;
            if (weaponBlockDefinition != null && this.GetInventory() == null) 
            {
                Components.Add<MyInventoryBase>(new MyInventory(weaponBlockDefinition.InventoryMaxVolume, new Vector3(1.2f, 0.98f, 0.98f), MyInventoryFlags.CanReceive, this));               
                resourceSinkGroup = weaponBlockDefinition.ResourceSinkGroup;
            }
            else
            {
                if (this.GetInventory() == null) // this could be already inicialized object builder
                {
                    if (cubeGrid.GridSizeEnum == MyCubeSize.Small)
                        Components.Add<MyInventoryBase>(new MyInventory(240.0f / 1000, new Vector3(1.2f, 0.45f, 0.45f), MyInventoryFlags.CanReceive, this)); // 4 missiles
                    else
                        Components.Add<MyInventoryBase>(new MyInventory(1140.0f / 1000, new Vector3(1.2f, 0.98f, 0.98f), MyInventoryFlags.CanReceive, this)); // 19 missiles
                }

                resourceSinkGroup = MyStringHash.GetOrCompute("Defense");
            }
            Debug.Assert(this.GetInventory().Owner == this, "Ownership was not set!");
            
            var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                resourceSinkGroup,
                MyEnergyConstants.MAX_REQUIRED_POWER_SHIP_GUN,
                () => (Enabled && IsFunctional) ? ResourceSink.MaxRequiredInput : 0.0f);
            ResourceSink = sinkComp;
            ResourceSink.IsPoweredChanged += Receiver_IsPoweredChanged;

            base.Init(builder, cubeGrid);
            
            if (MyFakes.ENABLE_INVENTORY_FIX)
            {
                FixSingleInventory();
            }
            ResourceSink.Update();
            this.GetInventory().Init(ob.Inventory);
            m_gunBase.Init(ob.GunBase, BlockDefinition, this);
	
			AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawPowerReciever(ResourceSink, this));

            m_useConveyorSystem.Value = ob.UseConveyorSystem;

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            LoadDummies();

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        protected override void OnInventoryComponentAdded(MyInventoryBase inventory)
        {
            base.OnInventoryComponentAdded(inventory);
            Debug.Assert(this.GetInventory() != null, "Added inventory to collector, but different type than MyInventory?! Check this.");
            if (this.GetInventory() != null)
            {
                this.GetInventory().ContentsChanged += m_ammoInventory_ContentsChanged;
            }
        }

        protected override void OnInventoryComponentRemoved(MyInventoryBase inventory)
        {
            base.OnInventoryComponentRemoved(inventory);
            var removedInventory = inventory as MyInventory;
            Debug.Assert(removedInventory != null, "Removed inventory is not MyInventory type? Check this.");
            if (removedInventory != null)
            {
                removedInventory.ContentsChanged -= m_ammoInventory_ContentsChanged;
            }
        }

        private void LoadDummies()
        {
            var finalModel = VRage.Game.Models.MyModels.GetModelOnlyDummies(BlockDefinition.Model);
            m_gunBase.LoadDummies(finalModel.Dummies);

            // backward compatibility for models without dummies or old dummies
            if (!m_gunBase.HasDummies)
            {
                foreach (var dummy in finalModel.Dummies)
                {
                    if (dummy.Key.ToLower().Contains("barrel"))
                    {
                        m_gunBase.AddMuzzleMatrix(MyAmmoType.Missile, dummy.Value.Matrix);
                    }
                }
            }
        }

        private void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
        }

        void m_ammoInventory_ContentsChanged(MyInventoryBase obj)
        {
            m_gunBase.RefreshAmmunitionAmount();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var builder = (MyObjectBuilder_SmallMissileLauncher)base.GetObjectBuilderCubeBlock(copy);
            builder.Inventory = this.GetInventory().GetObjectBuilder();
            builder.UseConveyorSystem = m_useConveyorSystem;
            builder.GunBase = m_gunBase.GetObjectBuilder();
            return builder;
        }

        protected override void OnEnabledChanged()
        {
            base.OnEnabledChanged();
			ResourceSink.Update();
        }

        void ComponentStack_IsFunctionalChanged()
        {
			ResourceSink.Update();
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

        Vector3 GetSmokePosition()
        {
            return m_gunBase.GetMuzzleWorldPosition() - WorldMatrix.Forward * 0.5f;
        }

        #region IMyGunObject

        public float BackkickForcePerSecond
        {
            get { return m_gunBase.BackkickForcePerSecond; }
        }

        public float ShakeAmount { get; protected set; }

        public void OnControlAcquired(MyCharacter owner)
        {

        }

        public void OnControlReleased()
        {

        }

        public bool EnabledInWorldRules { get { return MySession.Static.WeaponsEnabled; } }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();

            if (Sync.IsServer && IsFunctional && UseConveyorSystem)
            {
                if (MySession.Static.SurvivalMode && this.GetInventory().VolumeFillFactor < 0.5f)
                {
                    MyGridConveyorSystem.ItemPullRequest(this, this.GetInventory(), OwnerId, m_gunBase.CurrentAmmoMagazineId, 1);
                }
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (m_shoot)
            {
                ShootMissile();
            }

            m_shoot = false;
            NeedsUpdate &= ~MyEntityUpdateEnum.NONE;
        }

        public bool Zoom(bool newKeyPress)
        {
            return false;
        }

        public void DrawHud(IMyCameraController camera, long playerId)
        {
            MyGunStatusEnum status;
            CanShoot(MyShootActionEnum.PrimaryAction, playerId, out status);

            if (status == MyGunStatusEnum.OK || status == MyGunStatusEnum.Cooldown)
            {
                var matrix = m_gunBase.GetMuzzleWorldMatrix();
                var from = matrix.Translation;
                var to = from + 50 * matrix.Forward;

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

        public MyDefinitionId DefinitionId
        {       
            get { return BlockDefinition.Id; }
        }

        #endregion

        private void StartSound(MySoundPair cueEnum)
        {
            m_gunBase.StartShootSound(m_soundEmitter);
        }

        protected override void Closing()
        {
            if (m_soundEmitter != null)
                m_soundEmitter.StopSound(true);
            base.Closing();
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source); 
        }

        #region Inventory
        
        protected Sync<bool> m_useConveyorSystem;
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

        #endregion

        public int GetAmmunitionAmount()
        {
            return m_gunBase.GetTotalAmmunitionAmount();
        }

        public Vector3 DirectionToTarget(Vector3D target)
        {
            return WorldMatrix.Forward;
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

            if ((MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimeShoot) < m_gunBase.ShootIntervalInMiliseconds / (CubeGrid.GridSizeEnum == MyCubeSize.Small ? 1 : 4))
            {
                status = MyGunStatusEnum.Cooldown;
                return false;
            }

            if (!HasPlayerAccess(shooter))
            {
                status = MyGunStatusEnum.AccessDenied;
                return false;
            }
			if (!ResourceSink.IsPowered)
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
            if (!MySession.Static.CreativeMode && !m_gunBase.HasEnoughAmmunition())
            {
                status = MyGunStatusEnum.OutOfAmmo;
                return false;
            }
            return true;
        }

        public virtual void Shoot(MyShootActionEnum action, Vector3 direction, string gunAction)
        {         
            m_shoot = true;
            m_shootDirection = direction;
            m_lastTimeShoot = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            m_gunBase.ConsumeAmmo();

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        public void EndShoot(MyShootActionEnum action)
        { }

        public bool IsShooting
        {
            get
            {
                return (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimeShoot) < m_gunBase.ShootIntervalInMiliseconds;
            }
        }

        public int ShootDirectionUpdateTime
        {
            get { return 0; }
        }

        public void BeginFailReaction(MyShootActionEnum action, MyGunStatusEnum status)
        {
            if (status == MyGunStatusEnum.OutOfAmmo && !MySession.Static.CreativeMode)
                m_gunBase.StartNoAmmoSound(m_soundEmitter);
        }

        public void BeginFailReactionLocal(MyShootActionEnum action, MyGunStatusEnum status)
        { }

        public void ShootFailReactionLocal(MyShootActionEnum action, MyGunStatusEnum status)
        { }


        public void ShootMissile()
        {
            if (m_gunBase == null)
            {
                MySandboxGame.Log.WriteLine("Missile launcher barrel null");
                Debug.Fail("Missile launcher barrel null");
                return;
            }

            if (Parent.Physics == null || Parent.Physics.RigidBody == null)
            {
                Debug.Fail("Missile launcher parent physics null");
                MySandboxGame.Log.WriteLine("Missile launcher parent physics null");
                return;
            }

            Vector3 velocity = Parent.Physics.LinearVelocity;
            ShootMissile(velocity);


            //if (BackkickForcePerSecond > 0)
            //{
            //    CubeGrid.Physics.AddForce(
            //        Engine.Physics.MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE,
            //        -deviatedVector * BackkickForcePerSecond,
            //        GetPosition(),
            //        null);
            //}
        }

        public void ShootMissile(Vector3 velocity)
        {
            //  Play missile launch cue (one-time)
            StartSound(m_gunBase.ShootSound);

            m_gunBase.Shoot(velocity);
        }

        public MyGunBase GunBase
        {
            get { return m_gunBase; }
        }

        protected override void WorldPositionChanged(object source)
        {
            base.WorldPositionChanged(source);

            m_gunBase.WorldMatrix = WorldMatrix;
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
            get { return this; }
        }

        MyInventory IMyGunBaseUser.AmmoInventory
        {
            get { return this.GetInventory(); }
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

        bool IMySmallMissileLauncher.UseConveyorSystem { get { return m_useConveyorSystem; } }

        public override bool CanOperate()
        {
            return CheckIsWorking();
        }

        public override void ShootFromTerminal(Vector3 direction)
        {
            Shoot(MyShootActionEnum.PrimaryAction, direction, null);
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
    }
}
