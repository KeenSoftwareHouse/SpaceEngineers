using VRage.Game.ObjectBuilders.ComponentSystem;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRage.Utils;
using Sandbox.Game.Multiplayer;
using Sandbox.Engine.Multiplayer;
using VRage.Network;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Character;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Ingame;
using Sandbox.Game.SessionComponents;

namespace Sandbox.Game.Entities.Inventory
{
    /// <summary>
    /// This class implements basic functionality for the interface IMyInventoryAggregate. Use it as base class only if the basic functionality is enough.
    /// </summary>
    [MyComponentBuilder(typeof(MyObjectBuilder_InventoryAggregate))]
    [StaticEventOwner]
    public class MyInventoryAggregate : MyInventoryBase, IMyComponentAggregate, IMyEventProxy
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

        public override int MaxItemCount
        {
            get
            {
                int maxItemCount = 0;
                foreach (MyInventoryBase inventory in m_children.Reader)
                {
                    long tmpSum = (long)maxItemCount + (long)inventory.MaxItemCount;
                    if (tmpSum > (long)int.MaxValue)
                    {
                        maxItemCount = int.MaxValue;
                    }
                    else
                    {
                        maxItemCount = (int)tmpSum;
                    }
                }
                return maxItemCount;
            }
        }

        private float? m_forcedPriority;
        public override float? ForcedPriority
        {
            get { return m_forcedPriority; }
            set
            {
                m_forcedPriority = value;
                foreach (var child in m_children.Reader)
                {
                    var inv = child as MyInventoryBase;
                    inv.ForcedPriority = value;
                }
            }
        }

        private int m_inventoryCount;
        public event Action<MyInventoryAggregate, int> OnInventoryCountChanged;

        /// <summary>
        /// Returns number of inventories of MyInventory type contained in this aggregate
        /// </summary>
        public int InventoryCount
        {
            get
            {
                return m_inventoryCount;
            }
            private set
            {
                if (m_inventoryCount != value)
                {
                    int change = value - m_inventoryCount;
                    m_inventoryCount = value;
                    var handler = OnInventoryCountChanged;
                    if (handler != null)
                    {
                        OnInventoryCountChanged(this, change);
                    }
                }
            }
        }

        #endregion

        #region De/Constructor & Init

        public MyInventoryAggregate() : base("Inventory") { }

        public MyInventoryAggregate(string inventoryId) : base(inventoryId) { }

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

        public override MyFixedPoint ComputeAmountThatFits(MyDefinitionId contentId, float volumeRemoved = 0, float massRemoved = 0)
        {
            float amount = 0;
            foreach (MyInventoryBase inventory in m_children.Reader)
            {
                amount += (float)inventory.ComputeAmountThatFits(contentId, volumeRemoved, massRemoved);
            }
            return (MyFixedPoint)amount;
        }

        public override MyFixedPoint GetItemAmount(MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None, bool substitute = false)
        {
            float amount = 0;
            foreach (MyInventoryBase inventory in m_children.Reader)
            {
                amount += (float)inventory.GetItemAmount(contentId, flags, substitute);
            }
            return (MyFixedPoint)amount;
        }

