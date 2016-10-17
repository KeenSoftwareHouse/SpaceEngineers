#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.World;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using VRage;
using VRage.ObjectBuilders;
using VRage.Game.Components;
using Sandbox.ModAPI.Interfaces;
using VRage.Game.ObjectBuilders.ComponentSystem;
using Sandbox.Game.Entities.Inventory;
using Sandbox.Game.Gui;
using VRage.ModAPI;
using Sandbox.Game.Entities.Cube;
using VRage.Library.Utils;
using VRage.Network;
using VRage.Library.Collections;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.Components;
using VRage.Serialization;
using Sandbox.Game.Replication;
using Sandbox.Common;
using Sandbox.Engine.Utils;
using VRage.Game.Entity;
using Sandbox.Game.EntityComponents;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using Sandbox.Game.Entities.Interfaces;
using Sandbox.Game.GUI;
using Sandbox.Game.SessionComponents;
using VRage.Audio;
using VRage.Sync;
using IMyEntity = VRage.ModAPI.IMyEntity;

#endregion

namespace Sandbox.Game
{
    public struct MyInventoryTransferEventContent
    {
        public MyFixedPoint Amount;
        public uint ItemId;
        public long SourceOwnerId;
        public MyStringHash SourceInventoryId;
        public long DestinationOwnerId;
        public MyStringHash DestinationInventoryId;
    }

    [MyComponentBuilder(typeof(MyObjectBuilder_Inventory))]
    [StaticEventOwner]
    public partial class MyInventory : MyInventoryBase, IMyEventProxy
    {
        #region Fields

        /// <summary>
        /// Temporary data for processing changes
        /// </summary>
        private static Dictionary<MyDefinitionId, int> m_tmpItemsToAdd = new Dictionary<MyDefinitionId, int>();
        /// <summary>
        /// Items contained in the inventory
        /// </summary>
        List<MyPhysicalInventoryItem> m_items = new List<MyPhysicalInventoryItem>();
        /// <summary>
        /// Maximal allowed mass in inventory 
        /// </summary>
        MyFixedPoint m_maxMass = MyFixedPoint.MaxValue;
        /// <summary>
        /// Maximal allowed volume in inventor, in dm3 (1dm3 = 0.001m3, 1m3 = 1000dm3) stored in dm3 / litres because of floating errors
        /// </summary>
        MyFixedPoint m_maxVolume = MyFixedPoint.MaxValue;
        int m_maxItemCount = int.MaxValue;
        MySoundPair dropSound = new MySoundPair("PlayDropItem");
        /// <summary>
        /// Current occupied volume in inventory in dm3 / litres
        /// </summary>
        readonly Sync<MyFixedPoint> m_currentVolume;
        /// <summary>
        /// Current occupied mass in inventory
        /// </summary>
        readonly Sync<MyFixedPoint> m_currentMass;
        /// <summary>
        /// Flags indicating capabilities of inventory - can send/receive - used by conveiors etc.
        /// </summary>
        private MyInventoryFlags m_flags;
        /// <summary>
        /// Any attached data..
        /// </summary>
        public object UserData;

        public override float? ForcedPriority { get; set; }

        //Use NextItemID to get item id
        private uint m_nextItemID = 0;

        /// <summary>
        /// Stores used ids of the items..
        /// </summary>
        private HashSet<uint> m_usedIds = new HashSet<uint>();

        public readonly SyncType SyncType;

        private bool m_multiplierEnabled = true;

        /// <summary>
        /// Constraint filtering items added to inventory. If null, everything is allowed.
        /// Note that setting this constraint will not affect items already in the inventory.
        /// </summary>
        public MyInventoryConstraint Constraint = null;

        // CH: TODO: Remove this! It's only here because we are not able to specify inventory settings if it's in an aggregate
        private MyObjectBuilder_InventoryDefinition myObjectBuilder_InventoryDefinition;

        private MyHudNotification m_inventoryNotEmptyNotification;

        #endregion

        #region Init

        public MyInventory()
            : this(MyFixedPoint.MaxValue, MyFixedPoint.MaxValue, Vector3.Zero, 0)
        {
        }

        public MyInventory(float maxVolume, Vector3 size, MyInventoryFlags flags)
            : this((MyFixedPoint)maxVolume, MyFixedPoint.MaxValue, size, flags)
        {
        }

        public MyInventory(float maxVolume, float maxMass, Vector3 size, MyInventoryFlags flags)
            : this((MyFixedPoint)maxVolume, (MyFixedPoint)maxMass, size, flags)
        {
        }

        public MyInventory(MyFixedPoint maxVolume, MyFixedPoint maxMass, Vector3 size, MyInventoryFlags flags)
            : base("Inventory")
        {
            m_maxVolume = maxVolume;
            m_maxMass = maxMass;
            m_flags = flags;

#if !XB1 // !XB1_SYNC_NOREFLECTION
            SyncType = SyncHelpers.Compose(this);
#else // XB1
            SyncType = new SyncType(new List<SyncBase>());
            m_currentVolume = SyncType.CreateAndAddProp<MyFixedPoint>();
            m_currentMass = SyncType.CreateAndAddProp<MyFixedPoint>();
#endif // XB1
            m_currentVolume.ValueChanged += (x) => PropertiesChanged();
            m_currentVolume.ValidateNever();

            m_currentMass.ValueChanged += (x) => PropertiesChanged();
            m_currentMass.ValidateNever();

            m_inventoryNotEmptyNotification = new MyHudNotification(font: MyFontEnum.Red, priority: 2, text: MyCommonTexts.NotificationInventoryNotEmpty);

            Clear();
        }

        public MyInventory(MyObjectBuilder_InventoryDefinition definition, MyInventoryFlags flags)
            : this(definition.InventoryVolume, definition.InventoryMass, new Vector3(definition.InventorySizeX, definition.InventorySizeY, definition.InventorySizeZ), flags)
        {
            myObjectBuilder_InventoryDefinition = definition;

        }

        #endregion

        #region Properties

        public override MyFixedPoint MaxMass // in kg
        {
            get
            {
                return MyPerGameSettings.ConstrainInventory()
                    ? (m_multiplierEnabled ? MyFixedPoint.MultiplySafe(m_maxMass, MySession.Static.InventoryMultiplier) : m_maxMass)
                    : MyFixedPoint.MaxValue;
            }
        }

        public override MyFixedPoint MaxVolume // in m3
        {
            get
            {
                return MyPerGameSettings.ConstrainInventory()
                    ? (m_multiplierEnabled ? MyFixedPoint.MultiplySafe(m_maxVolume, MySession.Static.InventoryMultiplier) : m_maxVolume)
                    : MyFixedPoint.MaxValue;
            }
        }

        public override int MaxItemCount
        {
            get
            {
                if (!MyPerGameSettings.ConstrainInventory()) return int.MaxValue;
                if (!m_multiplierEnabled) return m_maxItemCount;

                long itemCount = Math.Max(1, (long)(m_maxItemCount * (double)MySession.Static.InventoryMultiplier));
                if (itemCount > (long)int.MaxValue) itemCount = (long)int.MaxValue;
                return (int)itemCount;
            }
        }

        public override MyFixedPoint CurrentVolume // in m3
        {
            get { return m_currentVolume; }
        }

        public float VolumeFillFactor
        {
            get
            {
                if (!MyPerGameSettings.ConstrainInventory()) return 0.0f;
                return (float)CurrentVolume / (float)MaxVolume;
            }
        }

        public override MyFixedPoint CurrentMass
        {
            get { return m_currentMass; }
        }

        public void SetFlags(MyInventoryFlags flags)
        {
            m_flags = flags;
        }

        public MyInventoryFlags GetFlags()
        {
            return m_flags;
        }

        public MyEntity Owner
        {
            get
            {
                if (Entity == null)
                {
                    Debug.Fail("Inventory always have to have owner!");
                    return null;
                }
                return Entity as MyEntity;
            }
        }

        public byte InventoryIdx
        {
            get
            {
                if (Owner != null)
                {
                    for (byte i = 0; i < Owner.InventoryCount; i++)
                    {
                        if (Owner.GetInventory(i).Equals(this))
                        {
                            return i;
                        }
                    }
                }
                return 0;
            }
        }

        public bool IsFull
        {
            get { return m_currentVolume >= MaxVolume || m_currentMass >= MaxMass; }
        }

        /// <summary>
        /// Returns a value in the range [0,1] that indicates how full this inventory is.
        /// 0 is empty
        /// 1 is full
        /// If there are no cargo constraints, will return empty
        /// </summary>
        public float CargoPercentage
        {
            get
            {
                if (!MyPerGameSettings.ConstrainInventory())
                    return 0;

                float currentVolume = (float)m_currentVolume.Value;
                float maxVolume = (float)MaxVolume;
                return MyMath.Clamp(currentVolume / maxVolume, 0, 1);
            }
        }

        #endregion

        #region Items

        // CH: TODO: Remove!
        public bool CanItemsBeAdded(MyFixedPoint amount, MyDefinitionId contentId)
        {
            return CanItemsBeAdded(amount, contentId, MaxVolume, MaxMass, m_currentVolume, m_currentMass) && CheckConstraint(contentId);
        }

