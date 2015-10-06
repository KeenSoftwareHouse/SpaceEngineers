using ProtoBuf;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_StockpileItem : MyObjectBuilder_Base
    {
        [ProtoMember]
        public int Amount;

        [ProtoMember]
        [DynamicObjectBuilder]
        public MyObjectBuilder_PhysicalObject PhysicalContent;
    }
}
