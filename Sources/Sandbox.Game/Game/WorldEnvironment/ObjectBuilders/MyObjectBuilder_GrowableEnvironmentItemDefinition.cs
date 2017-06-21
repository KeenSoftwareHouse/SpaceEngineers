using System.ComponentModel;
using System.Xml.Serialization;
using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Game.WorldEnvironment.ObjectBuilders
{
    [XmlType("VR.EI.GrowableEnvironmentItem")]
    public class MyObjectBuilder_GrowableEnvironmentItemDefinition : MyObjectBuilder_DefinitionBase
    {
        [XmlArrayItem("GrowthStep")]
        public GrowthStepDef[] GrowthSteps;

        public class GrowthStepDef
        {
            [XmlAttribute("Name")]
            [DefaultValue("")]
            public string Name = "";

            [DefaultValue(null)]
            public string ModelCollectionSubtypeId = null;

            [DefaultValue(null)]
            public string NextStep = null;

            [DefaultValue(0)]
            public float TimeToNextStepInHours = 0;

            [XmlAttribute("StartingProbability")]
            [DefaultValue(1.0f)]
            public float StartingProbability = 1.0f;

            [XmlArrayItem("Action")]
            [DefaultValue(null)]
            public EnvironmentItemActionDef[] Actions;
        }

        public class EnvironmentItemActionDef
        {
            [XmlAttribute("Name")]
            [DefaultValue("")]
            public string Name = "";

            [XmlAttribute("NextStep")]
            [DefaultValue(null)]
            public string NextStep = null;

            public SerializableDefinitionId Id;

            [DefaultValue(1)]
            public int Min = 1;

            [DefaultValue(1)]
            public int Max = 1;
        }
    }
}
