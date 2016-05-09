using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_DeviceBase : MyObjectBuilder_Base
    {
        [ProtoMember]
        public uint? InventoryItemId = null;
        public bool ShouldSerializeInventoryItemId() { return InventoryItemId.HasValue; }
    }
}
