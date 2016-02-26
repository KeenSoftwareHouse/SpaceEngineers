using System.ComponentModel;
using ProtoBuf;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_FloatingObject : MyObjectBuilder_EntityBase
    {
        [ProtoMember]
        public MyObjectBuilder_InventoryItem Item;

        [ProtoMember, DefaultValue(null)]
        [Serialize(MyObjectFlags.Nullable)]
        public string OreSubtypeId;
    }
}
