using ProtoBuf;
using System.ComponentModel;

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
        public string OpenSound;

        [ProtoMember]
        public string CloseSound;
    }
}
