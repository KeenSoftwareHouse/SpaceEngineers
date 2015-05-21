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
            [ProtoMember]
            public float ProjectileHitImpulse;

            [ProtoMember, DefaultValue(0.1f)]
            public float ProjectileTrailScale = 0.1f;

            [ProtoMember]
            public SerializableVector3 ProjectileTrailColor = new SerializableVector3(1.0f, 1.0f, 1.0f);

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
        }

        [ProtoMember, DefaultValue(null)]
        public AmmoProjectileProperties ProjectileProperties;
    }
}
