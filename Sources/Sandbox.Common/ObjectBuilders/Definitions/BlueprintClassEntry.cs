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
    public class BlueprintClassEntry
    {
        [ProtoMember]
        [XmlAttribute]
        public string Class;

        [XmlIgnore]
        public MyObjectBuilderType TypeId;

        [ProtoMember]
        [XmlAttribute]
        public string BlueprintTypeId
        {
            get { return TypeId.ToString(); }
            set { TypeId = MyObjectBuilderType.ParseBackwardsCompatible(value); }
        }

        [ProtoMember]
        [XmlAttribute]
        public string BlueprintSubtypeId;

        [ProtoMember, DefaultValue(true)]
        public bool Enabled = true;

        public override bool Equals(object other)
        {
            var otherBlueprint = other as BlueprintClassEntry;
            return otherBlueprint != null && otherBlueprint.Class.Equals(this.Class) && otherBlueprint.BlueprintSubtypeId.Equals(this.BlueprintSubtypeId);
        }

        public override int GetHashCode()
        {
            return Class.GetHashCode() * 7607 + BlueprintSubtypeId.GetHashCode();
        }
    }
}
