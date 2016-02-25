using ProtoBuf;
using System.ComponentModel;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ConveyorPacket : MyObjectBuilder_Base
    {
        [ProtoMember]
        public MyObjectBuilder_InventoryItem Item;

        [ProtoMember, DefaultValue(0)]
        public int LinePosition = 0;
        public bool ShouldSerializeLinePosition() { return LinePosition != 0; }
    }
}
