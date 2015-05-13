using ProtoBuf;
using Sandbox.Common.ObjectBuilders.VRageData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ProjectileAmmoDefinition : MyObjectBuilder_AmmoDefinition
    {
        [ProtoContract]
        public class AmmoProjectileProperties
        {
            [ProtoMember(1)]
            public float ProjectileHitImpulse;

            [ProtoMember(2), DefaultValue(0.1f)]
            public float ProjectileTrailScale = 0.1f;

            [ProtoMember(3)]
            public SerializableVector3 ProjectileTrailColor = new SerializableVector3(1.0f, 1.0f, 1.0f);

            [ProtoMember(4), DefaultValue(0.5f)]
            public float ProjectileTrailProbability = 0.5f;

            [ProtoMember(5), DefaultValue(MyCustomHitMaterialMethodType.Small)]
            public MyCustomHitMaterialMethodType ProjectileOnHitMaterialParticlesType = MyCustomHitMaterialMethodType.Small;

            [ProtoMember(6), DefaultValue(MyCustomHitParticlesMethodType.BasicSmall)]
            public MyCustomHitParticlesMethodType ProjectileOnHitParticlesType = MyCustomHitParticlesMethodType.BasicSmall;

            [ProtoMember(7)]
            public float ProjectileMassDamage;

            [ProtoMember(8)]
            public float ProjectileHealthDamage;
        }

        [ProtoMember(1), DefaultValue(null)]
        public AmmoProjectileProperties ProjectileProperties;
    }
}
