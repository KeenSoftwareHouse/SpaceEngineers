using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    public class BlueprintItem
    {
        [XmlIgnore]
        [ProtoMember]
        public SerializableDefinitionId Id;

        [XmlAttribute]
        public string TypeId
        {
            get { return Id.TypeId.ToString(); }
            set { Id.TypeId = MyObjectBuilderType.ParseBackwardsCompatible(value); }
        }

        [XmlAttribute]
        public string SubtypeId
        {
            get { return Id.SubtypeId; }
            set { Id.SubtypeId = value; }
        }

        /// <summary>
        /// Amount of item required or produced. For discrete objects this refers to
        /// pieces. For ingots and ore, this refers to volume in m^3.
        /// </summary>
        [XmlAttribute]
        [ProtoMember]
        public string Amount;
    }
}
