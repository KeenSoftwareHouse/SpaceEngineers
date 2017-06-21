using System.Collections.Generic;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_OutputScriptNode : MyObjectBuilder_ScriptNode
    {        
        public int SequenceInputID = -1;

        public List<MyInputParameterSerializationData> Inputs = new List<MyInputParameterSerializationData>(); 
    }
}