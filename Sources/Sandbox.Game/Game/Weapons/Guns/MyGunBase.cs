
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Localization;
using Sandbox.Game.Weapons.Guns;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using VRage;
using VRage.Import;
using VRage.Utils;
using VRage.Serialization;
using VRageMath;
using VRage.ObjectBuilders;
using Sandbox.Game.Multiplayer;
using Sandbox.Common;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders;
using System.Diagnostics;
using VRage.Sync;
using VRageRender.Import;

namespace Sandbox.Game.Weapons
{
    public class MyGunBase : MyDeviceBase
    {
        public class DummyContainer
        {
            public List<MatrixD> Dummies = new List<MatrixD>();
            public int DummyIndex = 0;
            public MatrixD DummyInWorld = Matrix.Identity;
            public bool Dirty = true;

            public MatrixD DummyToUse { get { return Dummies[DummyIndex]; } }
        }

        private class WeaponEffect
        {
            public string Name;
            public Matrix LocalMatrix;
            public int EffectId;
            public MyWeaponDefinition.WeaponEffectAction Action;
            public MyParticleEffect Effect;

            public WeaponEffect(string name, Matrix localMatrix, int effectId, MyWeaponDefinition.WeaponEffectAction action, MyParticleEffect effect)
            {
                this.Name = name;
                this.EffectId = effectId;
                this.Effect = effect;
                this.Action = action;
                this.LocalMatrix = localMatrix;
            }
        }

        public const int AMMO_PER_SHOOT = 1;

        #region Fields
        protected MyWeaponPropertiesWrapper m_weaponProperties;
        protected Dictionary<MyDefinitionId, int> m_remainingAmmos;
        protected readonly Sync<int> m_cachedAmmunitionAmount;
        protected Dictionary<int, DummyContainer> m_dummiesByAmmoType;
        protected MatrixD m_worldMatrix;
        protected IMyGunBaseUser m_user;
        private List<WeaponEffect> m_activeEffects = new List<WeaponEffect>();
        public Matrix m_holdingDummyMatrix;

        int m_shotProjectiles = 0;

        #endregion

        #region Properties

        private Dictionary<string, MyModelDummy> dummies = null;
        public int CurrentAmmo { get; set; }
        public MyWeaponPropertiesWrapper WeaponProperties { get { return m_weaponProperties; } }
        public MyAmmoMagazineDefinition CurrentAmmoMagazineDefinition { get { return WeaponProperties.AmmoMagazineDefinition; } }
        public MyDefinitionId CurrentAmmoMagazineId { get { return WeaponProperties.AmmoMagazineId; } }
        public MyAmmoDefinition CurrentAmmoDefinition { get { return WeaponProperties.AmmoDefinition; } }
        public float BackkickForcePerSecond
        {
            get
            {
                if (WeaponProperties != null && WeaponProperties.AmmoDefinition != null)
                {
                    return WeaponProperties.AmmoDefinition.BackkickForce;
                }
                return 0;
            }
        }
        public bool HasMissileAmmoDefined { get { return m_weaponProperties.WeaponDefinition.HasMissileAmmoDefined; } }
        public bool HasProjectileAmmoDefined { get { return m_weaponProperties.WeaponDefinition.HasProjectileAmmoDefined; } }
        public int MuzzleFlashLifeSpan { get { return m_weaponProperties.WeaponDefinition.MuzzleFlashLifeSpan; } }
        public int ShootIntervalInMiliseconds
        {
            get
            {
                return ShootIntervalModifier == 1
                    ? m_weaponProperties.CurrentWeaponShootIntervalInMiliseconds : (int)(ShootIntervalModifier * m_weaponProperties.CurrentWeaponShootIntervalInMiliseconds);
            }
        }
        public float ShootIntervalModifier { get; set; }
        public float ReleaseTimeAfterFire { get { return m_weaponProperties.WeaponDefinition.ReleaseTimeAfterFire; } }
        public MySoundPair ShootSound { get { return m_weaponProperties.CurrentWeaponShootSound; } }
        public MySoundPair NoAmmoSound { get { return m_weaponProperties.WeaponDefinition.NoAmmoSound; } }
        public MySoundPair ReloadSound { get { return m_weaponProperties.WeaponDefinition.ReloadSound; } }
        public MySoundPair SecondarySound { get { return m_weaponProperties.WeaponDefinition.SecondarySound; } }
        public bool UseDefaultMuzzleFlash { get { return m_weaponProperties.WeaponDefinition.UseDefaultMuzzleFlash; } }
        public float MechanicalDamage
        {
            get
            {
                if (WeaponProperties.AmmoDefinition != null)
                    return m_weaponProperties.AmmoDefinition.GetDamageForMechanicalObjects();
                return 0;
            }
        }
        public float DeviateAngle { get { return m_weaponProperties.WeaponDefinition.DeviateShotAngle; } }
        public bool HasAmmoMagazines { get { return m_weaponProperties.WeaponDefinition.HasAmmoMagazines(); } }
        public bool IsAmmoProjectile { get { return m_weaponProperties.IsAmmoProjectile; } }
        public bool IsAmmoMissile { get { return m_weaponProperties.IsAmmoMissile; } }
        public int ShotsInBurst { get { return WeaponProperties.ShotsInBurst; } }
        public int ReloadTime { get { return WeaponProperties.ReloadTime; } }