        // CH: TODO: Remove!
        public static bool CanItemsBeAdded(MyFixedPoint amount, MyDefinitionId contentId, MyFixedPoint maxVolume, MyFixedPoint maxMass, MyFixedPoint currentVolume, MyFixedPoint currentMass)
        {
            var adapter = MyInventoryItemAdapter.Static;
            adapter.Adapt(contentId);

            if (MyPerGameSettings.ConstrainInventory() && (amount * adapter.Volume + currentVolume > maxVolume) || (amount * adapter.Mass + currentMass > maxMass))
            {
                return false;
            }
            return true;
        }

        public static void GetItemVolumeAndMass(MyDefinitionId contentId, out float itemMass, out float itemVolume)
        {
            var adapter = MyInventoryItemAdapter.Static;
            if (adapter.TryAdapt(contentId))
            {
                itemMass = adapter.Mass;
                itemVolume = adapter.Volume;
            }
            else
            {
                itemMass = 0;
                itemVolume = 0;
            }
        }

        public override MyFixedPoint ComputeAmountThatFits(MyDefinitionId contentId, float volumeRemoved = 0, float massRemoved = 0)
        {
            if (!MyPerGameSettings.ConstrainInventory())
                return MyFixedPoint.MaxValue;

            var adapter = MyInventoryItemAdapter.Static;
            adapter.Adapt(contentId);

            // CH: TODO: It's probably the time to start thinking about abstracting this into "inventory limiters" - mass limiter, volume limiter, slot limiter, constraint limiter ...
            //           Or maybe extend constraints to do this?
            var amountThatFitsVolume = MyFixedPoint.Max((MyFixedPoint)(((float)MaxVolume - Math.Max(((float)m_currentVolume.Value - volumeRemoved * (float)adapter.Volume), 0)) * (1.0f / (float)adapter.Volume)), 0);
            var amountThatFitsMass = MyFixedPoint.Max((MyFixedPoint)(((float)MaxMass - Math.Max(((float)m_currentMass.Value - massRemoved * (float)adapter.Mass), 0)) * (1.0f / (float)adapter.Mass)), 0);
            var amountThatFits = MyFixedPoint.Min(amountThatFitsVolume, amountThatFitsMass);

            if (MaxItemCount != int.MaxValue)
            {
                amountThatFits = MyFixedPoint.Min(amountThatFits, FindFreeSlotSpace(contentId, adapter));
            }

            if (adapter.HasIntegralAmounts)
            {
                amountThatFits = MyFixedPoint.Floor(amountThatFits);
            }

            return amountThatFits;
        }

        public MyFixedPoint ComputeAmountThatFits(MyBlueprintDefinitionBase blueprint)
        {
            if (!MyPerGameSettings.ConstrainInventory())
                return MyFixedPoint.MaxValue;

            var amountThatFits = (MaxVolume - m_currentVolume) * (1.0f / blueprint.OutputVolume);
            amountThatFits = MyFixedPoint.Max(amountThatFits, 0); // In case we added more than we can carry using debug keys.

            if (blueprint.Atomic)
                amountThatFits = MyFixedPoint.Floor(amountThatFits);

            return amountThatFits;
        }

        public bool CheckConstraint(MyDefinitionId contentId)
        {
            if (Constraint != null)
                return Constraint.Check(contentId);

            return true;
        }

        public bool ContainItems(MyFixedPoint amount, MyObjectBuilder_PhysicalObject ob)
        {
            if (ob == null) return false;
            return ContainItems(amount, ob.GetObjectId());
        }

        public MyFixedPoint FindFreeSlotSpace(MyDefinitionId contentId, IMyInventoryItemAdapter adapter)
        {
            MyFixedPoint sum = 0;
            MyFixedPoint max = adapter.MaxStackAmount;
            for (int i = 0; i < m_items.Count; ++i)
            {
                if (m_items[i].Content.CanStack(contentId.TypeId, contentId.SubtypeId, MyItemFlags.None))
                {
                    sum = MyFixedPoint.AddSafe(sum, max - m_items[i].Amount);
                }
            }

            var diff = MaxItemCount - m_items.Count;
            if (diff > 0)
                sum = MyFixedPoint.AddSafe(sum, max * diff);

            return sum;
        }

        public override MyFixedPoint GetItemAmount(MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None, bool substitute = false)
        {
            MyFixedPoint amount = 0;

            foreach (var item in m_items)
            {
                var objectId = item.Content.GetId();

                if (contentId != objectId && item.Content.TypeId == typeof(MyObjectBuilder_BlockItem))
                {
                    //objectId = MyDefinitionManager.Static.GetComponentId(item.Content.GetObjectId());
                    objectId = item.Content.GetObjectId();
                }

                if (substitute && MySessionComponentEquivalency.Static != null)
                {
                    objectId = MySessionComponentEquivalency.Static.GetMainElement(objectId);
                    contentId = MySessionComponentEquivalency.Static.GetMainElement(contentId);
                }

                if (objectId == contentId && item.Content.Flags == flags)
                    amount += item.Amount;

                //if (objectId == contentId && item.Content.Flags == flags)
                //    amount += item.Amount;
            }

            return amount;
        }

        public MyPhysicalInventoryItem? FindItem(MyDefinitionId contentId)
        {
            int? itemPos = FindFirstPositionOfType(contentId);
            if (itemPos.HasValue)
                return m_items[itemPos.Value];
            else
                return null;
        }

        public MyPhysicalInventoryItem? FindItem(Func<MyPhysicalInventoryItem, bool> predicate)
        {
            foreach (var item in m_items)
            {
                if (predicate(item))
                    return item;
            }

            return null;
        }

        /// <summary>
        /// This will try to find the first item that can be use. This means, if durability is enabled on items, it will look for first item with durability HP > 0,
        /// if this is disabled, this will behave the same as FindItem method
        /// </summary>
        /// <param name="contentId">definition id of the item</param>
        /// <returns>item that has durability > 0 if found</returns>
        public MyPhysicalInventoryItem? FindUsableItem(MyDefinitionId contentId)
        {
            if (!MyFakes.ENABLE_DURABILITY_COMPONENT)
            {
                return FindItem(contentId);
            }

            int itemPosition = -1;
            while (TryFindNextPositionOfTtype(contentId, itemPosition, out itemPosition) && m_items.IsValidIndex(itemPosition))
            {
                if (m_items[itemPosition].Content == null ||
                    !m_items[itemPosition].Content.DurabilityHP.HasValue ||
                    m_items[itemPosition].Content.DurabilityHP.Value > 0)
                {
                    return m_items[itemPosition];
                }
            }
            return null;
        }

        private int? FindFirstStackablePosition(MyObjectBuilder_PhysicalObject toStack, MyFixedPoint wantedAmount)
        {
            for (int i = 0; i < m_items.Count; ++i)
            {
                if (m_items[i].Content.CanStack(toStack) && m_items[i].Amount <= wantedAmount) return i;
            }

            return null;
        }

        private int? FindFirstPositionOfType(MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None)
        {
            for (int i = 0; i < m_items.Count; ++i)
            {
                var content = m_items[i].Content;
                if (content.GetObjectId() == contentId &&
                    content.Flags == flags) return i;
            }

            return null;
        }

        private bool TryFindNextPositionOfTtype(MyDefinitionId contentId, int startPosition, out int nextPosition)
        {
            if (m_items.IsValidIndex(startPosition + 1))
            {
                for (int i = startPosition + 1; i < m_items.Count; ++i)
                {
                    var content = m_items[i].Content;
                    if (content.GetObjectId() == contentId)
                    {
                        nextPosition = i;
                        return true;
                    }
                }
            }
            nextPosition = -1;
            return false;
        }

        public bool ContainItems(MyFixedPoint? amount, MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None)
        {
            MyFixedPoint amountPresent = GetItemAmount(contentId, flags);
            return amountPresent >= amount;
        }

        public void TakeFloatingObject(MyFloatingObject obj)
        {
            MyFixedPoint amount = obj.Item.Amount;
            if (MyPerGameSettings.ConstrainInventory())
            {
                amount = MyFixedPoint.Min(ComputeAmountThatFits(obj.Item.Content.GetObjectId()), amount);
            }
            if (obj.MarkedForClose)
                return;

            if (amount > 0)
            {
                if (Sync.IsServer)
                {
                    MyFloatingObjects.RemoveFloatingObject(obj, amount);
                    AddItemsInternal(amount, obj.Item.Content);
                }

            }
        }

        public bool AddGrid(MyCubeGrid grid)
        {
            //TODO: create static list
            List<Vector3I> lst = new List<Vector3I>();

            foreach (var block in grid.GetBlocks())
            {
                if (block.FatBlock is MyCompoundCubeBlock)
                {
                    bool added = false;
                    foreach (var subb in (block.FatBlock as MyCompoundCubeBlock).GetBlocks())
                    {
                        if (AddBlock(subb))
                        {
                            if (!added)
                                lst.Add(block.Position);

                            added = true;
                        }
                    }
                }
                else
                {
                    if (AddBlock(block))
                    {
                        lst.Add(block.Position);
                    }
                }
            }

            if (lst.Count > 0)
            {
                grid.RazeBlocks(lst);
                return true;
            }

            return false;
        }

