using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRageMath;

namespace Sandbox.Definitions
{
    public class MyLootBagDefinition
    {
        public MyDefinitionId ContainerDefinition;
        public float SearchRadius;

        public void Init(MyObjectBuilder_Configuration.LootBagDefinition objectBuilder)
        {
            ContainerDefinition = objectBuilder.ContainerDefinition;
            SearchRadius = objectBuilder.SearchRadius;
        }
    }
}
