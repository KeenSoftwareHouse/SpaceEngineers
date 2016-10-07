using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.SessionComponents;

namespace Sandbox.Game.Components
{
    [MyComponentType(typeof(MyCraftingComponentBase))]
    public abstract class MyCraftingComponentBase : MyGameLogicComponent, IMyEventProxy
    {
        /// <summary>
        /// Normal class to hold information about blueprint being produced
        /// </summary>
        public class MyBlueprintToProduce
        {
            public MyFixedPoint Amount;
            public MyBlueprintDefinitionBase Blueprint;

            public MyBlueprintToProduce(MyFixedPoint amount, MyBlueprintDefinitionBase blueprint)
            {
                Amount = amount;
                Blueprint = blueprint;
            }
        }

        /// <summary>
        /// Use this class with blueprints, that are type of MyRepairBlueprintDefinition, intended for repairing items..
        /// </summary>
        public class MyRepairBlueprintToProduce : MyBlueprintToProduce
        {
            public uint InventoryItemId;

            public MyObjectBuilderType InventoryItemType;

            public MyStringHash InventoryItemSubtypeId;

            public MyRepairBlueprintToProduce(MyFixedPoint amount, MyBlueprintDefinitionBase blueprint, uint inventoryItemId, MyObjectBuilderType inventoryItemType, MyStringHash inventoryItemSubtypeId) : base(amount, blueprint)
            {
                System.Diagnostics.Debug.Assert(Blueprint is MyRepairBlueprintDefinition, "MyRepairBlueprintToProduce class should be used together with blueprints type of MyRepairBlueprintDefinition only!");
                InventoryItemId = inventoryItemId;
                InventoryItemType = inventoryItemType;
                InventoryItemSubtypeId = inventoryItemSubtypeId;
            }
        }

        #region Fields

        private List<MyBlueprintToProduce> m_itemsToProduce = new List<MyBlueprintToProduce>();
        protected List<MyBlueprintClassDefinition> m_blueprintClasses = new List<MyBlueprintClassDefinition>();
        public event Action<MyCraftingComponentBase, MyBlueprintDefinitionBase, MyFixedPoint> BlueprintProduced;
        public event Action<MyCraftingComponentBase, MyBlueprintDefinitionBase, MyBlueprintDefinitionBase.Item> MissingRequiredItem;
        public event Action<MyCraftingComponentBase> InventoryIsFull;
        public event Action<MyCraftingComponentBase, MyBlueprintToProduce> ProductionChanged;
        public event Action<MyCraftingComponentBase> OperatingChanged;
        public event Action LockAcquired;
        public event Action LockReleased;
        
        protected int m_currentItem = -1;
        protected float m_currentItemStatus;
        protected float m_lastItemStatus;
        protected MyFixedPoint m_currentProductionAmount = 1;
        protected int m_elapsedTimeMs = 0;
        protected float m_craftingSpeedMultiplier = 1.0f;
        
        private long m_lockedByEntityId = -1;

        #endregion

        #region Properties

        public bool IsProductionDone
        {
            get { return m_itemsToProduce.Count == 0; }
        }


        public List<MyBlueprintClassDefinition> AvailableBlueprintClasses
        {
            get { return m_blueprintClasses; }
        }

        public int BlueprintsToProduceCount
        {
            get
            {
                return m_itemsToProduce.Count;
            }
        }

        public abstract String DisplayNameText
        {
            get;
        }

        public abstract bool RequiresItemsToOperate
        {
            get;
        }

        public virtual String OperatingItemsDisplayNameText
        {
            get
            {
                return String.Empty;
            }
        }

        public abstract bool CanOperate
        {
            get;
        }

        public virtual float OperatingItemsLevel
        {
            get { return 1.0f; }
        }

        public virtual bool AcceptsOperatingItems
        {
            get { return false; }
        }

        public virtual float AvailableOperatingSpace
        {
            get { return 0; }
        }

        public bool IsProducing
        {
            get { return m_itemsToProduce.IsValidIndex(m_currentItem); }
        }

        public float CurrentItemStatus
        {
            get { return m_currentItemStatus; }
        }

        public bool IsLocked
        {
            get 
            {
                MyEntity entity;
                return MyEntities.TryGetEntityById(m_lockedByEntityId, out entity); 
            }
        }

        public long LockedByEntityId
        {
            get
            {
                return m_lockedByEntityId;
            }
        }

        public List<MyBlueprintToProduce> ItemsInProduction 
        { 
            get
            {
                return m_itemsToProduce;
            }
        }

        #endregion

        #region Virtuals and Abstracts

        protected abstract void UpdateProduction_Implementation();     

        public virtual void GetInsertedOperatingItems(List<MyPhysicalInventoryItem> itemsList) { }

        protected virtual void InsertOperatingItem_Implementation(MyPhysicalInventoryItem item) { }

        protected virtual void RemoveOperatingItem_Implementation(MyPhysicalInventoryItem item, MyFixedPoint amount) { }

        public virtual bool IsOperatingItem(MyPhysicalInventoryItem item) { return false; }

