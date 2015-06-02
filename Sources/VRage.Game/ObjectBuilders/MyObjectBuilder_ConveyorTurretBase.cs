using ProtoBuf;
using System.ComponentModel;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ConveyorTurretBase : MyObjectBuilder_TurretBase
    {
        [ProtoMember, DefaultValue(true)]
        public bool UseConveyorSystem = true;
    }
}
