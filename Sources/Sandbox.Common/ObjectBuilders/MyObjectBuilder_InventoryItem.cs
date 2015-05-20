using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Serializer;
using System;
using System.Diagnostics;
using System.Xml.Serialization;
using VRage;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_InventoryItem : MyObjectBuilder_Base
    {
        [ProtoMember]
        [XmlElement("Amount")]
        public MyFixedPoint Amount;

        [XmlElement("AmountDecimal")]
        public decimal Obsolete_AmountDecimal
        {
            get { return (decimal)Amount; }
            set { Amount = (MyFixedPoint)value; }
        }
        public bool ShouldSerializeAmountDecimal() { return false; }

        /// <summary>
        /// Obsolete. It is here only to keep backwards compatibility with old saves
        /// </summary>
        public MyObjectBuilder_Base Content
        {
            get { return PhysicalContent; }
            set
            {
                if (value is MyObjectBuilder_PhysicalObject)
                    PhysicalContent = (MyObjectBuilder_PhysicalObject)value;
                else if (value is MyObjectBuilder_HandDrill)
                {
                    var tmp = Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_PhysicalGunObject>("HandDrillItem");
                    tmp.GunEntity = (MyObjectBuilder_HandDrill)value;
                    tmp.GunEntity.EntityId = 0;
                    PhysicalContent = tmp;
                }
                else if (value is MyObjectBuilder_AutomaticRifle)
                {
                    var tmp = Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_PhysicalGunObject>("AutomaticRifleItem");
                    tmp.GunEntity = (MyObjectBuilder_AutomaticRifle)value;
                    tmp.GunEntity.EntityId = 0;
                    PhysicalContent = tmp;
                }
                else if (value is MyObjectBuilder_Welder)
                {
                    var tmp = Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_PhysicalGunObject>("WelderItem");
                    tmp.GunEntity = (MyObjectBuilder_Welder)value;
                    tmp.GunEntity.EntityId = 0;
                    PhysicalContent = tmp;
                }
                else if (value is MyObjectBuilder_AngleGrinder)
                {
                    var tmp = Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_PhysicalGunObject>("AngleGrinderItem");
                    tmp.GunEntity = (MyObjectBuilder_AngleGrinder)value;
                    tmp.GunEntity.EntityId = 0;
                    PhysicalContent = tmp;
                }
                else
                    Debug.Fail("Invalid branch reached.");
            }
        }
        public bool ShouldSerializeContent() { return false; }

        [ProtoMember]
        [XmlElement("PhysicalContent", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_PhysicalObject>))]
        public MyObjectBuilder_PhysicalObject PhysicalContent;

        [ProtoMember]
        public uint ItemId;
    }
}
