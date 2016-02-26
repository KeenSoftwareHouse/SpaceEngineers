using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_OxygenGeneratorDefinition))]
    public class MyOxygenGeneratorDefinition : MyProductionBlockDefinition
    {
        public struct MyGasGeneratorResourceInfo
        {
            public MyDefinitionId Id;
            public float IceToGasRatio;
        }
		public float IceConsumptionPerSecond;

        public MySoundPair GenerateSound;
        public MySoundPair IdleSound;

	    public MyStringHash ResourceSourceGroup;
	    public List<MyGasGeneratorResourceInfo> ProducedGases;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var obDefinition = builder as MyObjectBuilder_OxygenGeneratorDefinition;

			IceConsumptionPerSecond = obDefinition.IceConsumptionPerSecond;

            GenerateSound = new MySoundPair(obDefinition.GenerateSound);
            IdleSound = new MySoundPair(obDefinition.IdleSound);

			ResourceSourceGroup = MyStringHash.GetOrCompute(obDefinition.ResourceSourceGroup);

	        ProducedGases = null;
	        if (obDefinition.ProducedGases != null)
	        {
				ProducedGases = new List<MyGasGeneratorResourceInfo>(obDefinition.ProducedGases.Count);
		        foreach(var producedGasInfo in obDefinition.ProducedGases)
					ProducedGases.Add(new MyGasGeneratorResourceInfo { Id = producedGasInfo.Id, IceToGasRatio = producedGasInfo.IceToGasRatio });
	        }
        }
    }
}
