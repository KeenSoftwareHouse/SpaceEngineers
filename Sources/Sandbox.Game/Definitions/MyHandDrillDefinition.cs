using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_HandDrillDefinition))]
    public class MyHandDrillDefinition : MyEngineerToolBaseDefinition
    {
        public float HarvestRatioMultiplier;
        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_HandDrillDefinition;
            MyDebug.AssertDebug(ob != null);
            HarvestRatioMultiplier = ob.HarvestRatioMultiplier;
        }
    }
}
