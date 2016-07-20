using System.Collections.Generic;
using System.Linq;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.Definitions;
using VRage.Utils;



namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_PhysicalModelDefinition), typeof(Postprocessor))]
    public class MyPhysicalModelDefinition : MyDefinitionBase
    {
        public string Model;
        public MyPhysicalMaterialDefinition PhysicalMaterial;
        public float Mass;

        private string m_material;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_PhysicalModelDefinition;
            MyDebug.AssertDebug(ob != null);

            this.Model = ob.Model;
            if (GetType() == typeof (MyCubeBlockDefinition) || GetType().IsSubclassOf(typeof (MyCubeBlockDefinition)))
                this.PhysicalMaterial = MyDestructionData.GetPhysicalMaterial(this, ob.PhysicalMaterial);
            else
                m_material = ob.PhysicalMaterial;
            this.Mass = ob.Mass;
        }

        protected class Postprocessor : MyDefinitionPostprocessor
        {
            public override void AfterLoaded(ref Bundle definitions)
            {

            }

            public override void AfterPostprocess(MyDefinitionSet set, Dictionary<MyStringHash, MyDefinitionBase> definitions)
            {
                foreach (var def in definitions.Values.Cast<MyPhysicalModelDefinition>())
                {
                    def.PhysicalMaterial = MyDestructionData.GetPhysicalMaterial(def, def.m_material);
                }
            }
        }
    }

}
