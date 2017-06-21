using System.Collections.Generic;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ScriptScriptNode : MyObjectBuilder_ScriptNode
    {        
        public string Name = string.Empty;
        
        public string Path;

        public int SequenceOutput = -1;

        public int SequenceInput = -1;

        public List<MyInputParameterSerializationData> Inputs = new List<MyInputParameterSerializationData>();

        public List<MyOutputParameterSerializationData> Outputs = new List<MyOutputParameterSerializationData>(); 
    }
}