        public bool AddBlockAndRemoveFromGrid(MySlimBlock block)
        {
            bool added = false;

            if (block.FatBlock is MyCompoundCubeBlock)
            {
                foreach (var subb in (block.FatBlock as MyCompoundCubeBlock).GetBlocks())
                {
                    if (AddBlock(subb))
                    {
                        added = true;
                    }
                }
            }
            else
            {
                if (AddBlock(block))
                {
                    added = true;
                }
            }

            if (added)
            {
                block.CubeGrid.RazeBlock(block.Position);
                return true;
            }

            return false;
        }

        public bool AddBlocks(MyCubeBlockDefinition blockDef, MyFixedPoint amount)
        {
            MyObjectBuilder_BlockItem item = new MyObjectBuilder_BlockItem();
            item.BlockDefId = blockDef.Id;
            if (ComputeAmountThatFits(item.BlockDefId) >= amount)
            {
                AddItems(amount, item);
                return true;
            }
            return false;
        }

        private bool AddBlock(MySlimBlock block)
        {
            if (!MyFakes.ENABLE_GATHERING_SMALL_BLOCK_FROM_GRID && block.FatBlock != null && block.FatBlock.HasInventory) //we cannot store inventory in inventory now
                return false;

            MyObjectBuilder_BlockItem item = new MyObjectBuilder_BlockItem();
            if (MyGridPickupComponent.Static != null)
                item.BlockDefId = MyGridPickupComponent.Static.GetBaseBlock(block.BlockDefinition.Id);
            else
                item.BlockDefId = block.BlockDefinition.Id;

            if (ComputeAmountThatFits(item.BlockDefId) >= 1)
            {
                AddItems(1, item);
                return true;
            }
            return false;
        }

        public void PickupItem(MyFloatingObject obj, MyFixedPoint amount)
        {
            MyMultiplayer.RaiseEvent(this, x => x.PickupItem_Implementation, obj.EntityId, amount);
        }

        [Event, Reliable, Server]
        private void PickupItem_Implementation(long entityId, MyFixedPoint amount)
        {
            MyFloatingObject obj;
            if (MyEntities.TryGetEntityById<MyFloatingObject>(entityId, out obj) && obj != null && obj.MarkedForClose == false && obj.WasRemovedFromWorld == false)
            {
                amount = MyFixedPoint.Min(amount, obj.Item.Amount); // Limit by amount in floating object
                amount = MyFixedPoint.Min(amount, ComputeAmountThatFits(obj.Item.Content.GetObjectId())); // Limit by inventory available space

                if (AddItems(amount, obj.Item.Content))
                {
                    if (amount >= obj.Item.Amount)
                    {
                        MyFloatingObjects.RemoveFloatingObject(obj, true);

                        // Visual Scripting event Implementation
                        if(MyVisualScriptLogicProvider.PlayerPickedUp != null)
                        {
                            var character = Owner as MyCharacter;
                            if(character != null)
                            {
                                var playerId = character.ControllerInfo.ControllingIdentityId;
                                MyVisualScriptLogicProvider.PlayerPickedUp(obj.ItemDefinition.Id.TypeId.ToString(), obj.ItemDefinition.Id.SubtypeName, obj.Name, playerId, amount.ToIntSafe());
                            }
                        }
                    }
                    else
                    {
                        MyFloatingObjects.AddFloatingObjectAmount(obj, -amount);
                    }
                }
            }
        }

        public void DebugAddItems(MyFixedPoint amount, MyObjectBuilder_Base objectBuilder)
        {
            if (!MyFinalBuildConstants.IS_OFFICIAL)
            {
                MyMultiplayer.RaiseEvent(this, x => x.DebugAddItems_Implementation, amount, objectBuilder);
            }
            else
            {
                Trace.Fail("DebugAddItems not supported in official builds (it would be cheating)");
            }
        }

        public override bool AddItems(MyFixedPoint amount, MyObjectBuilder_Base objectBuilder)
        {
            return AddItems(amount, objectBuilder, null, -1);
        }

        private bool AddItems(MyFixedPoint amount, MyObjectBuilder_Base objectBuilder, uint? itemId, int index = -1)
        {
            if (amount == 0) return false;
            Debug.Assert(objectBuilder is MyObjectBuilder_PhysicalObject || (MyFakes.ENABLE_COMPONENT_BLOCKS && objectBuilder is MyObjectBuilder_CubeBlock), "This type of inventory can't add other types than PhysicalObjects!");

            MyObjectBuilder_PhysicalObject physicalObjectBuilder = objectBuilder as MyObjectBuilder_PhysicalObject;
            MyDefinitionId defId = objectBuilder.GetId();
            if (MyFakes.ENABLE_COMPONENT_BLOCKS)
            {
                if (physicalObjectBuilder == null)
                {
                    physicalObjectBuilder = new MyObjectBuilder_BlockItem();
                    (physicalObjectBuilder as MyObjectBuilder_BlockItem).BlockDefId = defId;
                }
                else
                {
                    MyCubeBlockDefinition blockDef = MyDefinitionManager.Static.TryGetComponentBlockDefinition(defId);
                    if (blockDef != null)
                    {
                        physicalObjectBuilder = new MyObjectBuilder_BlockItem();
                        (physicalObjectBuilder as MyObjectBuilder_BlockItem).BlockDefId = blockDef.Id;
                    }
                }
            }

            if (physicalObjectBuilder == null)
                return false;

            defId = physicalObjectBuilder.GetObjectId();
            MyFixedPoint fittingAmount = ComputeAmountThatFits(defId);

            if (fittingAmount < amount) return false;

            if (Sync.IsServer)
            {
                if (MyPerGameSettings.ConstrainInventory())
                    AffectAddBySurvival(ref amount, physicalObjectBuilder);
                if (amount == 0)
                    return false;
                AddItemsInternal(amount, physicalObjectBuilder, itemId, index);
            }
            return true;
        }

        private void AffectAddBySurvival(ref MyFixedPoint amount, MyObjectBuilder_PhysicalObject objectBuilder)
        {
            Debug.Assert(Sync.IsServer);
            MyFixedPoint space = ComputeAmountThatFits(objectBuilder.GetObjectId());
            if (space < amount)
            {
                Debug.Assert(Owner != null, "Owner can't be null!");
                if (Owner is MyCharacter)
                {
                    MyCharacter c = (Owner as MyCharacter);
                    Matrix m = c.GetHeadMatrix(true);
                    MyEntity entity = MyFloatingObjects.Spawn(new MyPhysicalInventoryItem(amount - space, objectBuilder), m.Translation, m.Forward, m.Up, c.Physics);
                    entity.Physics.ApplyImpulse(m.Forward.Cross(m.Up), c.PositionComp.GetPosition());
                }
                amount = space;
            }
        }

        private void AddItemsInternal(MyFixedPoint amount, MyObjectBuilder_PhysicalObject objectBuilder, uint? itemId = null, int index = -1)
        {
            Debug.Assert(amount > 0, "Adding 0 amount of item.");

            OnBeforeContentsChanged();

            MyFixedPoint maxStack = MyFixedPoint.MaxValue;

            var adapter = MyInventoryItemAdapter.Static;
            adapter.Adapt(objectBuilder.GetObjectId());
            maxStack = adapter.MaxStackAmount;

            // If this object can't even stack with itself, the max stack size would be 1
            bool canStackSelf = objectBuilder.CanStack(objectBuilder);
            if (!canStackSelf)
                maxStack = 1;

            // This is hack if we don't have entity created yet, components weren't intialized yet and OB don't contains thi and thus updated health points
            // TODO: This would reaquire in future to init also components when creating OB for entities, no just init components when creating entity instances
            if (MyFakes.ENABLE_DURABILITY_COMPONENT)
            {
                FixDurabilityForInventoryItem(objectBuilder);
            }

            bool clone = false;

            // First try to add a new item at the specified index
            if (index >= 0)
            {
                if (index >= m_items.Count && index < MaxItemCount)
                {
                    amount = AddItemsToNewStack(amount, maxStack, objectBuilder, itemId);
                    clone = true; // We already used the original OB, so we have to clone next time
                }
                else if (index < m_items.Count)
                {
                    var item = m_items[index];
                    if (item.Content.CanStack(objectBuilder))
                    {
                        amount = AddItemsToExistingStack(index, amount, maxStack);
                    }
                    else if (m_items.Count < MaxItemCount)
                    {
                        amount = AddItemsToNewStack(amount, maxStack, objectBuilder, itemId, index);
                        clone = true; // We already used the original OB, so we have to clone next time
                    }
                }
            }

            // Then, distribute the remaining items to the rest of the inventory
            for (int i = 0; i < MaxItemCount; ++i)
            {
                if (i < m_items.Count)
                {
                    var item = m_items[i];
                    if (item.Content.CanStack(objectBuilder))
                    {
                        amount = AddItemsToExistingStack(i, amount, maxStack);
                    }
                }
                else
                {
                    amount = AddItemsToNewStack(amount, maxStack, (clone ? (MyObjectBuilder_PhysicalObject)objectBuilder.Clone() : objectBuilder), itemId);
                    clone = true;
                }

                if (amount == 0) break;
            }

            RefreshVolumeAndMass();
            VerifyIntegrity();
            OnContentsChanged();
        }

