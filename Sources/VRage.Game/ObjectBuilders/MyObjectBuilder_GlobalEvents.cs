using ProtoBuf;
using System.Collections.Generic;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GlobalEvents : MyObjectBuilder_Base
    {
        [ProtoMember]
        public List<MyObjectBuilder_GlobalEventBase> Events = new List<MyObjectBuilder_GlobalEventBase>();
    }
}