        public bool HasDummies { get { return m_dummiesByAmmoType.Count > 0; } }
        public MatrixD WorldMatrix
        {
            set
            {
                m_worldMatrix = value;
                RecalculateMuzzles();
            }
            get { return m_worldMatrix; }
        }

        public DateTime LastShootTime { get; private set; }

        public int RemainingAmmo
        {
            set
            {
                m_cachedAmmunitionAmount.Value = value;
            }
        }

        #endregion

#if XB1 // XB1_SYNC_NOREFLECTION
        public MyGunBase(SyncType syncType)
#else // !XB1
        public MyGunBase()
#endif // !XB1
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_cachedAmmunitionAmount = syncType.CreateAndAddProp<int>();
#endif // XB1
            m_dummiesByAmmoType = new Dictionary<int, DummyContainer>();
            m_remainingAmmos = new Dictionary<MyDefinitionId, int>();
            ShootIntervalModifier = 1;
        }

        public MyObjectBuilder_GunBase GetObjectBuilder()
        {
            var gunBaseObjectBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_GunBase>();
            gunBaseObjectBuilder.CurrentAmmoMagazineName = CurrentAmmoMagazineId.SubtypeName;
            gunBaseObjectBuilder.RemainingAmmo = CurrentAmmo;
            gunBaseObjectBuilder.LastShootTime = LastShootTime.Ticks;
            gunBaseObjectBuilder.RemainingAmmosList = new List<MyObjectBuilder_GunBase.RemainingAmmoIns>();
            foreach (var ammoMagazineRemaining in m_remainingAmmos)
            {
                var copy = new MyObjectBuilder_GunBase.RemainingAmmoIns();
                copy.SubtypeName = ammoMagazineRemaining.Key.SubtypeName;
                copy.Amount = ammoMagazineRemaining.Value;
                gunBaseObjectBuilder.RemainingAmmosList.Add(copy);
            }
            gunBaseObjectBuilder.InventoryItemId = this.InventoryItemId;
            return gunBaseObjectBuilder;
        }

        public void Init(MyObjectBuilder_GunBase objectBuilder, MyCubeBlockDefinition cubeBlockDefinition, IMyGunBaseUser gunBaseUser)
        {
            if (cubeBlockDefinition is MyWeaponBlockDefinition)
            {
                MyWeaponBlockDefinition weaponBlockDefinition = cubeBlockDefinition as MyWeaponBlockDefinition;
                Init(objectBuilder, weaponBlockDefinition.WeaponDefinitionId, gunBaseUser);
            }
            else
            {
                // Backward compatibility
                MyDefinitionId weaponDefinitionId = GetBackwardCompatibleDefinitionId(cubeBlockDefinition.Id.TypeId);
                Init(objectBuilder, weaponDefinitionId, gunBaseUser);
            }
        }

