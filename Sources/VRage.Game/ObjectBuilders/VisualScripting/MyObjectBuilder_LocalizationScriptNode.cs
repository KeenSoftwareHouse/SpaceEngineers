using System;
using System.Collections.Generic;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.VisualScripting
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_LocalizationScriptNode : MyObjectBuilder_ScriptNode
    {
        public string Context = String.Empty;
        public string MessageId = String.Empty;
        public ulong ResourceId = UInt64.MaxValue;

        public List<MyVariableIdentifier> ParameterInputs = new List<MyVariableIdentifier>();
        public List<MyVariableIdentifier> ValueOutputs = new List<MyVariableIdentifier>(); 
    }
}
