﻿using System.Diagnostics;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_BatteryBlockDefinition))]
    public class MyBatteryBlockDefinition : MyPowerProducerDefinition
    {
        public float MaxStoredPower;
	    public MyStringHash ResourceSinkGroup;
        public float RequiredPowerInput;
	    public bool AdaptibleInput;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var batteryBlockBuilder = builder as MyObjectBuilder_BatteryBlockDefinition;
			Debug.Assert(batteryBlockBuilder != null);
	        if (batteryBlockBuilder == null)
		        return;

            MaxStoredPower = batteryBlockBuilder.MaxStoredPower;
	        ResourceSinkGroup = MyStringHash.GetOrCompute(batteryBlockBuilder.ResourceSinkGroup);
            RequiredPowerInput = batteryBlockBuilder.RequiredPowerInput;
	        AdaptibleInput = batteryBlockBuilder.AdaptibleInput;
        }
    }
}
