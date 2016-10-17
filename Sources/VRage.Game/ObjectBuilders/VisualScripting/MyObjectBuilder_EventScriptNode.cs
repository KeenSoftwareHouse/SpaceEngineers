using System.Collections.Generic;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_EventScriptNode : MyObjectBuilder_ScriptNode
    {        
        public string Name;
        
        public int SequenceOutputID = -1;
        
        public List<IdentifierList> OutputIDs = new List<IdentifierList>();
        
        public List<string> OutputNames = new List<string>();

        public List<string> OuputTypes = new List<string>();
    }
}