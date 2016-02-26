using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_LastSession : MyObjectBuilder_Base
    {
        [ProtoMember]
        public string Path;

        [ProtoMember]
        public bool IsContentWorlds;
    }
}
