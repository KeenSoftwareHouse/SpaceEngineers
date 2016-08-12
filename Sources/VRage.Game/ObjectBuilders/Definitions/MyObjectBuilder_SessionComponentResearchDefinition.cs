using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Definitions
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_SessionComponentResearchDefinition: MyObjectBuilder_SessionComponentDefinition
    {
        public bool WhitelistMode;

        [XmlElement("Research")]
        public List<SerializableDefinitionId> Researches;
    }
}
