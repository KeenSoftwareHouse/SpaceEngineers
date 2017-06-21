using System;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ConstantScriptNode : MyObjectBuilder_ScriptNode
    {
        public string Value = string.Empty;

        public string Type = string.Empty;

        public IdentifierList OutputIds = new IdentifierList();
    }
}