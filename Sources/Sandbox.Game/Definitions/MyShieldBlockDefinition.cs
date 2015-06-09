using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_ShieldBlockDefinition))]
    public class MyShieldBlockDefinition : MyCubeBlockDefinition
    {
        public float MinRequiredPowerInput;
        public float PowerConsumption;
        public float MaxShieldCapacity;
        public float ShieldUpRate;
   

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var obGenerator = builder as MyObjectBuilder_ShieldBlockDefinition;

            MinRequiredPowerInput = obGenerator.MinRequiredPowerInput;
            PowerConsumption = obGenerator.PowerConsumption;
            MaxShieldCapacity = obGenerator.MaxShieldCapacity;
            ShieldUpRate = obGenerator.ShieldUpRate;
        }
    }
}
