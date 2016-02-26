namespace VRage.Game.Extensions
{
    public static class MyObjectBuilderExtensions
    {
        public static bool HasPlanets(this MyObjectBuilder_ScenarioDefinition scenario)
        {
            if (scenario.WorldGeneratorOperations != null)
            {
                foreach (var op in scenario.WorldGeneratorOperations)
                {
                    if (op is MyObjectBuilder_WorldGeneratorOperation_CreatePlanet)
                        return true;
                    if (op is MyObjectBuilder_WorldGeneratorOperation_AddPlanetPrefab)
                        return true;
                }
            }
            return false;
        }

        public static bool HasPlanets(this MyObjectBuilder_Sector sector)
        {
            if(sector.SectorObjects != null)
            {
                foreach(var ob in sector.SectorObjects)
                {
                    if (ob is MyObjectBuilder_Planet)
                        return true;
                }
            }
            return false;
        }
    }
}
