using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_MotorAdvancedStatorDefinition))]
    public class MyMotorAdvancedStatorDefinition : MyMotorStatorDefinition
    {
        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

        }
    }
}