        private MyFixedPoint AddItemsToNewStack(MyFixedPoint amount, MyFixedPoint maxStack, MyObjectBuilder_PhysicalObject objectBuilder, uint? itemId, int index = -1)
        {
            Debug.Assert(m_items.Count < MaxItemCount, "Adding a new item beyond the max item count limit!");

            MyFixedPoint addedAmount = MyFixedPoint.Min(amount, maxStack);

            var newItem = new MyPhysicalInventoryItem() { Amount = addedAmount, Scale = 1f, Content = objectBuilder };
            newItem.ItemId = itemId.HasValue ? itemId.Value : GetNextItemID();

            if (index >= 0 && index < m_items.Count)
            {
                //GR: Shift items not add to last position. Slower but more consistent with game logic
                m_items.Add(m_items[m_items.Count - 1]);
                for (int i = m_items.Count - 3; i >= index; i--)
                {
                    m_items[i+1] = m_items[i];
                }
                m_items[index] = newItem;
                
            }
            else
            {
                m_items.Add(newItem);
            }

            m_usedIds.Add(newItem.ItemId);

            if (Sync.IsServer)
                NotifyHudChangedInventoryItem(addedAmount, ref newItem, true);

            return amount - addedAmount;
        }

        private MyFixedPoint AddItemsToExistingStack(int index, MyFixedPoint amount, MyFixedPoint maxStack)
        {
            var item = m_items[index];
            MyFixedPoint freeSpace = maxStack - item.Amount;
            if (freeSpace <= 0) return amount;

            MyFixedPoint addedAmount = MyFixedPoint.Min(freeSpace, amount);

            item.Amount = item.Amount + addedAmount;
            m_items[index] = item;

            if (Sync.IsServer)
                NotifyHudChangedInventoryItem(addedAmount, ref item, true);

            return amount - addedAmount;
        }

        private void NotifyHudChangedInventoryItem(MyFixedPoint amount, ref MyPhysicalInventoryItem newItem, bool added)
        {
            if (MyFakes.ENABLE_HUD_PICKED_UP_ITEMS && Entity != null && (Owner is MyCharacter) && MyHud.ChangedInventoryItems.Visible) // Only adding supported now
            {
                long localPlayerId = (Owner as MyCharacter).GetPlayerIdentityId();
                if (localPlayerId == MySession.Static.LocalPlayerId)
                    MyHud.ChangedInventoryItems.AddChangedPhysicalInventoryItem(newItem, amount, added);
            }
        }

        // CH: TODO: Unused, might be useful for when we activate the tool durability
        /// <summary>
        /// TODO: This should be removed when we can initialize components on items that are stored in inventory but don't have entity with components initialized yet.
        /// DurabilityComponent is not created until Entity is initialized.
        /// </summary>
        private void FixDurabilityForInventoryItem(MyObjectBuilder_PhysicalObject objectBuilder)
        {
            MyPhysicalItemDefinition definition = null;
            if (MyDefinitionManager.Static.TryGetPhysicalItemDefinition(objectBuilder.GetId(), out definition))
            {
                // Physical gun objects have different types of entities, therefore also different definitions
                MyContainerDefinition containerDefinition = null;

                if (!MyComponentContainerExtension.TryGetContainerDefinition(definition.Id.TypeId, definition.Id.SubtypeId, out containerDefinition))
                {
                    if (objectBuilder.GetObjectId().TypeId == typeof(MyObjectBuilder_PhysicalGunObject))
                    {
                        var handItemDefinition = MyDefinitionManager.Static.TryGetHandItemForPhysicalItem(objectBuilder.GetObjectId());
                        if (handItemDefinition != null)
                        {
                            MyComponentContainerExtension.TryGetContainerDefinition(handItemDefinition.Id.TypeId, handItemDefinition.Id.SubtypeId, out containerDefinition);
                        }
                    }
                }

                if (containerDefinition != null)
                {
                    if (containerDefinition.HasDefaultComponent("MyObjectBuilder_EntityDurabilityComponent") && !objectBuilder.DurabilityHP.HasValue)
                        objectBuilder.DurabilityHP = 100f;
                }
            }
        }

        public bool RemoveItemsOfType(MyFixedPoint amount, MyObjectBuilder_PhysicalObject objectBuilder, bool spawn = false)
        {
            return TransferOrRemove(this, amount, objectBuilder.GetObjectId(), objectBuilder.Flags, null, spawn) == amount;
        }

        public override MyFixedPoint RemoveItemsOfType(MyFixedPoint amount, MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None, bool spawn = false)
        {
            return TransferOrRemove(this, amount, contentId, flags, null, spawn, onlyWhole: false);
        }

        public void DropItemById(uint itemId, MyFixedPoint amount)
        {
            MyMultiplayer.RaiseEvent(this, x => x.DropItem_Implementation, amount, itemId);
        }

        public void DropItem(int itemIndex, MyFixedPoint amount)
        {
            uint itemId = m_items[itemIndex].ItemId;
            MyMultiplayer.RaiseEvent(this, x => x.DropItem_Implementation, amount, itemId);
        }

        public MyEntity RemoveItemsAt(int itemIndex, MyFixedPoint? amount = null, bool sendEvent = true, bool spawn = false, MatrixD? spawnPos = null)
        {
            if (itemIndex < 0 || itemIndex >= m_items.Count)
            {
                Debug.Assert(true, "Index out of range.");
                return null;
            }

            if (Sync.IsServer)
            {
                return RemoveItems(m_items[itemIndex].ItemId, amount, sendEvent, spawn, spawnPos);
            }
            else
            {
                MyMultiplayer.RaiseEvent(this, x => x.RemoveItemsAt_Request, itemIndex, amount, sendEvent, spawn, spawnPos);
            }
            return null;
        }

        [Event, Reliable, Server]
        private void RemoveItemsAt_Request(int itemIndex, MyFixedPoint? amount = null, bool sendEvent = true, bool spawn = false, MatrixD? spawnPos = null)
        {
            RemoveItemsAt(itemIndex, amount, sendEvent, spawn, spawnPos);
        }

        public MyEntity RemoveItems(uint itemId, MyFixedPoint? amount = null, bool sendEvent = true, bool spawn = false, MatrixD? spawnPos = null)
        {
            var item = GetItemByID(itemId);
            var am = amount.HasValue ? amount.Value : (item.HasValue ? item.Value.Amount : 1);
            MyEntity spawned = null;
            if (Sync.IsServer)
            {
                if (item.HasValue && RemoveItemsInternal(itemId, am, sendEvent))
                {
                    if (spawn)
                    {
                        MyEntity owner;
                        if (Owner is MyEntity)
                        {
                            owner = Owner as MyEntity;
                        }
                        else
                        {
                            owner = Container.Entity as MyEntity;
                        }

                        if (!spawnPos.HasValue)
                            spawnPos = MatrixD.CreateWorld(owner.PositionComp.GetPosition() + owner.PositionComp.WorldMatrix.Forward + owner.PositionComp.WorldMatrix.Up, owner.PositionComp.WorldMatrix.Forward, owner.PositionComp.WorldMatrix.Up);
                        spawned = item.Value.Spawn(am, spawnPos.Value, owner);

                        if (spawned != null && spawnPos.HasValue)
                        {
                            if (owner == MySession.Static.LocalCharacter)
                            {
                                MyGuiAudio.PlaySound(MyGuiSounds.PlayDropItem);
                            }
                            else
                            {
                                MyEntity3DSoundEmitter emitter = MyAudioComponent.TryGetSoundEmitter();
                                if (emitter != null)
                                {
                                    emitter.SetPosition(spawnPos.Value.Translation);
                                    emitter.PlaySound(dropSound);
                                }
                            }
                        }
                    }
                }
            }
            return spawned;
        }

        public bool RemoveItemsInternal(uint itemId, MyFixedPoint amount, bool sendEvent = true)
        {
            if (sendEvent)
                OnBeforeContentsChanged();
            bool found = false;
            for (int i = 0; i < m_items.Count; i++)
                if (m_items[i].ItemId == itemId)
                {
                    MyPhysicalInventoryItem item = m_items[i];
                    amount = MathHelper.Clamp(amount, 0, m_items[i].Amount);
                    item.Amount -= amount;
                    Debug.Assert(!(item.Amount != 0 && amount == 0), "Probably removing amount clamped by FixedPoint to 0.");
                    if (item.Amount == 0)
                    {
                        m_usedIds.Remove(m_items[i].ItemId);
                        m_items.RemoveAt(i);
                    }
                    else
                    {
                        m_items[i] = item;
                    }

                    found = true;

                    this.RaiseEntityEvent(MyStringHash.GetOrCompute("InventoryChanged"), new MyEntityContainerEventExtensions.InventoryChangedParams(item.ItemId, this, (float)item.Amount));

                    if (Sync.IsServer)
                        NotifyHudChangedInventoryItem(amount, ref item, false);

                    break;
                }

            if (!found)
            {
                Debug.Assert(!found, "Item is missing in inventory. Can't remove.");
                return false;
            }

            RefreshVolumeAndMass();

            if (sendEvent)
                OnContentsChanged();

            return true;
        }

