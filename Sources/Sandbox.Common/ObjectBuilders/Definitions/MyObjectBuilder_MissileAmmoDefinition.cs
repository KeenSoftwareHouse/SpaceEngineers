using ProtoBuf;
using Sandbox.Common.ObjectBuilders.VRageData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using VRage.Data;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MissileAmmoDefinition : MyObjectBuilder_AmmoDefinition
    {
        [ProtoContract]
        public class AmmoMissileProperties
        {
            [ProtoMember(1)]
            public float MissileMass;

            [ProtoMember(2)]
            public float MissileExplosionRadius;

            [ProtoMember(3)]
            [ModdableContentFile("mwm")]
            public string MissileModelName;

            [ProtoMember(4)]
            public float MissileAcceleration;

            [ProtoMember(5)]
            public float MissileInitialSpeed;

            [ProtoMember(6)]
            public bool MissileSkipAcceleration;

            [ProtoMember(7)]
            public float MissileExplosionDamage;
        }

        [ProtoMember(1), DefaultValue(null)]
        public AmmoMissileProperties MissileProperties;
    }
}
