using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.ObjectBuilders;

namespace Sandbox.Game.Entities.Inventory
{
    /// <summary>
    /// This class implements basic functionality for the interface IMyInventoryAggregate. Use it as base class only if the basic functionallity is enough.
    /// </summary>
    public class MyInventoryAggregateBase : IMyInventoryAggregate
    {         
        #region Fields

        private  IMyInventoryOwner m_owner;

        private List<IMyInventoryAggregate> m_children = new List<IMyInventoryAggregate>();

        private List<IMyComponentInventory> m_attachedInventories = new List<IMyComponentInventory>();

        #endregion

        #region Events

        public event Action<IMyComponentInventory> ContentsChanged;

        public event Action<IMyComponentInventory, IMyInventoryOwner> OwnerChanged;
        

        private void OnContentsChanged(IMyComponentInventory inventory)
        {
            if (ContentsChanged != null)
            {
                ContentsChanged(inventory);
            }
        }

        private void OnOwnerChanged(IMyComponentInventory inventory, IMyInventoryOwner owner)
        {
            if (OwnerChanged != null)
            {
                OwnerChanged(inventory, owner);
            }
        }

        #endregion

        #region Properties

        //TODO: This will be removed in future
        public IMyInventoryOwner Owner
        {
            get { return m_owner; }
        }

        /// <summary>
        /// If the owner is IMyComponentInventoryOwner, return all ComponentInventories, otherwise return the first inventory
        /// </summary>
        public List<IMyComponentInventory> OwnerInventories
        {
            get
            {
                List<IMyComponentInventory> list = new List<IMyComponentInventory>();

                // list.AddList(GetAllCompontentInventories()); -- this is not working now

                for (int i = 0; i < Owner.InventoryCount; ++i)
                {
                    if (!list.Contains(Owner.GetInventory(i)))
                    {
                        list.Add(Owner.GetInventory(i));
                    }
                }                

                foreach (var child in m_children)
                {
                    list = list.Union(child.OwnerInventories).ToList();
                }

                return list;
            }
        }

        /// <summary>
        /// Returns list of all inventories, including inventories from children aggregates
        /// </summary>
        public List<IMyComponentInventory> Inventories
        {
            get
            {
                List<IMyComponentInventory> inventories = new List<IMyComponentInventory>();
                foreach (var child in m_children)
                {
                    inventories = inventories.Union(child.Inventories).ToList();
                }
                inventories = inventories.Union(OwnerInventories).ToList();
                inventories = inventories.Union(m_attachedInventories).ToList();
                return inventories;
            }
        }

        public MyFixedPoint CurrentMass
        {
            get
            {
                float mass = 0;
                foreach (var inventory in Inventories)
                {
                    mass += (float)inventory.CurrentMass;
                }
                return (MyFixedPoint)mass;
            }
        }

        public MyFixedPoint MaxMass
        {
            get
            {
                float mass = 0;
                foreach (var inventory in Inventories)
                {
                    mass += (float)inventory.MaxMass;
                }
                return (MyFixedPoint)mass;
            }
        }

        public MyFixedPoint CurrentVolume
        {
            get
            {
                float volume = 0;
                foreach (var inventory in Inventories)
                {
                    volume += (float)inventory.CurrentVolume;
                }
                return (MyFixedPoint)volume;
            }
        }

        public MyFixedPoint MaxVolume
        {
            get
            {
                float volume = 0;
                foreach (var inventory in Inventories)
                {
                    volume += (float)inventory.MaxVolume;
                }
                return (MyFixedPoint)volume;
            }
        }

        public VRage.Utils.MyStringId InventoryName
        {
            get { return VRage.Utils.MyStringId.GetOrCompute("InventoryAggregate"); }
        }

        #endregion

        #region De/Constructor & Init

        public MyInventoryAggregateBase(IMyInventoryOwner owner) 
        {
            //TODO: This should be removed in the future, this is not necessary
            m_owner = owner;                                   
        }

