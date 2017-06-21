using System.Collections.Generic;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.VisualScripting
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_InterfaceMethodNode : MyObjectBuilder_ScriptNode
    {
        public string MethodName;

        public List<int> SequenceOutputIDs = new List<int>();

        public List<IdentifierList> OutputIDs = new List<IdentifierList>();

        public List<string> OutputNames = new List<string>();

        public List<string> OuputTypes = new List<string>();
    }
}
