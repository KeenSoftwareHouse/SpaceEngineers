using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_VariableSetterScriptNode : MyObjectBuilder_ScriptNode
    {        
        public string VariableName = string.Empty;
        
        public string VariableValue = string.Empty;
        
        public int SequenceInputID = -1;
        
        public int SequenceOutputID = -1;
        
        public MyVariableIdentifier ValueInputID = MyVariableIdentifier.Default;
    }
}