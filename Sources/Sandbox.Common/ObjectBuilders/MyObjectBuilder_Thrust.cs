using System.ComponentModel;
using ProtoBuf;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Thrust : MyObjectBuilder_FunctionalBlock
    {
        //[ProtoMember(1), DefaultValue(true)]
        //public bool Enabled = true;

        [ProtoMember(2), DefaultValue(0.0f)]
        public float ThrustOverride = 0.0f;
    }
}
