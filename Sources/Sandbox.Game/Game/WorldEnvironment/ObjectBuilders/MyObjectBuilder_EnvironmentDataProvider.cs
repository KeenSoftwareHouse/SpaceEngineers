using System.Xml.Serialization;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Game.WorldEnvironment.ObjectBuilders
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_EnvironmentDataProvider: MyObjectBuilder_Base
    {
        [XmlAttribute("Face")]
        public Base6Directions.Direction Face;
    }
}
