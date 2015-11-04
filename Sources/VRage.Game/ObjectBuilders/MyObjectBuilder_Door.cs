using ProtoBuf;
using System.ComponentModel;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Door : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember, DefaultValue(false)]
        public bool State = false;

        //[ProtoMember, DefaultValue(true)]
        //public bool Enabled = true;

        [ProtoMember, DefaultValue(0f)]
        public float Opening = 0f;
        
        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public string OpenSound;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public string CloseSound;
    }
}
