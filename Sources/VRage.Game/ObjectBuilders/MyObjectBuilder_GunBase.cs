using ProtoBuf;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;
using VRage.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GunBase : MyObjectBuilder_DeviceBase
    {
        [ProtoContract]
        public class RemainingAmmoIns
        {
            [XmlAttribute]
            [Nullable]
            public string SubtypeName;

            [XmlAttribute]
            public int Amount;
        }
        //[ProtoMember]
        // Obsolete!
        private SerializableDictionary<string, int> m_remainingAmmos;

        [NoSerialize]
        public SerializableDictionary<string, int> RemainingAmmos
        {
            get { Debug.Fail("Obsolete!"); return m_remainingAmmos; }
            set 
            { 
                m_remainingAmmos = value;
                if (RemainingAmmosList == null)
                    RemainingAmmosList = new List<RemainingAmmoIns>();
                foreach (var keyVal in value.Dictionary)
                {
                    var copy = new RemainingAmmoIns();
                    copy.SubtypeName = keyVal.Key;
                    copy.Amount = keyVal.Value;
                    RemainingAmmosList.Add(copy);
                }
            }
        }
        public bool ShouldSerializeRemainingAmmos() { return false; }

        [ProtoMember]
        public int RemainingAmmo = 0;

        [ProtoMember]
        public string CurrentAmmoMagazineName = "";

        [ProtoMember]
        public List<RemainingAmmoIns> RemainingAmmosList = new List<RemainingAmmoIns>();

        [ProtoMember]
        public long LastShootTime;
    }
}
