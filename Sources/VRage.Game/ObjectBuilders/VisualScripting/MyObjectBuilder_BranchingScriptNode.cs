using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BranchingScriptNode : MyObjectBuilder_ScriptNode
    {        
        public MyVariableIdentifier InputID = MyVariableIdentifier.Default;

        public int SequenceInputID = -1;

        public int SequenceTrueOutputID = -1;

        public int SequnceFalseOutputID = -1;
    }
}