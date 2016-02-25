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
    [MyDefinitionType(typeof(MyObjectBuilder_EngineerToolBaseDefinition))]
    public class MyEngineerToolBaseDefinition : MyHandItemDefinition
    {
        public float SpeedMultiplier;
        public float DistanceMultiplier;
        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_EngineerToolBaseDefinition;
            MyDebug.AssertDebug(ob != null);
            SpeedMultiplier = ob.SpeedMultiplier;
            DistanceMultiplier = ob.DistanceMultiplier;
        }
    }
}