        // Register callbacks
        public void Init()
        {
            foreach (var inventory in Inventories)
            {
                inventory.OwnerChanged += OnOwnerChanged;
                inventory.ContentsChanged += OnContentsChanged;
            }
        }

        // Detach callbacks
        public void DetachCallbacks()
        {
            foreach (var inventory in Inventories)
            {
                inventory.OwnerChanged -= OnOwnerChanged;
                inventory.ContentsChanged -= OnContentsChanged;
            }
        }

        ~MyInventoryAggregateBase()
        {
            DetachCallbacks();
        }

        #endregion

        
        virtual public MyFixedPoint ComputeAmountThatFits(MyDefinitionId contentId)
        {
            float amount = 0;
            foreach (var inventory in OwnerInventories)
            {
                amount += (float)inventory.ComputeAmountThatFits(contentId);
            }
            return (MyFixedPoint)amount;
        }

        virtual public MyFixedPoint GetItemAmount(MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None)
        {
            float amount = 0;
            foreach (var inventory in OwnerInventories)
            {
                amount += (float)inventory.GetItemAmount(contentId, flags);
            }
            return (MyFixedPoint) amount;
        }

        virtual public bool AddItems(MyFixedPoint amount, MyObjectBuilder_Base objectBuilder, int index = -1)
        {
            var maxAmount = ComputeAmountThatFits(objectBuilder.GetId());
            var restAmount = amount;
            if (amount <= maxAmount)
            {
                foreach (var inventory in Inventories)
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

        virtual public void RemoveItemsOfType(MyFixedPoint amount, MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None, bool spawn = false)
        {
            var restAmount = amount;
            foreach (var inventory in Inventories)
            {
                var contains = inventory.GetItemAmount( contentId, flags);
                if (contains > restAmount)
                {
                    contains = restAmount;
                }
                if (contains > 0) inventory.RemoveItemsOfType(contains, contentId, flags, spawn);
                restAmount -= contains;
            }
        }

        virtual public List<ModAPI.Interfaces.IMyInventoryItem> GetItems()
        {
            List<ModAPI.Interfaces.IMyInventoryItem> items = new List<ModAPI.Interfaces.IMyInventoryItem>();
            foreach (var inventory in Inventories)
            {
                items.AddList(inventory.GetItems());
            }
            return items;
        }

        virtual public IMyComponentInventory GetInventoryTypeId(VRage.Utils.MyStringId id)
        {
            foreach (var inventory in Inventories)
            {
                if (inventory.InventoryName == id) return inventory;
            }
            return null;
        }

        /// <summary>
        /// Use this to add children aggregates - basically on character should be only two, CharacterInventoryAggregate and AreInventoryAggregate
        /// </summary>
        /// <param name="child">some child m_aggregate</param>
        virtual public void AddChild(IMyInventoryAggregate child)
        {
            Debug.Assert(child != this, "Cannot add itself!");
            m_children.Add(child);
            child.OwnerChanged += OnOwnerChanged;
            child.ContentsChanged += OnContentsChanged;
        }

        /// <summary>
        /// Use this to remove children m_aggregate, usually you want to remove only AreaInventoryAggregate from dead character
        /// </summary>
        /// <param name="child">some child m_aggregate implementation</param>
        virtual public void RemoveChild(IMyInventoryAggregate child)
        {
            child.ContentsChanged -= OnContentsChanged;
            child.OwnerChanged -= OnOwnerChanged;
            m_children.Remove(child);

        }

        virtual public void AddInventory(IMyComponentInventory inventory)
        {
            //TODO: Consider adding inventories to the children aggregates
            m_attachedInventories.Add(inventory);
        }

        virtual public void RemoveInventory(IMyComponentInventory inventory)
        {
            foreach (var child in m_children)
            {
                if (child.Inventories.Contains(inventory))
                {
                    child.RemoveInventory(inventory);
                    return;
                }
            }
            m_attachedInventories.Remove(inventory);
        }
        
    }
}
