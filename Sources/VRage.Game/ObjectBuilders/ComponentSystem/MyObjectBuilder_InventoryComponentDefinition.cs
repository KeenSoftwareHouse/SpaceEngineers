using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_InventoryComponentDefinition : MyObjectBuilder_ComponentDefinitionBase
    {
        [ProtoMember]
        public SerializableVector3? Size; //in m (if defined then it overrides Volume)

        [ProtoMember]
        public float Volume = float.MaxValue; //in m3

        [ProtoMember]
        public float Mass = float.MaxValue; // in kg

        [ProtoMember]
        public bool RemoveEntityOnEmpty = false;

        [ProtoMember]
        public bool MultiplierEnabled = true;
    }
}