        public override List<MyPhysicalInventoryItem> GetItems()
        {
            return m_items;
        }

        public bool Empty()
        {
            if (m_items.Count == 0)
            {
                Debug.Assert(m_currentMass.Value == 0, "Non-zero mass of an empty inventory.");
                Debug.Assert(m_currentVolume.Value == 0, "Non-zero volume of an empty inventory.");
                return true;
            }
            return false;
        }

        public static MyFixedPoint Transfer(MyInventory src, MyInventory dst, MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None, MyFixedPoint? amount = null, bool spawn = false)
        {
            return TransferOrRemove(src, amount, contentId, flags, dst);
        }

        private static MyFixedPoint TransferOrRemove(MyInventory src, MyFixedPoint? amount, MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None, MyInventory dst = null, bool spawn = false, bool onlyWhole = true)
        {
            MyFixedPoint removedAmount = 0;

            if (!onlyWhole)
            {
                amount = MyFixedPoint.Min(amount.Value, src.GetItemAmount(contentId, flags));
            }

            //Debug.Assert(!amount.HasValue || amount.Value > 0, "Transfering 0 amount of item.");
            if (!onlyWhole || src.ContainItems(amount, contentId, flags))
            {
                bool transferAll = !amount.HasValue;
                MyFixedPoint remainingAmount = transferAll ? 0 : amount.Value;

                //TODO(AF) Remove oxygen specific code from inventory.
                //Will be fixed once MyInventory will support Entities.
                // If the requested item is an oxygen container, do a preliminary loop to pull any non-full items first.
                if (contentId.TypeId == typeof(MyObjectBuilder_OxygenContainerObject) || contentId.TypeId == typeof(MyObjectBuilder_GasContainerObject))
                {
                    int k = 0;
                    while (k < src.m_items.Count)
                    {
                        if (!transferAll && remainingAmount == 0)
                            break;

                        MyPhysicalInventoryItem item = src.m_items[k];

                        // Skip full oxygen bottles in this loop.  They will not be skipped in the next one.
                        var oxygenBottle = item.Content as MyObjectBuilder_GasContainerObject;
                        if (oxygenBottle != null && oxygenBottle.GasLevel == 1f)
                        {
                            k++;
                            continue;
                        }

                        if (item.Content.GetObjectId() != contentId)
                        {
                            k++;
                            continue;
                        }

                        if (transferAll || remainingAmount >= item.Amount)
                        {
                            removedAmount += item.Amount;
                            remainingAmount -= item.Amount;
                            Transfer(src, dst, item.ItemId, -1, spawn: spawn);
                        }
                        else
                        {
                            removedAmount += item.Amount;
                            Transfer(src, dst, item.ItemId, -1, remainingAmount, spawn);
                            remainingAmount = 0;
                        }
                    }
                }
                // End of oxygen specific code

                int i = 0;
                while (i < src.m_items.Count)
                {
                    if (!transferAll && remainingAmount == 0)
                        break;

                    MyPhysicalInventoryItem item = src.m_items[i];

                    var objectId = item.Content.GetId();
                    if (objectId != contentId && item.Content.TypeId == typeof(MyObjectBuilder_BlockItem))
                    {
                        //objectId = MyDefinitionManager.Static.GetComponentId(item.Content.GetObjectId());
                        objectId = item.Content.GetObjectId();
                    }

                    if (objectId != contentId)
                    {
                        i++;
                        continue;
                    }

                    if (transferAll || remainingAmount >= item.Amount)
                    {
                        removedAmount += item.Amount;
                        remainingAmount -= item.Amount;
                        Transfer(src, dst, item.ItemId, -1, spawn: spawn);
                    }
                    else
                    {
                        removedAmount += remainingAmount;
                        Transfer(src, dst, item.ItemId, -1, remainingAmount, spawn);
                        remainingAmount = 0;
                    }
                }
            }

            return removedAmount;
        }

        public void Clear(bool sync = true)
        {
            if (sync == false)
            {
                m_items.Clear();
                m_usedIds.Clear();
                RefreshVolumeAndMass();
                return;
            }

            MyPhysicalInventoryItem[] items = new MyPhysicalInventoryItem[m_items.Count];
            m_items.CopyTo(items);
            foreach (var it in items)
            {
                RemoveItems(it.ItemId);
            }
        }

        public void TransferItemFrom(MyInventory sourceInventory, int sourceItemIndex, int? targetItemIndex = null, bool? stackIfPossible = null, MyFixedPoint? amount = null)
        {
            Debug.Assert(sourceInventory != null);
            if (this == sourceInventory)
                return;
            if (sourceItemIndex < 0 || sourceItemIndex >= sourceInventory.m_items.Count)
                return;
            Transfer(sourceInventory, this, sourceInventory.GetItems()[sourceItemIndex].ItemId, targetItemIndex.HasValue ? targetItemIndex.Value : -1, amount);
        }

        public MyPhysicalInventoryItem? GetItemByID(uint id)
        {
            foreach (var item in m_items)
                if (item.ItemId == id)
                    return item;
            return null;
        }

        //this is from client only
        public static void TransferByUser(MyInventory src, MyInventory dst, uint srcItemId, int dstIdx = -1, MyFixedPoint? amount = null)
        {
            // CH: TODO: Remove if date > 15.5.2016 :-) It's only to catch a nullref
            if (dst.Owner == null) MyLog.Default.WriteLine("dst.Owner == null");

            if (src == null)
            {
                return;
            }

            var itemNullable = src.GetItemByID(srcItemId);
            if (!itemNullable.HasValue)
                return;

            var item = itemNullable.Value;
            
            // CH: TODO: Remove if date > 15.5.2016 :-) It's only to catch a nullref
            if (item.Content == null) MyLog.Default.WriteLine("item.Content == null");

            if (dst != null && !dst.CheckConstraint(item.Content.GetObjectId()))
                return;

            var transferAmount = amount ?? item.Amount;

            if (dst == null)
            {
                src.RemoveItems(srcItemId, amount, true, false);
                return;
            }

            //TransferItemsInternal(src, dst, srcItemId, false, dstIdx, transferAmount);

            // CH: TODO: Remove if date > 15.5.2016 :-) It's only to catch a nullref
            for (int i = 0; i < dst.Owner.InventoryCount; i++)
            {
                if (dst.Owner.GetInventory(i) == null) MyLog.Default.WriteLine("dst.Owner.GetInventory(i) == null");
            }

            byte inventoryIndex = 0;
            for (byte i = 0; i < dst.Owner.InventoryCount; i++)
            {
                if (dst.Owner.GetInventory(i).Equals(dst))
                {
                    inventoryIndex = i;
                    break;
                }
            }

            MyMultiplayer.RaiseEvent(src, x => x.InventoryTransferItem_Implementation, transferAmount, srcItemId, dst.Owner.EntityId, inventoryIndex, dstIdx);
        }

        public static void TransferAll(MyInventory src, MyInventory dst)
        {
            Debug.Assert(Sync.IsServer, "Calling a server-only method on the client!");
            if (!Sync.IsServer)
                return;

            int prevItemCount = src.m_items.Count + 1;
            while (src.m_items.Count != prevItemCount && src.m_items.Count != 0)
            {
                prevItemCount = src.m_items.Count;

                Transfer(src, dst, src.m_items[0].ItemId);
            }
            Debug.Assert(src.m_items.Count == 0, "Could not move all inventory items!");
        }

        public static void Transfer(MyInventory src, MyInventory dst, uint srcItemId, int dstIdx = -1, MyFixedPoint? amount = null, bool spawn = false)
        {
            var itemNullable = src.GetItemByID(srcItemId);
            if (!itemNullable.HasValue)
                return;

            var item = itemNullable.Value;
            if (dst != null && !dst.CheckConstraint(item.Content.GetObjectId()))
                return;
            if (Sync.IsServer)
            {
                var transferAmount = amount ?? item.Amount;

                if (dst == null)
                {
                    src.RemoveItems(srcItemId, amount, true, spawn);
                    return;
                }

                TransferItemsInternal(src, dst, srcItemId, spawn, dstIdx, transferAmount);
            }
        }

