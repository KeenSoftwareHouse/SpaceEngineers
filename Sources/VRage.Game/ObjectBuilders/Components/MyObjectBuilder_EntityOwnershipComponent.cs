using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Components
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_EntityOwnershipComponent: MyObjectBuilder_ComponentBase
    {
        public long OwnerId;

        public MyOwnershipShareModeEnum ShareMode;
    }
}
