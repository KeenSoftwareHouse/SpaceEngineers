using System.Collections.Generic;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Components
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_SessionComponentResearch: MyObjectBuilder_SessionComponent
    {
        public struct ResearchData
        {
            [XmlAttribute("Identity")]
            public long IdentityId;

            [XmlElement("Entry")]
            public List<SerializableDefinitionId> Definitions;
        }

        [XmlElement("Research")]
        public List<ResearchData> Researches;

        public bool WhitelistMode = false;
    }
}
