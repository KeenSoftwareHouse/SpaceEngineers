using System.Xml.Serialization;
using VRage.ObjectBuilders;
using VRageMath;

namespace VRage.Game.ObjectBuilders.Definitions.GUI
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ButtonListStyleDefinition : MyObjectBuilder_DefinitionBase
    {
        [XmlAttribute]
        public string StyleName;

        public SerializableVector2 ButtonSize = new Vector2(75f, 75f);

        public SerializableVector2 ButtonMargin = new Vector2(10f, 10f);
    }
}
