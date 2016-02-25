using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Utils;

namespace Sandbox.Game.Entities.Inventory
{
    public static class MyInventoryBaseExtensions
    {
        static List<MyComponentBase> m_tmpList = new List<MyComponentBase>();

        public static MyInventoryBase GetInventory(this MyEntity entity, MyStringHash inventoryId)
        {
            MyInventoryBase inventory = null;

            inventory = entity.Components.Get<MyInventoryBase>();
            if (inventory != null)
            {
                if (inventoryId.Equals(MyStringHash.GetOrCompute(inventory.InventoryId.ToString())))
                {
                    return inventory;
                }
            }

            if (inventory is MyInventoryAggregate)
            {
                var aggregate = inventory as MyInventoryAggregate;
                m_tmpList.Clear();
                aggregate.GetComponentsFlattened(m_tmpList);
                foreach (var component in m_tmpList)
                {
                    var componentInventory = component as MyInventoryBase;
                    if (inventoryId.Equals(MyStringHash.GetOrCompute(componentInventory.InventoryId.ToString())))
                    {
                        return componentInventory;
                    }
                }
            }

            return null;
        }

    }
}
