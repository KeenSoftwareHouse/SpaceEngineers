using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Definitions;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace Sandbox.Game.Entities
{
    [ProtoContract]
    public struct MyStockpileItem
    {
        [ProtoMember]
        public int Amount;

        [ProtoMember]
        [Serialize(MyObjectFlags.Dynamic | MyObjectFlags.Nullable, DynamicSerializerType = typeof(MyObjectBuilderDynamicSerializer))]
        public MyObjectBuilder_PhysicalObject Content;

        public override string ToString()
        {
            return String.Format("{0}x {1}", Amount, Content.SubtypeName);
        }
    }

    public class MyConstructionStockpile
    {
        private List<MyStockpileItem> m_items = new List<MyStockpileItem>();
        private static List<MyStockpileItem> m_syncItems = new List<MyStockpileItem>();

        public MyConstructionStockpile() {}

        public MyObjectBuilder_ConstructionStockpile GetObjectBuilder()
        {
            MyObjectBuilder_ConstructionStockpile objectBuilder;

            objectBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ConstructionStockpile>();
            objectBuilder.Items = new MyObjectBuilder_StockpileItem[m_items.Count];
            for (int index = 0; index < m_items.Count; index++)
            {
                var item = m_items[index];
                var itemBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_StockpileItem>();
                itemBuilder.Amount = item.Amount;
                itemBuilder.PhysicalContent = item.Content;
                objectBuilder.Items[index] = itemBuilder;
            }

            return objectBuilder;
        }

        public void Init(MyObjectBuilder_ConstructionStockpile objectBuilder)
        {
            m_items.Clear();

            if (objectBuilder == null)
                return;

            foreach (var item in objectBuilder.Items)
            {
                if (item.Amount > 0)
                {
                    MyStockpileItem newItem = new MyStockpileItem();
                    newItem.Amount = item.Amount;
                    newItem.Content = item.PhysicalContent;
                    m_items.Add(newItem);
                }
            }
        }

        public void Init(MyObjectBuilder_Inventory objectBuilder)
        {
            m_items.Clear();

            if (objectBuilder == null)
                return;

            foreach (var item in objectBuilder.Items)
            {
                if (item.Amount > 0)
                {
                    MyStockpileItem newItem = new MyStockpileItem();
                    newItem.Amount = (int)(item.Amount);
                    newItem.Content = item.PhysicalContent;
                    m_items.Add(newItem);
                }
            }
        }

        public bool IsEmpty()
        {
            return m_items.Count == 0;
        }

        public void ClearSyncList()
        {
            m_syncItems.Clear();
        }

        public List<MyStockpileItem> GetSyncList()
        {
            return m_syncItems;
        }

        public bool AddItems(int count, MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None)
        {
            var componentBuilder = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(contentId);

            Debug.Assert(componentBuilder != null, "Could not cast object builder to physical object");
            if (componentBuilder == null) return false;

            componentBuilder.Flags = flags;
            return AddItems(count, componentBuilder);
        }

        public bool AddItems(int count, MyObjectBuilder_PhysicalObject physicalObject)
        {
            int index = 0;
            foreach (var item in m_items)
            {
                if (item.Content.CanStack(physicalObject))
                    break;
                index++;
            }
            if (index == m_items.Count)
            {
                Debug.Assert(count < int.MaxValue, "Trying to add more items into construction stockpile than int.MaxValue");
                if (count >= int.MaxValue) return false;

                MyStockpileItem item = new MyStockpileItem();
                item.Amount = (int)count;
                item.Content = physicalObject;
                m_items.Add(item);
                AddSyncItem(item);
                return true;
            }
            else
            {
                Debug.Assert((long)m_items[index].Amount + count < int.MaxValue, "Trying to add more items into construction stockpile than int.MaxValue");
                if ((long)m_items[index].Amount + count >= int.MaxValue) return false;

                MyStockpileItem item = new MyStockpileItem();
                item.Amount = (int)(m_items[index].Amount + count);
                item.Content = m_items[index].Content;
                m_items[index] = item;

                MyStockpileItem syncItem = new MyStockpileItem();
                syncItem.Content = m_items[index].Content;
                syncItem.Amount = (int)count;
                AddSyncItem(syncItem);
                return true;
            }

            return false;
        }

        public bool RemoveItems(int count, MyObjectBuilder_PhysicalObject physicalObject)
        {
            return RemoveItems(count, physicalObject.GetId(), physicalObject.Flags);
        }

        public bool RemoveItems(int count, MyDefinitionId id, MyItemFlags flags = MyItemFlags.None)
        {
            int index = 0;
            foreach (var item in m_items)
            {
                if (item.Content.CanStack(id.TypeId, id.SubtypeId, flags))
                    break;
                index++;
            }
            return RemoveItemsInternal(index, count);
        }

        private bool RemoveItemsInternal(int index, int count)
        {
            if (index >= m_items.Count) return false;

            if (m_items[index].Amount == count)
            {
                MyStockpileItem syncItem = m_items[index];
                syncItem.Amount = -syncItem.Amount;

                AddSyncItem(syncItem);
                m_items.RemoveAt(index);
                return true;
            }
            else if (count < m_items[index].Amount)
            {
                MyStockpileItem item = new MyStockpileItem();
                item.Amount = m_items[index].Amount - count;
                item.Content = m_items[index].Content;
                m_items[index] = item;

                MyStockpileItem syncItem = new MyStockpileItem();
                syncItem.Content = item.Content;
                syncItem.Amount = -count;
                AddSyncItem(syncItem);
                return true;
            }
            Debug.Assert(count < m_items[index].Amount, "Removing more items from the construction stockpile than how many are contained in it");
            return false;
        }

        private void AddSyncItem(MyStockpileItem diffItem)
        {
            int index = 0;
            foreach (var item in m_syncItems)
            {
                if (item.Content.CanStack(diffItem.Content))
                {
                    var tmpItem = new MyStockpileItem();
                    tmpItem.Amount = item.Amount + diffItem.Amount;
                    tmpItem.Content = item.Content;
                    m_syncItems[index] = tmpItem;
                    return;
                }
                index++;
            }

            m_syncItems.Add(diffItem);
        }

        public List<MyStockpileItem> GetItems()
        {
            return m_items;
        }

        public int GetItemAmount(MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None)
        {
            foreach (var item in m_items)
            {
                if (item.Content.CanStack(contentId.TypeId, contentId.SubtypeId, flags))
                {
                    return item.Amount;
                }
            }
            return 0;
        }

        internal void Change(List<MyStockpileItem> items)
        {
            // We don't have to iterate over the newly added items, as they should not be stackable (server would have sent them together)
            int originalCount = m_items.Count;

            foreach (var diffItem in items)
            {
                int i;
                for (i = 0; i < originalCount; ++i)
                {
                    if (m_items[i].Content.CanStack(diffItem.Content))
                    {
                        MyStockpileItem changedItem = m_items[i];
                        changedItem.Amount += diffItem.Amount;
                        m_items[i] = changedItem;
                        break;
                    }
                }
                if (i == originalCount)
                {
                    MyStockpileItem changedItem = new MyStockpileItem();
                    changedItem.Amount = diffItem.Amount;
                    changedItem.Content = diffItem.Content;
                    m_items.Add(changedItem);
                }
            }

            // Remove items with zero count
            for (int i = m_items.Count - 1; i >= 0; --i)
            {
                if (m_items[i].Amount == 0)
                {
                    m_items.RemoveAtFast(i);
                }
            }
        }
    }
}
