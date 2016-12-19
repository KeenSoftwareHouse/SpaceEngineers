#region Using

using System;
using System.Text;
#if XB1
using System.Collections.Generic;
#endif // XB1
using VRageMath;
using Sandbox.Game.Entities;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Gui;

using Sandbox.Graphics.GUI;
using System.Diagnostics;
using Sandbox.Game.GUI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Character.Components;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using VRage;
using VRage.Network;
using VRage.Game.Models;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game;
using VRage.Game.ModAPI.Interfaces;
using Sandbox.ModAPI.Weapons;
using VRage.Audio;
using VRage.Sync;

#endregion

namespace Sandbox.Game.Weapons
{
    [MyEntityType(typeof(MyObjectBuilder_AutomaticRifle))]
    public class MyAutomaticRifleGun : MyEntity, IMyHandheldGunObject<MyGunBase>, IMyGunBaseUser, IMyEventProxy, IMyAutomaticRifleGun
    {
        int m_lastTimeShoot;
        public int LastTimeShoot { get { return m_lastTimeShoot;} }
         
        int m_lastDirectionChangeAnnounce;

        MyParticleEffect m_smokeEffect;

        MyGunBase m_gunBase;
        static MyDefinitionId m_handItemDefId = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalGunObject), "AutomaticRifleGun");
        MyPhysicalItemDefinition m_physicalItemDef;

        MyCharacter m_owner;

        int m_shootingCounter;
        public MyCharacter Owner { get { return m_owner; } }

        private bool m_canZoom = true;

        public bool IsShooting { get; private set; }

        public int ShootDirectionUpdateTime
        {
            get { return 200; }
        }

        public bool ForceAnimationInsteadOfIK { get { return false; } }

        public bool IsBlocking
        {
            get { return false; }
        }

        private MyEntity3DSoundEmitter m_soundEmitter;

        private int m_shotsFiredInBurst;

        //TODO: Why it is not used?
        private MyHudNotification m_outOfAmmoNotification;

        public MyObjectBuilder_PhysicalGunObject PhysicalObject { get; set; }

        private bool m_isAfterReleaseFire = false;
        public readonly SyncType SyncType;

        private MyEntity[] m_shootIgnoreEntities;   // for projectiles to know which entities to ignore

        public MyAutomaticRifleGun()
        {
            m_shootIgnoreEntities = new MyEntity[] {this};

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            Render.NeedsDraw = true;
#if XB1 // XB1_SYNC_NOREFLECTION
            SyncType = new SyncType(new List<SyncBase>());
            m_gunBase = new MyGunBase(SyncType);
#else // !XB1
            m_gunBase = new MyGunBase();
#endif // !XB1
            m_soundEmitter = new MyEntity3DSoundEmitter(this);
            (PositionComp as MyPositionComponent).WorldPositionChanged = WorldPositionChanged;
            this.Render = new MyRenderComponentAutomaticRifle();
#if !XB1 // !XB1_SYNC_NOREFLECTION
            SyncType = SyncHelpers.Compose(this);
            SyncType.Append(m_gunBase);
#endif // !XB1
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (objectBuilder.SubtypeName != null && objectBuilder.SubtypeName.Length > 0)
                m_handItemDefId = new MyDefinitionId(typeof(MyObjectBuilder_AutomaticRifle), objectBuilder.SubtypeName);

            MyObjectBuilder_AutomaticRifle rifleBuilder = (MyObjectBuilder_AutomaticRifle)objectBuilder;
           
            var handItemDef = MyDefinitionManager.Static.TryGetHandItemDefinition(ref m_handItemDefId);
            m_physicalItemDef = MyDefinitionManager.Static.GetPhysicalItemForHandItem(m_handItemDefId);

            MyDefinitionId weaponDefinitionId;
            if (m_physicalItemDef is MyWeaponItemDefinition)
                weaponDefinitionId = (m_physicalItemDef as MyWeaponItemDefinition).WeaponDefinitionId;
            else
                weaponDefinitionId = new MyDefinitionId(typeof(MyObjectBuilder_WeaponDefinition), "AutomaticRifleGun");

            // muzzle location           
            m_gunBase.Init(rifleBuilder.GunBase, weaponDefinitionId, this);
            
            base.Init(objectBuilder);

            Init(new StringBuilder("Rifle"), m_physicalItemDef.Model, null, null, null);

            var model = VRage.Game.Models.MyModels.GetModelOnlyDummies(m_physicalItemDef.Model);
            m_gunBase.LoadDummies(model.Dummies);

            // backward compatibility for models without dummies or old dummies
            if (!m_gunBase.HasDummies)
            {
                Matrix muzzleMatrix = Matrix.CreateTranslation(handItemDef.MuzzlePosition);
                m_gunBase.AddMuzzleMatrix(MyAmmoType.HighSpeed, muzzleMatrix);
            }

            PhysicalObject = (MyObjectBuilder_PhysicalGunObject)MyObjectBuilderSerializer.CreateNewObject(m_physicalItemDef.Id.TypeId, m_physicalItemDef.Id.SubtypeName);
            PhysicalObject.GunEntity = (MyObjectBuilder_EntityBase)rifleBuilder.Clone();
            PhysicalObject.GunEntity.EntityId = this.EntityId;
            CurrentAmmunition = rifleBuilder.CurrentAmmo;
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            MyObjectBuilder_AutomaticRifle rifleBuilder = (MyObjectBuilder_AutomaticRifle)base.GetObjectBuilder(copy);
            rifleBuilder.SubtypeName = DefinitionId.SubtypeName;
            rifleBuilder.GunBase = m_gunBase.GetObjectBuilder();
            rifleBuilder.CurrentAmmo = CurrentAmmunition;
            return rifleBuilder;
        }

        public float BackkickForcePerSecond
        {
            get { return m_gunBase.BackkickForcePerSecond; }
        }

        public float ShakeAmount
        {
            get;
            protected set;
        }

        public bool EnabledInWorldRules { get { return MySession.Static.WeaponsEnabled; } }

        public Vector3 DirectionToTarget(Vector3D target)
        {
            MyCharacterWeaponPositionComponent weaponPositionComponent =
                m_owner.Components.Get<MyCharacterWeaponPositionComponent>();

            Vector3D direction;
            if (weaponPositionComponent != null)
            {
                direction = Vector3D.Normalize(target - weaponPositionComponent.LogicalPositionWorld);
            }
            else
            {
                direction = Vector3D.Normalize(target - PositionComp.WorldMatrix.Translation);
            }

            Vector3D gunDirection = m_owner.WeaponPosition.LogicalOrientationWorld;
            double d = Vector3D.Dot(direction, gunDirection);
            //Too big angle to target
            if (d < 0.98)
                direction = gunDirection;
            return direction;
        }

        public bool CanShoot(MyShootActionEnum action, long shooter, out MyGunStatusEnum status)
        {
            status = MyGunStatusEnum.OK;

            if (action == MyShootActionEnum.PrimaryAction)
            {
                if (!m_gunBase.HasAmmoMagazines)
                {
                    status = MyGunStatusEnum.Failed;
                    return false;
                }

                if (m_gunBase.ShotsInBurst>0 && m_shotsFiredInBurst >= m_gunBase.ShotsInBurst)
                {
                    status = MyGunStatusEnum.BurstLimit;
                    return false;
                }

                if ((MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimeShoot) < m_gunBase.ShootIntervalInMiliseconds)
                {
                    status = MyGunStatusEnum.Cooldown;
                    return false;
                }
                if (m_owner == null)
                {
                    status = MyGunStatusEnum.Failed;
                    return false;
                }

                if (m_owner.GetCurrentMovementState() == MyCharacterMovementEnum.Sprinting)
                {
                    status = MyGunStatusEnum.Failed;
                    return false;
                }

                if (!MySession.Static.CreativeMode)
                {
                    var ownerGun = m_owner.CurrentWeapon as MyAutomaticRifleGun;

                    if (ownerGun == null || !m_gunBase.HasEnoughAmmunition())
                    {
                        status = MyGunStatusEnum.OutOfAmmo;
                        return false;
                    }
                }

                status = MyGunStatusEnum.OK;
                return true;
            }

            else if (action == MyShootActionEnum.SecondaryAction)
            {
                if (!m_canZoom)
                {
                    status = MyGunStatusEnum.Cooldown;
                    return false;
                }

                return true;
            }

            status = MyGunStatusEnum.Failed;
            return false;
        }

        public void Shoot(MyShootActionEnum action, Vector3 direction, Vector3D? overrideWeaponPos, string gunAction)
        {
            if (action == MyShootActionEnum.PrimaryAction)
            {
                Shoot(direction, overrideWeaponPos);
                m_shotsFiredInBurst++;
                IsShooting = true;

                if (m_owner.ControllerInfo.IsLocallyControlled() && m_owner.IsInFirstPersonView)
                {
                    MySector.MainCamera.CameraShake.AddShake(0.5f);
                }
            }
            else if (action == MyShootActionEnum.SecondaryAction)
            {
                if (MySession.Static.ControlledEntity == m_owner)
                {
                    m_owner.Zoom(true);
                    m_canZoom = false;
                }
            }
        }

        public void EndShoot(MyShootActionEnum action)
        {
            if (action == MyShootActionEnum.PrimaryAction)
            {
                IsShooting = false;
                m_shotsFiredInBurst = 0;
                m_gunBase.StopShoot();
            }
            else if (action == MyShootActionEnum.SecondaryAction)
            {
                m_canZoom = true;
            }
        }

        private void Shoot(Vector3 direction, Vector3D? overrideWeaponPos)
        {
            m_lastTimeShoot = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            //CreateSmokeEffect();

            // initial position has offset, otherwise we shoot through close objects
      		if (!overrideWeaponPos.HasValue)
            {
	            if (m_owner!=null)
    	            m_gunBase.ShootWithOffset(m_owner.Physics.LinearVelocity, direction, -0.25f, (MyEntity)m_owner);
        	    else
            	    m_gunBase.ShootWithOffset(Vector3.Zero, direction, -0.25f, null);
            }
            else
            {
                m_gunBase.Shoot((overrideWeaponPos.Value) + direction * (-0.25f), 
                    m_owner.Physics.LinearVelocity, direction, (MyEntity)m_owner);
            }            m_isAfterReleaseFire = false;
            if (m_gunBase.ShootSound != null)
            {
                StartLoopSound(m_gunBase.ShootSound);
            }

            m_gunBase.ConsumeAmmo();
        }

        private void CreateSmokeEffect()
        {
            if (m_smokeEffect == null)
            {
                if (MySector.MainCamera.GetDistanceFromPoint(PositionComp.GetPosition()) < 150)
                {
                    if (MyParticlesManager.TryCreateParticleEffect((int)MyParticleEffectsIDEnum.Smoke_Autocannon, out m_smokeEffect))
                    {
                        m_smokeEffect.WorldMatrix = PositionComp.WorldMatrix;
                        m_smokeEffect.OnDelete += OnSmokeEffectDelete;
                    }
                }
            }
        }

        private void OnSmokeEffectDelete(object sender, EventArgs eventArgs)
        {
            m_smokeEffect = null;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (m_smokeEffect != null)
            {
                float smokeOffset = 0.2f;

                m_smokeEffect.WorldMatrix = MatrixD.CreateTranslation(m_gunBase.GetMuzzleWorldPosition() + PositionComp.WorldMatrix.Forward * smokeOffset);
                m_smokeEffect.UserBirthMultiplier = 50;
            }
            m_gunBase.UpdateEffects();

            if (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimeShoot > m_gunBase.ReleaseTimeAfterFire && !m_isAfterReleaseFire)
            {
                StopLoopSound();

                if (m_smokeEffect != null)
                {
                    m_smokeEffect.Stop();
                }

                m_isAfterReleaseFire = true;
                m_gunBase.RemoveOldEffects(MyWeaponDefinition.WeaponEffectAction.Shoot);
            }
        }

        public void BeginFailReaction(MyShootActionEnum action, MyGunStatusEnum status)
        {
            if (status == MyGunStatusEnum.OutOfAmmo)
            {
                m_gunBase.StartNoAmmoSound(m_soundEmitter);
            }
        }

        public void BeginFailReactionLocal(MyShootActionEnum action, MyGunStatusEnum status)
        {
            if (status == MyGunStatusEnum.Failed)
                MyGuiAudio.PlaySound(MyGuiSounds.HudUnable);
        }

        public void StartLoopSound(MySoundPair cueEnum)
        {
            m_gunBase.StartShootSound(m_soundEmitter);
        }

        public void StopLoopSound()
        {
            if (m_soundEmitter.Loop)
                m_soundEmitter.StopSound(false);
        }

        private void WorldPositionChanged(object source)
        {
            m_gunBase.WorldMatrix = WorldMatrix;
        }

        protected override void Closing()
        {
            IsShooting = false;
            m_gunBase.RemoveOldEffects(MyWeaponDefinition.WeaponEffectAction.Shoot);

            if (m_smokeEffect != null)
            {
                m_smokeEffect.Stop();
                m_smokeEffect = null;
            }

            if (m_soundEmitter.Loop)
                m_soundEmitter.StopSound(false);

            base.Closing();
        }

        public void OnControlAcquired(MyCharacter owner)
        {
            m_owner = owner;
            if (m_owner != null)
            {
                m_shootIgnoreEntities = new MyEntity[] { this, m_owner };

                var inventory = m_owner.GetInventory() as MyInventory;
                System.Diagnostics.Debug.Assert(inventory != null, "Null or unexpected inventory type returned!");
                if (inventory != null)
                {
                    inventory.ContentsChanged += MyAutomaticRifleGun_ContentsChanged;
                }
            }
            m_gunBase.RefreshAmmunitionAmount();
        }

        void MyAutomaticRifleGun_ContentsChanged(MyInventoryBase obj)
        {
            m_gunBase.RefreshAmmunitionAmount();
        }

        public void OnControlReleased()
        {
            if (m_owner != null)
            {
                var inventory = m_owner.GetInventory() as MyInventory;
                System.Diagnostics.Debug.Assert(inventory != null, "Null or unexpected inventory type returned!");
                if (inventory != null)
                {
                    inventory.ContentsChanged -= MyAutomaticRifleGun_ContentsChanged;
                }
            }
            m_owner = null;
        }

        public void DrawHud(IMyCameraController camera, long playerId)
        {
        }

        public MyDefinitionId DefinitionId
        {
            get { return m_handItemDefId; }
        }

        public int GetAmmunitionAmount()
        {
            return m_gunBase.GetTotalAmmunitionAmount();
        }

        public void ShootFailReactionLocal(MyShootActionEnum action, MyGunStatusEnum status)
        { }

        public MyGunBase GunBase
        {
            get { return m_gunBase; }
        }

        #region IMyGunBaseUser

        MyEntity[] IMyGunBaseUser.IgnoreEntities
        {
            get { return m_shootIgnoreEntities; }
        }

        MyEntity IMyGunBaseUser.Weapon
        {
            get { return this; }
        }

        MyEntity IMyGunBaseUser.Owner
        {
            get { return m_owner; }
        }

        IMyMissileGunObject IMyGunBaseUser.Launcher
        {
            get { return null; }
        }

        MyInventory IMyGunBaseUser.AmmoInventory
        {
            get
            {
                if (m_owner != null)
                {
                    return m_owner.GetInventory() as MyInventory;
                }

                return null;
            }
        }

        MyDefinitionId IMyGunBaseUser.PhysicalItemId
        {
            get { return m_physicalItemDef.Id; }
        }

        MyInventory IMyGunBaseUser.WeaponInventory
        {
            get
            {
                if (m_owner != null)
                {
                    return m_owner.GetInventory() as MyInventory;
                }

                return null;
            }
        }

        long IMyGunBaseUser.OwnerId
        {
            get
            {
                if (m_owner != null)
                    return m_owner.ControllerInfo.ControllingIdentityId;
                return 0;
            }
        }

        string IMyGunBaseUser.ConstraintDisplayName
        {
            get { return null; }
        }

        #endregion

        public MyPhysicalItemDefinition PhysicalItemDefinition
        {
            get { return m_physicalItemDef; }
        }

        public int CurrentAmmunition 
        {
            set
            {
                m_gunBase.RemainingAmmo = value;
            }

            get
            {
                return m_gunBase.GetTotalAmmunitionAmount();
            }
        }

        public int CurrentMagazineAmmunition 
        { 
            set
            {
                m_gunBase.CurrentAmmo = value;
            }
            get
            {
                return m_gunBase.CurrentAmmo;
            }
        }

        public void UpdateSoundEmitter()
        {
            if (m_soundEmitter != null)
                m_soundEmitter.Update();
        }
    }
}
