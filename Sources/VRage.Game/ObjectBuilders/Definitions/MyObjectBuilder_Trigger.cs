using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Trigger : MyObjectBuilder_Base
    {
        [ProtoMember]
        public bool IsTrue;
        [ProtoMember]
        public string Message;
        [ProtoMember]
        public string WwwLink;
        [ProtoMember]
        public string NextMission;
    }
}
