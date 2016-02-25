using ProtoBuf;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ConstructionStockpile : MyObjectBuilder_Base
    {
        [ProtoMember]
        [XmlElement(Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_StockpileItem>))]
        public MyObjectBuilder_StockpileItem[] Items = new MyObjectBuilder_StockpileItem[0];
    }
}
