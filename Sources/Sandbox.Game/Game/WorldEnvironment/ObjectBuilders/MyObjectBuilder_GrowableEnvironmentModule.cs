using System.Collections.Generic;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace Sandbox.Game.WorldEnvironment.ObjectBuilders
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GrowableEnvironmentModule: MyObjectBuilder_EnvironmentModuleBase
    {
        public struct GrowthStepInfo
        {
            [XmlAttribute("Position")]
            public int Position;

            [XmlAttribute("Step")]
            public int Step;

            [XmlAttribute("Time")]
            public long Time;
        }

        public List<GrowthStepInfo> SavedGrowthSteps = new List<GrowthStepInfo>();
    }
}
