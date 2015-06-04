using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;

using VRageMath;
using Sandbox.Engine.Utils;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_AdvancedDoorDefinition))]
    public class MyAdvancedDoorDefinition : MyCubeBlockDefinition
    {
        public float PowerConsumptionIdle;
        public float PowerConsumptionMoving;

        public MyObjectBuilder_AdvancedDoorDefinition.SubpartDefinition[] Subparts;
        public MyObjectBuilder_AdvancedDoorDefinition.Opening[] OpeningSequence;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var advancedDoorBuilder = builder as MyObjectBuilder_AdvancedDoorDefinition;
            MyDebug.AssertDebug(advancedDoorBuilder != null);

            PowerConsumptionIdle = advancedDoorBuilder.PowerConsumptionIdle;
            PowerConsumptionMoving = advancedDoorBuilder.PowerConsumptionMoving;

            Subparts = advancedDoorBuilder.Subparts;
            OpeningSequence = advancedDoorBuilder.OpeningSequence;
        }
    }
}
