using ProtoBuf;
using System.ComponentModel;
using VRage;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    public enum MyProjectileType
    {
        Bullet,
        Bolt
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ProjectileAmmoDefinition : MyObjectBuilder_AmmoDefinition
    {
        [ProtoContract]
        public class AmmoProjectileProperties
        {
            [ProtoMember]
            public float ProjectileHitImpulse;

            [ProtoMember, DefaultValue(0.1f)]
            public float ProjectileTrailScale = 0.1f;

            [ProtoMember]
            public SerializableVector3 ProjectileTrailColor = new SerializableVector3(1.0f, 1.0f, 1.0f);

            [ProtoMember, DefaultValue(null)]
            public string ProjectileTrailMaterial = null;

            [ProtoMember, DefaultValue(0.5f)]
            public float ProjectileTrailProbability = 0.5f;

            [ProtoMember, DefaultValue(MyCustomHitMaterialMethodType.Small)]
            public MyCustomHitMaterialMethodType ProjectileOnHitMaterialParticlesType = MyCustomHitMaterialMethodType.Small;

            [ProtoMember, DefaultValue(MyCustomHitParticlesMethodType.BasicSmall)]
            public MyCustomHitParticlesMethodType ProjectileOnHitParticlesType = MyCustomHitParticlesMethodType.BasicSmall;

            [ProtoMember]
            public float ProjectileMassDamage;

            [ProtoMember]
            public float ProjectileHealthDamage;

            [ProtoMember]
            public bool HeadShot;

            [ProtoMember, DefaultValue(120)]
            public float ProjectileHeadShotDamage = 120;

            [ProtoMember, DefaultValue(MyProjectileType.Bullet)]
            public MyProjectileType ProjectileType = MyProjectileType.Bullet;
        }

        [ProtoMember, DefaultValue(null)]
        public AmmoProjectileProperties ProjectileProperties;
    }
}