        public void Init(MyObjectBuilder_GunBase objectBuilder, MyDefinitionId weaponDefinitionId, IMyGunBaseUser gunBaseUser)
        {
            if (objectBuilder != null)
            {
                base.Init(objectBuilder);
            }

            m_user = gunBaseUser;
            m_weaponProperties = new MyWeaponPropertiesWrapper(weaponDefinitionId);
            //MyDebug.AssertDebug(m_weaponProperties.AmmoMagazinesCount > 0, "Weapon definition has no ammo magazines attached.");

            // object builder area - Start
            m_remainingAmmos = new Dictionary<MyDefinitionId, int>(WeaponProperties.AmmoMagazinesCount);
            if (objectBuilder != null)
            {
                MyDefinitionId ammoMagazineDef = new MyDefinitionId(typeof(MyObjectBuilder_AmmoMagazine), objectBuilder.CurrentAmmoMagazineName);
                if (m_weaponProperties.CanChangeAmmoMagazine(ammoMagazineDef))
                {
                    CurrentAmmo = objectBuilder.RemainingAmmo;
                    m_weaponProperties.ChangeAmmoMagazine(ammoMagazineDef);
                }
                else
                {
                    if (WeaponProperties.WeaponDefinition.HasAmmoMagazines())
                        m_weaponProperties.ChangeAmmoMagazine(m_weaponProperties.WeaponDefinition.AmmoMagazinesId[0]);
                }

                foreach (var remainingAmmo in objectBuilder.RemainingAmmosList)
                {
                    m_remainingAmmos.Add(new MyDefinitionId(typeof(MyObjectBuilder_AmmoMagazine), remainingAmmo.SubtypeName), remainingAmmo.Amount);
                }

                LastShootTime = new DateTime(objectBuilder.LastShootTime);
            }
            else
            {
                if (WeaponProperties.WeaponDefinition.HasAmmoMagazines())
                    m_weaponProperties.ChangeAmmoMagazine(m_weaponProperties.WeaponDefinition.AmmoMagazinesId[0]);

                LastShootTime = new DateTime(0);
            }
            // object builder area - END

            if (m_user.AmmoInventory != null)
            {
                if (m_user.PutConstraint())
                {
                    MyDebug.AssertDebug(!String.IsNullOrEmpty(m_user.ConstraintDisplayName), "Display name of weapon is empty.");
                    m_user.AmmoInventory.Constraint = CreateAmmoInventoryConstraints(m_user.ConstraintDisplayName);
                }

                RefreshAmmunitionAmount();
            }

            if (m_user.Weapon != null)
            {
                m_user.Weapon.OnClosing += Weapon_OnClosing;
            }
        }

        void Weapon_OnClosing(MyEntity obj)
        {
            if (m_user.Weapon != null)
            {
                m_user.Weapon.OnClosing -= Weapon_OnClosing;
            }
        }

        void MySyncGunBase_AmmoCountChanged(long weaponId, int ammoCount)
        {
            if (m_user.Weapon != null && m_user.Weapon.EntityId == weaponId)
                CurrentAmmo = ammoCount;
        }

        public Vector3 GetDeviatedVector(float deviateAngle, Vector3 direction)
        {
            return MyUtilRandomVector3ByDeviatingVector.GetRandom(direction, deviateAngle);
        }

        public Vector3 GetDeviatedVector(int hash, float deviateAngle, Vector3 direction)
        {
            return MyUtilRandomVector3ByDeviatingVector.GetRandom(hash, direction, deviateAngle);
        }

        private void AddProjectile(MyWeaponPropertiesWrapper weaponProperties, Vector3D initialPosition, Vector3D initialVelocity, Vector3D direction, MyEntity owner)
        {
            Vector3 projectileForwardVector = direction;
            if (weaponProperties.IsDeviated)
            {
                projectileForwardVector = GetDeviatedVector(m_shotProjectiles + direction.GetHashCode(), weaponProperties.WeaponDefinition.DeviateShotAngle, direction);
                projectileForwardVector.Normalize();
            }

            m_shotProjectiles++;

            MyProjectiles.Add(weaponProperties.GetCurrentAmmoDefinitionAs<MyProjectileAmmoDefinition>(), initialPosition, initialVelocity, projectileForwardVector, m_user, owner);
        }