        public override bool AddItems(MyFixedPoint amount, MyObjectBuilder_Base objectBuilder)
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
                        if (inventory.AddItems(availableSpace, objectBuilder))
                        {
                            restAmount -= availableSpace;
                        }
                    }
                    if (restAmount == 0) break;
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

        public MyInventoryBase GetInventory(MyStringHash id)
        {
            foreach (var item in m_children.Reader)
            {
                MyInventoryBase inventory = item as MyInventoryBase;
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
            var inv = component as MyInventoryBase;
            inv.ForcedPriority = ForcedPriority;
            inv.ContentsChanged += child_OnContentsChanged;
            if (component is MyInventory)
            {
                InventoryCount++;
            }
            else if (component is MyInventoryAggregate)
            {
                (component as MyInventoryAggregate).OnInventoryCountChanged += OnChildAggregateCountChanged;
                InventoryCount += (component as MyInventoryAggregate).InventoryCount;
            }
            if (OnAfterComponentAdd != null)
            {
                OnAfterComponentAdd(this, inv);
            }
        }

        private void OnChildAggregateCountChanged(MyInventoryAggregate obj, int change)
        {
            InventoryCount += change;
        }

        public void BeforeComponentRemove(MyComponentBase component)
        {
            var inv = component as MyInventoryBase;
            inv.ForcedPriority = null;
            inv.ContentsChanged -= child_OnContentsChanged;
            if (OnBeforeComponentRemove != null)
            {
                OnBeforeComponentRemove(this, inv);
            }
            if (component is MyInventory)
            {
                InventoryCount--;
            }
            else if (component is MyInventoryAggregate)
            {
                (component as MyInventoryAggregate).OnInventoryCountChanged -= OnChildAggregateCountChanged;
                InventoryCount -= (component as MyInventoryAggregate).InventoryCount;
            }
        }

        private void child_OnContentsChanged(MyComponentBase obj)
        {
            OnContentsChanged();
        }

        public override MyObjectBuilder_ComponentBase Serialize(bool copy = false)
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
                    var comp = MyComponentFactory.CreateInstanceByTypeId(obInv.TypeId);
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
            foreach (MyInventoryBase inventory in m_children.Reader)
            {
                if (inventory.ItemsCanBeAdded(amount, item))
                    return true;
            }
            return false;
        }

        public override bool ItemsCanBeRemoved(MyFixedPoint amount, IMyInventoryItem item)
        {
            foreach (MyInventoryBase inventory in m_children.Reader)
            {
                if (inventory.ItemsCanBeRemoved(amount, item))
                    return true;
            }
            return false;
        }

        public override bool Add(IMyInventoryItem item, MyFixedPoint amount)
        {
            foreach (MyInventoryBase inventory in m_children.Reader)
            {
                if (inventory.ItemsCanBeAdded(amount, item) && inventory.Add(item, amount))
                    return true;
            }
            return false;
        }

        public override bool Remove(IMyInventoryItem item, MyFixedPoint amount)
        {
            foreach (MyInventoryBase inventory in m_children.Reader)
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

        public override void OnContentsChanged()
        {
            RaiseContentsChanged();
            if (Sync.IsServer && RemoveEntityOnEmpty && GetItemsCount() == 0)
            {
                Container.Entity.Close();
            }
        }

        public override void OnBeforeContentsChanged()
        {
            RaiseBeforeContentsChanged();
        }

        /// <summary>
        /// Transfers safely given item from inventory given as parameter to this instance.
        /// </summary>
        /// <returns>true if items were succesfully transfered, otherwise, false</returns>
        public override bool TransferItemsFrom(MyInventoryBase sourceInventory, IMyInventoryItem item, MyFixedPoint amount)
        {
            if (sourceInventory == null)
            {
                System.Diagnostics.Debug.Fail("Source inventory is null!");
                return false;
            }
            MyInventoryBase destinationInventory = this;
            if (destinationInventory == null)
            {
                System.Diagnostics.Debug.Fail("Destionation inventory is null!");
                return false;
            }
            if (item == null)
            {
                System.Diagnostics.Debug.Fail("Item is null!");
                return false;
            }
            if (amount == 0)
            {
                return true;
            }

            bool transfered = false;
            if ((destinationInventory.ItemsCanBeAdded(amount, item) || destinationInventory == sourceInventory) && sourceInventory.ItemsCanBeRemoved(amount, item))
            {
                if (Sync.IsServer)
                {
                    if (destinationInventory != sourceInventory)
                    {
                        // try to add first and then remove to ensure this items don't disappear
                        if (destinationInventory.Add(item, amount))
                        {
                            if (sourceInventory.Remove(item, amount))
                            {
                                // successfull transaction
                                return true;
                            }
                            else
                            {
                                // This can happend, that it can't be removed due to some lock, then we need to revert the add.
                                destinationInventory.Remove(item, amount);
                            }
                        }
                    }
                    else
                    {
                        // same inventory transfer = splitting amount, need to remove first and add second
                        if (sourceInventory.Remove(item, amount) && destinationInventory.Add(item, amount))
                        {
                            return true;
                        }
                        else
                        {
                            System.Diagnostics.Debug.Fail("Error! Unsuccesfull splitting!");
                        }
                    }
                }
                else
                {
                    Debug.Assert(sourceInventory != null);
                    MyInventoryTransferEventContent eventParams = new MyInventoryTransferEventContent();
                    eventParams.Amount = amount;
                    eventParams.ItemId = item.ItemId;
                    eventParams.SourceOwnerId = sourceInventory.Entity.EntityId;
                    eventParams.SourceInventoryId = sourceInventory.InventoryId;
                    eventParams.DestinationOwnerId = destinationInventory.Entity.EntityId;
                    eventParams.DestinationInventoryId = destinationInventory.InventoryId;
                    MyMultiplayer.RaiseStaticEvent(s => InventoryBaseTransferItem_Implementation, eventParams);
                }
            }

            return transfered;
        }

        [Event, Reliable, Server]
        private static void InventoryBaseTransferItem_Implementation(MyInventoryTransferEventContent eventParams)
        {
            if (!MyEntities.EntityExists(eventParams.DestinationOwnerId) || !MyEntities.EntityExists(eventParams.SourceOwnerId)) return;

            MyEntity sourceOwner = MyEntities.GetEntityById(eventParams.SourceOwnerId);
            MyInventoryBase source = sourceOwner.GetInventory(eventParams.SourceInventoryId);
            MyEntity destOwner = MyEntities.GetEntityById(eventParams.DestinationOwnerId);
            MyInventoryBase dst = destOwner.GetInventory(eventParams.DestinationInventoryId);
            var items = source.GetItems();
            MyPhysicalInventoryItem? foundItem = null;
            foreach (var item in items)
            {
                if (item.ItemId == eventParams.ItemId)
                {
                    foundItem = item;
                }
            }

            if (foundItem.HasValue)
                dst.TransferItemsFrom(source, foundItem, eventParams.Amount);
        }

        public override void ConsumeItem(MyDefinitionId itemId, MyFixedPoint amount, long consumerEntityId = 0)
        {
            SerializableDefinitionId serializableID = itemId;
            MyMultiplayer.RaiseEvent(this, x => x.InventoryConsumeItem_Implementation, amount, serializableID, consumerEntityId);
        }

        /// <summary>
        /// Returns number of embedded inventories - this inventory can be aggregation of other inventories.
        /// </summary>
        /// <returns>Return one for simple inventory, different number when this instance is an aggregation.</returns>
        public override int GetInventoryCount()
        {
            return InventoryCount;
        }

        /// <summary>
        /// Search for inventory having given search index. 
        /// Aggregate inventory: Iterates through aggregate inventory until simple inventory with matching index is found.
        /// Simple inventory: Returns itself if currentIndex == searchIndex.
        /// 
        /// Usage: searchIndex = index of inventory being searched, leave currentIndex = 0.
        /// </summary>
        public override MyInventoryBase IterateInventory(int searchIndex, int currentIndex)
        {
            foreach (var inventoryComp in ChildList.Reader)
            {
                var inventory = inventoryComp as MyInventoryBase;
                if (inventory != null)
                {
                    var foundInventory = inventory.IterateInventory(searchIndex, currentIndex); // recursive search (it might be aggregate inventory again)
                    if (foundInventory != null)
                    {
                        return foundInventory; // we found it!
                    }
                    else if (inventory is MyInventory)
                    {
                        currentIndex++; // we did not found correct inventory - advance current index
                    }
                }
            }
            return null;
        }

        [Event, Reliable, Server]
        private void InventoryConsumeItem_Implementation(MyFixedPoint amount, SerializableDefinitionId itemId, long consumerEntityId)
        {
            if ((consumerEntityId != 0 && !MyEntities.EntityExists(consumerEntityId)))
            {
                return;
            }

            var existingAmount = GetItemAmount(itemId);
            if (existingAmount < amount)
                amount = existingAmount;

            MyEntity entity = null;
            if (consumerEntityId != 0)
            {
                entity = MyEntities.GetEntityById(consumerEntityId);
                if (entity == null)
                    return;
            }

            bool removeItem = true;

            if (entity.Components != null)
            {
                var definition = MyDefinitionManager.Static.GetDefinition(itemId) as MyUsableItemDefinition;
                if (definition != null)
                {
                    var character = entity as MyCharacter;
                    if (character != null)
                        character.SoundComp.StartSecondarySound(definition.UseSound, true);

                    var consumableDef = definition as MyConsumableItemDefinition;
                    if (consumableDef != null)
                    {
                        var statComp = entity.Components.Get<MyEntityStatComponent>() as MyCharacterStatComponent;
                        if (statComp != null)
                        {
                            statComp.Consume(amount, consumableDef);
                        }
                    }

                    var schematicDef = definition as MySchematicItemDefinition;
                    if (schematicDef != null)
                        removeItem &= MySessionComponentResearch.Static.UnlockResearch(character, schematicDef.Research);
                }
            }

            if (removeItem)
                RemoveItemsOfType(amount, itemId);
        }

        #region Fixing wrong inventories

        /// <summary>
        /// Naive looking for inventories with some items..
        /// </summary>
        static public MyInventoryAggregate FixInputOutputInventories(MyInventoryAggregate inventoryAggregate, MyInventoryConstraint inputInventoryConstraint, MyInventoryConstraint outputInventoryConstraint)
        {
            MyInventory inputInventory = null;
            MyInventory outputInventory = null;

            foreach (var inventory in inventoryAggregate.ChildList.Reader)
            {
                var myInventory = inventory as MyInventory;

                if (myInventory == null)
                    continue;

                if (myInventory.GetItemsCount() > 0)
                {
                    if (inputInventory == null)
                    {
                        bool check = true;
                        if (inputInventoryConstraint != null)
                        {
                            foreach (var item in myInventory.GetItems())
                            {
                                check &= inputInventoryConstraint.Check(item.GetDefinitionId());
                            }
                        }
                        if (check)
                        {
                            inputInventory = myInventory;
                        }
                    }
                    if (outputInventory == null && inputInventory != myInventory)
                    {
                        bool check = true;
                        if (outputInventoryConstraint != null)
                        {
                            foreach (var item in myInventory.GetItems())
                            {
                                check &= outputInventoryConstraint.Check(item.GetDefinitionId());
                            }
                        }
                        if (check)
                        {
                            outputInventory = myInventory;
                        }
                    }
                }
            }

            if (inputInventory == null || outputInventory == null)
            {
                foreach (var inventory in inventoryAggregate.ChildList.Reader)
                {
                    var myInventory = inventory as MyInventory;
                    if (myInventory == null)
                        continue;
                    if (inputInventory == null)
                    {
                        inputInventory = myInventory;
                    }
                    else if (outputInventory == null)
                    {
                        outputInventory = myInventory;
                    }
                    else
                    {
                        break;
                    }
                }
            }


            inventoryAggregate.RemoveComponent(inputInventory);
            inventoryAggregate.RemoveComponent(outputInventory);
            var fixedAggregate = new MyInventoryAggregate();
            fixedAggregate.AddComponent(inputInventory);
            fixedAggregate.AddComponent(outputInventory);
            return fixedAggregate;
        }

        #endregion
    }
}
