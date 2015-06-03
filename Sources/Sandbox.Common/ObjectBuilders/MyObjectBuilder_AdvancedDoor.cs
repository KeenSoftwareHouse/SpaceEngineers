using ProtoBuf;
using System.ComponentModel;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AdvancedDoor : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember, DefaultValue(false)]
        public bool Open = false;
    }
}
