using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;


namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_RefineryDefinition))]
    public class MyRefineryDefinition : MyProductionBlockDefinition
    {
        /// <summary>
        /// Speed of refining ore in kg per hour.
        /// </summary>
        public float RefineSpeed;

        /// <summary>
        /// Percentage of material kept after refining process. Value is in range from
        /// 0 to 1, where 0.7 means that 30% of material is lost and 70% is kept.
        /// </summary>
        public float MaterialEfficiency;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            MyDebug.AssertDebug(builder is MyObjectBuilder_RefineryDefinition);
            var obRefineryDef = builder as MyObjectBuilder_RefineryDefinition;
            this.RefineSpeed        = obRefineryDef.RefineSpeed;
            this.MaterialEfficiency = obRefineryDef.MaterialEfficiency;
        }

        protected override bool BlueprintClassCanBeUsed(MyBlueprintClassDefinition blueprintClass)
        {
            foreach (var blueprint in blueprintClass)
                if (blueprint.Atomic)
                {
                    System.Diagnostics.Debug.Assert(false, "Blueprint " + blueprint.DisplayNameText + " is atomic, but it is in a class used by refinery block");
                    MySandboxGame.Log.WriteLine("Blueprint " + blueprint.DisplayNameText + " is atomic, but it is in a class used by refinery block");
                    return false;
                }

            return base.BlueprintClassCanBeUsed(blueprintClass);
        }

        protected override void InitializeLegacyBlueprintClasses(MyObjectBuilder_ProductionBlockDefinition ob)
        {
            ob.BlueprintClasses = new string[] { "Ingots" };
        }
    }
}
