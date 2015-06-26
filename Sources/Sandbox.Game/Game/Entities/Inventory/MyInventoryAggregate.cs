using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.ComponentSystem;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Components;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Sandbox.Game.Entities.Inventory
{
    /// <summary>
    /// This class implements basic functionality for the interface IMyInventoryAggregate. Use it as base class only if the basic functionallity is enough.
    /// </summary>
    [MyComponentBuilder(typeof(MyObjectBuilder_InventoryAggregate))]
    public class MyInventoryAggregate : MyInventoryBase, IMyComponentAggregate
    {
        private MyAggregateComponentList m_children = new MyAggregateComponentList();
        public event Action<MyInventoryAggregate, MyInventoryBase> OnAfterComponentAdd;
        public event Action<MyInventoryAggregate, MyInventoryBase> OnBeforeComponentRemove;

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

        public override MyStringId InventoryId
        {
            get { return MyStringId.GetOrCompute("InventoryAggregate"); }
        }

        #endregion

        #region De/Constructor & Init

        public MyInventoryAggregate() { }

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

        public override bool AddItems(MyFixedPoint amount, MyObjectBuilder_Base objectBuilder, int index = -1)
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
                    if (inventory.AddItems(availableSpace, objectBuilder))
                    {
                        restAmount -= availableSpace;
                    }
                }
            }
            return restAmount == 0;
        }

        public override void RemoveItemsOfType(MyFixedPoint amount, MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None, bool spawn = false)
        {
            var restAmount = amount;
            foreach (MyInventoryBase inventory in m_children.Reader)
            {
                var contains = inventory.GetItemAmount(contentId, flags);
                if (contains > restAmount)
                {
                    contains = restAmount;
                }
                if (contains > 0) inventory.RemoveItemsOfType(contains, contentId, flags, spawn);
                restAmount -= contains;
            }
        }

        // CH: TODO: Do with a supplied preallocated list as output
        public override List<MyPhysicalInventoryItem> GetItems()
        {
            List<MyPhysicalInventoryItem> items = new List<MyPhysicalInventoryItem>();
            foreach (MyInventoryBase inventory in m_children.Reader)
            {
                items.AddList(inventory.GetItems());
            }
            return items;
        }

        public MyInventoryBase GetInventory(MyStringId id)
        {
            foreach (MyInventoryBase inventory in m_children.Reader)
            {
                if (inventory.InventoryId == id) return inventory;
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
    }
}
