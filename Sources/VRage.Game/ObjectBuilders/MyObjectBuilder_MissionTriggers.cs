using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System.Collections.Generic;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    //[MyObjectBuilderDefinition]
    public class MyObjectBuilder_MissionTriggers : MyObjectBuilder_Base
    {
        [ProtoMember]
        public List<MyObjectBuilder_Trigger> WinTriggers =new List<MyObjectBuilder_Trigger>();
        [ProtoMember]
        public List<MyObjectBuilder_Trigger> LoseTriggers =new List<MyObjectBuilder_Trigger>();
        [ProtoMember]
        public string message;
        [ProtoMember]
        public bool Won;
        [ProtoMember]
        public bool Lost;

    }
}
