using System;
using System.Collections.Generic;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.VisualScripting
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_SwitchScriptNode : MyObjectBuilder_ScriptNode
    {
        public struct OptionData
        {
            public string Option;
            public int SequenceOutput;
        }

        public int SequenceInput = -1;
        // Every option can have only one sequence output 
        public readonly List<OptionData> Options = new List<OptionData>();
        // Value input identifier
        public MyVariableIdentifier ValueInput;
        // Type signature
        public string NodeType = String.Empty;
    }
}
