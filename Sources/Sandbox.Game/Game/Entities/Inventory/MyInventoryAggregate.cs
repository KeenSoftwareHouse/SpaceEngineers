using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.ComponentSystem;
using Sandbox.Definitions;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Components;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Sandbox.Game.Entities.Inventory
{
    /// <summary>
    /// This class implements basic functionality for the interface IMyInventoryAggregate. Use it as base class only if the basic functionality is enough.
    /// </summary>
    [MyComponentBuilder(typeof(MyObjectBuilder_InventoryAggregate))]
    public class MyInventoryAggregate : MyInventoryBase, IMyComponentAggregate
    {
        private MyAggregateComponentList m_children = new MyAggregateComponentList();
        public virtual event Action<MyInventoryAggregate, MyInventoryBase> OnAfterComponentAdd;
        public virtual event Action<MyInventoryAggregate, MyInventoryBase> OnBeforeComponentRemove;
        private List<MyComponentBase> tmp_list = new List<MyComponentBase>();
        private List<MyPhysicalInventoryItem> m_allItems = new List<MyPhysicalInventoryItem>();

        #region Properties

        public override MyFixedPoint CurrentMass
        {
            get
            {
                float mass = 0;
                foreach (MyInventoryBase inventory in m_children.Reader)
                {
                    mass += (float)inventory.CurrentMass;
                }
                return (MyFixedPoint)mass;
            }
        }

        public override MyFixedPoint MaxMass
        {
            get
            {
                float mass = 0;
                foreach (MyInventoryBase inventory in m_children.Reader)
                {
                    mass += (float)inventory.MaxMass;
                }
                return (MyFixedPoint)mass;
            }
        }

        public override MyFixedPoint CurrentVolume
        {
            get
            {
                float volume = 0;
                foreach (MyInventoryBase inventory in m_children.Reader)
                {
                    volume += (float)inventory.CurrentVolume;
                }
                return (MyFixedPoint)volume;
            }
        }

        public override MyFixedPoint MaxVolume
        {
            get
            {
                float volume = 0;
                foreach (MyInventoryBase inventory in m_children.Reader)
                {
                    volume += (float)inventory.MaxVolume;
                }
                return (MyFixedPoint)volume;
            }
        }

        #endregion

        #region De/Constructor & Init

        public MyInventoryAggregate(): base("Inventory") { }

        public MyInventoryAggregate(string inventoryId) : base(inventoryId) {}

        // Register callbacks
        public void Init()
        {
            foreach (MyInventoryBase inventory in m_children.Reader)
            {
                inventory.ContentsChanged += child_OnContentsChanged;
            }
        }

        // Detach callbacks
        public void DetachCallbacks()
        {
            foreach (MyInventoryBase inventory in m_children.Reader)
            {
                inventory.ContentsChanged -= child_OnContentsChanged;
            }
        }

        ~MyInventoryAggregate()
        {
            DetachCallbacks();
        }

        #endregion

        public override MyFixedPoint ComputeAmountThatFits(MyDefinitionId contentId)
        {
            float amount = 0;
            foreach (MyInventoryBase inventory in m_children.Reader)
            {
                amount += (float)inventory.ComputeAmountThatFits(contentId);
            }
            return (MyFixedPoint)amount;
        }

        public override MyFixedPoint GetItemAmount(MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None)
        {
            float amount = 0;
            foreach (MyInventoryBase inventory in m_children.Reader)
            {
                amount += (float)inventory.GetItemAmount(contentId, flags);
            }
            return (MyFixedPoint) amount;
        }

        public override bool AddItems(MyFixedPoint amount, MyObjectBuilder_Base objectBuilder, int index = -1, bool stack = true)
        {
            var maxAmount = ComputeAmountThatFits(objectBuilder.GetId());
            var restAmount = amount;
            if (amount <= maxAmount)
            {
                foreach (MyInventoryBase inventory in m_children.Reader)
                {
                    var availableSpace = inventory.ComputeAmountThatFits(objectBuilder.GetId());
                    if (availableSpace > restAmount)
                    {
                        availableSpace = restAmount;
                    }
                    if (availableSpace > 0)
                    {
                        if (inventory.AddItems(availableSpace, objectBuilder, index, stack))
                        {
                            restAmount -= availableSpace;
                        }
                    }
                }
            }
            return restAmount == 0;
        }

        public override MyFixedPoint RemoveItemsOfType(MyFixedPoint amount, MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None, bool spawn = false)
        {
            var restAmount = amount;
            foreach (MyInventoryBase inventory in m_children.Reader)
            {
                restAmount -= inventory.RemoveItemsOfType(restAmount, contentId, flags, spawn);
            }
            return amount - restAmount;
        }

        public MyInventoryBase GetInventory(MyStringId id)        
        {
            tmp_list.Clear();
            this.GetComponentsFlattened(tmp_list);
            foreach (var item in tmp_list)
            {
                MyInventoryBase inventory = item as MyInventoryBase;
                if (inventory.InventoryId == id)
                {
                    return inventory;
                }
            }
            return null;
        }

        public MyInventoryBase GetInventory(MyStringHash id)
        {
            tmp_list.Clear();
            this.GetComponentsFlattened(tmp_list);
            foreach (var item in tmp_list)
            {
                MyInventoryBase inventory = item as MyInventoryBase;
                if (MyStringHash.GetOrCompute(inventory.InventoryId.ToString()) == id)
                {
                    return inventory;
                }
            }
            return null;
        }

        public MyAggregateComponentList ChildList
        {
            get { return m_children; }
        }

        public void AfterComponentAdd(MyComponentBase component)
        {
            (component as MyInventoryBase).ContentsChanged += child_OnContentsChanged;
            if (OnAfterComponentAdd != null)
            {
                OnAfterComponentAdd(this, component as MyInventoryBase);
            }                
        }

        public void BeforeComponentRemove(MyComponentBase component)
        {
            (component as MyInventoryBase).ContentsChanged -= child_OnContentsChanged;
            if (OnBeforeComponentRemove != null)
            {
                OnBeforeComponentRemove(this, component as MyInventoryBase);
            }
        }

        private void child_OnContentsChanged(MyComponentBase obj)
        {
            OnContentsChanged();
        }

        public override MyObjectBuilder_ComponentBase Serialize()
        {
            var ob = base.Serialize() as MyObjectBuilder_InventoryAggregate;

            var reader = m_children.Reader;
            if (reader.Count > 0)
            {
                ob.Inventories = new List<MyObjectBuilder_InventoryBase>(reader.Count);
                foreach (var inventory in reader)
                {
                    var invOb = inventory.Serialize() as MyObjectBuilder_InventoryBase;
                    if (invOb != null)
                        ob.Inventories.Add(invOb);
                }
            }

            return ob;
        }

        public override void Deserialize(MyObjectBuilder_ComponentBase builder)
        {
            base.Deserialize(builder);

            var ob = builder as MyObjectBuilder_InventoryAggregate;

            if (ob != null && ob.Inventories != null)
            {
                foreach (var obInv in ob.Inventories)
                {
                    var comp = MyComponentFactory.CreateInstance(obInv.GetType());
                    comp.Deserialize(obInv);
                    this.AddComponent(comp);
                }
            }
        }

        public override void CountItems(Dictionary<MyDefinitionId, MyFixedPoint> itemCounts)
        {
            foreach (MyInventoryBase inventory in m_children.Reader)
            {
                inventory.CountItems(itemCounts);
            }
        }

        public override void ApplyChanges(List<MyComponentChange> changes)
        {
            foreach (MyInventoryBase inventory in m_children.Reader)
            {
                inventory.ApplyChanges(changes);
            }
        }

		// MK: TODO: ItemsCanBeAdded, ItemsCanBeRemoved, Add and Remove should probably support getting stuff from several inventories at once
        public override bool ItemsCanBeAdded(MyFixedPoint amount, IMyInventoryItem item)
        {
            foreach(MyInventoryBase inventory in m_children.Reader)
			{
				if (inventory.ItemsCanBeAdded(amount, item))
					return true;
			}
			return false;
        }

        public override bool ItemsCanBeRemoved(MyFixedPoint amount, IMyInventoryItem item)
        {
            foreach(MyInventoryBase inventory in m_children.Reader)
			{
				if (inventory.ItemsCanBeRemoved(amount, item))
					return true;
			}
			return false;
        }

        public override bool Add(IMyInventoryItem item, MyFixedPoint amount, bool stack = true)
        {
            foreach(MyInventoryBase inventory in m_children.Reader)
			{
				if (inventory.ItemsCanBeAdded(amount, item) && inventory.Add(item, amount, stack))
					return true;
			}
			return false;
        }

        public override bool Remove(IMyInventoryItem item, MyFixedPoint amount)
        {
            foreach(MyInventoryBase inventory in m_children.Reader)
			{
				if (inventory.ItemsCanBeRemoved(amount, item) && inventory.Remove(item, amount))
					return true;
			}
			return false;
        }

        public override List<MyPhysicalInventoryItem> GetItems()
        {
            m_allItems.Clear();
            foreach (MyInventoryBase inventory in m_children.Reader)
            {
                m_allItems.AddRange(inventory.GetItems());
            }
            return m_allItems;
        }
    }
}
