using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.ComponentSystem;

namespace Sandbox.Game.EntityComponents
{
    [MyDefinitionType(typeof(MyObjectBuilder_TimerComponentDefinition))]
    public class MyTimerComponentDefinition : MyComponentDefinitionBase
    {
        public float TimeToRemoveMin;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_TimerComponentDefinition;
            TimeToRemoveMin = ob.TimeToRemoveMin;
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var ob = base.GetObjectBuilder() as MyObjectBuilder_TimerComponentDefinition;
            ob.TimeToRemoveMin = TimeToRemoveMin;
            return ob;
        }
    }
}
