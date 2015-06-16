
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_PhysicalItemDefinition))]
    public class MyPhysicalItemDefinition : MyDefinitionBase
    {
        public Vector3 Size; // in meters
        public float Mass; // in Kg
        public string Model;
        public MyStringId? IconSymbol;
        public float Volume; // in m3
        public MyStringHash PhysicalMaterial;
		public bool HasDeconstructor;

        public bool HasIntegralAmounts
        {
            get
            {
                return Id.TypeId != typeof(MyObjectBuilder_Ingot) &&
                       Id.TypeId != typeof(MyObjectBuilder_Ore);
            }
        }

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_PhysicalItemDefinition;
            MyDebug.AssertDebug(ob != null);
            this.Size = ob.Size;
            this.Mass = ob.Mass;
            this.Model = ob.Model;
            this.Volume = ob.Volume.HasValue? ob.Volume.Value / 1000f : ob.Size.Volume;
            if (string.IsNullOrEmpty(ob.IconSymbol))
                this.IconSymbol = null;
            else
                this.IconSymbol = MyStringId.GetOrCompute(ob.IconSymbol);
            PhysicalMaterial = MyStringHash.GetOrCompute(ob.PhysicalMaterial);
			HasDeconstructor = ob.HasDeconstructor;
        }
    }
}
