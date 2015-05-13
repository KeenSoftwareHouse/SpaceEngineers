using Sandbox.Common.ObjectBuilders.AI;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_BehaviorTreeDefinition))]
    public class MyBehaviorDefinition : MyDefinitionBase
    {
        public MyObjectBuilder_BehaviorTreeNode FirstNode;
        public string Behavior;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = (MyObjectBuilder_BehaviorTreeDefinition)builder;

            FirstNode = ob.FirstNode;
            Behavior = ob.Behavior;
        }
    }
}
