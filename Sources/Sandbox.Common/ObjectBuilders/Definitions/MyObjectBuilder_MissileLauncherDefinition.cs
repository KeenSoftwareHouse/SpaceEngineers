using VRage.ObjectBuilders;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MissileLauncherDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public string ProjectileMissile;
    }
}
