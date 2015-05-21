using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_WeaponDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public class WeaponAmmoData
        {
            [XmlAttribute]
            public int RateOfFire;

            [XmlAttribute]
            public string ShootSoundName;
        }

        [ProtoContract]
        public class WeaponAmmoMagazine
        {
            [XmlIgnore]
            public MyObjectBuilderType Type = typeof(MyObjectBuilder_AmmoMagazine);

            [XmlAttribute]
            [ProtoMember]
            public string Subtype;
        }

        [ProtoMember]
        public WeaponAmmoData ProjectileAmmoData = null;

        [ProtoMember]
        public WeaponAmmoData MissileAmmoData = null;

        [ProtoMember]
        public string NoAmmoSoundName = null;

        [ProtoMember]
        public float DeviateShotAngle = 0;

        [ProtoMember]
        public float ReleaseTimeAfterFire = 0;

        [ProtoMember]
        public int MuzzleFlashLifeSpan = 0;

        [XmlArrayItem("AmmoMagazine")]
        [ProtoMember]
        public WeaponAmmoMagazine[] AmmoMagazines;
    }
}
