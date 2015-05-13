using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Engine.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_OreDetectorDefinition))]
    public class MyOreDetectorDefinition : MyCubeBlockDefinition
    {
        public float MaximumRange;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var ob = builder as MyObjectBuilder_OreDetectorDefinition;
            MaximumRange = ob.MaximumRange;
        }
    }
}
