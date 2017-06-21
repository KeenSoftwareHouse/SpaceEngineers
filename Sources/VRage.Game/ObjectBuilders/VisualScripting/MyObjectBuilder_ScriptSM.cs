using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.VisualScripting
{
    [MyObjectBuilderDefinition]
    // ReSharper disable once InconsistentNaming
    public class MyObjectBuilder_ScriptSM : MyObjectBuilder_Base
    {
        public string Name;
        public long OwnerId = 0;
        public MyObjectBuilder_ScriptSMCursor []       Cursors;
        public MyObjectBuilder_ScriptSMNode []         Nodes;
        public MyObjectBuilder_ScriptSMTransition []   Transitions;
    }
}
