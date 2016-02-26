using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;


namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_AssemblerDefinition))]
    public class MyAssemblerDefinition : MyProductionBlockDefinition
    {
        /// <summary>
        /// Assembly speed multiplier
        /// </summary>,
        private float m_assemblySpeed;
        public float AssemblySpeed
        {
            get { return m_assemblySpeed; }
        }

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            MyDebug.AssertDebug(builder is MyObjectBuilder_AssemblerDefinition);
            var obRefineryDef = builder as MyObjectBuilder_AssemblerDefinition;
            m_assemblySpeed = obRefineryDef.AssemblySpeed;
        }

        protected override void InitializeLegacyBlueprintClasses(MyObjectBuilder_ProductionBlockDefinition ob)
        {
            ob.BlueprintClasses = new string[] { "LargeBlocks", "SmallBlocks", "Components", "Tools" };
        }
    }
}
