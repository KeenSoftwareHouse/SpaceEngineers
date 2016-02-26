using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_RepairBlueprintDefinition : MyObjectBuilder_BlueprintDefinition
    {
        [ProtoMember]
        public float RepairAmount = 0;
    }
}
