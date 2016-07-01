using ProtoBuf;
using System.ComponentModel;
using VRage.ObjectBuilders;

namespace VRage.Game
{
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

            [ProtoMember]
            public string ProjectileOnHitEffectName = "Hit_BasicAmmoSmall";

            [ProtoMember]
            public float ProjectileMassDamage;

            [ProtoMember]
            public float ProjectileHealthDamage;

            [ProtoMember]
            public bool HeadShot;

            [ProtoMember, DefaultValue(120)]
            public float ProjectileHeadShotDamage = 120;

            [ProtoMember, DefaultValue(1)]
            public int ProjectileCount = 1;
        }

        [ProtoMember, DefaultValue(null)]
        public AmmoProjectileProperties ProjectileProperties;
    }
}