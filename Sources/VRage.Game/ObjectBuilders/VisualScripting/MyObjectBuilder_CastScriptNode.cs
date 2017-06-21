using System.Collections.Generic;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CastScriptNode : MyObjectBuilder_ScriptNode
    {
        public string Type;
   
        public int SequenceInputID = -1;

        public int SequenceOuputID = -1;
        
        public MyVariableIdentifier InputID = MyVariableIdentifier.Default;

        public List<MyVariableIdentifier> OuputIDs = new List<MyVariableIdentifier>();
    }
}