        private static void TransferItemsInternal(MyInventory src, MyInventory dst, uint srcItemId, bool spawn, int destItemIndex, MyFixedPoint amount)
        {
            Debug.Assert(Sync.IsServer);
            MyFixedPoint remove = amount;

            MyPhysicalInventoryItem srcItem = default(MyPhysicalInventoryItem);
            int srcIndex = -1;
            for (int i = 0; i < src.m_items.Count; ++i)
            {
                if (src.m_items[i].ItemId == srcItemId)
                {
                    srcIndex = i;
                    srcItem = src.m_items[i];
                    break;
                }
            }
            if (srcIndex == -1) return;

            FixTransferAmount(src, dst, srcItem, spawn, ref remove, ref amount);

            if (amount != 0)
            {
                if (src == dst && destItemIndex >= 0 && destItemIndex < dst.m_items.Count && !dst.m_items[destItemIndex].Content.CanStack(srcItem.Content))
                {
                    dst.SwapItems(srcIndex, destItemIndex);
                }
                else
                {
                    dst.AddItemsInternal(amount, srcItem.Content, dst == src && remove == 0 ? srcItemId : (uint?)null, destItemIndex);
                    if (remove != 0)
                        src.RemoveItems(srcItemId, remove);
                }
            }
        }

        private void SwapItems(int srcIndex, int dstIndex)
        {
            MyPhysicalInventoryItem dstItem = m_items[dstIndex];
            m_items[dstIndex] = m_items[srcIndex];
            m_items[srcIndex] = dstItem;
            VerifyIntegrity();
            OnContentsChanged();
        }

        private static void FixTransferAmount(MyInventory src, MyInventory dst, MyPhysicalInventoryItem? srcItem, bool spawn, ref MyFixedPoint remove, ref MyFixedPoint add)
        {
            Debug.Assert(Sync.IsServer);
            if (srcItem.Value.Amount < remove)
            {
                remove = srcItem.Value.Amount;
                add = remove;
            }

            if (!MySession.Static.CreativeMode && src != dst)
            {
                MyFixedPoint space = dst.ComputeAmountThatFits(srcItem.Value.Content.GetObjectId());
                if (space < remove)
                {
                    if (spawn)
                    {
                        MyEntity e = (dst.Owner as MyEntity);
                        Matrix m = e.WorldMatrix;
                        MyFloatingObjects.Spawn(new MyPhysicalInventoryItem(remove - space, srcItem.Value.Content), e.PositionComp.GetPosition() + m.Forward + m.Up, m.Forward, m.Up, e.Physics);
                    }
                    else
                    {
                        remove = space;
                    }
                    add = space;
                }
            }
        }

        public bool FilterItemsUsingConstraint()
        {
            bool somethingRemoved = false;
            for (int i = m_items.Count - 1; i >= 0; --i)
            {
                if (!CheckConstraint(m_items[i].Content.GetObjectId()))
                {
                    RemoveItems(m_items[i].ItemId, sendEvent: false);
                    somethingRemoved = true;
                }
            }

            if (somethingRemoved)
                OnContentsChanged();

            return somethingRemoved;
        }

        public bool IsItemAt(int position)
        {
            return m_items.IsValidIndex(position);
        }

        public override void CountItems(Dictionary<MyDefinitionId, MyFixedPoint> itemCounts)
        {
            foreach (var item in m_items)
            {
                MyDefinitionId id = item.Content.GetId();

                if (id.TypeId == typeof(MyObjectBuilder_BlockItem))
                {
                    id = item.Content.GetObjectId();
                }

                if (id.TypeId.IsNull || id.SubtypeId == MyStringHash.NullOrEmpty)
                {
                    Debug.Assert(false, "Item definition id is empty!");
                    continue;
                }

                //MyComponentSubstitutionDefinition substitutionDefinition = null;
                //if (MyDefinitionManager.Static.TryGetProvidingComponentDefinition(id, out substitutionDefinition))
                //{
                //    id = substitutionDefinition.RequiredComponent;
                //}
                if (MySessionComponentEquivalency.Static != null)
                {
                    id = MySessionComponentEquivalency.Static.GetMainElement(id);
                }

                MyFixedPoint amount = 0;
                itemCounts.TryGetValue(id, out amount);
                itemCounts[id] = amount + (int)item.Amount;
            }
        }

        public override void ApplyChanges(List<MyComponentChange> changes)
        {
            Debug.Assert(Sync.IsServer);
            if (!Sync.IsServer) return;

            m_tmpItemsToAdd.Clear();

            bool changed = false;
            for (int j = 0; j < changes.Count; ++j)
            {
                var change = changes[j];
                if (change.IsAddition())
                {
                    throw new NotImplementedException();
                }

                if (change.Amount > 0)
                {
                    for (int i = m_items.Count - 1; i >= 0; i--)
                    {
                        var item = m_items[i];
                        MyDefinitionId id = item.Content.GetId();
                        if (id.TypeId == typeof(MyObjectBuilder_BlockItem))
                        {
                            id = item.Content.GetObjectId();
                        }

                        if (change.ToRemove != id) continue;

                        MyFixedPoint amount = change.Amount;
                        if (amount > 0)
                        {
                            MyFixedPoint removed = MyFixedPoint.Min(amount, item.Amount);

                            amount -= removed;
                            if (amount == 0)
                            {
                                changes.RemoveAtFast(j);
                                change.Amount = 0;
                                j--;
                            }
                            else
                            {
                                change.Amount = (int)amount;
                                changes[j] = change;
                            }

                            if (item.Amount - removed == 0)
                            {
                                m_usedIds.Remove(m_items[i].ItemId);
                                m_items.RemoveAt(i);
                            }
                            else
                            {
                                item.Amount = item.Amount - removed;
                                m_items[i] = item;
                            }

                            if (change.IsChange())
                            {
                                int presentAmount = 0;
                                m_tmpItemsToAdd.TryGetValue(change.ToAdd, out presentAmount);
                                presentAmount += (int)removed;
                                if (presentAmount != 0)
                                    m_tmpItemsToAdd[change.ToAdd] = presentAmount;
                            }

                            changed = true;

                            this.RaiseEntityEvent(MyStringHash.GetOrCompute("InventoryChanged"), new MyEntityContainerEventExtensions.InventoryChangedParams(item.ItemId, this, (float)item.Amount));
                        }
                    }
                }
            }

            RefreshVolumeAndMass();

            foreach (var addition in m_tmpItemsToAdd)
            {
                MyCubeBlockDefinition blockDef = MyDefinitionManager.Static.GetComponentBlockDefinition(addition.Key);
                Debug.Assert(blockDef != null, "Could not find block definition for adding an inventory item");
                if (blockDef == null) return;

                bool success = AddBlocks(blockDef, addition.Value);
                Debug.Assert(success, "Could not add blocks to the inventory!");

                changed = true;
                RefreshVolumeAndMass();
            }

            if (changed)
            {
                RefreshVolumeAndMass();
                OnContentsChanged();
            }
        }

        public void ClearItems()
        {
            m_items.Clear();
            m_usedIds.Clear();
        }

        public void AddItemClient(int position, MyPhysicalInventoryItem item)
        {
            if (Sync.IsServer)
            {
                return;
            }

            if (position >= m_items.Count)
            {
                m_items.Add(item);
            }
            else
            {
                m_items.Insert(position, item);
            }
            m_usedIds.Add(item.ItemId);

            NotifyHudChangedInventoryItem(item.Amount, ref item, true);
        }

        public void SwapItemClient(int position, int newPosition)
        {
            var tmp = m_items[position];
            m_items[position] = m_items[newPosition];
            m_items[newPosition] = tmp;
        }

        #endregion

        #region Serialization

        public MyObjectBuilder_Inventory GetObjectBuilder()
        {
            var objBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Inventory>();
            objBuilder.Items.Clear();

            objBuilder.Mass = m_maxMass;
            objBuilder.Volume = m_maxVolume;
            objBuilder.MaxItemCount = m_maxItemCount;

            objBuilder.InventoryFlags = m_flags;

            objBuilder.nextItemId = m_nextItemID;

            objBuilder.RemoveEntityOnEmpty = RemoveEntityOnEmpty;

            foreach (var item in m_items)
                objBuilder.Items.Add(item.GetObjectBuilder());

            return objBuilder;
        }

