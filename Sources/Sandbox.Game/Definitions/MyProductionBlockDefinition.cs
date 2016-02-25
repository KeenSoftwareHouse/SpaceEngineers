using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game;
using Sandbox.Game.Localization;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_ProductionBlockDefinition))]
    public class MyProductionBlockDefinition : MyCubeBlockDefinition
    {
        public float InventoryMaxVolume;
        public Vector3 InventorySize;

	    public MyStringHash ResourceSinkGroup;
        public float StandbyPowerConsumption;
        public float OperationalPowerConsumption;
        public List<MyBlueprintClassDefinition> BlueprintClasses;

        public MyInventoryConstraint InputInventoryConstraint;
        public MyInventoryConstraint OutputInventoryConstraint;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            MyDebug.AssertDebug(builder is MyObjectBuilder_ProductionBlockDefinition);
            var obDefinition = builder as MyObjectBuilder_ProductionBlockDefinition;

            this.InventoryMaxVolume = obDefinition.InventoryMaxVolume;
            this.InventorySize = obDefinition.InventorySize;
	        ResourceSinkGroup = MyStringHash.GetOrCompute(obDefinition.ResourceSinkGroup);
            this.StandbyPowerConsumption = obDefinition.StandbyPowerConsumption;
            this.OperationalPowerConsumption = obDefinition.OperationalPowerConsumption;

            System.Diagnostics.Debug.Assert(obDefinition.BlueprintClasses != null, "Production block has no blueprint classes specified");
            if (obDefinition.BlueprintClasses == null)
                InitializeLegacyBlueprintClasses(obDefinition);

            BlueprintClasses = new List<MyBlueprintClassDefinition>();
            for (int i = 0; i < obDefinition.BlueprintClasses.Length; ++i)
            {
                var className = obDefinition.BlueprintClasses[i];
                var classDef = MyDefinitionManager.Static.GetBlueprintClass(className);
                System.Diagnostics.Debug.Assert(classDef != null, "Production block references non-existent blueprint class");
                if (classDef == null) continue;

                BlueprintClasses.Add(classDef);
            }
        }

        protected virtual bool BlueprintClassCanBeUsed(MyBlueprintClassDefinition blueprintClass)
        {
            return true;
        }

        // A legacy function to ensure that the old mods work with new blueprint system
        protected virtual void InitializeLegacyBlueprintClasses(MyObjectBuilder_ProductionBlockDefinition ob) { ob.BlueprintClasses = new string[0]; }

        public void LoadPostProcess()
        {
            // Remove invalid blueprint classes
            for (int i = 0; i < BlueprintClasses.Count;)
            {
                if (!BlueprintClassCanBeUsed(BlueprintClasses[i]))
                    BlueprintClasses.RemoveAt(i);
                else
                    ++i;
            }

            // Set the constraint icons only if all the blueprint classes agree upon them
            string firstInputIcon = null;
            string firstOutputIcon = null;
            if (BlueprintClasses.Count != 0)
            {
                firstInputIcon = BlueprintClasses[0].InputConstraintIcon;
                firstOutputIcon = BlueprintClasses[0].OutputConstraintIcon;

                for (int i = 1; i < BlueprintClasses.Count; ++i)
                {
                    if (BlueprintClasses[i].InputConstraintIcon != firstInputIcon) firstInputIcon = null;
                    if (BlueprintClasses[i].OutputConstraintIcon != firstOutputIcon) firstOutputIcon = null;
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(MySpaceTexts.ToolTipItemFilter_GenericProductionBlockInput, DisplayNameText);
            InputInventoryConstraint = new MyInventoryConstraint(sb.ToString(), firstInputIcon);

            sb = new StringBuilder();
            sb.AppendFormat(MySpaceTexts.ToolTipItemFilter_GenericProductionBlockOutput, DisplayNameText);
            OutputInventoryConstraint = new MyInventoryConstraint(sb.ToString(), firstOutputIcon);

            for (int i = 0; i < BlueprintClasses.Count; ++i)
            {
                foreach (var blueprint in BlueprintClasses[i])
                {
                    foreach (var input in blueprint.Prerequisites)
                        InputInventoryConstraint.Add(input.Id);
                    foreach (var output in blueprint.Results)
                        OutputInventoryConstraint.Add(output.Id);
                }
            }
        }
    }
}
