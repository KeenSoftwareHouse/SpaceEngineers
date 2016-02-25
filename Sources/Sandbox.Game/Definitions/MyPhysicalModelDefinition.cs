using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;



namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_PhysicalModelDefinition))]
    public class MyPhysicalModelDefinition : MyDefinitionBase
    {
        public string Model;
        public MyPhysicalMaterialDefinition PhysicalMaterial;
        public float Mass;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_PhysicalModelDefinition;
            MyDebug.AssertDebug(ob != null);

            this.Model = ob.Model;
            this.PhysicalMaterial = MyDestructionData.GetPhysicalMaterial(this, ob.PhysicalMaterial);
            this.Mass = ob.Mass;
        }
    }
}
