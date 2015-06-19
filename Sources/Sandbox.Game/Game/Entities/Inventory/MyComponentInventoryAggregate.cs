using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Components;
using VRage.ObjectBuilders;
using Sandbox.Game.Entities.Inventory;
namespace Sandbox.Game.Entities.Inventory
{
    /// <summary>
    /// This class is a aggregate of all component inventories on the character. This is intended to be used for buildng and crafting.
    /// </summary>
    public class MyComponentInventoryAggregate : MyEntityComponentBase, IMyInventoryAggregate
    {
        #region Fields

        private MyEntity m_entity;

        private List<IMyInventoryAggregate> m_children = new List<IMyInventoryAggregate>();

        private List<IMyComponentInventory> m_attachedInventories = new List<IMyComponentInventory>();

        private MyInventoryAggregateBase m_aggregate;

        #endregion

        #region Events

        public event Action<IMyComponentInventory> ContentsChanged
        {
            add { m_aggregate.ContentsChanged += value; }
            remove { m_aggregate.ContentsChanged -= value; }
        }

        public event Action<IMyComponentInventory, IMyInventoryOwner> OwnerChanged
        {
            add { m_aggregate.OwnerChanged += value; }
            remove { m_aggregate.OwnerChanged -= value; }
        }

        #endregion

        #region Properties

        public IMyInventoryOwner Owner { get { return m_aggregate.Owner; } }

        public List<IMyComponentInventory> OwnerInventories { get { return m_aggregate.OwnerInventories; } }

        public List<IMyComponentInventory> Inventories { get { return m_aggregate.Inventories; } }

        public MyFixedPoint CurrentMass { get { return m_aggregate.CurrentMass; } }

        public MyFixedPoint MaxMass { get { return m_aggregate.MaxMass; } }

        public MyFixedPoint CurrentVolume { get { return m_aggregate.CurrentVolume; } }

        public MyFixedPoint MaxVolume { get { return m_aggregate.MaxVolume; } }

        public VRage.Utils.MyStringId InventoryName { get { return m_aggregate.InventoryName; } } // TODO: Consider returning another name

        #endregion

        #region Constructor & Init

        public MyComponentInventoryAggregate(MyEntity entity) 
        {
            //TODO: This should be removed in the future, this is not necessary, when we know how to get the inventory owner 
            Debug.Assert(entity is IMyInventoryOwner, "Entity must implement IMyInventoryOwner interface");
            m_aggregate = new MyInventoryAggregateBase(entity as IMyInventoryOwner);
            m_entity = entity;                        
        }

        public void Init(bool registerComponentInventories = true)
        {
            if (registerComponentInventories)
            {
                var otherInventories = GetComponentInventories();
                foreach (var inventory in otherInventories)
                {
                    AddInventory(inventory);
                }
            }
            // TODO: Any other inicialization?
            m_aggregate.Init();
        }

        #endregion

        private List<IMyComponentInventory> GetComponentInventories()
        {
            List<IMyComponentInventory> inventories = new List<IMyComponentInventory>();
            foreach (var component in m_entity.Components)
            {
                if (component is IMyComponentInventory && component != this)
                {
                    inventories.Add(component as IMyComponentInventory);
                }
            }
            return inventories;
        }


        public MyFixedPoint GetItemAmount(MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None)
        {
            return m_aggregate.GetItemAmount(contentId, flags);
        }

        public bool AddItems(MyFixedPoint amount, MyObjectBuilder_Base objectBuilder, int index = -1)
        {
            return m_aggregate.AddItems(amount, objectBuilder, index);
        }

        public void RemoveItemsOfType(MyFixedPoint amount, MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None, bool spawn = false)
        {
            m_aggregate.RemoveItemsOfType(amount, contentId, flags, spawn);
        }

        public List<ModAPI.Interfaces.IMyInventoryItem> GetItems()
        {
            return m_aggregate.GetItems();
        }

        public IMyComponentInventory GetInventoryTypeId(VRage.Utils.MyStringId id)
        {
            return m_aggregate.GetInventoryTypeId(id);
        }

        public void AddChild(IMyInventoryAggregate child)
        {
            Debug.Assert(child != this, "Cannot add itself!");
            m_aggregate.AddChild(child);
        }

        public void RemoveChild(IMyInventoryAggregate child)
        {
            m_aggregate.RemoveChild(child);
        }

        public void AddInventory(IMyComponentInventory inventory)
        {
            m_aggregate.AddInventory(inventory);
        }

        public void RemoveInventory(IMyComponentInventory inventory)
        {
            m_aggregate.RemoveInventory(inventory);
        }
        
        public MyFixedPoint ComputeAmountThatFits(MyDefinitionId contentId)
        {
            return m_aggregate.ComputeAmountThatFits(contentId);
        }
    }
}
