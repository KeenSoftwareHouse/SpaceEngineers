using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders.Definitions;

using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Game.Definitions;
using VRageMath;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_WeaponDefinition))]
    public class MyWeaponDefinition : MyDefinitionBase
    {
        public class MyWeaponAmmoData
        {
            public int RateOfFire; // rounds per minute (round == 1 bullet)
            public int ShotsInBurst;
            public MySoundPair ShootSound;
            public int ShootIntervalInMiliseconds; // derivative of Rate of fire

            public MyWeaponAmmoData(MyObjectBuilder_WeaponDefinition.WeaponAmmoData data) : this(data.RateOfFire, data.ShootSoundName, data.ShotsInBurst)
            {
            }

            public MyWeaponAmmoData(int rateOfFire, string soundName, int shotsInBurst)
            {
                this.RateOfFire = rateOfFire;
                this.ShotsInBurst = shotsInBurst;
                this.ShootSound = new MySoundPair(soundName);
                this.ShootIntervalInMiliseconds = (int)(1000 / (RateOfFire * oneSixtieth));
            }
        }

        public enum WeaponEffectAction
        {
            Unknown,
            Shoot
        }

        public class MyWeaponEffect
        {
            public WeaponEffectAction Action = WeaponEffectAction.Unknown;
            public string Dummy = "";
            public string Particle = "";
            public bool Loop = false;

            public MyWeaponEffect(string action, string dummy, string particle, bool loop)
            {
                this.Dummy = dummy;
                this.Particle = particle;
                this.Loop = loop;

                foreach (WeaponEffectAction act in Enum.GetValues(typeof(WeaponEffectAction)))
                {
                    if (act.ToString().Equals(action))
                    {
                        Action = act;
                        break;
                    }
                }
            }
        }

        public const float oneSixtieth = 1.0f / 60.0f;
        private static readonly string ErrorMessageTemplate = "No weapon ammo data specified for {0} ammo (<{1}AmmoData> tag is missing in weapon definition)";

        public MySoundPair NoAmmoSound;
        public MySoundPair ReloadSound;
        public MySoundPair SecondarySound;
        public float DeviateShotAngle;
        public float ReleaseTimeAfterFire;
        public int MuzzleFlashLifeSpan;
        public MyDefinitionId[] AmmoMagazinesId;
        public MyWeaponAmmoData[] WeaponAmmoDatas;
        public MyWeaponEffect[] WeaponEffects;
        public MyStringHash PhysicalMaterial;
        public bool UseDefaultMuzzleFlash;
        public int ReloadTime = 2000;

        public bool HasProjectileAmmoDefined
        {
            get { return WeaponAmmoDatas[(int)MyAmmoType.HighSpeed] != null; }
        }
        public bool HasMissileAmmoDefined
        {
            get { return WeaponAmmoDatas[(int)MyAmmoType.Missile] != null; }
        }

        public bool HasSpecificAmmoData(MyAmmoDefinition ammoDefinition)
        {
            return WeaponAmmoDatas[(int)ammoDefinition.AmmoType] != null; 
        }

        public bool HasAmmoMagazines()
        {
            return AmmoMagazinesId != null && AmmoMagazinesId.Length > 0;
        }

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_WeaponDefinition;
            MyDebug.AssertDebug(ob != null);

            this.WeaponAmmoDatas = new MyWeaponAmmoData[Enum.GetValues(typeof(MyAmmoType)).Length];
            this.WeaponEffects = new MyWeaponEffect[ob.Effects == null ? 0 : ob.Effects.Length];
            if(ob.Effects != null){
                for (int i = 0; i < ob.Effects.Length; i++)
                    this.WeaponEffects[i] = new MyWeaponEffect(ob.Effects[i].Action, ob.Effects[i].Dummy, ob.Effects[i].Particle, ob.Effects[i].Loop);
            }
            this.PhysicalMaterial = MyStringHash.GetOrCompute(ob.PhysicalMaterial);
            this.UseDefaultMuzzleFlash = ob.UseDefaultMuzzleFlash;
            this.NoAmmoSound = new MySoundPair(ob.NoAmmoSoundName);
            this.ReloadSound = new MySoundPair(ob.ReloadSoundName);
            this.SecondarySound = new MySoundPair(ob.SecondarySoundName);
            this.DeviateShotAngle = MathHelper.ToRadians(ob.DeviateShotAngle);
            this.ReleaseTimeAfterFire = ob.ReleaseTimeAfterFire;
            this.MuzzleFlashLifeSpan = ob.MuzzleFlashLifeSpan;
            this.ReloadTime = ob.ReloadTime;

            this.AmmoMagazinesId = new MyDefinitionId[ob.AmmoMagazines.Length];
            for (int i = 0; i < this.AmmoMagazinesId.Length; i++)
            {
                var ammoMagazine = ob.AmmoMagazines[i];
                this.AmmoMagazinesId[i] = new MyDefinitionId(ammoMagazine.Type, ammoMagazine.Subtype);

                var ammoMagazineDefinition = MyDefinitionManager.Static.GetAmmoMagazineDefinition(this.AmmoMagazinesId[i]);
                MyAmmoType ammoType = MyDefinitionManager.Static.GetAmmoDefinition(ammoMagazineDefinition.AmmoDefinitionId).AmmoType;
                string errorMessage = null;
                switch (ammoType)
                {
                    case MyAmmoType.HighSpeed:
                        MyDebug.AssertDebug(ob.ProjectileAmmoData != null, "No weapon ammo data specified for projectile ammo");
                        if (ob.ProjectileAmmoData != null)
                        {
                            this.WeaponAmmoDatas[(int)MyAmmoType.HighSpeed] = new MyWeaponAmmoData(ob.ProjectileAmmoData);
                        }
                        else
                        {
                            errorMessage  = string.Format(ErrorMessageTemplate, "projectile", "Projectile");
                        }
                        break;
                    case MyAmmoType.Missile:
                         MyDebug.AssertDebug(ob.MissileAmmoData != null, "No weapon ammo data specified for missile ammo");
                        if (ob.MissileAmmoData != null)
                        {
                            this.WeaponAmmoDatas[(int)MyAmmoType.Missile] = new MyWeaponAmmoData(ob.MissileAmmoData);
                        }
                        else
                        {
                            errorMessage = string.Format(ErrorMessageTemplate, "missile", "Missile");
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    MyDefinitionErrors.Add(Context, errorMessage, TErrorSeverity.Critical);
                }
            }
        }

        public bool IsAmmoMagazineCompatible(MyDefinitionId ammoMagazineDefinitionId)
        {
            for (int i = 0; i < AmmoMagazinesId.Length; i++)
                if (ammoMagazineDefinitionId.SubtypeId == AmmoMagazinesId[i].SubtypeId)
                {
                    return true;
                }
            return false;
        }

        public int GetAmmoMagazineIdArrayIndex(MyDefinitionId ammoMagazineId)
        {
            for (int i = 0; i < AmmoMagazinesId.Length; i++)
            {
                if (ammoMagazineId.SubtypeId == AmmoMagazinesId[i].SubtypeId)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
