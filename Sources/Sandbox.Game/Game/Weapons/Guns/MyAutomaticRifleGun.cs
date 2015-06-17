#region Using

using System;
using System.Text;
using VRageMath;
using Sandbox.Graphics.TransparentGeometry.Particles;
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
using VRage.ObjectBuilders;
using VRage.ModAPI;
#endregion

namespace Sandbox.Game.Weapons
{
    [MyEntityType(typeof(MyObjectBuilder_AutomaticRifle))]
    class MyAutomaticRifleGun : MyEntity, IMyHandheldGunObject<MyGunBase>, IMyGunBaseUser
    {
        int m_lastTimeShoot;
        public int LastTimeShoot { get { return m_lastTimeShoot;} }
         
        int m_lastDirectionChangeAnnounce;

        MyParticleEffect m_smokeEffect;

        MyGunBase m_gunBase;
        MyDefinitionId m_handItemDefId;
        MyPhysicalItemDefinition m_physicalItemDef;

        MyCharacter m_owner;

        private bool m_canZoom = true;

        public bool IsShooting { get; private set; }
		public bool IsDeconstructor { get { return false; } }

        public int ShootDirectionUpdateTime
        {
            get { return 200; }
        }

        private MyEntity3DSoundEmitter m_soundEmitter;

        //TODO: Why it is not used?
        private MyHudNotification m_outOfAmmoNotification;

        public MyObjectBuilder_PhysicalGunObject PhysicalObject { get; set; }

        private bool m_isAfterReleaseFire = false;

        public MyAutomaticRifleGun()
        {
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            Render.NeedsDraw = true;

            m_soundEmitter = new MyEntity3DSoundEmitter(this);
            (PositionComp as MyPositionComponent).WorldPositionChanged = WorldPositionChanged;
            this.Render = new MyRenderComponentAutomaticRifle();
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            MyObjectBuilder_AutomaticRifle rifleBuilder = (MyObjectBuilder_AutomaticRifle)objectBuilder;
            m_handItemDefId = rifleBuilder.GetId();
            if (string.IsNullOrEmpty(m_handItemDefId.SubtypeName))
                m_handItemDefId = new MyDefinitionId(typeof(MyObjectBuilder_AutomaticRifle), "RifleGun");
           
            var handItemDef = MyDefinitionManager.Static.TryGetHandItemDefinition(ref m_handItemDefId);
            m_physicalItemDef = MyDefinitionManager.Static.GetPhysicalItemForHandItem(m_handItemDefId);

            MyDefinitionId weaponDefinitionId;
            if (m_physicalItemDef is MyWeaponItemDefinition)
                weaponDefinitionId = (m_physicalItemDef as MyWeaponItemDefinition).WeaponDefinitionId;
            else
                weaponDefinitionId = new MyDefinitionId(typeof(MyObjectBuilder_WeaponDefinition), "AutomaticRifleGun");

            // muzzle location
            m_gunBase = new MyGunBase();
            m_gunBase.Init(rifleBuilder.GunBase, weaponDefinitionId, this);
            
            base.Init(objectBuilder);

            Init(new StringBuilder("Rifle"), m_physicalItemDef.Model, null, null, null);

            var model = Engine.Models.MyModels.GetModelOnlyDummies(m_physicalItemDef.Model);
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
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            MyObjectBuilder_AutomaticRifle rifleBuilder = (MyObjectBuilder_AutomaticRifle)base.GetObjectBuilder(copy);
            rifleBuilder.SubtypeName = DefinitionId.SubtypeName;
            rifleBuilder.GunBase = m_gunBase.GetObjectBuilder();
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
            Vector3D direction = Vector3D.Normalize(target - PositionComp.WorldMatrix.Translation);
            Vector3D gunDirection = PositionComp.WorldMatrix.Forward;
            double d = Vector3D.Dot(direction, gunDirection);
            //Too big angle to target
            if (d < 0.75)
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

                if ((MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimeShoot) < m_gunBase.ShootIntervalInMiliseconds)
                {
                    status = MyGunStatusEnum.Cooldown;
                    return false;
                }

                Debug.Assert(m_owner is MyCharacter, "Only character can use automatic rifle!");
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

        public void Shoot(MyShootActionEnum action, Vector3 direction)
        {
            if (action == MyShootActionEnum.PrimaryAction)
            {
                Shoot(direction);
                IsShooting = true;
            }
            else if (action == MyShootActionEnum.SecondaryAction)
            {
                if (MySession.ControlledEntity == m_owner)
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
            }
            else if (action == MyShootActionEnum.SecondaryAction)
            {
                m_canZoom = true;
            }
        }

        private void Shoot(Vector3 direction)
        {
            m_lastTimeShoot = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            CreateSmokeEffect();

            m_gunBase.Shoot(m_owner.Physics.LinearVelocity, direction);
            m_isAfterReleaseFire = false;
            if (m_gunBase.ShootSound != null)
            {
                StartLoopSound(m_gunBase.ShootSound);
            }

            if (!MySession.Static.CreativeMode)
            {
                m_gunBase.ConsumeAmmo();
            }
        }

        private void CreateSmokeEffect()
        {
            if (m_smokeEffect == null)
            {
                if (MySector.MainCamera.GetDistanceWithFOV(PositionComp.GetPosition()) < 150)
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

            if (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastTimeShoot > m_gunBase.ReleaseTimeAfterFire
                && !m_isAfterReleaseFire)
            {
                StopLoopSound();

                if (m_smokeEffect != null)
                {
                    m_smokeEffect.Stop(false);
                }

                m_isAfterReleaseFire = true;
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
            m_soundEmitter.StopSound(true);
        }

        private void WorldPositionChanged(object source)
        {
            m_gunBase.WorldMatrix = WorldMatrix;
        }

        protected override void Closing()
        {
            if (m_smokeEffect != null)
            {
                m_smokeEffect.Stop();
                m_smokeEffect = null;
            }

            m_soundEmitter.StopSound(true);

            base.Closing();
        }

        public void OnControlAcquired(MyCharacter owner)
        {
            m_owner = owner;
            m_owner.GetInventory().ContentsChanged += MyAutomaticRifleGun_ContentsChanged;
            m_gunBase.RefreshAmmunitionAmount();
        }

        void MyAutomaticRifleGun_ContentsChanged(MyInventory obj)
        {
            m_gunBase.RefreshAmmunitionAmount();
        }

        public void OnControlReleased()
        {
            m_owner.GetInventory().ContentsChanged -= MyAutomaticRifleGun_ContentsChanged;
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

        MyEntity IMyGunBaseUser.IgnoreEntity
        {
            get { return this; }
        }

        MyEntity IMyGunBaseUser.Weapon
        {
            get { return this; }
        }

        MyEntity IMyGunBaseUser.Owner
        {
            get { return null; }
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
                    return m_owner.GetInventory();
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
    }
}
