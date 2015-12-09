using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.EntityComponents;
using VRageMath;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_ThrustDefinition))]
    public class MyThrustDefinition : MyCubeBlockDefinition
    {
	    public MyStringHash ResourceSinkGroup;
        public MyFuelConverterInfo FuelConverter;
	    public float SlowdownFactor;
        public float ForceMagnitude;
        public float MaxPowerConsumption;
        public float MinPowerConsumption;
        public float FlameDamageLengthScale;
        public float FlameDamage;

        public float FlameLengthScale;
        public Vector4 FlameFullColor;
        public Vector4 FlameIdleColor;
        public string FlamePointMaterial;
        public string FlameLengthMaterial;
        public string FlameGlareMaterial;
        public float FlameVisibilityDistance;
        public float FlameGlareSize;
        public float FlameGlareQuerySize;

        public float EffectivenessAtMaxInfluence;
        public float EffectivenessAtMinInfluence;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var thrustBuilder = builder as MyObjectBuilder_ThrustDefinition;
            MyDebug.AssertDebug(thrustBuilder != null, "Initializing thrust definition using wrong object builder.");

	        ResourceSinkGroup = MyStringHash.GetOrCompute(thrustBuilder.ResourceSinkGroup);
            FuelConverter = thrustBuilder.FuelConverter;
			SlowdownFactor = thrustBuilder.SlowdownFactor;
            ForceMagnitude = thrustBuilder.ForceMagnitude;
            MaxPowerConsumption = thrustBuilder.MaxPowerConsumption;
            MinPowerConsumption = thrustBuilder.MinPowerConsumption;
            FlameDamageLengthScale = thrustBuilder.FlameDamageLengthScale;
            FlameDamage = thrustBuilder.FlameDamage;
            FlameLengthScale = thrustBuilder.FlameLengthScale;
            FlameFullColor = thrustBuilder.FlameFullColor;
            FlameIdleColor = thrustBuilder.FlameIdleColor;
            FlamePointMaterial = thrustBuilder.FlamePointMaterial;
            FlameLengthMaterial = thrustBuilder.FlameLengthMaterial;
            FlameGlareMaterial = thrustBuilder.FlameGlareMaterial;
            FlameVisibilityDistance = thrustBuilder.FlameVisibilityDistance;
            FlameGlareSize = thrustBuilder.FlameGlareSize;
            FlameGlareQuerySize = thrustBuilder.FlameGlareQuerySize;

            EffectivenessAtMinInfluence = thrustBuilder.EffectivenessAtMinInfluence;
            EffectivenessAtMaxInfluence = thrustBuilder.EffectivenessAtMaxInfluence;
        }
    }
}