        private void AddMissile(MyWeaponPropertiesWrapper weaponProperties, Vector3D initialPosition, Vector3D initialVelocity, Vector3D direction)
        {
            MyMissileAmmoDefinition missileAmmoDefinition = weaponProperties.GetCurrentAmmoDefinitionAs<MyMissileAmmoDefinition>();

            Vector3 missileDeviatedVector = direction;
            if (weaponProperties.IsDeviated)
            {
                missileDeviatedVector = GetDeviatedVector(weaponProperties.WeaponDefinition.DeviateShotAngle, direction);
                missileDeviatedVector.Normalize();
            }

            initialVelocity += missileDeviatedVector * missileAmmoDefinition.MissileInitialSpeed;

            if (m_user.Launcher != null)
                MyMissiles.Add(weaponProperties, initialPosition, initialVelocity, missileDeviatedVector, m_user.OwnerId);
            else
                MyMissiles.AddUnsynced(weaponProperties, initialPosition + 2 * missileDeviatedVector, initialVelocity, missileDeviatedVector, m_user.OwnerId);//start missile 2 beters in front of launcher - prevents hit of own turret
        }

        public void Shoot(Vector3 initialVelocity, MyEntity owner = null)
        {
            MatrixD currentDummy = GetMuzzleWorldMatrix();
            Shoot(currentDummy.Translation, initialVelocity, currentDummy.Forward, owner);
        }

        public void Shoot(Vector3 initialVelocity, Vector3 direction, MyEntity owner = null)
        {
            MatrixD currentDummy = GetMuzzleWorldMatrix();
            Shoot(currentDummy.Translation, initialVelocity, direction, owner);
        }

        /// <summary>
        /// This function changes default shooting position (muzzle flash) with an offset.
        /// (allow us to shoot at close objects)
        /// </summary>
        public void ShootWithOffset(Vector3 initialVelocity, Vector3 direction, float offset, MyEntity owner = null)
        {
            MatrixD currentDummy = GetMuzzleWorldMatrix();
            Shoot(currentDummy.Translation + direction * offset, initialVelocity, direction, owner);
        }

        public void Shoot(Vector3D initialPosition, Vector3 initialVelocity, Vector3 direction, MyEntity owner = null)
        {
            //VRageRender.MyRenderProxy.DebugDrawLine3D(initialPosition, initialPosition + direction * 1000, Color.Red, Color.Red, true, true);
            MyAmmoDefinition ammoDef = m_weaponProperties.AmmoDefinition;
            switch (ammoDef.AmmoType)
            {
                case MyAmmoType.HighSpeed:
                    var cnt = (ammoDef as MyProjectileAmmoDefinition).ProjectileCount;
                    for (int i = 0; i < cnt; i++)
                        AddProjectile(m_weaponProperties, initialPosition, initialVelocity, direction, owner);
                    break;
                case MyAmmoType.Missile:
                    AddMissile(m_weaponProperties, initialPosition, initialVelocity, direction);
                    break;
            }

            MoveToNextMuzzle(ammoDef.AmmoType);

            CreateEffects(MyWeaponDefinition.WeaponEffectAction.Shoot);

            LastShootTime = DateTime.UtcNow;
        }

        protected void CreateEffects(MyWeaponDefinition.WeaponEffectAction action)
        {
            if (dummies != null && dummies.Count > 0 && WeaponProperties.WeaponDefinition.WeaponEffects.Length > 0)
            {
                for (int i = 0; i < WeaponProperties.WeaponDefinition.WeaponEffects.Length; i++)
                {
                    if (WeaponProperties.WeaponDefinition.WeaponEffects[i].Action == action)
                    {
                        MyModelDummy dummy;
                        if (dummies.TryGetValue(WeaponProperties.WeaponDefinition.WeaponEffects[i].Dummy, out dummy))
                        {
                            MyParticleEffect effect;
                            bool add = true;
                            int effectId = -1;
                            MyParticlesLibrary.GetParticleEffectsID(WeaponProperties.WeaponDefinition.WeaponEffects[i].Particle, out effectId);
                            if (WeaponProperties.WeaponDefinition.WeaponEffects[i].Loop)
                            {
                                for (int j = 0; j < m_activeEffects.Count; j++)
                                {
                                    if (m_activeEffects[j].Name == dummy.Name && m_activeEffects[j].EffectId == effectId)
                                    {
                                        add = false;
                                        break;
                                    }
                                }
                            }
                            if (add && MyParticlesManager.TryCreateParticleEffect(effectId, out effect))
                            {
                                var dummyNormalized = MatrixD.Normalize(dummy.Matrix);
                                effect.WorldMatrix = MatrixD.Multiply(dummyNormalized, WorldMatrix);

                                if (WeaponProperties.WeaponDefinition.WeaponEffects[i].Loop)
                                {
                                    m_activeEffects.Add(new WeaponEffect(dummy.Name, dummyNormalized, effectId, action, effect));
                                }
                            }
                        }
                    }
                }
            }
        }

