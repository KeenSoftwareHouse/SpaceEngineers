using VRage.Game;

namespace Sandbox.Game.WorldEnvironment.ObjectBuilders
{
    public abstract class MyObjectBuilder_WorldEnvironmentBase : MyObjectBuilder_DefinitionBase
    {
        // Sector size in meters
        public double SectorSize = 64;

        // Number of items per sqr metre.
        public double ItemsPerSqMeter = 0.0017;

        // Maximum lod untill sectors are synchronized
        public int MaxSyncLod = 1;
    }
}
