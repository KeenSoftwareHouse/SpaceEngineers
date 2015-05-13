using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_AgentDefinition))]
    public class MyAgentDefinition : MyBotDefinition
    {
        public string BotModel;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_AgentDefinition;
            this.BotModel = ob.BotModel;
        }
    }
}
