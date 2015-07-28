using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Common.ObjectBuilders.VRageData;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_AmmoDefinition))]
    public abstract class MyAmmoDefinition : MyDefinitionBase
    {
        public MyAmmoType AmmoType;
        public float DesiredSpeed;     // In metres/second
        public float SpeedVar;         // speed *= MyMwcUtils.GetRandomFloat(1.0f - SpeedVar, 1.0f + SpeedVar)
        public float MaxTrajectory;    // How far can projectile fly before we kill it (it's like distance timeout)
        public bool IsExplosive;       // Ammo explodes with some probability
        public float BackkickForce;
        //public float Impulse;          // Projectile impulse on hit
        //public float MassDamage;       // Damage to armors, floating objects, meteors
        //public float HealthDamage;     // Damage to living species
        //// Radius to create voxel damage
        //public Vector3 TrailColor;     // Color of ammo trail, if any
              
        ////       public float DecalEmissivity;    // Decals shines in dark, not used

        ////       public MyStringHash DamageType; //Type of damage caused by this ammo
        //public MyAmmoType AmmoType; //Type of ammo


        ////        public bool IsMissile;

        //public float TrailScale;
        //public float TrailProbability;

        //public MyCustomHitParticlesMethod OnHitParticles;  // Delegate to method to create hit particles
        //public MyCustomHitMaterialMethod OnHitMaterialSpecificParticles; // Delegate to method to create material specific particles

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_AmmoDefinition;
            MyDebug.AssertDebug(ob != null);

            this.DesiredSpeed = ob.BasicProperties.DesiredSpeed;
            this.SpeedVar = ob.BasicProperties.SpeedVariance;
            this.MaxTrajectory = ob.BasicProperties.MaxTrajectory;
            this.IsExplosive = ob.BasicProperties.IsExplosive;
            this.BackkickForce = ob.BasicProperties.BackkickForce;
 //           this.HealthDamage = ob.DamageProperties.HealthDamage;
 //           this.MassDamage = ob.DamageProperties.MassDamage;
 //           this.ExplosionRadius = ob.DamageProperties.ExplosionRadius;  
 //           this.IsExplosive = ob.DamageProperties.IsExplosive;
 //           if (this.IsExplosive)
 //               MyDebug.AssertDebug(this.ExplosionRadius > MINIMAL_EXPLOSION_RADIUS);
 //           this.DamageType = ob.DamageProperties.DamageType;
 //           this.AmmoType = ob.DamageProperties.AmmoType;

 //           this.OnHitParticles = MyParticleEffects.GetCustomHitParticlesMethodById((int)ob.AdditionalProperties.OnHitParticlesType);
 //           this.OnHitMaterialSpecificParticles = MyParticleEffects.GetCustomHitMaterialMethodById((int)ob.AdditionalProperties.OnHitMaterialParticlesType);

 ////           if (ob.AdditionalProperties.Trail.Color.HasValue)
 //           this.TrailColor = ob.AdditionalProperties.TrailColor;
 //           this.TrailScale = ob.AdditionalProperties.TrailScale;
 //           this.TrailProbability = ob.AdditionalProperties.TrailProbability;
            
 // //          else
 // //              this.TrailColor = MyProjectilesConstants.GetProjectileTrailColorByType(this.AmmoType);

            
 //           this.IsMissile = ob.AdditionalProperties.IsMissile;
            //if (this.IsMissile)
            //{
            //    MyDebug.AssertDebug(ob.AdditionalProperties.ModelName != null, "When ammo is a missile, set its model name!");
            //    if (ob.AdditionalProperties.ModelName == null)
            //        this.ModelName = "Models\\Weapons\\Projectile_Missile.mwm";
            //    else
            //        this.ModelName = ob.AdditionalProperties.ModelName;
            //}     
        }

        public abstract float GetDamageForMechanicalObjects();
    }

    [MyDefinitionType(typeof(MyObjectBuilder_MissileAmmoDefinition))]
    public class MyMissileAmmoDefinition : MyAmmoDefinition
    {
        public const float MINIMAL_EXPLOSION_RADIUS = 0.6f;

        public float MissileMass;
        public float MissileExplosionRadius;
        public string MissileModelName;
        public float MissileAcceleration;
        public float MissileInitialSpeed;
        public bool MissileSkipAcceleration;
        public float MissileExplosionDamage;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_MissileAmmoDefinition;
            MyDebug.AssertDebug(ob != null);

            this.AmmoType = MyAmmoType.Missile;

            MyObjectBuilder_MissileAmmoDefinition.AmmoMissileProperties missileProperties = ob.MissileProperties;
            MyDebug.AssertDebug(missileProperties != null);

            this.MissileAcceleration = missileProperties.MissileAcceleration;
            this.MissileExplosionDamage = missileProperties.MissileExplosionDamage;
            this.MissileExplosionRadius = missileProperties.MissileExplosionRadius;
            this.MissileInitialSpeed = missileProperties.MissileInitialSpeed;
            this.MissileMass = missileProperties.MissileMass;
            this.MissileModelName = missileProperties.MissileModelName;
            this.MissileSkipAcceleration = missileProperties.MissileSkipAcceleration;
        }

        public override float GetDamageForMechanicalObjects()
        {
            return MissileExplosionDamage;
        }
    }

    [MyDefinitionType(typeof(MyObjectBuilder_ProjectileAmmoDefinition))]
    public class MyProjectileAmmoDefinition : MyAmmoDefinition
    {
        public float ProjectileHitImpulse;
        public float ProjectileTrailScale;
        public Vector3 ProjectileTrailColor;
        public string ProjectileTrailMaterial;
        public float ProjectileTrailProbability;
        public MyCustomHitMaterialMethod ProjectileOnHitMaterialParticles;
        public MyCustomHitParticlesMethod ProjectileOnHitParticles;
        public float ProjectileMassDamage;
        public float ProjectileHealthDamage;
        public bool HeadShot;
        public float ProjectileHeadShotDamage;
        public MyProjectileType ProjectileType;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_ProjectileAmmoDefinition;
            MyDebug.AssertDebug(ob != null);

            this.AmmoType = MyAmmoType.HighSpeed;

            MyObjectBuilder_ProjectileAmmoDefinition.AmmoProjectileProperties projectileProperties = ob.ProjectileProperties;
            MyDebug.AssertDebug(projectileProperties != null);

            this.ProjectileHealthDamage = projectileProperties.ProjectileHealthDamage;
            this.ProjectileHitImpulse = projectileProperties.ProjectileHitImpulse;
            this.ProjectileMassDamage = projectileProperties.ProjectileMassDamage;
            this.ProjectileOnHitMaterialParticles = MyParticleEffects.GetCustomHitMaterialMethodById((int)projectileProperties.ProjectileOnHitMaterialParticlesType);
            this.ProjectileOnHitParticles = MyParticleEffects.GetCustomHitParticlesMethodById((int)projectileProperties.ProjectileOnHitParticlesType);
            this.ProjectileTrailColor = projectileProperties.ProjectileTrailColor;
            this.ProjectileTrailMaterial = projectileProperties.ProjectileTrailMaterial;
            this.ProjectileTrailProbability = projectileProperties.ProjectileTrailProbability;
            this.ProjectileTrailScale = projectileProperties.ProjectileTrailScale;
            this.HeadShot = projectileProperties.HeadShot;
            this.ProjectileHeadShotDamage = projectileProperties.ProjectileHeadShotDamage;
            this.ProjectileType = projectileProperties.ProjectileType;
        }

        public override float GetDamageForMechanicalObjects()
        {
            return ProjectileMassDamage;
        }
    }
}
