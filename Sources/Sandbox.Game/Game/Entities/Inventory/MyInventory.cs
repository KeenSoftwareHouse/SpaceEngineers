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
using VRage.Components;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Common.ObjectBuilders.ComponentSystem;
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
using Sandbox.Game.Replicables;
using Sandbox.Common;

#endregion

namespace Sandbox.Game
{
    [MyComponentBuilder(typeof(MyObjectBuilder_Inventory))]
    public partial class MyInventory : MyInventoryBase, IMyInventoryOwner, IMyEventProxy
    {
        #region Fields

        List<MyPhysicalInventoryItem> m_items = new List<MyPhysicalInventoryItem>();

        private static Dictionary<MyDefinitionId, int> m_tmpItemsToAdd = new Dictionary<MyDefinitionId, int>();

        //in m3 (1dm3 = 0.001m3, 1m3 = 1000dm3)
        MyFixedPoint m_maxMass = MyFixedPoint.MaxValue;
        MyFixedPoint m_maxVolume = MyFixedPoint.MaxValue; //stored in dm3 / litres because of floating errors
        MyFixedPoint m_currentVolume = 0;   //stored in dm3 because of floating errors
        MyFixedPoint m_currentMass = 0;

        //in meters
        Vector3 m_size = new Vector3(float.MaxValue);

        MyInventoryFlags m_flags;

        // CH: TODO: Remove this!
        IMyInventoryOwner m_owner;

        public object UserData;

        //Use NextItemID
        private uint m_nextItemID = 0;
        //Autoincrements - dont make it as a getter property because debugger will auto-increment it while stepping through the code.
        private uint GetNextItemID()
        {
            return m_nextItemID++;
        }

        #endregion

        public static event Action<MyInventory> OnCreated;
        #region Init

        public MyInventory()
            : this(MyFixedPoint.MaxValue, MyFixedPoint.MaxValue, Vector3.Zero, 0, null)
        {
        }

        public MyInventory(float maxVolume, Vector3 size, MyInventoryFlags flags, IMyInventoryOwner owner)
            : this((MyFixedPoint)maxVolume, MyFixedPoint.MaxValue, size, flags, owner)
        {
        }

        public MyInventory(float maxVolume, float maxMass, Vector3 size, MyInventoryFlags flags, IMyInventoryOwner owner)
            : this((MyFixedPoint)maxVolume, (MyFixedPoint)maxMass, size, flags, owner)
        {
        }

        public MyInventory(MyFixedPoint maxVolume, MyFixedPoint maxMass, Vector3 size, MyInventoryFlags flags, IMyInventoryOwner owner)
            : base("Inventory")
        {
            m_maxVolume = maxVolume;
            m_maxMass = maxMass;
            m_size = size;
            m_flags = flags;
            m_owner = owner;

            Clear();

            SyncObject = new MySyncInventory();

            var handler = OnCreated;
            if (handler != null && m_owner != null) handler(this);
        }

        public MyInventory(MyObjectBuilder_InventoryDefinition definition, MyInventoryFlags flags, IMyInventoryOwner owner)
            : this(definition.InventoryVolume, definition.InventoryMass, new Vector3(definition.InventorySizeX, definition.InventorySizeY, definition.InventorySizeZ), flags, owner)
        {
            myObjectBuilder_InventoryDefinition = definition;
        }

        #endregion

        #region Properties

        public override MyFixedPoint MaxMass // in kg
        {
            get { return m_maxMass; }
        }

