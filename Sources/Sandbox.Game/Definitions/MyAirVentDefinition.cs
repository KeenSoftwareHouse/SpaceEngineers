using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_AirVentDefinition))]
    public class MyAirVentDefinition : MyCubeBlockDefinition
    {
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
            
            StandbyPowerConsumption = airVent.StandbyPowerConsumption;
            OperationalPowerConsumption = airVent.OperationalPowerConsumption;
            VentilationCapacityPerSecond = airVent.VentilationCapacityPerSecond;

            PressurizeSound = new MySoundPair(airVent.PressurizeSound);
            DepressurizeSound = new MySoundPair(airVent.DepressurizeSound);
            IdleSound = new MySoundPair(airVent.IdleSound);
        }
    }
}