        public virtual bool ContainsOperatingItem(MyPhysicalInventoryItem item) { return false; }

        public virtual MyFixedPoint GetOperatingItemRemovableAmount(MyPhysicalInventoryItem item) { return 0; }

        protected virtual void StopOperating_Implementation() { }

        protected virtual void UpdateOperatingLevel() { }

        protected virtual void StartProduction_Implementation() { }

        protected virtual void StopProduction_Implementation() { }

        #endregion

        #region Methods

        protected void RaiseEvent_MissingRequiredItem(MyBlueprintDefinitionBase blueprint, MyBlueprintDefinitionBase.Item missingItem)
        {
            MyMultiplayer.RaiseEvent(this, x => x.MissingRequiredItem_Implementation, (SerializableDefinitionId)blueprint.Id, (SerializableDefinitionId)missingItem.Id);
        }

        [Event, Reliable, Server, Broadcast]
        private void MissingRequiredItem_Implementation(SerializableDefinitionId blueprintId, SerializableDefinitionId missingItemId)
        {
            MyBlueprintDefinitionBase.Item missingItem = default(MyBlueprintDefinitionBase.Item);
            MyDefinitionId definitionId = missingItemId;
            bool found = false;

            MyBlueprintDefinitionBase blueprint = MyDefinitionManager.Static.GetBlueprintDefinition(blueprintId);

            if (blueprint == null)
            {
                System.Diagnostics.Debug.Fail("Couldn't find blueprint definition for: " + blueprintId);
                return;
            }

            foreach (var requiredItem in blueprint.Prerequisites)
            {
                if (requiredItem.Id == definitionId)
                {
                    missingItem = requiredItem;
                    found = true;
                }
            }

            if (!found)
            {
                System.Diagnostics.Debug.Fail("Item " + definitionId + " wasn't found in blueprint " + blueprint);
            }

            var handler = MissingRequiredItem;

            if (handler != null && found)
            {
                handler(this, blueprint, missingItem);
            }
        }

        protected void RaiseEvent_InventoryIsFull()
        {
            MyMultiplayer.RaiseEvent(this, x => x.InventoryIsFull_Implementation);
        }

        [Event, Reliable, Server, Broadcast]
        private void InventoryIsFull_Implementation()
        {
            var handler = InventoryIsFull;

            if (handler != null)
            {
                handler(this);
            }
        }

        public MyBlueprintToProduce GetItemToProduce(int index)
        {
            int ind = index;
            if (!m_itemsToProduce.IsValidIndex(ind))
            {
                System.Diagnostics.Debug.Fail("Invalid index!");
                return null;
            }
            return m_itemsToProduce[index];
        }

        protected void OnBlueprintProduced(MyBlueprintDefinitionBase blueprint, MyFixedPoint amount)
        {
            SerializableDefinitionId blueprintId = blueprint.Id;
            MyMultiplayer.RaiseEvent(this, x => x.OnBlueprintProduced_Implementation, blueprintId, amount);   
        }

        [Event, Reliable, Server, Broadcast]
        private void OnBlueprintProduced_Implementation(SerializableDefinitionId blueprintId, MyFixedPoint amount)
        {
            MyBlueprintDefinitionBase blueprint = MyDefinitionManager.Static.GetBlueprintDefinition(blueprintId);

            if (blueprint == null)
            {
                System.Diagnostics.Debug.Fail("Couldn't find blueprint definition for: " + blueprintId);
                return;
            }

            var handler = BlueprintProduced;

            if (handler != null)
            {
                handler(this, blueprint, amount);
            }
        }

        public void StartProduction(long senderEntityId)
        {   
            MyMultiplayer.RaiseEvent(this, x => x.StartProduction_Request, senderEntityId);
        }

        protected void StartProduction()
        {
            MyMultiplayer.RaiseEvent(this, x => x.StartProduction_Request, m_lockedByEntityId);
        }

        [Event, Reliable, Server]
        private void StartProduction_Request(long senderEntityId)
        {
            if (IsLocked && senderEntityId != m_lockedByEntityId)
                return;

            MyMultiplayer.RaiseEvent(this, x => x.StartProduction_Event);
        }

        [Event, Reliable, Server, Broadcast]
        private void StartProduction_Event()
        {
            StartProduction_Implementation();
        }

        public void StopProduction(long senderEntityId)
        {
            MyMultiplayer.RaiseEvent(this, x => x.StopProduction_Request, senderEntityId);
        }

        protected void StopProduction()
        {
            MyMultiplayer.RaiseEvent(this, x => x.StopProduction_Request, m_lockedByEntityId);
        }

        [Event, Reliable, Server]
        private void StopProduction_Request(long senderEntityId)
        {
            if (IsLocked && senderEntityId != m_lockedByEntityId)
                return;

            MyMultiplayer.RaiseEvent(this, x => x.StopProduction_Event);
        }

        [Event, Reliable, Server, Broadcast]
        private void StopProduction_Event()
        {
            StopProduction_Implementation();
        }