        public void Init(MyObjectBuilder_Inventory objectBuilder)
        {
            Clear(false);

            if (objectBuilder == null)
            {
                if (myObjectBuilder_InventoryDefinition != null)
                {
                    m_maxMass = (MyFixedPoint)myObjectBuilder_InventoryDefinition.InventoryMass;
                    m_maxVolume = (MyFixedPoint)myObjectBuilder_InventoryDefinition.InventoryVolume;
                    m_maxItemCount = myObjectBuilder_InventoryDefinition.MaxItemCount;
                }
                return;
            }

            if (objectBuilder.Mass.HasValue)
                m_maxMass = objectBuilder.Mass.Value;
            if (objectBuilder.Volume.HasValue)
            {
                MyFixedPoint savedValue = objectBuilder.Volume.Value;
                if (savedValue != MyFixedPoint.MaxValue || MyPerGameSettings.ConstrainInventory() == false)
                {
                    m_maxVolume = savedValue;
                }
            }
            if (objectBuilder.MaxItemCount.HasValue)
                m_maxItemCount = objectBuilder.MaxItemCount.Value;
            if (objectBuilder.InventoryFlags.HasValue)
                m_flags = objectBuilder.InventoryFlags.Value;

            RemoveEntityOnEmpty = objectBuilder.RemoveEntityOnEmpty;

            // HACK: when the session is being loaded, ids get reset. Later on they stay the same. This fixes the issue with desync of ids which may cause inventories to work incorrectly
            bool keepIds = !Sync.IsServer || MySession.Static.Ready;
            if (keepIds)
                m_nextItemID = objectBuilder.nextItemId;
            else
                m_nextItemID = 0;

            int i = 0;
            foreach (var item in objectBuilder.Items)
            {
                if (item.Amount <= 0)
                {
                    // Remove all items with 0 amount when loading inventory.
                    // Should only solve backward problems with saves. 0 amount items should not be created.
                    Debug.Fail(string.Format("Removing item with invalid amount: {0}x '{1}'. This is safe to ignore.", item.Amount, item.PhysicalContent.GetObjectId()));
                    continue;
                }

                if (item.PhysicalContent == null)
                {
                    continue;
                }

                if (!MyInventoryItemAdapter.Static.TryAdapt(item.PhysicalContent.GetObjectId()))
                {
                    Debug.Assert(false, "Invalid inventory item: " + item.PhysicalContent.GetObjectId().ToString() + " Not adding it!");
                    continue;
                }

                var contentId = item.PhysicalContent.GetObjectId();

                var fittingAmount = ComputeAmountThatFits(contentId);
                var addedAmount = MyFixedPoint.Min(fittingAmount, item.Amount);

                if (addedAmount == MyFixedPoint.Zero)
                    continue;

                var canStackWithItself = item.PhysicalContent.CanStack(item.PhysicalContent);
                if (!canStackWithItself)
                {
                    MyFixedPoint added = 0;
                    while (added < addedAmount)
                    {
                        AddItemsInternal(1, item.PhysicalContent, itemId: !keepIds ? null : (uint?)item.ItemId, index: i);
                        added += 1;
                        ++i;
                    }
                }
                else
                {
                    AddItemsInternal(addedAmount, item.PhysicalContent, itemId: !keepIds ? null : (uint?)item.ItemId, index: i);
                }
                i++;
            }
            VerifyIntegrity();
        }

        public override void Init(MyComponentDefinitionBase definition)
        {
            base.Init(definition);

            var inventoryComponentDefinition = definition as MyInventoryComponentDefinition;
            if (inventoryComponentDefinition != null)
            {
                m_maxVolume = (MyFixedPoint)inventoryComponentDefinition.Volume;
                m_maxMass = (MyFixedPoint)inventoryComponentDefinition.Mass;
                RemoveEntityOnEmpty = inventoryComponentDefinition.RemoveEntityOnEmpty;
                m_multiplierEnabled = inventoryComponentDefinition.MultiplierEnabled;
                m_maxItemCount = inventoryComponentDefinition.MaxItemCount;
                Constraint = inventoryComponentDefinition.InputConstraint;
            }
        }

        public void GenerateContent(MyContainerTypeDefinition containerDefinition)
        {
            int itemNumber = MyUtils.GetRandomInt(containerDefinition.CountMin, containerDefinition.CountMax);
            for (int i = 0; i < itemNumber; ++i)
            {
                MyContainerTypeDefinition.ContainerTypeItem item = containerDefinition.SelectNextRandomItem();
                MyFixedPoint amount = (MyFixedPoint)MyRandom.Instance.NextFloat((float)item.AmountMin, (float)item.AmountMax);

                if (ContainItems(1, item.DefinitionId))
                {
                    var currentAmount = GetItemAmount(item.DefinitionId);
                    amount -= currentAmount;
                    if (amount <= 0)
                    {
                        continue;
                    }
                }

                if (MyDefinitionManager.Static.GetPhysicalItemDefinition(item.DefinitionId).HasIntegralAmounts)
                {
                    amount = MyFixedPoint.Ceiling(amount); // Use ceiling to avoid amounts equal to 0
                }

                amount = MyFixedPoint.Min(ComputeAmountThatFits(item.DefinitionId), amount);
                if (amount > 0)
                {
                    var inventoryItem = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(item.DefinitionId);
                    AddItems(amount, inventoryItem);
                }
            }
            containerDefinition.DeselectAll();
        }

        public override MyObjectBuilder_ComponentBase Serialize(bool copy = false)
        {
            return GetObjectBuilder();
        }

        public override void Deserialize(MyObjectBuilder_ComponentBase builder)
        {
            var ob = builder as MyObjectBuilder_Inventory;
            Init(ob);
        }

        #endregion

        private void RefreshVolumeAndMass()
        {
            m_currentMass.Value = 0;
            m_currentVolume.Value = 0;
            MyFixedPoint currentMass = 0;
            MyFixedPoint currentVolume = 0;
            foreach (var item in m_items)
            {
                Debug.Assert(item.Amount > 0);
                var adapter = MyInventoryItemAdapter.Static;
                adapter.Adapt(item as IMyInventoryItem);
                currentMass += adapter.Mass * item.Amount;
                currentVolume += adapter.Volume * item.Amount;
            }

            m_currentMass.Value = currentMass;
            m_currentVolume.Value = currentVolume;

            Debug.Assert(m_currentVolume.Value >= 0);
            Debug.Assert(m_currentMass.Value >= 0);
        }

        [Conditional("DEBUG")]
        private void VerifyIntegrity()
        {
            var itemIds = new HashSet<uint>();
            foreach (var item in m_items)
            {
                Debug.Assert(!itemIds.Contains(item.ItemId), "Non-unique item ID in inventory.");
                itemIds.Add(item.ItemId);
                bool canStackWithItself = item.Content.CanStack(item.Content);
                Debug.Assert(canStackWithItself || item.Amount == 1, "More than one item that can't stack with itself is stacked.");
            }
        }

        //TODO(AF) These functions should be removed when MyInventory will support entities
        #region Oxygen Specific Functions
        public void UpdateGasAmount()
        {
            RefreshVolumeAndMass();
            OnContentsChanged();
        }

        #endregion

        public void AddEntity(IMyEntity entity, bool blockManipulatedEntity = true)
        {
            MyMultiplayer.RaiseEvent(this, x => x.AddEntity_Implementation, entity.EntityId, blockManipulatedEntity);
        }

        [Event, Reliable, Server]
        private void AddEntity_Implementation(long entityId, bool blockManipulatedEntity)
        {
            MyEntity entity;
            if (MyEntities.TryGetEntityById(entityId, out entity) && entity != null)
            {
                AddEntityInternal(entity, blockManipulatedEntity);
            }
        }

        private void AddEntityInternal(IMyEntity ientity, bool blockManipulatedEntity = true)
        {
            MyEntity entity = ientity as MyEntity;
            if (entity == null)
            {
                return;
            }
            MyCubeBlock block;

            Vector3D? hitPosition = null;
            MyCharacterDetectorComponent detectorComponent = Owner.Components.Get<MyCharacterDetectorComponent>();
            if (detectorComponent != null)
                hitPosition = detectorComponent.HitPosition;

            MyDefinitionId dummy;
            // this code checks if this entity can be used as component for building, we want add only those cubegrids to inventories
            entity = TestEntityForPickup(entity, hitPosition, out dummy, blockManipulatedEntity);

            if (entity is MyCubeGrid)
            {
                if (!AddGrid(entity as MyCubeGrid))
                {
                    MyHud.Notifications.Add(MyNotificationSingletons.InventoryFull);
                }
            }
            else if (entity is MyCubeBlock)
            {
                if (!AddBlockAndRemoveFromGrid((entity as MyCubeBlock).SlimBlock))
                {
                    MyHud.Notifications.Add(MyNotificationSingletons.InventoryFull);
                }
            }
            else if (entity is MyFloatingObject)
            {
                TakeFloatingObject(entity as MyFloatingObject);
            }
        }

        /// <summary>
        /// Returns the entity that should be picked up (doesn't always have to be the provided entity)
        /// </summary>
        public MyEntity TestEntityForPickup(MyEntity entity, Vector3D? hitPosition, out MyDefinitionId entityDefId, bool blockManipulatedEntity = true)
        {
            MyCubeBlock block;
            MyCubeGrid grid = MyItemsCollector.TryGetAsComponent(entity, out block, blockManipulatedEntity: blockManipulatedEntity, hitPosition: hitPosition);
            MyUseObjectsComponentBase useObjects = null;

            entityDefId = new MyDefinitionId(null);

            if (grid != null)
            {
                if (!MyCubeGrid.IsGridInCompleteState(grid))
                {
                    MyHud.Notifications.Add(MyNotificationSingletons.IncompleteGrid);
                    return null;
                }
                entityDefId = new MyDefinitionId(typeof(MyObjectBuilder_CubeGrid));
                return grid;
            }
            else if (MyFakes.ENABLE_GATHERING_SMALL_BLOCK_FROM_GRID && block != null && block.BlockDefinition.CubeSize == MyCubeSize.Small)
            {
                var baseEntity = block.GetBaseEntity();
                if (baseEntity != null && baseEntity.HasInventory && !baseEntity.GetInventory().Empty())
                {
                    MyHud.Notifications.Add(m_inventoryNotEmptyNotification);
                    return null;
                }
                entityDefId = block.BlockDefinition.Id;
                return block;
            }
            else if (entity is MyFloatingObject)
            {
                var floating = entity as MyFloatingObject;
                var amount = floating.Item.Amount;
                amount = MyFixedPoint.Min(amount, ComputeAmountThatFits(floating.Item.Content.GetObjectId()));
                if (amount == 0)
                {
                    MyHud.Notifications.Add(MyNotificationSingletons.InventoryFull);
                    return null;
                }
                entityDefId = floating.Item.GetDefinitionId();
                return entity;
            }
            else if (entity.Components.TryGet(out useObjects))
            {
                //entity.DefinitionId
            }
            return null;
        }

