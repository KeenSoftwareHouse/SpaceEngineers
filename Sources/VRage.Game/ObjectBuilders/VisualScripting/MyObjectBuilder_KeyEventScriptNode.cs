using System.Collections.Generic;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_KeyEventScriptNode : MyObjectBuilder_EventScriptNode
    {
        public List<string> Keys = new List<string>(); 
    }
}