        public void ClearItemsToProduce(long senderEntityId)
        {            
            MyMultiplayer.RaiseEvent(this, x => x.ClearItemsToProduce_Request, senderEntityId);
        }

        protected void ClearItemsToProduce()
        {
            MyMultiplayer.RaiseEvent(this, x => x.ClearItemsToProduce_Request, m_lockedByEntityId);
        }

        [Event, Reliable, Server]
        private void ClearItemsToProduce_Request(long senderEntityId)
        {
            if (IsLocked && senderEntityId != m_lockedByEntityId)
                return;

            MyMultiplayer.RaiseEvent(this, x => x.ClearItemsToProduce_Event);
        }

        [Event, Reliable, Server, Broadcast]
        private void ClearItemsToProduce_Event()
        {
            foreach (var item in m_itemsToProduce)
            {
                item.Amount = 0;

                var handler = ProductionChanged;
                if (handler != null)
                {
                    handler(this, item);
                }
            }

            m_itemsToProduce.Clear();
        }

        public bool CanUseBlueprint(MyBlueprintDefinitionBase blueprint)
        {
            System.Diagnostics.Debug.Assert(blueprint != null, "Passing blueprint as null argument!");

            foreach (var blueprintClass in m_blueprintClasses)
                if (blueprintClass.ContainsBlueprint(blueprint)) return true;

            return false;
        }

        public void AddItemToProduce(MyFixedPoint amount, MyBlueprintDefinitionBase blueprint, long senderEntityId)
        {           
            SerializableDefinitionId blueprintId = blueprint.Id;
            MyMultiplayer.RaiseEvent(this, x => x.AddItemToProduce_Request, amount, blueprintId, senderEntityId);
        }

        [Event, Reliable, Server]
        private void AddItemToProduce_Request(MyFixedPoint amount, SerializableDefinitionId blueprintId, long senderEntityId)
        {
            if (IsLocked && senderEntityId != m_lockedByEntityId)
                return;
            
            MyMultiplayer.RaiseEvent(this, x => x.AddItemToProduce_Implementation, amount, blueprintId);
        }

        [Event, Reliable, Server, Broadcast]
        private void AddItemToProduce_Implementation(MyFixedPoint amount, SerializableDefinitionId blueprintId)
        {
            System.Diagnostics.Debug.Assert(amount > 0, "Adding zero or negative amount!");

            MyBlueprintDefinitionBase blueprint = MyDefinitionManager.Static.GetBlueprintDefinition(blueprintId);

            if (blueprint == null)
            {
                System.Diagnostics.Debug.Fail("Couldn't find blueprint definition for: " + blueprintId);
                return;
            }
            
            MyBlueprintToProduce itemToProduce = m_itemsToProduce.Find(x => x.Blueprint == blueprint);
            if (itemToProduce != null)
            {
                itemToProduce.Amount = itemToProduce.Amount + amount;
            }
            else
            {
                itemToProduce = new MyBlueprintToProduce(amount, blueprint);
                m_itemsToProduce.Add(itemToProduce);
            }

            var handler = ProductionChanged;
            if (handler != null)
            {
                handler(this, itemToProduce);
            }
        }

        public void AddItemToRepair(MyFixedPoint amount, MyBlueprintDefinitionBase blueprint, long senderEntityId, uint inventoryItemId, MyObjectBuilderType inventoryItemType, MyStringHash inventoryItemSubtypeId)
        {
            SerializableDefinitionId blueprintId = blueprint.Id;
            MyMultiplayer.RaiseEvent(this, x => x.AddItemToRepair_Request, amount, blueprintId, senderEntityId, inventoryItemId, inventoryItemType, inventoryItemSubtypeId);
        }

        [Event, Reliable, Server]
        private void AddItemToRepair_Request(MyFixedPoint amount, SerializableDefinitionId blueprintId, long senderEntityId, uint inventoryItemId, MyObjectBuilderType inventoryItemType, MyStringHash inventoryItemSubtypeId)
        {
            if (IsLocked && senderEntityId != m_lockedByEntityId)
                return;

            MyMultiplayer.RaiseEvent(this, x => x.AddItemToRepair_Implementation, amount, blueprintId, inventoryItemId, inventoryItemType, inventoryItemSubtypeId);
        }

