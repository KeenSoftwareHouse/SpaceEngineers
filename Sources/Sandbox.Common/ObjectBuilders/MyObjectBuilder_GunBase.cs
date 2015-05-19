using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRage.Serialization;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GunBase : MyObjectBuilder_DeviceBase
    {
        [ProtoContract]
        public class RemainingAmmoIns
        {
            [XmlAttribute]
            public string SubtypeName;

            [XmlAttribute]
            public int Amount;
        }
        //[ProtoMember]
        // Obsolete!
        private SerializableDictionary<string, int> m_remainingAmmos;
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
    }
}