        public void UpdateEffects()
        {
            for (int i = 0; i < m_activeEffects.Count; i++)
            {
                if (!m_activeEffects[i].Effect.IsStopped || m_activeEffects[i].Effect.GetParticlesCount() > 0)
                {
                    m_activeEffects[i].Effect.WorldMatrix = MatrixD.Multiply(m_activeEffects[i].LocalMatrix, WorldMatrix);
                }
                else
                {
                    m_activeEffects.RemoveAt(i);
                    i--;
                }
            }
        }

        public void RemoveOldEffects(MyWeaponDefinition.WeaponEffectAction action = MyWeaponDefinition.WeaponEffectAction.Shoot)
        {
            for (int i = 0; i < m_activeEffects.Count; i++)
            {
                if (m_activeEffects[i].Action == action)
                {
                    m_activeEffects[i].Effect.Stop();
                    m_activeEffects[i].Effect.Close(false);
                    m_activeEffects.RemoveAt(i);
                    i--;
                }
            }
        }

        public MyInventoryConstraint CreateAmmoInventoryConstraints(String displayName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(MyTexts.GetString(MySpaceTexts.ToolTipItemFilter_AmmoMagazineInput), displayName);
            MyInventoryConstraint output = new MyInventoryConstraint(sb.ToString());

            foreach (MyDefinitionId ammoMagazineId in m_weaponProperties.WeaponDefinition.AmmoMagazinesId)
                output.Add(ammoMagazineId);

            return output;
        }

        public bool IsAmmoMagazineCompatible(MyDefinitionId ammoMagazineId)
        {
            return WeaponProperties.CanChangeAmmoMagazine(ammoMagazineId);
        }

        public override bool CanSwitchAmmoMagazine()
        {
            return m_weaponProperties != null && m_weaponProperties.WeaponDefinition.HasAmmoMagazines();
        }

        public bool SwitchAmmoMagazine(MyDefinitionId ammoMagazineId)
        {
            m_remainingAmmos[CurrentAmmoMagazineId] = CurrentAmmo;
            WeaponProperties.ChangeAmmoMagazine(ammoMagazineId);

            int newCurrentAmmo = 0;
            m_remainingAmmos.TryGetValue(ammoMagazineId, out newCurrentAmmo);
            CurrentAmmo = newCurrentAmmo;

            RefreshAmmunitionAmount();

            return ammoMagazineId == WeaponProperties.AmmoMagazineId;
        }

        public override bool SwitchAmmoMagazineToNextAvailable()
        {
            MyWeaponDefinition weaponDefinition = WeaponProperties.WeaponDefinition;

            if (!weaponDefinition.HasAmmoMagazines())
                return false;

            int currentIndex = weaponDefinition.GetAmmoMagazineIdArrayIndex(CurrentAmmoMagazineId);
            int ammoMagazinesCount = weaponDefinition.AmmoMagazinesId.Length;
            for (int i = currentIndex + 1, j = 0; j != ammoMagazinesCount; i++, j++)
            {
                if (i == ammoMagazinesCount)
                {
                    i = 0; // reset counter to not overflow
                }

                if (weaponDefinition.AmmoMagazinesId[i].SubtypeId != CurrentAmmoMagazineId.SubtypeId)
                {
                    if (MySession.Static.CreativeMode)
                    {
                        return SwitchAmmoMagazine(weaponDefinition.AmmoMagazinesId[i]);
                    }
                    else
                    {
                        int remainingAmmo = 0;
                        if (m_remainingAmmos.TryGetValue(weaponDefinition.AmmoMagazinesId[i], out remainingAmmo))
                        {
                            if (remainingAmmo > 0)
                            {
                                return SwitchAmmoMagazine(weaponDefinition.AmmoMagazinesId[i]);
                            }
                        }
                        if (m_user.AmmoInventory.GetItemAmount(weaponDefinition.AmmoMagazinesId[i]) > 0)
                        {
                            return SwitchAmmoMagazine(weaponDefinition.AmmoMagazinesId[i]);
                        }
                    }
                }
            }

            return false;
        }

