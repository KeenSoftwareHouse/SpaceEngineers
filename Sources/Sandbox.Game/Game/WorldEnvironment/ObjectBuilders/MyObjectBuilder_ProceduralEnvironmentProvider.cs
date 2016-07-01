using System.Collections.Generic;
using System.Xml.Serialization;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Game.WorldEnvironment.ObjectBuilders
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ProceduralEnvironmentProvider : MyObjectBuilder_EnvironmentDataProvider
    {
        [XmlElement("Sector")]
        public List<MyObjectBuilder_ProceduralEnvironmentSector> Sectors = new List<MyObjectBuilder_ProceduralEnvironmentSector>();
    }
}
