using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.VisualScripting
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CommentScriptNode : MyObjectBuilder_ScriptNode
    {
        public string CommentText = "Insert Comment...";
        public SerializableVector2 CommentSize = new SerializableVector2(50, 20);
    }
}