        public override bool SwitchToNextAmmoMagazine()
        {
            MyWeaponDefinition weaponDefinition = WeaponProperties.WeaponDefinition;
            int currentIndex = weaponDefinition.GetAmmoMagazineIdArrayIndex(CurrentAmmoMagazineId);
            int ammoMagazinesCount = weaponDefinition.AmmoMagazinesId.Length;
            currentIndex += 1;

            if (currentIndex == ammoMagazinesCount)
                currentIndex = 0;

            return SwitchAmmoMagazine(weaponDefinition.AmmoMagazinesId[currentIndex]);
        }

        public bool SwitchAmmoMagazineToFirstAvailable()
        {
            MyWeaponDefinition weaponDefinition = WeaponProperties.WeaponDefinition;
            for (int i = 0; i < WeaponProperties.AmmoMagazinesCount; i++)
            {
                int remainingAmmo = 0;
                if (m_remainingAmmos.TryGetValue(weaponDefinition.AmmoMagazinesId[i], out remainingAmmo))
                {
                    if (remainingAmmo > 0)
                    {
                        return SwitchAmmoMagazine(weaponDefinition.AmmoMagazinesId[i]);
                    }
                }
                if (m_user.AmmoInventory.GetItemAmount(weaponDefinition.AmmoMagazinesId[i]) > 0)
                {
                    return SwitchAmmoMagazine(weaponDefinition.AmmoMagazinesId[i]);
                }
            }
            return false;
        }

        public bool HasEnoughAmmunition()
        {
            if (Sync.IsServer == false)
            {
                return m_cachedAmmunitionAmount > 0;
            }

            if (MySession.Static.CreativeMode)
                return true;

            if (CurrentAmmo < AMMO_PER_SHOOT) // so far it is always one bullet per shot. If anything, WeaponDefinition has to be extended.
            {
                return m_user.AmmoInventory.GetItemAmount(CurrentAmmoMagazineId) > 0;
            }
            return true;
        }

        public void ConsumeAmmo()
        {
            if (Sync.IsServer)
            {
                CurrentAmmo -= AMMO_PER_SHOOT;
                if (CurrentAmmo < 0 && HasEnoughAmmunition())
                {
                    CurrentAmmo = WeaponProperties.AmmoMagazineDefinition.Capacity - 1;

                    if (!MySession.Static.CreativeMode)
                        m_user.AmmoInventory.RemoveItemsOfType(1, CurrentAmmoMagazineId);
                }

                RefreshAmmunitionAmount();
            }

            var weaponInventory = m_user.AmmoInventory;
            if (weaponInventory != null)
            {
                MyPhysicalInventoryItem? inventoryItem = null;
                if (InventoryItemId.HasValue)
                {
                    inventoryItem = weaponInventory.GetItemByID(InventoryItemId.Value);
                }
                else
                {
                    inventoryItem = weaponInventory.FindUsableItem(m_user.PhysicalItemId);
                    if (inventoryItem.HasValue)
                    {
                        InventoryItemId = inventoryItem.Value.ItemId;
                    }
                }

                if (inventoryItem.HasValue)
                {
                    var pgo = inventoryItem.Value.Content as MyObjectBuilder_PhysicalGunObject;
                    if (pgo != null)
                    {
                        var gunBaseObjectBuilder = pgo.GunEntity as IMyObjectBuilder_GunObject<MyObjectBuilder_GunBase>;
                        Debug.Assert(gunBaseObjectBuilder != null, "ObjectBuilder of an entity implementing IMyGunObject probably does not implement IMyObjectBuilder_GunObject!");

                        if (gunBaseObjectBuilder != null)
                        {
                            if (gunBaseObjectBuilder.DeviceBase == null)
                            {
                                gunBaseObjectBuilder.InitializeDeviceBase(GetObjectBuilder());
                            }
                            else
                            {
                                gunBaseObjectBuilder.GetDevice().RemainingAmmo = CurrentAmmo;
                            }
                        }
                    }
                }
            }
        }

