using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_SoundBlock : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public float Range = 50;

        [ProtoMember]
        public float Volume = 1;

        [ProtoMember]
        public string CueName = null;

        [ProtoMember]
        public float LoopPeriod = 1f;
    }
}
