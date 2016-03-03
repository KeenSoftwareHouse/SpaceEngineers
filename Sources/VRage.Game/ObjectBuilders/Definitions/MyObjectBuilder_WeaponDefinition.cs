using ProtoBuf;
using VRage.ObjectBuilders;
using System.Xml.Serialization;

namespace VRage.Game
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

            [XmlAttribute]
            public int BurstFireRate;
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
        public string ReloadSoundName = null;

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
