using ProtoBuf;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_StockpileItem : MyObjectBuilder_Base
    {
        [ProtoMember]
        public int Amount;

        [ProtoMember]
        [DynamicObjectBuilder]
        [Nullable]
        public MyObjectBuilder_PhysicalObject PhysicalContent;
    }
}
