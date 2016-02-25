using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_RepairBlueprintDefinition))]
    public class MyRepairBlueprintDefinition : MyBlueprintDefinition
    {
        public float RepairAmount = 0;

        protected override void Init(MyObjectBuilder_DefinitionBase ob)
        {
            base.Init(ob);

            var def = ob as MyObjectBuilder_RepairBlueprintDefinition;
            RepairAmount = def.RepairAmount;
        }
    }
}
