using ProtoBuf;
using System.ComponentModel;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AirVent : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public bool IsDepressurizing;

        [ProtoMember, DefaultValue(null)]
        public MyObjectBuilder_ToolbarItem OnEmptyAction = null;

        [ProtoMember, DefaultValue(null)]
        public MyObjectBuilder_ToolbarItem OnFullAction = null;
    }
}
