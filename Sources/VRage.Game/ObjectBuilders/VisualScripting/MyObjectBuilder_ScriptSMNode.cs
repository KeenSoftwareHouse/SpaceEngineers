using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.VisualScripting
{
    [MyObjectBuilderDefinition]
    // ReSharper disable once InconsistentNaming
    public class MyObjectBuilder_ScriptSMNode : MyObjectBuilder_Base
    {
        // Visual Data
        public SerializableVector2 Position;
        // Logic Data
        public string Name;
        public string ScriptFilePath;
        public string ScriptClassName;
    }

    // Derived classes just to distinguish the node types.

    [MyObjectBuilderDefinition]
    // ReSharper disable once InconsistentNaming
    public class MyObjectBuilder_ScriptSMBarrierNode : MyObjectBuilder_ScriptSMNode
    {
    }

    [MyObjectBuilderDefinition]
    // ReSharper disable once InconsistentNaming
    public class MyObjectBuilder_ScriptSMSpreadNode : MyObjectBuilder_ScriptSMNode
    {
    }

    [MyObjectBuilderDefinition]
    // ReSharper disable once InconsistentNaming
    public class MyObjectBuilder_ScriptSMFinalNode : MyObjectBuilder_ScriptSMNode
    {
    }
}
