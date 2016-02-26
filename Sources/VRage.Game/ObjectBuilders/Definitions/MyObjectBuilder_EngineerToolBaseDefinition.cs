using ProtoBuf;
using System.ComponentModel;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_EngineerToolBaseDefinition : MyObjectBuilder_HandItemDefinition
    {
        [ProtoMember, DefaultValue(1)]
        public float SpeedMultiplier = 1;

        [ProtoMember, DefaultValue(1)]
        public float DistanceMultiplier = 1;
    }
}
