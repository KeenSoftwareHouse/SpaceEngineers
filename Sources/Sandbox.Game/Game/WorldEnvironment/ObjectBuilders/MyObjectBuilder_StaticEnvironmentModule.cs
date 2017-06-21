using System.Collections.Generic;
using VRage.ObjectBuilders;
using VRage.Serialization;
using VRageMath;

namespace Sandbox.Game.WorldEnvironment.ObjectBuilders
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_StaticEnvironmentModule: MyObjectBuilder_EnvironmentModuleBase
    {
        public HashSet<int> DisabledItems;

        [Nullable]
        public List<MyOrientedBoundingBoxD> Boxes;

        public int MinScanned = MyEnvironmentSectorConstants.MaximumLod;
    }
}
