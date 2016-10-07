using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Definitions
{
    [MyObjectBuilderDefinition]
    [Description("Main definition for a game.")]
    [XmlType("VR.GameDefinition")]
    public class MyObjectBuilder_GameDefinition : MyObjectBuilder_DefinitionBase
    {
        [Description("What object builder to inherit from if any.")]
        [DefaultValue(null)]
        public string InheritFrom;

        [Description("Weather this game definition is the default for new scenarios.")]
        [DefaultValue(false)]
        public bool Default;

        public struct Comp
        {
            [XmlAttribute]
            public string Type;

            [XmlAttribute]
            public string Subtype;

            [XmlText]
            public string ComponentName;
        }

        [Description("List of session components to load for this Game.")]
        [DefaultValue("empty")]
        [XmlArrayItem("Component")]
        public List<Comp> SessionComponents = new List<Comp>();
    }
}
