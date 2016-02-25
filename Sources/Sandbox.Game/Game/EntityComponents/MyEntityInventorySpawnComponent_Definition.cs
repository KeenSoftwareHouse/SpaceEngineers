using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.ComponentSystem;

namespace Sandbox.Game.EntityComponents
{

    [MyDefinitionType(typeof(MyObjectBuilder_InventorySpawnComponent_Definition))]
    public class MyEntityInventorySpawnComponent_Definition : MyComponentDefinitionBase
    {
        public MyDefinitionId ContainerDefinition;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var spawnDef = builder as MyObjectBuilder_InventorySpawnComponent_Definition;

            ContainerDefinition = spawnDef.ContainerDefinition;
        }

        
    }
}
