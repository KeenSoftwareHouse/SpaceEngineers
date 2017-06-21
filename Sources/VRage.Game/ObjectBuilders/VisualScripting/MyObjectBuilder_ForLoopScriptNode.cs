using System.Collections.Generic;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.VisualScripting
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ForLoopScriptNode : MyObjectBuilder_ScriptNode
    {
        public List<int> SequenceInputs = new List<int>();
        public int SequenceBody = -1;
        public int SequenceOutput = -1;
        public MyVariableIdentifier FirstIndexValueInput = MyVariableIdentifier.Default;
        public MyVariableIdentifier LastIndexValueInput = MyVariableIdentifier.Default;
        public MyVariableIdentifier IncrementValueInput = MyVariableIdentifier.Default;
        public List<MyVariableIdentifier> CounterValueOutputs = new List<MyVariableIdentifier>(); 
        public string FirstIndexValue = "0";
        public string LastIndexValue = "0";
        public string IncrementValue = "1";
    }
}