        [Event, Reliable, Server, Broadcast]
        private void AddItemToRepair_Implementation(MyFixedPoint amount, SerializableDefinitionId blueprintId, uint inventoryItemId, MyObjectBuilderType inventoryItemType, MyStringHash inventoryItemSubtypeId)
        {
            System.Diagnostics.Debug.Assert(amount > 0, "Adding zero or negative amount!");

            MyBlueprintDefinitionBase blueprint = MyDefinitionManager.Static.GetBlueprintDefinition(blueprintId);

            if (blueprint == null)
            {
                System.Diagnostics.Debug.Fail("Couldn't find blueprint definition for: " + blueprintId);
                return;
            }

            Predicate<MyBlueprintToProduce> condition = x => (x is MyRepairBlueprintToProduce) && 
                                                             (x as MyRepairBlueprintToProduce).Blueprint == blueprint &&
                                                             (x as MyRepairBlueprintToProduce).InventoryItemId == inventoryItemId &&
                                                             (x as MyRepairBlueprintToProduce).InventoryItemType == inventoryItemType &&
                                                             (x as MyRepairBlueprintToProduce).InventoryItemSubtypeId == inventoryItemSubtypeId;
            MyRepairBlueprintToProduce itemToProduce = m_itemsToProduce.Find(condition) as MyRepairBlueprintToProduce;
            if (itemToProduce != null)
            {
                itemToProduce.Amount = itemToProduce.Amount + amount;
            }
            else
            {
                itemToProduce = new MyRepairBlueprintToProduce(amount, blueprint, inventoryItemId, inventoryItemType, inventoryItemSubtypeId);
                m_itemsToProduce.Add(itemToProduce);                
            }

            var handler = ProductionChanged;
            if (handler != null)
            {
                handler(this, itemToProduce);
            }
        }

        public void RemoveItemToProduce(MyFixedPoint amount, MyBlueprintDefinitionBase blueprint, long senderEntityId, int itemId = -1)
        {        
            SerializableDefinitionId blueprintId = blueprint.Id;
            MyMultiplayer.RaiseEvent(this, x => x.RemoveItemToProduce_Request, amount, blueprintId, senderEntityId, itemId);
        }

        public void RemoveItemToProduce(MyFixedPoint amount, MyBlueprintToProduce blueprintInProduction, long senderEntityId)
        {
            SerializableDefinitionId blueprintId = blueprintInProduction.Blueprint.Id;
            
            int itemId = m_itemsToProduce.IndexOf(blueprintInProduction);

            if (m_itemsToProduce.IsValidIndex(itemId))
            {
                MyMultiplayer.RaiseEvent(this, x => x.RemoveItemToProduce_Request, amount, blueprintId, senderEntityId, itemId);
            }
        }


        [Event, Reliable, Server]
        private void RemoveItemToProduce_Request(MyFixedPoint amount, SerializableDefinitionId blueprintId, long senderEntityId, int itemId = -1)
        {
            if (IsLocked && senderEntityId != m_lockedByEntityId)
                return;            
            MyMultiplayer.RaiseEvent(this, x => x.RemoveItemToProduce_Implementation, amount, blueprintId, itemId);
        }


        [Event, Reliable, Server, Broadcast]
        private void RemoveItemToProduce_Implementation(MyFixedPoint amount, SerializableDefinitionId blueprintId, int itemId = -1)
        {
            System.Diagnostics.Debug.Assert(amount > 0, "Removing zero or negative amount!");

            MyBlueprintDefinitionBase blueprint = MyDefinitionManager.Static.GetBlueprintDefinition(blueprintId);

            if (blueprint == null)
            {
                System.Diagnostics.Debug.Fail("Couldn't find blueprint definition for: " + blueprintId);
                return;
            }

            MyBlueprintToProduce removingItem;
            MyBlueprintToProduce currentItem = GetCurrentItemInProduction();

            if (m_itemsToProduce.IsValidIndex(itemId))
            {
                removingItem = m_itemsToProduce[itemId];
                System.Diagnostics.Debug.Assert(removingItem.Blueprint == blueprint, "The item was retrieved with index, but the passed blueprint don't match items blueprint!");
            }
            else
            {
                removingItem = m_itemsToProduce.Find(x => x.Blueprint == blueprint);
            }

            if (removingItem != null)
            {                
                System.Diagnostics.Debug.Assert(removingItem.Amount - amount >= 0, "Trying to remove more amount, than was set to be produced!");
                
                removingItem.Amount = removingItem.Amount - amount;

                if (removingItem.Amount <= 0)
                {                    
                    m_itemsToProduce.Remove(removingItem);
                }

                if (currentItem == removingItem && (m_currentItemStatus >= 1.0f || removingItem.Amount == 0))
                {
                    SelectItemToProduction();
                }

                var handler = ProductionChanged;
                if (handler != null)
                {
                    handler(this, removingItem);
                }
            }
            else
            {
                // On MP it can easily happen that we are removing an item that was already produced on the server, so don't assert in that case
                System.Diagnostics.Debug.Assert(Sync.Clients.Count != 0, "Trying to remove item from production, but item wasn't found!");
            }            
        }

        public MyFixedPoint MaxProducableAmount(MyBlueprintDefinitionBase blueprintDefinition, bool raiseMissingRequiredItemEvent = false)
        {
            if (MySession.Static.CreativeMode)
                return MyFixedPoint.MaxValue;

            var inventory = (Entity as MyEntity).GetInventory();

            if (inventory == null)
            {
                System.Diagnostics.Debug.Fail("Inventory was not found on the entity!");
                return 0;
            }

            MyFixedPoint maxProducableAmount = MyFixedPoint.MaxValue;

            foreach (var requiredItem in blueprintDefinition.Prerequisites)
            {
                var itemAmount = inventory.GetItemAmount( requiredItem.Id, substitute: true);
                var producableAmount = MyFixedPoint.Floor((MyFixedPoint)((float)itemAmount / (float)requiredItem.Amount));
                maxProducableAmount = MyFixedPoint.Min(maxProducableAmount, producableAmount);
                if (maxProducableAmount == 0)
                {
                    if (raiseMissingRequiredItemEvent)
                    {
                        RaiseEvent_MissingRequiredItem(blueprintDefinition, requiredItem);
                    }
                    return maxProducableAmount;
                }
            }

            return maxProducableAmount;           
        }

