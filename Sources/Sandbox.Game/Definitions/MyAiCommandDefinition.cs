using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_AiCommandDefinition))]
    public class MyAiCommandDefinition : MyDefinitionBase
    {
        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
        }
    }
}
