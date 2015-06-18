using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using VRage.Utils;
using VRage.ObjectBuilders.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_TreeDefinition))]
    public class MyTreeDefinition : MyEnvironmentItemDefinition
    {
        public float BranchesStartHeight;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_TreeDefinition;
            MyDebug.AssertDebug(ob != null);

            this.BranchesStartHeight = ob.BranchesStartHeight;
        }
    }
}
