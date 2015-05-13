using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sandbox.Engine.Utils;
using Sandbox.Engine.Utils;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_ProjectorDefinition))]
    public class MyProjectorDefinition : MyCubeBlockDefinition
    {
        public float RequiredPowerInput;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var obProjector = builder as MyObjectBuilder_ProjectorDefinition;
            MyDebug.AssertDebug(obProjector != null, "Initializing camera definition using wrong object builder.!");
            RequiredPowerInput = obProjector.RequiredPowerInput;
        }

    }
}
