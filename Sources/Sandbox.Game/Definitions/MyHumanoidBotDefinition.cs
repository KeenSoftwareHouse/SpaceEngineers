using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_HumanoidBotDefinition))]
    public class MyHumanoidBotDefinition : MyAgentDefinition
    {
        public MyDefinitionId StartingWeaponDefinitionId;
        public List<MyDefinitionId> InventoryItems;

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
        }

        public override void AddItems(Sandbox.Game.Entities.Character.MyCharacter character)
        {
            base.AddItems(character);
            var ob = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_PhysicalGunObject>(StartingWeaponDefinitionId.SubtypeName);
            if (character.WeaponTakesBuilderFromInventory(StartingWeaponDefinitionId))
            {
                System.Diagnostics.Debug.Assert((character.GetInventory(0) as MyInventory) != null, "Null or unexpected inventory type!");
                (character.GetInventory(0) as MyInventory).AddItems(1, ob);
            }

            // else // allowing the inventory items to be added
            {
                foreach (var weaponDef in InventoryItems)
                {
                    ob = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_PhysicalGunObject>(weaponDef.SubtypeName);
                    System.Diagnostics.Debug.Assert((character.GetInventory(0) as MyInventory) != null, "Null or unexpected inventory type!");
                    (character.GetInventory(0) as MyInventory).AddItems(1, ob);
                }
            }

            character.SwitchToWeapon(StartingWeaponDefinitionId);

            {
                MyDefinitionId weaponDefinitionId;
                weaponDefinitionId = new MyDefinitionId(typeof(MyObjectBuilder_WeaponDefinition), StartingWeaponDefinitionId.SubtypeName);

                MyWeaponDefinition weaponDefinition;

                if (MyDefinitionManager.Static.TryGetWeaponDefinition(weaponDefinitionId, out weaponDefinition)) //GetWeaponDefinition(StartingWeaponId);
                {
                    if (weaponDefinition.HasAmmoMagazines())
                    {
                        var ammo = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_AmmoMagazine>(weaponDefinition.AmmoMagazinesId[0].SubtypeName);
                        System.Diagnostics.Debug.Assert((character.GetInventory(0) as MyInventory) != null, "Null or unexpected inventory type!");
                        (character.GetInventory(0) as MyInventory).AddItems(3, ammo);
                    }
                }
            }
        }
    }
}
