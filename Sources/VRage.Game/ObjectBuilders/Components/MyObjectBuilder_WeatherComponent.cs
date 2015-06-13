using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_WeatherComponent : MyObjectBuilder_SessionComponent
    {
        [ProtoMember]
        public bool WeatherEnabled;
    }
}
