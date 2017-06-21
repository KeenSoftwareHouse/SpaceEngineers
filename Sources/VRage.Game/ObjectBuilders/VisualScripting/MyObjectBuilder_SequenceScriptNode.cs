using System.Collections.Generic;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.VisualScripting
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_SequenceScriptNode : MyObjectBuilder_ScriptNode
    {
        public int SequenceInput = -1;
        public List<int> SequenceOutputs = new List<int>();
    }
}
