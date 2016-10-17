using System.Diagnostics;

namespace VRage.Game.VisualScripting.ScriptBuilder.Nodes
{
    /// <summary>
    /// Special case of Event node. The logic is the same for both,
    /// but on the gui side the logic is different. Thats why I kept
    /// it separated also here.
    /// </summary>
    public class MyVisualSyntaxInputNode : MyVisualSyntaxEventNode
    {
        public MyVisualSyntaxInputNode(MyObjectBuilder_ScriptNode ob) : base(ob)
        {
            Debug.Assert(ob is MyObjectBuilder_InputScriptNode);
        }
    }
}
