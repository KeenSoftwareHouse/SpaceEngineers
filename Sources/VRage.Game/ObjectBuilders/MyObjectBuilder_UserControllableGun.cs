using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_UserControllableGun : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public bool IsShooting = false;

        [ProtoMember]
        public bool IsShootingFromTerminal = false;

        [ProtoMember]
        public bool IsLargeTurret = false;
    }
}
