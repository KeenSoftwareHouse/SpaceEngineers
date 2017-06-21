using System.ComponentModel;
using System.Xml.Serialization;
using VRage.Game.ObjectBuilders.Gui;
using VRage.Game.ObjectBuilders.VisualScripting;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_VisualScriptManagerSessionComponent : MyObjectBuilder_SessionComponent
    {
        public bool FirstRun = true;

        [XmlArray("LevelScriptFiles", IsNullable = true)]
        [XmlArrayItem("FilePath")]
        public string[] LevelScriptFiles;

        [XmlArray("StateMachines", IsNullable = true)]
        [XmlArrayItem("FilePath")]
        public string[] StateMachines;

        [DefaultValue(null)]
        public MyObjectBuilder_ScriptStateMachineManager ScriptStateMachineManager;

        [DefaultValue(null)]
        public MyObjectBuilder_Questlog Questlog;
    }
}
