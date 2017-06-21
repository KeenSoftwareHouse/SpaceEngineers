using Sandbox.Common;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;
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
        public MyStringHash PhysicalMaterial;

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
            this.PhysicalMaterial = MyStringHash.GetOrCompute(ob.BasicProperties.PhysicalMaterial);
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
        public string ProjectileOnHitEffectName;
        public float ProjectileMassDamage;
        public float ProjectileHealthDamage;
        public bool HeadShot;
        public float ProjectileHeadShotDamage;
        public int ProjectileCount;//# of pellets (shotgun)

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
            this.ProjectileOnHitEffectName = projectileProperties.ProjectileOnHitEffectName;
            this.ProjectileTrailColor = projectileProperties.ProjectileTrailColor;
            this.ProjectileTrailMaterial = projectileProperties.ProjectileTrailMaterial;
            this.ProjectileTrailProbability = projectileProperties.ProjectileTrailProbability;
            this.ProjectileTrailScale = projectileProperties.ProjectileTrailScale;
            this.HeadShot = projectileProperties.HeadShot;
            this.ProjectileHeadShotDamage = projectileProperties.ProjectileHeadShotDamage;
            this.ProjectileCount = projectileProperties.ProjectileCount;
        }

        public override float GetDamageForMechanicalObjects()
        {
            return ProjectileMassDamage;
        }
    }
}
