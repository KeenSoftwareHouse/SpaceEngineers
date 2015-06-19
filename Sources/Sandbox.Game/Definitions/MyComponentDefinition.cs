using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Utils;


namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_ComponentDefinition))]
    public class MyComponentDefinition : MyPhysicalItemDefinition
    {
        /// <summary>
        /// HP of the component. Used when calculating overall HP of block from its components.
        /// </summary>
        public int MaxIntegrity;

        /// <summary>
        /// Chance that the damaged component will be dropped when damage is inflicted to a component stack.
        /// Percentage given as value from 0 to 1.
        /// </summary>
        public float DropProbability;

		public float DeconstructionEfficiency;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_ComponentDefinition;
            MyDebug.AssertDebug(ob != null);
            this.MaxIntegrity = ob.MaxIntegrity;
            this.DropProbability = ob.DropProbability;
			DeconstructionEfficiency = ob.DeconstructionEfficiency;
        }
    }
}
