using System.Xml.Serialization;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [XmlRoot("ScenarioDefinitions")]
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ScenarioDefinitions : MyObjectBuilder_Base
    {
        [XmlArrayItem("ScenarioDefinition")]
        [ProtoMember]
        public MyObjectBuilder_ScenarioDefinition[] Scenarios;
    }
}
