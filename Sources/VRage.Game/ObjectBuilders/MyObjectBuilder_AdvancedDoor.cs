using ProtoBuf;
using System.ComponentModel;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AdvancedDoor : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember, DefaultValue(false)]
        public bool Open = false;

        [ProtoMember, DefaultValue(2f)]
        public float AutocloseInterval = 2f;

        [ProtoMember, DefaultValue(false)]
        public bool Autoclose = false;
    }
}