        public override bool ItemsCanBeAdded(MyFixedPoint amount, IMyInventoryItem item)
        {
            if (amount == 0) return true;
            if (item == null) return false;
            if (Entity == null || Entity.MarkedForClose) return false;
            return CanItemsBeAdded(amount, item.GetDefinitionId());
        }

        public override bool ItemsCanBeRemoved(MyFixedPoint amount, IMyInventoryItem item)
        {
            if (amount == 0) return true;
            if (item == null) return false;
            var physItemObject = GetItemByID(item.ItemId);
            if (physItemObject.HasValue)
            {
                MyPhysicalInventoryItem physicalItem = physItemObject.Value;
                if (physicalItem.Amount >= amount)
                {
                    return true;
                }
            }
            return false;
        }

        public override bool Add(IMyInventoryItem item, MyFixedPoint amount)
        {
            // CH: TODO: use this line only in local transfers (otherwise, there's no point imho)
            uint? itemId = m_usedIds.Contains(item.ItemId) ? (uint?)null : item.ItemId;
            return AddItems(amount, item.Content, itemId, -1);
        }

        public override bool Remove(IMyInventoryItem item, MyFixedPoint amount)
        {
            if (item.Content is MyObjectBuilder_PhysicalObject)
            {
                var index = GetItemIndexById(item.ItemId);
                if (index != -1)
                {
                    RemoveItemsAt(index, amount);
                    return true;
                }
                return RemoveItemsOfType(amount, item.Content as MyObjectBuilder_PhysicalObject);
            }
            else
            {
                return false;
            }
        }

        public override int GetItemsCount()
        {
            return m_items.Count;
        }

        public int GetItemIndexById(uint id)
        {
            for (int index = 0; index < m_items.Count; ++index)
            {
                if (m_items[index].ItemId == id)
                    return index;
            }
            return -1;
        }

        [Event, Reliable, Server]
        private void InventoryTransferItem_Implementation(MyFixedPoint amount, uint itemId, long destinationOwnerId, byte destInventoryIndex, int destinationIndex)
        {
            if (!MyEntities.EntityExists(destinationOwnerId)) return;

            var destOwner = MyEntities.GetEntityById(destinationOwnerId);
            MyInventory dst = destOwner.GetInventory(destInventoryIndex) as MyInventory;

            MyInventory.TransferItemsInternal(this, dst, itemId, false, destinationIndex, amount);
        }

        [Event, Reliable, Server]
        private void DebugAddItems_Implementation(MyFixedPoint amount, [DynamicObjectBuilder] MyObjectBuilder_Base objectBuilder)
        {
            if (!MyFinalBuildConstants.IS_OFFICIAL)
            {
                AddItems(amount, objectBuilder);
            }
            else
            {
                MyLog.Default.WriteLine("DebugAddItems not supported on OFFICIAL builds (it's cheating)");
            }
        }

        [Event, Reliable, Server]
        private void DropItem_Implementation(MyFixedPoint amount, uint itemIndex)
        {
            if (MyVisualScriptLogicProvider.PlayerDropped != null)
            {
                var character = Owner as MyCharacter;
                if (character != null)
                {
                    var item = GetItemByID(itemIndex);
                    var playerId = character.ControllerInfo.ControllingIdentityId;
                    MyVisualScriptLogicProvider.PlayerDropped(item.Value.Content.TypeId.ToString(), item.Value.Content.SubtypeName, playerId, amount.ToIntSafe());
                }
            }
            RemoveItems(itemIndex, amount, true, true);
        }

        public void UpdateItem(MyDefinitionId contentId, uint? itemId = null, float? amount = null, float? itemHP = null)
        {
            if (!amount.HasValue && !itemHP.HasValue)
                return;

            int? itemPos = null;

            if (itemId.HasValue)
            {
                int index = GetItemIndexById(itemId.Value);
                if (m_items.IsValidIndex(index))
                {
                    itemPos = index;
                }
            }
            else
            {
                itemPos = FindFirstPositionOfType(contentId);
            }

            bool changed = false;

            if (itemPos.HasValue && m_items.IsValidIndex(itemPos.Value))
            {
                var item = m_items[itemPos.Value];

                if (amount.HasValue && amount.Value != (float)item.Amount)
                {
                    item.Amount = (MyFixedPoint)amount.Value;
                    changed = true;
                }

                if (itemHP.HasValue && item.Content != null && (!item.Content.DurabilityHP.HasValue || item.Content.DurabilityHP.Value != itemHP.Value))
                {
                    item.Content.DurabilityHP = itemHP.Value;
                    changed = true;
                }

                if (changed)
                {
                    m_items[itemPos.Value] = item;
                    OnContentsChanged();
                }
            }
        }

        public bool IsUniqueId(uint idToTest)
        {
            return !m_usedIds.Contains(idToTest);
        }

        //Autoincrements - dont make it as a getter property because debugger will auto-increment it while stepping through the code.
        private uint GetNextItemID()
        {
            while (!IsUniqueId(m_nextItemID))
            {
                if (m_nextItemID == uint.MaxValue)
                    m_nextItemID = 0;
                else
                    ++m_nextItemID;
            }

            return m_nextItemID++;
        }

        private void PropertiesChanged()
        {
            if (Sync.IsServer == false)
            {
                OnContentsChanged();
            }
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
            if ((ItemsCanBeAdded(amount, item) || this == sourceInventory) && sourceInventory.ItemsCanBeRemoved(amount, item))
            {
                if (Sync.IsServer)
                {
                    if (this != sourceInventory)
                    {
                        // try to add first and then remove to ensure this items don't disappear
                        if (Add(item, amount))
                        {
                            if (sourceInventory.Remove(item, amount))
                            {
                                // successfull transaction
                                return true;
                            }
                            else
                            {
                                // This can happend, that it can't be removed due to some lock, then we need to revert the add.
                                Remove(item, amount);
                            }
                        }
                    }
                    else
                    {
                        // same inventory transfer = splitting amount, need to remove first and add second
                        if (sourceInventory.Remove(item, amount) && Add(item, amount))
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
                    eventParams.DestinationOwnerId = Entity.EntityId;
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

            /*if (foundItem.HasValue)
                dstT.ransferItemsFrom(source, foundItem, eventParams.Amount, eventParams.DestinationItemIndex);*/
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
            return 1;
        }

        /// <summary>
        /// Search for inventory having given search index. 
        /// Aggregate inventory: Iterates through aggregate inventory until simple inventory with matching index is found.
        /// Simple inventory: Returns itself if currentIndex == searchIndex.
        /// 
        /// Usage: searchIndex = index of inventory being searched, leave currentIndex = 0.
        /// </summary>
        public override MyInventoryBase IterateInventory(int searchIndex, int currentIndex = 0)
        {
            return currentIndex == searchIndex ? this : null;
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


        public void UpdateItemAmoutClient(uint itemId, MyFixedPoint amount)
        {
            if (Sync.IsServer)
            {
                return;
            }

            MyPhysicalInventoryItem? item = null;
            int index = -1;
            for (int i = 0; i < m_items.Count; ++i)
            {
                if (m_items[i].ItemId == itemId)
                {
                    item = m_items[i];
                    index = i;
                    break;
                }
            }

            if (index != -1)
            {
                MyPhysicalInventoryItem item2 = item.Value;
                var gasContainerItem = item2.Content as MyObjectBuilder_GasContainerObject;
                if (gasContainerItem != null)
                {
                    gasContainerItem.GasLevel += (float)amount;
                }
                else
                {
                    item2.Amount += amount;
                }
                m_items[index] = item2;

                NotifyHudChangedInventoryItem(amount, ref item2, amount > 0);
            }
        }

        public void RemoveItemClient(uint itemId)
        {
            if (Sync.IsServer)
            {
                return;
            }

            int index = -1;
            for (int i = 0; i < m_items.Count; ++i)
            {
                if (m_items[i].ItemId == itemId)
                {
                    index = i;
                    break;
                }
            }

            if (index != -1)
            {
                var item = m_items[index];
                NotifyHudChangedInventoryItem(item.Amount, ref item, false);

                m_items.RemoveAt(index);
                m_usedIds.Remove(itemId);
            }
        }

        public void Refresh()
        {
            RefreshVolumeAndMass();
            OnContentsChanged();
        }

        public void FixInventoryVolume(float newValue)
        {
            if (m_maxVolume == MyFixedPoint.MaxValue)
            {
                m_maxVolume = (MyFixedPoint)newValue;
            }
        }

        public void ResetVolume()
        {
            m_maxVolume = MyFixedPoint.MaxValue;
        }
    }
}
