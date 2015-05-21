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
            [ProtoMember]
            public float MissileMass;

            [ProtoMember]
            public float MissileExplosionRadius;

            [ProtoMember]
            [ModdableContentFile("mwm")]
            public string MissileModelName;

            [ProtoMember]
            public float MissileAcceleration;

            [ProtoMember]
            public float MissileInitialSpeed;

            [ProtoMember]
            public bool MissileSkipAcceleration;

            [ProtoMember]
            public float MissileExplosionDamage;
        }

        [ProtoMember, DefaultValue(null)]
        public AmmoMissileProperties MissileProperties;
    }
}
