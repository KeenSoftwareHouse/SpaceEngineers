using ProtoBuf;
using System.ComponentModel;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Door : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember(1), DefaultValue(false)]
        public bool State = false;

        //[ProtoMember(2), DefaultValue(true)]
        //public bool Enabled = true;

        [ProtoMember(3), DefaultValue(0f)]
        public float Opening = 0f;
        
        [ProtoMember(4)]
        public string OpenSound;

        [ProtoMember(5)]
        public string CloseSound;
    }
}
