using ProtoBuf;
using System.Xml.Serialization;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_InventoryItem : MyObjectBuilder_Base
    {
        [ProtoMember]
        [XmlElement("Amount")]
        public MyFixedPoint Amount = 1;

        [ProtoMember]
        [XmlElement("Scale")]
        public float Scale = 1.0f;
        public bool ShouldSerializeScale() { return Scale != 1.0f; }

        [XmlElement("AmountDecimal")]
        [NoSerialize]
        public decimal Obsolete_AmountDecimal
        {
            get { return (decimal)Amount; }
            set { Amount = (MyFixedPoint)value; }
        }
        public bool ShouldSerializeObsolete_AmountDecimal() { return false; }

        /// <summary>
        /// Obsolete. It is here only to keep backwards compatibility with old saves. Nulls content when unsupported.
        /// </summary>
        [ProtoMember]
        [XmlElement(Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_PhysicalObject>))]
        [DynamicObjectBuilder]
        [Serialize(MyObjectFlags.Nullable)]
        public MyObjectBuilder_PhysicalObject Content;

        public bool ShouldSerializeContent() { return false; }

        [ProtoMember]
        [XmlElement(Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_PhysicalObject>))]
        [DynamicObjectBuilder]
        [Serialize(MyObjectFlags.Nullable)]
        public MyObjectBuilder_PhysicalObject PhysicalContent;

        [ProtoMember]
        public uint ItemId = 0;
    }
}
