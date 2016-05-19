using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_SessionComponent : MyObjectBuilder_Base
    {
        public SerializableDefinitionId? Definition { get; set; }

        public bool ShouldSerializeDefinition()
        {
            return Definition.HasValue;
        }
    }
}
