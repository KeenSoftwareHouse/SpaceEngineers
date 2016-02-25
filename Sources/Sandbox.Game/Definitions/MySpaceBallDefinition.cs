using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;


namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_SpaceBallDefinition))]
    public class MySpaceBallDefinition : MyCubeBlockDefinition
    {
        public float MaxVirtualMass;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var obMass = builder as MyObjectBuilder_SpaceBallDefinition;
            MyDebug.AssertDebug(obMass != null, "Initializing sphere mass definition using wrong object builder.");

            MaxVirtualMass = obMass.MaxVirtualMass;
        }
    }
}
