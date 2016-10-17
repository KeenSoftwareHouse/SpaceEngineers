using System.Collections.Generic;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TriggerScriptNode: MyObjectBuilder_ScriptNode
    {        
        public string TriggerName = string.Empty;
        
        public int SequenceInputID = -1;
        
        public List<MyVariableIdentifier> InputIDs = new List<MyVariableIdentifier>();
        
        public List<string> InputNames = new List<string>();
        
        public List<string> InputTypes = new List<string>();
    }
}