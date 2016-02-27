using ProtoBuf;
using VRage.ObjectBuilders;
using System.ComponentModel;
using VRage.Data;
using VRage.Game;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
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
