using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.VisualScripting
{
    [MyObjectBuilderDefinition]
    // ReSharper disable once InconsistentNaming
    public class MyObjectBuilder_ScriptSMTransition : MyObjectBuilder_Base
    {
        // Visual Data
        public string Name;
        // Node Name
        public string From;
        // Node Name
        public string To;
    }
}