        public override MyFixedPoint MaxVolume // in m3
        {
            get { return MyPerGameSettings.ConstrainInventory() ? m_maxVolume * MySession.Static.InventoryMultiplier : MyFixedPoint.MaxValue; }
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

        public Vector3 Size
        {
            get { return m_size; }
        }

        public void SetFlags(MyInventoryFlags flags)
        {
            m_flags = flags;
        }

        public MyInventoryFlags GetFlags()
        {
            return m_flags;
        }

        public IMyInventoryOwner Owner
        {
            get
            {
                if (m_owner == null)
                {
                    return this;
                }
                return m_owner;
            }
        }


        /// <summary>
        /// It set's the inventory owner to self. This is a hack as all sync layers etc. are expecting to have IMyInventoryOwner. Before we rewrite it, we need to keep InventoryOwner.
        /// TODO: This can be deleted, when owner is not needed
        /// </summary>
        public void RemoveOwner()
        {
            m_owner = this;
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

        /// <summary>
        /// Constraint filtering items added to inventory. If null, everything is allowed.
        /// Note that setting this constraint will not affect items already in the inventory.
        /// </summary>
        public MyInventoryConstraint Constraint = null;
        private MyObjectBuilder_InventoryDefinition myObjectBuilder_InventoryDefinition;
        private int p;
        private MyCharacter myCharacter;

        public bool IsFull
        {
            get { return m_currentVolume >= MaxVolume || m_currentMass >= m_maxMass; }
        }

        #endregion

        #region Items

        public bool CanItemsBeAdded(MyFixedPoint amount, MyDefinitionId contentId)
        {
            return CanItemsBeAdded(amount, contentId, MaxVolume, m_maxMass, m_currentVolume, m_currentMass) && CheckConstraint(contentId);
        }

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

        public override MyFixedPoint ComputeAmountThatFits(MyDefinitionId contentId)
        {
            if (!MyPerGameSettings.ConstrainInventory())
                return MyFixedPoint.MaxValue;

            var adapter = MyInventoryItemAdapter.Static;
            adapter.Adapt(contentId);

            var amountThatFitsVolume = MyFixedPoint.Max((MyFixedPoint)((float)MaxVolume - (float)m_currentVolume) * (1.0f / (float)adapter.Volume), 0);
            var amountThatFitsMass = MyFixedPoint.Max((MyFixedPoint)(((float)m_maxMass - (float)m_currentMass) * (1.0f / (float)adapter.Mass)), 0);
            var amountThatFits = MyFixedPoint.Min(amountThatFitsVolume, amountThatFitsMass);

            MyPhysicalItemDefinition physicalItemDefinition = null;
            MyDefinitionManager.Static.TryGetPhysicalItemDefinition(contentId, out physicalItemDefinition);
            if (contentId.TypeId == typeof(MyObjectBuilder_CubeBlock) || (physicalItemDefinition != null && physicalItemDefinition.HasIntegralAmounts))
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

        public override MyFixedPoint GetItemAmount(MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None)
        {
            MyFixedPoint amount = 0;
            foreach (var item in m_items)
            {
                var objectId = item.Content.GetObjectId();
                if (contentId != objectId && item.Content.TypeId == typeof(MyObjectBuilder_BlockItem))
                {
                    objectId = MyDefinitionManager.Static.GetComponentId(item.Content.GetObjectId());
                }

                if (objectId == contentId &&
                    item.Content.Flags == flags)
                    amount += item.Amount;
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

        private int? FindFirstStackablePosition(MyObjectBuilder_PhysicalObject toStack)
        {
            for (int i = 0; i < m_items.Count; ++i)
            {
                if (m_items[i].Content.CanStack(toStack)) return i;
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
            HashSet<Sandbox.Game.Entities.Cube.MySlimBlock> lst = new HashSet<Sandbox.Game.Entities.Cube.MySlimBlock>();

            foreach (var block in grid.GetBlocks())
            {
                if (block.FatBlock is MyCompoundCubeBlock)
                {
                    bool added = false;
                    foreach (var subb in (block.FatBlock as MyCompoundCubeBlock).GetBlocks())
                    {
                        if (AddBlock(subb))
                        {
                            added = true;
                        }
                    }
                    if (added)
                    {
                        lst.Add(block);
                    }
                }
                else
                {
                    if (AddBlock(block))
                    {
                        lst.Add(block);
                    }
                }
            }


            foreach (var b in lst)
            {
                Vector3I pos = b.Position;
                Vector3UByte size = new Vector3UByte(1, 1, 1);
                grid.RazeBlocks(ref pos, ref size);
                //   grid.RemoveBlock(b, true); not synced
            }

            return lst.Count > 0;
        }

        private bool AddBlocks(MyCubeBlockDefinition blockDef, MyFixedPoint amount)
        {
            MyObjectBuilder_BlockItem item = new MyObjectBuilder_BlockItem();
            item.BlockDefId = blockDef.Id;
            if (CanItemsBeAdded(amount, item.BlockDefId))
            {
                AddItems(amount, item);
                return true;
            }
            return false;
        }

        private bool AddBlock(MySlimBlock block)
        {
            if (block.FatBlock is IMyInventoryOwner) //we cannot store inventory in inventory now
                return false;

            MyObjectBuilder_BlockItem item = new MyObjectBuilder_BlockItem();
            item.BlockDefId = block.BlockDefinition.Id;
            if (CanItemsBeAdded(1, item.BlockDefId))
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
            if (MyEntities.TryGetEntityById<MyFloatingObject>(entityId, out obj))
            {
                amount = MyFixedPoint.Min(amount, obj.Item.Amount);
                if(amount >= obj.Item.Amount)
                {
                    MyFloatingObjects.RemoveFloatingObject(obj, true);
                }
                else
                {
                    MyFloatingObjects.AddFloatingObjectAmount(obj, -amount);
                }
                AddItems(amount, obj.Item.Content);
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

        public override bool AddItems(MyFixedPoint amount, MyObjectBuilder_Base objectBuilder, int index = -1, bool stack = true)
        {
            Debug.Assert(objectBuilder is MyObjectBuilder_PhysicalObject, "This type of inventory can't add other types than PhysicalObjects!");
            MyObjectBuilder_PhysicalObject physicalObjectBuilder = objectBuilder as MyObjectBuilder_PhysicalObject;
            if (physicalObjectBuilder == null)
            {
                return false;
            }
            if (amount == 0) return false;
            if (!CanItemsBeAdded(amount, physicalObjectBuilder.GetObjectId())) return false;

            if (Sync.IsServer)
            {
                if (MyPerGameSettings.ConstrainInventory())
                    AffectAddBySurvival(ref amount, physicalObjectBuilder);
                if (amount == 0)
                    return false;
                AddItemsInternal(amount, physicalObjectBuilder, index, null, stack);

            }
            return true;
        }

        private void AffectAddBySurvival(ref MyFixedPoint amount, MyObjectBuilder_PhysicalObject objectBuilder)
        {
            Debug.Assert(Sync.IsServer);
            MyFixedPoint space = ComputeAmountThatFits(objectBuilder.GetObjectId());
            if (space < amount)
            {
                if (Owner != null && Owner.InventoryOwnerType == MyInventoryOwnerTypeEnum.Character)
                {
                    MyCharacter c = (Owner as MyCharacter);
                    Matrix m = c.GetHeadMatrix(true);
                    MyEntity entity = MyFloatingObjects.Spawn(new MyPhysicalInventoryItem(amount - space, objectBuilder), m.Translation, m.Forward, m.Up, c.Physics);
                    entity.Physics.ApplyImpulse(m.Forward.Cross(m.Up), c.PositionComp.GetPosition());
                }
                amount = space;
            }
        }

        public void AddItemsInternal(MyFixedPoint amount, MyObjectBuilder_PhysicalObject objectBuilder, int index = -1, uint? itemId = null, bool stack = true)
        {
            Debug.Assert(amount > 0, "Adding 0 amount of item.");

            var newItem = new MyPhysicalInventoryItem() { Amount = amount, Content = objectBuilder };

            MyFixedPoint maxStack = MyFixedPoint.MaxValue;
            MyComponentDefinition compDef = null;
            if (MyDefinitionManager.Static.TryGetComponentDefinition(objectBuilder.GetId(), out compDef))
                maxStack = compDef.MaxStackAmount;

            if (index >= 0 && index < m_items.Count)
            {
                if (m_items[index].Content.CanStack(objectBuilder))
                {
                    var newStackVal = m_items[index].Amount + newItem.Amount - maxStack;
                    if (newStackVal > 0)
                    {
                        newItem.Amount = maxStack;
                        newItem.ItemId = m_items[index].ItemId;
                        m_items[index] = newItem;

                        newItem.Amount = newStackVal;
                        newItem.ItemId = GetNextItemID();
                        newItem.Content = objectBuilder.Clone() as MyObjectBuilder_PhysicalObject;
                        m_items.Add(newItem);
                    }
                    else
                    {
                        newItem.Amount += m_items[index].Amount;
                        newItem.ItemId = m_items[index].ItemId;
                        m_items[index] = newItem;
                    }
                }
                else
                {
                    newItem.ItemId = GetNextItemID();
                    m_items.Insert(index, newItem);
                }
            }
            else
            {
                bool add = true;
                bool canStackWithItself = newItem.Content.CanStack(newItem.Content);
                if (index < 0 && canStackWithItself && stack)
                {
                    int? itemPos = FindFirstStackablePosition(objectBuilder, maxStack - amount);
                    if (itemPos.HasValue)
                    {
                        newItem.ItemId = m_items[itemPos.Value].ItemId;
                        newItem.Amount += m_items[itemPos.Value].Amount;
                        m_items[itemPos.Value] = newItem;
                        add = false;
                    }
                }
                if (add)
                {
                    MyFixedPoint stackSize = canStackWithItself ? MyFixedPoint.Min(maxStack, amount) : 1;
                    var targetAmount = newItem.Amount;
                    MyFixedPoint addAmount = stackSize;
                    while (targetAmount > 0)
                    {
                        targetAmount -= stackSize;
                        if (targetAmount < 0)
                            addAmount = targetAmount + stackSize;
                        newItem.Amount = addAmount;
                        newItem.ItemId = itemId.HasValue ? itemId.Value : GetNextItemID();
                        itemId = null; // so we use NextItemID next time
                        m_items.Add(newItem);
                        newItem.Content = newItem.Content.Clone() as MyObjectBuilder_PhysicalObject;
                        Debug.Assert(newItem.Content != null);
                    }
                }
            }

            RefreshVolumeAndMass();

            VerifyIntegrity();

            OnContentsChanged();
        }

        public bool RemoveItemsOfType(MyFixedPoint amount, MyObjectBuilder_PhysicalObject objectBuilder, bool spawn = false)
        {
            return TransferOrRemove(this, amount, objectBuilder.GetObjectId(), objectBuilder.Flags, null, spawn) == amount;
        }

        public override MyFixedPoint RemoveItemsOfType(MyFixedPoint amount, MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None, bool spawn = false)
        {
            return TransferOrRemove(this, amount, contentId, flags, null, spawn, onlyWhole: false);
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

            return RemoveItems(m_items[itemIndex].ItemId, amount, sendEvent, spawn, spawnPos);
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
                    }
                }
            }
            return spawned;
        }

        public bool RemoveItemsInternal(uint itemId, MyFixedPoint amount, bool sendEvent = true)
        {
            bool found = false;
            for (int i = 0; i < m_items.Count; i++)
                if (m_items[i].ItemId == itemId)
                {
                    MyPhysicalInventoryItem item = m_items[i];
                    amount = MathHelper.Clamp(amount, 0, m_items[i].Amount);
                    item.Amount -= amount;
                    if (item.Amount == 0 || amount == 0)
                        m_items.RemoveAt(i);
                    else
                        m_items[i] = item;

                    found = true;
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
            if (m_items.Count() == 0)
            {
                Debug.Assert(m_currentMass == 0, "Non-zero mass of an empty inventory.");
                Debug.Assert(m_currentVolume == 0, "Non-zero volume of an empty inventory.");
                return true;
            }
            return false;
        }

        public static void Transfer(MyInventory src, MyInventory dst, MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None, MyFixedPoint? amount = null, bool spawn = false)
        {
            TransferOrRemove(src, amount, contentId, flags, dst);
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

                    var objectId = item.Content.GetObjectId();
                    if (objectId != contentId && item.Content.TypeId == typeof(MyObjectBuilder_BlockItem))
                    {
                        objectId = MyDefinitionManager.Static.GetComponentId(item.Content.GetObjectId());
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
            var itemNullable = src.GetItemByID(srcItemId);
            if (!itemNullable.HasValue)
                return;

            var item = itemNullable.Value;
            if (dst != null && !dst.CheckConstraint(item.Content.GetObjectId()))
                return;

            var transferAmount = amount ?? item.Amount;

            if (dst == null)
            {
                src.RemoveItems(srcItemId, amount, true, false);
                return;
            }

            //TransferItemsInternal(src, dst, srcItemId, false, dstIdx, transferAmount);

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

        public static void TransferItemsInternal(MyInventory src, MyInventory dst, uint itemId, bool spawn, int destItemIndex, MyFixedPoint amount)
        {
            Debug.Assert(Sync.IsServer);
            MyFixedPoint remove = amount;

            var srcItem = src.GetItemByID(itemId);
            if (!srcItem.HasValue) return;

            FixTransferAmount(src, dst, srcItem, spawn, ref remove, ref amount);

            if (amount != 0)
            {
                if (dst.AddItems(amount, srcItem.Value.Content, destItemIndex))
                {
                    if (remove != 0)
                        src.RemoveItems(itemId, remove);
                }
            }
        }

        private static void FixTransferAmount(MyInventory src, MyInventory dst, MyPhysicalInventoryItem? srcItem, bool spawn, ref MyFixedPoint remove, ref MyFixedPoint add)
        {
            Debug.Assert(Sync.IsServer);
            if (srcItem.Value.Amount < remove)
            {
                remove = srcItem.Value.Amount;
                add = remove;
            }

            if (!MySession.Static.CreativeMode && !src.Equals(dst))
            {
                MyFixedPoint space = dst.ComputeAmountThatFits(srcItem.Value.Content.GetId());
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
                    id = MyDefinitionManager.Static.GetComponentId(item.Content.GetObjectId());
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
                            id = MyDefinitionManager.Static.GetComponentId(item.Content.GetObjectId());
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
        }
        public void SetItems(List<MyPhysicalInventoryItem> items)
        {
            m_items = items;
        }

        public void AddItems(List<MyPhysicalInventoryItem> items)
        {
            m_items.AddRange(items);
        }

        public void ReplaceItems(int position, List<MyPhysicalInventoryItem> items)
        {
            if (position >= m_items.Count)
            {
                AddItems(items);
            }
            else
            {
                for (int i = position; i < position + items.Count; ++i)
                {
                    if (i < m_items.Count)
                    {
                        m_items[i] = items[i - position];
                    }
                    else
                    {
                        m_items.Add(items[i - position]);
                    }
                }
            }
        }
        #endregion

        #region Serialization

        public MyObjectBuilder_Inventory GetObjectBuilder()
        {
            var objBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Inventory>();
            objBuilder.Items.Clear();

            objBuilder.Mass = m_maxMass;

            objBuilder.Volume = m_maxVolume;
            objBuilder.Size = m_size;
            objBuilder.InventoryFlags = m_flags;

            objBuilder.nextItemId = m_nextItemID;

            foreach (var item in m_items)
                objBuilder.Items.Add(item.GetObjectBuilder());

            return objBuilder;
        }

        public void Init(MyObjectBuilder_Inventory objectBuilder)
        {
            Clear();

            if (objectBuilder == null)
            {
                if (myObjectBuilder_InventoryDefinition != null)
                {
                    m_maxMass = (MyFixedPoint)myObjectBuilder_InventoryDefinition.InventoryMass;
                    m_maxVolume = (MyFixedPoint)myObjectBuilder_InventoryDefinition.InventoryVolume;
                    m_size = new Vector3(myObjectBuilder_InventoryDefinition.InventorySizeX, myObjectBuilder_InventoryDefinition.InventorySizeY, myObjectBuilder_InventoryDefinition.InventorySizeZ);
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
            if (objectBuilder.Size.HasValue)
                m_size = objectBuilder.Size.Value;
            if (objectBuilder.InventoryFlags.HasValue)
                m_flags = objectBuilder.InventoryFlags.Value;

            // HACK: when the session is being loaded, ids get reset. Later on they stay the same. This fixes the issue with desync of ids which may cause inventories to work incorrectly
            bool keepIds = !Sync.IsServer || MySession.Ready;
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

                if (!MyInventoryItemAdapter.Static.TryAdapt(item.PhysicalContent.GetObjectId()))
                {
                    Debug.Assert(false, "Invalid inventory item: " + item.PhysicalContent.GetObjectId().ToString() + " Not adding it!");
                    continue;
                }

                var contentId = item.PhysicalContent.GetObjectId();

                if (!CanItemsBeAdded(item.Amount, contentId))
                    continue;

                var canStackWithItself = item.PhysicalContent.CanStack(item.PhysicalContent);
                if (!canStackWithItself)
                {
                    MyFixedPoint added = 0;
                    while (added < item.Amount)
                    {
                        AddItemsInternal(1, item.PhysicalContent, i, itemId: !keepIds ? null : (uint?)item.ItemId);
                        added += 1;
                        ++i;
                    }
                }
                else
                {
                    if (!keepIds)
                        AddItemsInternal(item.Amount, item.PhysicalContent, i);
                    else
                        //Building from information recieved from server - dont send msg about adding this
                        AddItemsInternal(item.Amount, item.PhysicalContent, i, itemId: item.ItemId);
                }
                i++;
            }
            VerifyIntegrity();
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

        public override MyObjectBuilder_ComponentBase Serialize()
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
            m_currentMass = 0;
            m_currentVolume = 0;
            foreach (var item in m_items)
            {
                Debug.Assert(item.Amount > 0);
                var adapter = MyInventoryItemAdapter.Static;
                adapter.Adapt(item as IMyInventoryItem);
                m_currentVolume += adapter.Volume * item.Amount;
                m_currentMass += adapter.Mass * item.Amount;
            }

            Debug.Assert(m_currentVolume >= 0);
            Debug.Assert(m_currentMass >= 0);
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

        public void AddEntity(IMyEntity ientity, bool blockManipulatedEntity = true)
        {
            MyEntity entity = ientity as MyEntity;
            if (entity == null)
            {
                return;
            }
            MyCubeBlock block;

            // this code checks if this entity can be used as component for building, we want add only those cubegrids to inventories
            MyCubeGrid grid = MyItemsCollector.TryGetAsComponent(entity, out block, blockManipulatedEntity);

            if (grid != null)
            {
                if (!MyCubeGrid.IsGridInCompleteState(grid))
                {
                    MyHud.Notifications.Add(MyNotificationSingletons.IncompleteGrid);
                }
                else if (!AddGrid(grid))
                {
                    MyHud.Notifications.Add(MyNotificationSingletons.InventoryFull);
                }
            }
            else if (entity is MyFloatingObject)
            {
                var floating = entity as MyFloatingObject;
                var amount = floating.Item.Amount;
                amount = MyFixedPoint.Min(amount, ComputeAmountThatFits(floating.Item.Content.GetObjectId()));
                if (amount == 0)
                {
                    MyHud.Notifications.Add(MyNotificationSingletons.InventoryFull);
                }
                else
                {
                    TakeFloatingObject(floating);
                }
            }
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

        public override bool Add(IMyInventoryItem item, MyFixedPoint amount, bool stack = true)
        {
            return AddItems(amount, item.Content, -1, stack);
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

        // TODO: This is hack due to inventory still needs to keep the InventoryOwner, but not all entities will be implementing its interface
        // This should be removed or deleted after we rewrite the code
        int IMyInventoryOwner.InventoryCount
        {
            get { return 1; }
        }

        string IMyInventoryOwner.DisplayNameText
        {
            get { return Container.Entity.DisplayName; }
        }

        MyInventory IMyInventoryOwner.GetInventory(int index)
        {
            return this;
        }

        public void SetInventory(MyInventory inventory, int index)
        {

        }

        MyInventoryOwnerTypeEnum IMyInventoryOwner.InventoryOwnerType
        {
            get { return MyInventoryOwnerTypeEnum.Character; } // as this is the most common now
        }

        long IMyInventoryOwner.EntityId
        {
            get { return Container.Entity.EntityId; }
        }

        bool IMyInventoryOwner.UseConveyorSystem
        {
            get
            {
                return false;
            }
            set
            {
            }
        }

        int ModAPI.Interfaces.IMyInventoryOwner.InventoryCount
        {
            get { return 1; }
        }

        IMyInventory ModAPI.Interfaces.IMyInventoryOwner.GetInventory(int index)
        {
            return this;
        }

        long ModAPI.Interfaces.IMyInventoryOwner.EntityId
        {
            get { return Container.Entity.EntityId; }
        }

        bool ModAPI.Interfaces.IMyInventoryOwner.UseConveyorSystem
        {
            get
            {
                return false;
            }
            set
            {
            }
        }

        public override void OnAddedToContainer()
        {
            Debug.Assert(m_owner == null || m_owner == Container.Entity, "Owner is supposed to be null before added to container or set to proper entity");
            bool wasNull = m_owner == null;

            base.OnAddedToContainer();
            m_owner = Container.Entity as IMyInventoryOwner;

            var handler = OnCreated;
            if (handler != null && wasNull) handler(this);
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

        public float GetPriority(MyClientStateBase client)
        {
            return GetPriorityStateGroup(client);
        }

        public float GetPriorityStateGroup(MyClientStateBase client)
        {
            MyClientState state = client as MyClientState;
            if (state.Context == Sandbox.Engine.Multiplayer.MyClientState.MyContextKind.Inventory || state.Context == Sandbox.Engine.Multiplayer.MyClientState.MyContextKind.Production)
            {
                if (state.ContextEntity != null)
                {
                    MyCubeGrid parent = state.ContextEntity.GetTopMostParent() as MyCubeGrid;

                    if (parent != null)
                    {
                        foreach (var block in parent.GridSystems.TerminalSystem.Blocks)
                        {
                            if (block == m_owner)
                            {
                                if (state.Context == Sandbox.Engine.Multiplayer.MyClientState.MyContextKind.Production && (block is MyAssembler) == false)
                                {
                                    continue;
                                }
                                return 1.0f;
                            }
                        }
                    }
                }
            }
            return 0;
        }

        [Event, Reliable, Server]
        private void InventoryTransferItem_Implementation(MyFixedPoint amount, uint itemId, long destinationOwnerId, byte destInventoryIndex, int destinationIndex)
        {
            if (!MyEntities.EntityExists(destinationOwnerId)) return;

            IMyInventoryOwner destOwner = MyEntities.GetEntityById(destinationOwnerId) as IMyInventoryOwner;
            MyInventory dst = destOwner.GetInventory(destInventoryIndex);

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
            RemoveItems(itemIndex, amount, true, true);
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

            if (entity.Components != null)
            {
                var statComp = entity.Components.Get<MyEntityStatComponent>() as MyCharacterStatComponent;
                if (statComp != null)
                {
                    var definition = MyDefinitionManager.Static.GetDefinition(itemId) as MyConsumableItemDefinition;
                    statComp.Consume(amount, definition);
                    var character = entity as MyCharacter;
                    if (character != null)
                        character.SoundComp.StartSecondarySound(definition.EatingSound, true);
                }
            }

            RemoveItemsOfType(amount, itemId);
        }

        public void ConsumeItem(MyDefinitionId itemId, MyFixedPoint amount, long consumerEntityId = 0)
        {
            SerializableDefinitionId serializableID = itemId;
            MyMultiplayer.RaiseEvent(this, x => x.InventoryConsumeItem_Implementation, amount, serializableID, consumerEntityId);
        }
    }
}
