using ProtoBuf;
using System.ComponentModel;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace VRage.Game.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ModStorageComponentDefinition : MyObjectBuilder_ComponentDefinitionBase
    {
        [ProtoMember, DefaultValue(null)]
        [Serialize(MyObjectFlags.Nullable | MyObjectFlags.DefaultValueOrEmpty)]
        public System.Guid[] RegisteredStorageGuids;
    }
}
