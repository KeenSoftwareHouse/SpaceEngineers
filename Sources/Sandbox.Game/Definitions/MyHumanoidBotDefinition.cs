using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_HumanoidBotDefinition))]
    public class MyHumanoidBotDefinition : MyAgentDefinition
    {
        public MyDefinitionId StartingWeaponDefinitionId;
        public List<MyDefinitionId> InventoryItems;
        public bool InventoryContentGenerated = false;
        public MyDefinitionId InventoryContainerTypeId;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_HumanoidBotDefinition;
            if (ob.StartingItem != null && !string.IsNullOrWhiteSpace(ob.StartingItem.Subtype))
                this.StartingWeaponDefinitionId = new MyDefinitionId(ob.StartingItem.Type, ob.StartingItem.Subtype);
            InventoryItems = new List<MyDefinitionId>();
            if (ob.InventoryItems != null)
            {
                foreach (var item in ob.InventoryItems)
                    InventoryItems.Add(new MyDefinitionId(item.Type, item.Subtype));
            }
            InventoryContentGenerated = ob.InventoryContentGenerated;
            if (ob.InventoryContainerTypeId.HasValue)
            {
                InventoryContainerTypeId = ob.InventoryContainerTypeId.Value;
            }
        }
    }
}
