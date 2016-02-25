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
        public MyFixedPoint Amount;

        [ProtoMember]
        [XmlElement("Scale")]
        public float Scale = 1.0f;

        [XmlElement("AmountDecimal")]
        [NoSerialize]
        public decimal Obsolete_AmountDecimal
        {
            get { return (decimal)Amount; }
            set { Amount = (MyFixedPoint)value; }
        }
        public bool ShouldSerializeAmountDecimal() { return false; }

        /// <summary>
        /// Obsolete. It is here only to keep LIMITED backwards compatibility with old saves. Nulls content when unsupported.
        /// </summary>
        [NoSerialize]
        public MyObjectBuilder_Base Content
        {
            get { return PhysicalContent; }
            set { PhysicalContent = value as MyObjectBuilder_PhysicalObject; }
        }
        public bool ShouldSerializeContent() { return false; }

        [ProtoMember]
        [XmlElement(Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_PhysicalObject>))]
        [DynamicObjectBuilder]
        public MyObjectBuilder_PhysicalObject PhysicalContent;

        [ProtoMember]
        public uint ItemId;
    }
}
