using System.Collections.Generic;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_FunctionScriptNode : MyObjectBuilder_ScriptNode
    {     
        public int Version = 0;

        public string DeclaringType = string.Empty;

        public string Type = string.Empty;
        
        // Extension of type
        public string ExtOfType = string.Empty;

        public int SequenceInputID = -1;
        
        public int SequenceOutputID = -1;
        
        public MyVariableIdentifier InstanceInputID = MyVariableIdentifier.Default;
        
        public List<MyVariableIdentifier> InputParameterIDs = new List<MyVariableIdentifier>();
        
        public List<IdentifierList> OutputParametersIDs = new List<IdentifierList>();
        
        public List<MyParameterValue> InputParameterValues = new List<MyParameterValue>();
    }
}