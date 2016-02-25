using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Definitions;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Utils;

namespace Sandbox.Game.EntityComponents
{
    [MyDefinitionType(typeof(MyObjectBuilder_CraftingComponentBasicDefinition))]
    public class MyCraftingComponentBasicDefinition : MyComponentDefinitionBase
    {
        public List<string> AvailableBlueprintClasses = new List<string>();
        public string ActionSound = "";
        public float CraftingSpeedMultiplier = 1.0f;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_CraftingComponentBasicDefinition;

            ActionSound = ob.ActionSound;
            CraftingSpeedMultiplier = ob.CraftingSpeedMultiplier;

            if (ob.AvailableBlueprintClasses != null && ob.AvailableBlueprintClasses != String.Empty)
            {
                AvailableBlueprintClasses = ob.AvailableBlueprintClasses.Split(' ').ToList();

                // TODO: REMOVE WHEN DURABILITY CAN BE ENABLED BYT DEFAULT..
                if (!MyFakes.ENABLE_DURABILITY_COMPONENT)
                {
                    if (AvailableBlueprintClasses.Contains("ToolsRepair"))
                        AvailableBlueprintClasses.Remove("ToolsRepair");
                }
            }
            else
            {
                System.Diagnostics.Debug.Fail(String.Format("Problem initializing crafting block component definition {0}, it is missing available blueprint classes!", Id.ToString()));
            }
        }
    }
}
