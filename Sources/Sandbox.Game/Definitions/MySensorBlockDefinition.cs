using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Engine.Utils;
using System;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_SensorBlockDefinition))]
    public class MySensorBlockDefinition : MyCubeBlockDefinition
    {
        public float RequiredPowerInput;
        public float MaxRange;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var obGenerator = builder as MyObjectBuilder_SensorBlockDefinition;
            MyDebug.AssertDebug(obGenerator != null, "Initializing sensor block definition using wrong object builder.");
            MyDebug.AssertDebug(obGenerator.MaxRange >= 1, "Invalid max range of sensor definition, using max range 1");
            RequiredPowerInput = obGenerator.RequiredPowerInput;
            MaxRange = Math.Max(obGenerator.MaxRange, 1);
        }
    }
}
