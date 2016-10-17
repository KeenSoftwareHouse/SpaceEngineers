using System;
using System.Collections.Generic;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.VisualScripting
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_NewListScriptNode : MyObjectBuilder_ScriptNode
    {
        public string Type = String.Empty;
        public readonly List<string>  DefaultEntries = new List<string>();
        public readonly List<MyVariableIdentifier> Connections = new List<MyVariableIdentifier>(); 
    }
}