        protected void RemovePrereqItemsFromInventory(MyBlueprintDefinitionBase definition, MyFixedPoint amountMult)
        {
            System.Diagnostics.Debug.Assert(Sync.IsServer, "This method should be called only on server!");

            if (!Sync.IsServer)
                return;

            if (MySession.Static.CreativeMode)
                return;

            var inventory = (Entity as MyEntity).GetInventory();

            if (inventory == null)
            {
                System.Diagnostics.Debug.Fail("Inventory was not found on the entity!");
                return;
            }

            foreach (var reqItem in definition.Prerequisites)
            {
                MyFixedPoint amountToRemove = reqItem.Amount * amountMult;
                MyDefinitionId itemId = reqItem.Id;
                MyFixedPoint removed = 0;

                if (MySessionComponentEquivalency.Static != null && MySessionComponentEquivalency.Static.HasEquivalents(itemId))
                {
                    MyFixedPoint amountRemaining = amountToRemove;
                    var eqGroup = MySessionComponentEquivalency.Static.GetEquivalents(itemId);
                    foreach (var element in eqGroup)
                    {
                        MyFixedPoint removedThisItem = inventory.RemoveItemsOfType(amountRemaining, element);
                        amountRemaining -= removedThisItem;
                        removed += removedThisItem;
                        if (amountRemaining == 0) break;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Assert(amountToRemove <= inventory.GetItemAmount(reqItem.Id, substitute: true), "Trying to remove higher amount than is present in inventory!");
                    removed += inventory.RemoveItemsOfType(amountToRemove, itemId);
                }
                System.Diagnostics.Debug.Assert(removed == amountToRemove, "Removed different amount, than expected!");
            }
        }

        protected virtual void AddProducedItemToInventory(MyBlueprintDefinitionBase definition, MyFixedPoint amountMult)
        {
            System.Diagnostics.Debug.Assert(Sync.IsServer, "This method should be called only on server!");

            if (!Sync.IsServer)
                return;

            var inventory = (Entity as MyEntity).GetInventory();

            if (inventory == null)
            {
                System.Diagnostics.Debug.Fail("Inventory was not found on the entity!");
                return;
            }

            foreach (var prodItem in definition.Results)
            {
                var amountToAdd = prodItem.Amount * amountMult;
                IMyInventoryItem inventoryItem;

                if (definition is MyBlockBlueprintDefinition)
                    inventoryItem = CreateInventoryBlockItem(prodItem.Id, amountToAdd);
                else
                    inventoryItem = CreateInventoryItem(prodItem.Id, amountToAdd);

                var resultAdded = inventory.Add(inventoryItem, inventoryItem.Amount);

                System.Diagnostics.Debug.Assert(resultAdded, "Result of adding is false!");
            }
        }

        protected IMyInventoryItem CreateInventoryItem(MyDefinitionId itemDefinition, MyFixedPoint amount)
        {
            var content = MyObjectBuilderSerializer.CreateNewObject(itemDefinition) as MyObjectBuilder_PhysicalObject;
            
            System.Diagnostics.Debug.Assert(content != null, "Can not create the requested type from definition!");

            MyPhysicalInventoryItem inventoryItem = new MyPhysicalInventoryItem(amount, content);            

            return inventoryItem;
        }

        protected IMyInventoryItem CreateInventoryBlockItem(MyDefinitionId blockDefinition, MyFixedPoint amount)
        {
            var content = new MyObjectBuilder_BlockItem() { BlockDefId = blockDefinition };
            MyPhysicalInventoryItem inventoryItem = new MyPhysicalInventoryItem(amount, content);

            return inventoryItem;
        }

        protected MyFixedPoint MaxAmountToFitInventory(MyBlueprintDefinitionBase definition)
        {
            var inventory = (Entity as MyEntity).GetInventory();

            if (inventory == null)
            {
                System.Diagnostics.Debug.Fail("Inventory was not found on the entity!");
                return 0;
            }

            var maxAmount = MyFixedPoint.MaxValue;

            float volumeToBeRemoved = 0;
            float massToBeRemoved = 0;

            if (!MySession.Static.CreativeMode)
            {
                foreach (var item in definition.Prerequisites)
                {
                    float itemMass, itemVolume;
                    MyInventory.GetItemVolumeAndMass(item.Id, out itemMass, out itemVolume);
                    volumeToBeRemoved += itemVolume;
                    massToBeRemoved += itemMass;
                }
            }

            foreach (var item in definition.Results)
            {
                var fittableAmount = inventory.ComputeAmountThatFits(item.Id, volumeToBeRemoved, massToBeRemoved);               

                maxAmount = MyFixedPoint.Min(fittableAmount, maxAmount);
            }

            return maxAmount;
        }
        
        public MyBlueprintToProduce TryGetItemToProduce(MyBlueprintDefinitionBase blueprint)
        {
            return m_itemsToProduce.Find(x => x.Blueprint == blueprint);
        }

        public MyRepairBlueprintToProduce TryGetItemToRepair(uint inventoryItemId, MyObjectBuilderType inventoryItemType, MyStringHash inventoryItemSubtypeId)
        {
            return m_itemsToProduce.Find(x =>
                (x is MyRepairBlueprintToProduce) &&
                (x as MyRepairBlueprintToProduce).InventoryItemId == inventoryItemId &&
                (x as MyRepairBlueprintToProduce).InventoryItemType == inventoryItemType &&
                (x as MyRepairBlueprintToProduce).InventoryItemSubtypeId == inventoryItemSubtypeId) as MyRepairBlueprintToProduce;
        }

        public void InsertOperatingItem(MyPhysicalInventoryItem item, long senderEntityId)
        {
            MyMultiplayer.RaiseEvent(this, x => x.InsertOperatingItem_Request, item.GetObjectBuilder(), senderEntityId);
        }

        [Event, Reliable, Server]
        private void InsertOperatingItem_Request([DynamicObjectBuilder] MyObjectBuilder_InventoryItem itemBuilder, long senderEntityId)
        {
            if (IsLocked && senderEntityId != m_lockedByEntityId)
                return;
            MyMultiplayer.RaiseEvent(this, x => x.InsertOperatingItem_Event, itemBuilder);
        }

        [Event, Reliable, Server, Broadcast]
        private void InsertOperatingItem_Event([DynamicObjectBuilder] MyObjectBuilder_InventoryItem itemBuilder)
        {           
            var item = new MyPhysicalInventoryItem(itemBuilder);
            InsertOperatingItem_Implementation(item);
        }

        public void RemoveOperatingItem(MyPhysicalInventoryItem item, MyFixedPoint amount, long senderEntityId)
        {           
            MyMultiplayer.RaiseEvent(this, x => x.RemoveOperatingItem_Request, item.GetObjectBuilder(), amount, senderEntityId);
        }

        protected void RemoveOperatingItem(MyPhysicalInventoryItem item, MyFixedPoint amount)
        {
            MyMultiplayer.RaiseEvent(this, x => x.RemoveOperatingItem_Request, item.GetObjectBuilder(), amount, m_lockedByEntityId);
        }

        [Event, Reliable, Server]
        private void RemoveOperatingItem_Request([DynamicObjectBuilder] MyObjectBuilder_InventoryItem itemBuilder, MyFixedPoint amount, long senderEntityId)
        {
            if (IsLocked && senderEntityId != m_lockedByEntityId)
                return;
            MyMultiplayer.RaiseEvent(this, x => x.RemoveOperatingItem_Event, itemBuilder, amount);
        }

        [Event, Reliable, Server, Broadcast]
        private void RemoveOperatingItem_Event([DynamicObjectBuilder] MyObjectBuilder_InventoryItem itemBuilder, MyFixedPoint amount)
        {  
            var item = new MyPhysicalInventoryItem(itemBuilder);
            RemoveOperatingItem_Implementation(item, amount);
        }

        protected void RaiseEvent_OperatingChanged() 
        {
            var handler = OperatingChanged;

            if (handler != null)
            {
                handler(this);
            }
        }

        protected void StopOperating()
        {
            MyMultiplayer.RaiseEvent(this, x => x.StopOperating_Event);
        }

        [Event, Reliable, Server, Broadcast]
        private void StopOperating_Event()
        {
            StopOperating_Implementation();
        }

        protected void RaiseEvent_ProductionChanged()
        {   
            if (m_itemsToProduce.IsValidIndex(m_currentItem))
            {
                if (ProductionChanged != null)
                {
                    ProductionChanged(this, m_itemsToProduce[m_currentItem]);
                }                
            }
            else
            {
                if (ProductionChanged != null)
                {
                    ProductionChanged(this,null);
                } 
            }
        }

        protected void SelectItemToProduction()
        {
            if (!Sync.IsServer)
                return;

            if (BlueprintsToProduceCount == 0 || !CanOperate)
            {
                RaiseEvent_NewItemSelected(-1);
                return;
            }

            if (CanItemBeProduced(m_currentItem))
            {
                RaiseEvent_NewItemSelected(m_currentItem);
                return;
            }
            
            float totalFittableAmount = 0;
            
            for (int i = 0; i < BlueprintsToProduceCount; ++i)
            {
                var item = GetItemToProduce(i);
                var blueprint = item.Blueprint;
                var fittableAmount = MaxAmountToFitInventory(blueprint);
                totalFittableAmount += (float)fittableAmount;
                if (CanItemBeProduced(i))
                {
                    RaiseEvent_NewItemSelected(i);
                    return;
                }
            }

            if (totalFittableAmount == 0 && BlueprintsToProduceCount > 0)
            {
                RaiseEvent_InventoryIsFull();
            }

            RaiseEvent_NewItemSelected(-1);
        }

        private void RaiseEvent_NewItemSelected(int index)
        {
            m_currentItem = index;
            m_currentItemStatus = 0.0f;

            if (Sync.IsServer)
            {
                MyMultiplayer.RaiseEvent(this, x => x.NewItemSelected_Event, index);
            }

            RaiseEvent_ProductionChanged();
        }

        [Event, Reliable, Broadcast]
        private void NewItemSelected_Event(int index)
        {
            if (m_itemsToProduce.IsValidIndex(index))
            {
                m_currentItem = index;                
            }
            else
            {
                System.Diagnostics.Debug.Assert(index == -1, "Server sent message, that some item was selected to production, but its index is invalid on client");
                m_currentItem = -1;                
            }
            m_currentItemStatus = 0.0f;
            
            RaiseEvent_ProductionChanged();
        }

        private bool CanItemBeProduced(int i)
        {
            if (!m_itemsToProduce.IsValidIndex(i))
                return false;

            if (!CanOperate)
                return false;

            var itemToProduce = GetItemToProduce(i);
            if (itemToProduce != null)
            {
                var blueprint = itemToProduce.Blueprint;
                var requestedAmount = itemToProduce.Amount;
                if (CanUseBlueprint(blueprint))
                {
                    var producableAmount = MaxProducableAmount(blueprint, true);
                    var fittableAmount = MaxAmountToFitInventory(blueprint);                    
                    var amountToProduce = MyFixedPoint.Min(producableAmount, fittableAmount);
                    amountToProduce = MyFixedPoint.Min(requestedAmount, amountToProduce);

                    if (amountToProduce > 0)
                    {
                        return true;
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.Fail("Returned null instance!");
            }
            return false;
        }

        protected void UpdateCurrentItem()
        {
            System.Diagnostics.Debug.Assert(m_itemsToProduce.IsValidIndex(m_currentItem), "Current item index to craft is invalid");

            var itemInProduction = GetItemToProduce(m_currentItem);

            if (itemInProduction == null)
            {
                return;
            }

            if (!CanItemBeProduced(m_currentItem))
            {
                SelectItemToProduction();
            }

            if (itemInProduction is MyRepairBlueprintToProduce)
            {
                var repairItem = itemInProduction as MyRepairBlueprintToProduce;

                if (!IsItemInInventory(repairItem.InventoryItemId, repairItem.InventoryItemType, repairItem.InventoryItemSubtypeId))
                {                    
                    if (Sync.IsServer)
                    {
                        RemoveItemToProduce(m_currentProductionAmount, repairItem.Blueprint, m_lockedByEntityId, m_currentItem);
                    }                    
                }
            }

            var blueprint = itemInProduction.Blueprint;

            UpdateCurrentItemStatus(0.0f);

            bool statusChanged = Math.Abs(m_lastItemStatus - m_currentItemStatus) > 0.01f;

            m_lastItemStatus = m_currentItemStatus;

            if (statusChanged)
            {
                RaiseEvent_ProductionChanged();
            }


            if (m_currentItemStatus >= 1.0f)
            {
                var repairItem = itemInProduction as MyRepairBlueprintToProduce;
                var repairDefinition = blueprint as MyRepairBlueprintDefinition;
                System.Diagnostics.Debug.Assert(!(repairItem == null ^ repairDefinition == null), "Produced blueprint is type of MyRepairBlueprintDefinition, but it was added to production as normal item, this shouldn't happend!");

                if (repairItem != null && repairDefinition != null)
                {
                    RepairInventoryItem(repairItem.InventoryItemId, repairItem.InventoryItemType, repairItem.InventoryItemSubtypeId, repairDefinition.RepairAmount);

                    if (Sync.IsServer)
                    {
                        RemovePrereqItemsFromInventory(blueprint, m_currentProductionAmount);
                        RemoveItemToProduce(m_currentProductionAmount, blueprint, m_lockedByEntityId, m_currentItem);
                        OnBlueprintProduced(blueprint, m_currentProductionAmount);
                    }
                }
                else if (Sync.IsServer)
                {
                    RemovePrereqItemsFromInventory(blueprint, m_currentProductionAmount);
                    AddProducedItemToInventory(blueprint, m_currentProductionAmount);
                    RemoveItemToProduce(m_currentProductionAmount, blueprint, m_lockedByEntityId);
                    OnBlueprintProduced(blueprint, m_currentProductionAmount);
                }
            }
        }

        public virtual void UpdateCurrentItemStatus(float statusDelta)
        {
            if (!IsProducing)
                return;

            var itemInProduction = GetItemToProduce(m_currentItem);
            if (itemInProduction == null)
            {
                return;
            }
            var blueprint = itemInProduction.Blueprint;

            m_currentItemStatus = Math.Min(1.0f, m_currentItemStatus + (m_elapsedTimeMs * m_craftingSpeedMultiplier) / (blueprint.BaseProductionTimeInSeconds * 1000f));
        }

        private bool IsItemInInventory(uint itemId, MyObjectBuilderType objectBuilderType, MyStringHash subtypeId)
        {
            var inventory = (Entity as MyEntity).GetInventory();

            if (inventory == null)
            {
                System.Diagnostics.Debug.Fail("Inventory was not found on the entity while trying to repair item in it!");
                return false;
            }

            var item = inventory.GetItemByID(itemId);

            System.Diagnostics.Debug.Assert(item != null, "Item being repaired wasn't found by its Id in inventory");

            if (item.HasValue && item.Value.Content != null)
            {
                return item.Value.Content.TypeId == objectBuilderType && item.Value.Content.SubtypeId == subtypeId;
            }

            return false;
        }

        private void RepairInventoryItem(uint itemId, MyObjectBuilderType objectBuilderType, MyStringHash subtypeId, float amount)
        {
            var inventory = (Entity as MyEntity).GetInventory();

            if (inventory == null)
            {
                System.Diagnostics.Debug.Fail("Inventory was not found on the entity while trying to repair item in it!");
                return;
            }

            var item = inventory.GetItemByID(itemId);
            
            System.Diagnostics.Debug.Assert(item != null, "Item being repaired wasn't found by its Id in inventory");

            if (item.HasValue && item.Value.Content != null && item.Value.Content.TypeId == objectBuilderType && item.Value.Content.SubtypeId == subtypeId)
            {
                float itemHP = item.Value.Content != null && item.Value.Content.DurabilityHP.HasValue ? item.Value.Content.DurabilityHP.Value : 0;

                itemHP += amount;
                itemHP = MathHelper.Clamp(itemHP, 0f, 100f);
                inventory.UpdateItem(item.GetDefinitionId(), itemId, null, itemHP);
            }
        }

        public MyBlueprintToProduce GetCurrentItemInProduction()
        {
            if (m_itemsToProduce.IsValidIndex(m_currentItem))
            {
                return m_itemsToProduce[m_currentItem];
            }
            return null;
        }

        public void AcquireLockRequest(long entityId)
        {
            MyMultiplayer.RaiseEvent(this, x => x.AcquireLock_Event, entityId);
        }

        public void ReleaseLockRequest(long entityId)
        {
            MyMultiplayer.RaiseEvent(this, x => x.ReleaseLock_Event, entityId);
        }

        [Event, Reliable, Server]
        private void ReleaseLock_Event(long entityId)
        {
            if (m_lockedByEntityId == entityId)
            {
                MyMultiplayer.RaiseEvent(this, x => x.ReleaseLock_Implementation, entityId);            
            }            
        }

        [Event, Reliable, Server, Broadcast]
        private void ReleaseLock_Implementation(long entityId)
        {
            MyEntity entity = null;
            if (MyEntities.TryGetEntityById(entityId, out entity))
            {
                entity.OnClose -= lockEntity_OnClose;
            }
            m_lockedByEntityId = -1;
            if (LockReleased != null)
            {
                LockReleased();
            }
        }

        [Event, Reliable, Server]
        private void AcquireLock_Event(long entityId)
        {
            MyEntity entity = null;
            if (!IsLocked && MyEntities.TryGetEntityById(entityId, out entity))
            {
                MyMultiplayer.RaiseEvent(this, x => x.AcquireLock_Implementation, entityId);             
            }            
        }

        [Event, Reliable, Server, Broadcast]
        private void AcquireLock_Implementation(long entityId)
        {
            MyEntity entity = null;
            if (MyEntities.TryGetEntityById(entityId, out entity))
            {                
                entity.OnClose += lockEntity_OnClose;
            }
            m_lockedByEntityId = entityId;
            if (LockAcquired != null)
            {
                LockAcquired();
            }
        }

        void lockEntity_OnClose(MyEntity obj)
        {
            obj.OnClose -= lockEntity_OnClose;
            m_lockedByEntityId = -1;
            if (LockReleased != null)
            {
                LockReleased();
            }
        }

        public override void Close()
        {
            base.Close();
            MyEntity entity = null;
            if (MyEntities.TryGetEntityById(m_lockedByEntityId, out entity))
            {
                entity.OnClose -= lockEntity_OnClose;
            }
            m_lockedByEntityId = -1;
        }

        public override MyObjectBuilder_ComponentBase Serialize(bool copy = false)
        {
            var builder = base.Serialize();
            var craftBuilder = builder as MyObjectBuilder_CraftingComponentBase;
            craftBuilder.LockedByEntityId = m_lockedByEntityId;
            return craftBuilder;
        }

        public override void Deserialize(VRage.Game.ObjectBuilders.ComponentSystem.MyObjectBuilder_ComponentBase builder)
        {
            base.Deserialize(builder);
            var craftBuilder = builder as MyObjectBuilder_CraftingComponentBase;
            m_lockedByEntityId = craftBuilder.LockedByEntityId;        
        }

        #endregion
    }
}