        public void StopShoot()
        {
            m_shotProjectiles = 0;
        }

        public int GetTotalAmmunitionAmount()
        {
            return m_cachedAmmunitionAmount;
        }

        public int GetInventoryAmmoMagazinesCount()
        {
            return (int)m_user.AmmoInventory.GetItemAmount(CurrentAmmoMagazineId);
        }

        public void RefreshAmmunitionAmount()
        {
            if (Sync.IsServer == false)
            {
                return;
            }

            if (MySession.Static.CreativeMode)
            {
                m_cachedAmmunitionAmount.Value = CurrentAmmo;
                return;
            }

            if (m_user != null && m_user.AmmoInventory != null && m_weaponProperties.WeaponDefinition.HasAmmoMagazines())
            {
                if (!HasEnoughAmmunition())
                {
                    SwitchAmmoMagazineToFirstAvailable();
                }
                m_cachedAmmunitionAmount.Value = CurrentAmmo + (int)m_user.AmmoInventory.GetItemAmount(CurrentAmmoMagazineId) * m_weaponProperties.AmmoMagazineDefinition.Capacity;
            }
            else
            {
                m_cachedAmmunitionAmount.Value = 0;
            }
        }

        private MyDefinitionId GetBackwardCompatibleDefinitionId(MyObjectBuilderType typeId)
        {
            if (typeId == typeof(MyObjectBuilder_LargeGatlingTurret))
                return new MyDefinitionId(typeof(MyObjectBuilder_WeaponDefinition), "LargeGatlingTurret");
            else if (typeId == typeof(MyObjectBuilder_LargeMissileTurret))
                return new MyDefinitionId(typeof(MyObjectBuilder_WeaponDefinition), "LargeMissileTurret");
            else if (typeId == typeof(MyObjectBuilder_InteriorTurret))
                return new MyDefinitionId(typeof(MyObjectBuilder_WeaponDefinition), "LargeInteriorTurret");
            else if (typeId == typeof(MyObjectBuilder_SmallMissileLauncher)
                || typeId == typeof(MyObjectBuilder_SmallMissileLauncherReload))
                return new MyDefinitionId(typeof(MyObjectBuilder_WeaponDefinition), "SmallMissileLauncher");
            else if (typeId == typeof(MyObjectBuilder_SmallGatlingGun))
                return new MyDefinitionId(typeof(MyObjectBuilder_WeaponDefinition), "GatlingGun");

            return new MyDefinitionId();
        }

        public void AddMuzzleMatrix(MyAmmoType ammoType, Matrix localMatrix)
        {
            int iAmmoType = (int)ammoType;
            if (!m_dummiesByAmmoType.ContainsKey(iAmmoType))
            {
                m_dummiesByAmmoType[iAmmoType] = new DummyContainer();
            }

            m_dummiesByAmmoType[iAmmoType].Dummies.Add(MatrixD.Normalize(localMatrix));
        }

        public void LoadDummies(Dictionary<string, MyModelDummy> dummies)
        {
            this.dummies = dummies;
            m_dummiesByAmmoType.Clear();
            foreach (var dummy in dummies)
            {
                if (dummy.Key.ToLower().Contains("muzzle_projectile"))
                {
                    AddMuzzleMatrix(MyAmmoType.HighSpeed, dummy.Value.Matrix);

                    //just for testing with old models, holding dummy
                    m_holdingDummyMatrix = dummy.Value.Matrix;
                    m_holdingDummyMatrix = Matrix.CreateScale(1f / dummy.Value.Matrix.Scale) * m_holdingDummyMatrix;
                    m_holdingDummyMatrix = Matrix.Invert(m_holdingDummyMatrix);
                }
                else if (dummy.Key.ToLower().Contains("muzzle_missile"))
                    AddMuzzleMatrix(MyAmmoType.Missile, dummy.Value.Matrix);
                else if (dummy.Key.ToLower().Contains("holding_dummy") || dummy.Key.ToLower().Contains("holdingdummy"))//("muzzledummy"))//("holdingdummy"))
                {
                    m_holdingDummyMatrix = dummy.Value.Matrix;

                    //m_holdingDummyMatrix = Matrix.CreateScale(1f / dummy.Value.Matrix.Scale) * m_holdingDummyMatrix;
                    m_holdingDummyMatrix = Matrix.Normalize(m_holdingDummyMatrix);
                    //m_holdingDummyMatrix = Matrix.Invert(m_holdingDummyMatrix);
                }
            }
        }

