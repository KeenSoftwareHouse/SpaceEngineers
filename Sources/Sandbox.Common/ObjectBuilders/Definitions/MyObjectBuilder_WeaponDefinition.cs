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
            [ProtoMember(1)]
            public string Subtype;
        }

        [ProtoMember(1)]
        public WeaponAmmoData ProjectileAmmoData = null;

        [ProtoMember(2)]
        public WeaponAmmoData MissileAmmoData = null;

        [ProtoMember(3)]
        public string NoAmmoSoundName = null;

        [ProtoMember(4)]
        public float DeviateShotAngle = 0;

        [ProtoMember(5)]
        public float ReleaseTimeAfterFire = 0;

        [ProtoMember(6)]
        public int MuzzleFlashLifeSpan = 0;

        [XmlArrayItem("AmmoMagazine")]
        [ProtoMember(8)]
        public WeaponAmmoMagazine[] AmmoMagazines;
    }
}
