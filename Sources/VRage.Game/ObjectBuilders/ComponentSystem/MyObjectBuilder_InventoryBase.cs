using ProtoBuf;
using System.ComponentModel;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace VRage.Game.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_InventoryBase : MyObjectBuilder_ComponentBase
    {
        [ProtoMember, DefaultValue(null)]
        [Serialize(MyObjectFlags.Nullable)]
        public string InventoryId = null;

        public virtual void Clear()
        {
        }

    }
}
