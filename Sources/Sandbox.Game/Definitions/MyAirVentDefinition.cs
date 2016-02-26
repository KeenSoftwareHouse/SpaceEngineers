using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_AirVentDefinition))]
    public class MyAirVentDefinition : MyCubeBlockDefinition
    {
	    public MyStringHash ResourceSinkGroup;
        public MyStringHash ResourceSourceGroup;
        public float StandbyPowerConsumption;
        public float OperationalPowerConsumption;
        public float VentilationCapacityPerSecond;

        public MySoundPair PressurizeSound;
        public MySoundPair DepressurizeSound;
        public MySoundPair IdleSound;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var airVent = builder as MyObjectBuilder_AirVentDefinition;
            MyDebug.AssertDebug(airVent != null, "Initializing air vent definition using wrong object builder.");

	        ResourceSinkGroup = MyStringHash.GetOrCompute(airVent.ResourceSinkGroup);
            ResourceSourceGroup = MyStringHash.GetOrCompute(airVent.ResourceSourceGroup);
            StandbyPowerConsumption = airVent.StandbyPowerConsumption;
            OperationalPowerConsumption = airVent.OperationalPowerConsumption;
            VentilationCapacityPerSecond = airVent.VentilationCapacityPerSecond;

            PressurizeSound = new MySoundPair(airVent.PressurizeSound);
            DepressurizeSound = new MySoundPair(airVent.DepressurizeSound);
            IdleSound = new MySoundPair(airVent.IdleSound);
        }
    }
}
