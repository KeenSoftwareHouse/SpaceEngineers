using VRageMath;
using System;
using VRage.Utils;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using System.Collections.Generic;
using VRage.Game;

namespace Sandbox.Game.Weapons
{
    public class MyWeaponPropertiesWrapper
    {
        private MyWeaponDefinition m_weaponDefinition;
        private MyAmmoDefinition m_ammoDefinition;
        private MyAmmoMagazineDefinition m_ammoMagazineDefinition;

        public MyDefinitionId WeaponDefinitionId { get; private set; }
        public MyDefinitionId AmmoMagazineId { get; private set; }
        public MyDefinitionId AmmoDefinitionId { get; private set; }

        public MyWeaponPropertiesWrapper(MyDefinitionId weaponDefinitionId)
        {
            WeaponDefinitionId = weaponDefinitionId;
            m_weaponDefinition = MyDefinitionManager.Static.GetWeaponDefinition(WeaponDefinitionId);
        }

        public bool CanChangeAmmoMagazine(MyDefinitionId newAmmoMagazineId)
        {
            return WeaponDefinition.IsAmmoMagazineCompatible(newAmmoMagazineId);
        }

        public void ChangeAmmoMagazine(MyDefinitionId newAmmoMagazineId)
        {
            MyDebug.AssertDebug(WeaponDefinition.IsAmmoMagazineCompatible(newAmmoMagazineId), "Ammo magazine is changed to not compatible one");

            AmmoMagazineId = newAmmoMagazineId;
            m_ammoMagazineDefinition = MyDefinitionManager.Static.GetAmmoMagazineDefinition(AmmoMagazineId);
            AmmoDefinitionId = AmmoMagazineDefinition.AmmoDefinitionId;
            m_ammoDefinition = MyDefinitionManager.Static.GetAmmoDefinition(AmmoDefinitionId);

        }

        public T GetCurrentAmmoDefinitionAs<T>() where T : MyAmmoDefinition
        {
            return AmmoDefinition as T;
        }
       
        public MyAmmoDefinition AmmoDefinition
        {
            get { return m_ammoDefinition; }
        }

        public MyWeaponDefinition WeaponDefinition
        {
            get { return m_weaponDefinition; }
        }

        public MyAmmoMagazineDefinition AmmoMagazineDefinition
        {
            get { return m_ammoMagazineDefinition;  }
        }

        public int AmmoMagazinesCount
        {
            get { return WeaponDefinition.AmmoMagazinesId.Length; }
        }

        public bool IsAmmoProjectile
        {
            get { return AmmoDefinition.AmmoType == MyAmmoType.HighSpeed; }
        }

        public bool IsAmmoMissile
        {
            get { return AmmoDefinition.AmmoType == MyAmmoType.Missile; }
        }

        public bool IsDeviated
        {
            get { return WeaponDefinition.DeviateShotAngle != 0.0f; }
        }

        public int CurrentWeaponRateOfFire
        {
            get { return m_weaponDefinition.WeaponAmmoDatas[(int)AmmoDefinition.AmmoType].RateOfFire; }
        }

        public int ShotsInBurst
        {
            get { return m_weaponDefinition.WeaponAmmoDatas[(int)AmmoDefinition.AmmoType].ShotsInBurst; }
        }

        public int ReloadTime
        {
            get { return m_weaponDefinition.ReloadTime; }
        }

        public int CurrentWeaponShootIntervalInMiliseconds
        {
            get { return m_weaponDefinition.WeaponAmmoDatas[(int)AmmoDefinition.AmmoType].ShootIntervalInMiliseconds; }
        }

        public MySoundPair CurrentWeaponShootSound
        {
            get { return m_weaponDefinition.WeaponAmmoDatas[(int)AmmoDefinition.AmmoType].ShootSound; }
        }
    }

    static class MyAmmoConstants
    {
        public const int AMMO_ENUM_START = 100;
        public const float ArmorEffectivityVsPiercingAmmo = 0.5f;

        static MyAmmoConstants()
        {
            //VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyAmmoConstants()");

            //VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }
    }
}