        public override Vector3D GetMuzzleLocalPosition()
        {
            if (m_weaponProperties.AmmoDefinition == null)
                return Vector3D.Zero;

            MyDebug.AssertDebug(m_dummiesByAmmoType.ContainsKey((int)m_weaponProperties.AmmoDefinition.AmmoType), "Muzzle dummy missing for given ammo type");

            DummyContainer container;
            if (m_dummiesByAmmoType.TryGetValue((int)m_weaponProperties.AmmoDefinition.AmmoType, out container))
            {
                return container.DummyToUse.Translation;
            }

            return Vector3D.Zero;
        }

        public override Vector3D GetMuzzleWorldPosition()
        {
            if (m_weaponProperties.AmmoDefinition == null)
                return m_worldMatrix.Translation;

            MyDebug.AssertDebug(m_dummiesByAmmoType.ContainsKey((int)m_weaponProperties.AmmoDefinition.AmmoType), "Muzzle dummy missing for given ammo type");

            DummyContainer container;
            if (m_dummiesByAmmoType.TryGetValue((int)m_weaponProperties.AmmoDefinition.AmmoType, out container))
            {
                if (container.Dirty)
                {
                    container.DummyInWorld = container.DummyToUse * m_worldMatrix;
                    container.Dirty = false;
                }
                return container.DummyInWorld.Translation;
            }

            return m_worldMatrix.Translation;
        }

        public MatrixD GetMuzzleWorldMatrix()
        {
            if (m_weaponProperties.AmmoDefinition == null)
                return m_worldMatrix;

            MyDebug.AssertDebug(m_dummiesByAmmoType.ContainsKey((int)m_weaponProperties.AmmoDefinition.AmmoType), "Muzzle dummy missing for given ammo type");

            DummyContainer container;
            if (m_dummiesByAmmoType.TryGetValue((int)m_weaponProperties.AmmoDefinition.AmmoType, out container))
            {
                if (container.Dirty)
                {
                    container.DummyInWorld = container.DummyToUse * m_worldMatrix;
                    container.Dirty = false;
                }
                return container.DummyInWorld;
            }

            return m_worldMatrix;
        }

        private void MoveToNextMuzzle(MyAmmoType ammoType)
        {
            int index = (int)ammoType;
            DummyContainer container;
            if (m_dummiesByAmmoType.TryGetValue(index, out container))
            {
                if (container.Dummies.Count > 1)
                {
                    container.DummyIndex++;
                    if (container.DummyIndex == container.Dummies.Count)
                    {
                        container.DummyIndex = 0;
                    }
                    container.Dirty = true;
                }
            }
        }

        private void RecalculateMuzzles()
        {
            foreach (DummyContainer container in m_dummiesByAmmoType.Values)
            {
                container.Dirty = true;
            }
        }

        public void StartShootSound(MyEntity3DSoundEmitter soundEmitter)
        {
            if (ShootSound != null && soundEmitter != null)
            {
                if (soundEmitter.IsPlaying)
                {
                    if (!soundEmitter.Loop)
                        soundEmitter.PlaySound(ShootSound, false);
                }
                else
                    soundEmitter.PlaySound(ShootSound, true);
            }
        }

        internal void StartNoAmmoSound(MyEntity3DSoundEmitter soundEmitter)
        {
            if (NoAmmoSound != null && soundEmitter != null)
            {
                soundEmitter.StopSound(true);
                soundEmitter.PlaySingleSound(NoAmmoSound, true);
            }
        }
    }
}
