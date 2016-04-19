using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using VRage.Collections;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.World;
using VRage.Game.Entity;
using Sandbox.Game.Entities.Inventory;
using VRage;
using Sandbox.Engine.Utils;
using VRage.Game;

namespace Sandbox.Game.Gui
{
    public class MyHudChangedInventoryItems
    {
        private const int GROUP_ITEMS_COUNT = 6;
        private const float TIME_TO_REMOVE_ITEM_SEC = 5f;

        public struct MyItemInfo
        {
            public MyDefinitionId DefinitionId;
            public string[] Icons;
            public MyFixedPoint ChangedAmount;
            public MyFixedPoint TotalAmount;
            public bool Added;
            public double AddTime; // Added time in total second (can be delayed for next group)
            public double RemoveTime; // Time to remove the item
        }

        private List<MyItemInfo> m_items = new List<MyItemInfo>();
        public ListReader<MyItemInfo> Items { get { return new ListReader<MyItemInfo>(m_items); } }

        public bool Visible { get; private set; }

        public MyHudChangedInventoryItems()
        {
            Visible = false;
        }

        public void Show()
        {
            Visible = true;
        }

        public void Hide()
        {
            Visible = false;
        }

        private void AddItem(MyItemInfo item)
        {
            var addedTime = MySession.Static.ElapsedGameTime.TotalSeconds;

            if (m_items.Count > 0)
            {
                var lastItem = m_items[m_items.Count - 1];
                // Added the same item as last one?
                if (lastItem.DefinitionId == item.DefinitionId && lastItem.Added == item.Added)
                {
                    lastItem.ChangedAmount += item.ChangedAmount;
                    lastItem.TotalAmount = item.TotalAmount;

                    if (m_items.Count <= GROUP_ITEMS_COUNT)
                        lastItem.RemoveTime = addedTime + TIME_TO_REMOVE_ITEM_SEC;

                    m_items[m_items.Count - 1] = lastItem;

                    return;
                }
                else
                {
                    if (m_items.Count >= GROUP_ITEMS_COUNT)
                    {
                        int prevGroupIndex = m_items.Count - GROUP_ITEMS_COUNT;
                        Debug.Assert(prevGroupIndex >= 0 && prevGroupIndex < m_items.Count);
                        var prevGroupItem = m_items[prevGroupIndex];
                        var nextGroupTime = prevGroupItem.AddTime + TIME_TO_REMOVE_ITEM_SEC;
                        addedTime = Math.Max(addedTime, nextGroupTime);
                    }
                }
            }

            item.AddTime = addedTime;
            item.RemoveTime = addedTime + TIME_TO_REMOVE_ITEM_SEC;
            m_items.Add(item);
        }

        public void AddChangedPhysicalInventoryItem(MyPhysicalInventoryItem intentoryItem, MyFixedPoint changedAmount, bool added)
        {
            Debug.Assert(changedAmount > 0 || (!added && changedAmount < 0));

            var definition = intentoryItem.GetItemDefinition();
            if (definition == null)
                return;

            if (changedAmount < 0)
                changedAmount = -changedAmount;
            Debug.Assert(changedAmount > 0);

            var item = new MyItemInfo()
            {
                DefinitionId = definition.Id,
                Icons = definition.Icons,
                TotalAmount = intentoryItem.Amount,
                ChangedAmount = changedAmount,
                Added = added
            };

            AddItem(item);
        }

        public void Update()
        {
            double totalSeconds = MySession.Static.ElapsedGameTime.TotalSeconds;
            for (int i = m_items.Count - 1; i >= 0; --i)
            {
                if (totalSeconds - Items[i].RemoveTime > 0)
                    m_items.RemoveAt(i);
            }
        }

        public void Clear()
        {
            m_items.Clear();
        }
    }
}
