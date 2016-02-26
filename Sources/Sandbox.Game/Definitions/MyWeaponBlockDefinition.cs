using Sandbox.Common.ObjectBuilders.Definitions;
using System.Diagnostics;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_WeaponBlockDefinition))]
    public class MyWeaponBlockDefinition : MyCubeBlockDefinition
    {
        public MyDefinitionId WeaponDefinitionId;

		public MyStringHash ResourceSinkGroup;

        public float InventoryMaxVolume;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_WeaponBlockDefinition;
			Debug.Assert(builder != null);

            WeaponDefinitionId = new MyDefinitionId(ob.WeaponDefinitionId.Type, ob.WeaponDefinitionId.Subtype);
			ResourceSinkGroup = MyStringHash.GetOrCompute(ob.ResourceSinkGroup);
            InventoryMaxVolume = ob.InventoryMaxVolume;
        }
    }
}
