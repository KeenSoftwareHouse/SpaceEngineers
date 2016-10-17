using VRage.Game.ObjectBuilders.Campaign;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.VisualScripting
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_VSFiles : MyObjectBuilder_Base
    {
        public MyObjectBuilder_VisualScript VisualScript;
        public MyObjectBuilder_VisualLevelScript LevelScript;
        public MyObjectBuilder_Campaign Campaign;
        public MyObjectBuilder_ScriptSM StateMachine;
    }
}
