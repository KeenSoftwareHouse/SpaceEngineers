using System.Collections.Generic;
using VRage.ObjectBuilders;

namespace Sandbox.Game.WorldEnvironment.ObjectBuilders
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_DummyEnvironmentModule: MyObjectBuilder_EnvironmentModuleBase
    {
        public HashSet<int> DisabledItems;
    }
}
