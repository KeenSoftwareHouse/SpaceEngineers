using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GetterScriptNode : MyObjectBuilder_ScriptNode
    {
        public string BoundVariableName = string.Empty;

        public IdentifierList OutputIDs = new IdentifierList();
    }
}