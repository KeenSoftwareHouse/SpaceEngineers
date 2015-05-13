using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Engine.Utils;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_WeaponBlockDefinition))]
    public class MyWeaponBlockDefinition : MyCubeBlockDefinition
    {
        public MyDefinitionId WeaponDefinitionId;

        public float InventoryMaxVolume;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_WeaponBlockDefinition;

            WeaponDefinitionId = new MyDefinitionId(ob.WeaponDefinitionId.Type, ob.WeaponDefinitionId.Subtype);
            InventoryMaxVolume = ob.InventoryMaxVolume;
        }
    }
}
