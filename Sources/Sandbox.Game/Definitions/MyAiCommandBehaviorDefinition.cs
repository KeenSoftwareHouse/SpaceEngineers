using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_AiCommandBehaviorDefinition))]
    public class MyAiCommandBehaviorDefinition : MyAiCommandDefinition
    {
        public string BehaviorTreeName;
        public MyAiCommandEffect CommandEffect;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_AiCommandBehaviorDefinition;
            BehaviorTreeName = ob.BehaviorTreeName;
            CommandEffect = ob.CommandEffect;
        }
    }
}
