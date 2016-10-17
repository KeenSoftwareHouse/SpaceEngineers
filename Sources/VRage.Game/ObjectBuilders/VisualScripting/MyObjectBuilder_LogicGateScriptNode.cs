using System.Collections.Generic;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.VisualScripting
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_LogicGateScriptNode : MyObjectBuilder_ScriptNode
    {
        public enum LogicOperation
        {
            AND, OR, XOR, NAND, NOR, NOT
        }

        public List<MyVariableIdentifier> ValueInputs = new List<MyVariableIdentifier>();
        public List<MyVariableIdentifier> ValueOutputs = new List<MyVariableIdentifier>();
        public LogicOperation Operation = LogicOperation.NOT;
    }
}
