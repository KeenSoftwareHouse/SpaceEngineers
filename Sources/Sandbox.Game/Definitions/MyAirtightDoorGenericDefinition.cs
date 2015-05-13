using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_AirtightDoorGeneric))]
    public class MyAirtightDoorGenericDefinition : MyCubeBlockDefinition
    {
        public float PowerConsumptionIdle;
        public float PowerConsumptionMoving;
        public float OpeningSpeed;
        public string Sound;
        public float SubpartMovementDistance=2.5f;

        protected override void Init(MyObjectBuilder_DefinitionBase builderBase)
        {
            base.Init(builderBase);

            var builder = builderBase as MyObjectBuilder_AirtightDoorGenericDefinition;
            MyDebug.AssertDebug(builder != null, "Wrong object builder used in MyAirtightDoorBaseDefinition");
            
            PowerConsumptionIdle = builder.PowerConsumptionIdle;
            PowerConsumptionMoving = builder.PowerConsumptionMoving;
            OpeningSpeed = builder.OpeningSpeed;
            Sound = builder.Sound;
            SubpartMovementDistance = builder.SubpartMovementDistance;
        }
    }
}

