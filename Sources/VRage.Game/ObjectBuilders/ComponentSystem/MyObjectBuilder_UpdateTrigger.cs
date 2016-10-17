using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_UpdateTrigger : MyObjectBuilder_TriggerBase
    {
        public int Size = 25000;
    }
}
