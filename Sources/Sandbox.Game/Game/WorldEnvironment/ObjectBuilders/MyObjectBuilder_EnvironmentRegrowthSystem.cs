using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Game.WorldEnvironment.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_EnvironmentRegrowthSystem: MyObjectBuilder_SessionComponent
    {
        [ProtoMember]
        public int TimeSinceLastEventInMs;
    }
}
