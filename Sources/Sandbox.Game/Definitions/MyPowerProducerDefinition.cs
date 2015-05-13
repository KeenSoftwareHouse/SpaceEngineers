using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Engine.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_PowerProducerDefinition))]
    public class MyPowerProducerDefinition : MyCubeBlockDefinition
    {
        public float MaxPowerOutput;
        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var powerProducerBuilder = builder as MyObjectBuilder_PowerProducerDefinition;
            MaxPowerOutput = powerProducerBuilder.MaxPowerOutput;
        }

    }
}
