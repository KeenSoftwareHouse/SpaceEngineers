#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.World;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using VRage;
using VRage.ObjectBuilders;

#endregion

namespace Sandbox.Game
{
    [Flags]
    public enum MyInventoryFlags
    {
        CanReceive = 1,
        CanSend = 2
    }

    public partial class MyInventory
    {

        #region Fields

        List<MyPhysicalInventoryItem> m_items = new List<MyPhysicalInventoryItem>();

        //in m3 (1dm3 = 0.001m3, 1m3 = 1000dm3)
        MyFixedPoint m_maxMass = MyFixedPoint.MaxValue;
        MyFixedPoint m_maxVolume = MyFixedPoint.MaxValue; //stored in dm3 / litres because of floating errors
        MyFixedPoint m_currentVolume = 0;   //stored in dm3 because of floating errors
        MyFixedPoint m_currentMass = 0;

        //in meters
        Vector3 m_size = new Vector3(float.MaxValue);

        MyInventoryFlags m_flags;

        IMyInventoryOwner m_owner;

        static MySyncInventory SyncObject;

        public object UserData;

        //Use NextItemID
        private uint m_nextItemID = 0;
        //Autoincrements
        private uint NextItemID
        {
            get { return m_nextItemID++; }
        }

        #endregion

        #region Init

        public MyInventory(float maxVolume, Vector3 size, MyInventoryFlags flags, IMyInventoryOwner owner)
            : this((MyFixedPoint)maxVolume, MyFixedPoint.MaxValue, size, flags, owner)
        {
        }

        public MyInventory(float maxVolume, float maxMass, Vector3 size, MyInventoryFlags flags, IMyInventoryOwner owner)
            : this((MyFixedPoint)maxVolume,(MyFixedPoint)maxMass, size, flags, owner)
        {
        }

        public MyInventory(MyFixedPoint maxVolume, MyFixedPoint maxMass, Vector3 size, MyInventoryFlags flags, IMyInventoryOwner owner)
        {
            m_maxVolume = MyPerGameSettings.ConstrainInventory() ? maxVolume * MySession.Static.InventoryMultiplier : MyFixedPoint.MaxValue;
            m_maxMass = maxMass;
            m_size = size;
            m_flags = flags;
            m_owner = owner;

            Clear();

            SyncObject = new MySyncInventory();

            ContentsChanged += OnContentsChanged;
        }

        #endregion

        #region Properties

        public MyFixedPoint MaxMass // in kg
        {
            get { return m_maxMass; }
        }

        public MyFixedPoint MaxVolume // in m3
        {
            get { return m_maxVolume; }
        }

        public MyFixedPoint CurrentVolume // in m3
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

        public MyFixedPoint CurrentMass
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
            get { return m_owner; }
        }

        public byte InventoryIdx
        {
            get
            {
                for (byte i = 0; i < Owner.InventoryCount; i++)
                {
                    if (Owner.GetInventory(i).Equals(this))
                    {
                        return i;
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

        public bool IsFull
        {
            get { return m_currentVolume >= m_maxVolume || m_currentMass >= m_maxMass ; }
        }

        #endregion

        #region Items

        public bool CanItemsBeAdded(MyFixedPoint amount, MyDefinitionId contentId)
        {
            float volume, mass;
            if (!GetVolumeAndMass(ref contentId, out volume, out mass))
            {
                return false;
            }

            if (MyPerGameSettings.ConstrainInventory() && amount * volume + m_currentVolume > m_maxVolume)
            {
                return false;
            }

            return CheckConstraint(contentId);
        }

        private static bool GetVolumeAndMass(ref MyDefinitionId contentId, out float volume, out float mass)
        {
            volume = mass = 0;
            MyPhysicalItemDefinition definition;
            MyCubeBlockDefinition blockDef;
            if (MyDefinitionManager.Static.TryGetPhysicalItemDefinition(contentId, out definition))
            {
                volume = definition.Volume;
                mass = definition.Mass;
                return true;
            }
            else if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(contentId, out blockDef))
            {
                volume = (blockDef.Size * MyDefinitionManager.Static.GetCubeSize(blockDef.CubeSize)).Volume;
                if (MyDestructionData.Static != null && Sync.IsServer)
                {
                    mass = MyDestructionData.Static.GetBlockMass(blockDef.Model, blockDef);
                }
                else
                {
                    mass = blockDef.Mass;
                }
                return true;
            }
            return false;
        }

        public MyFixedPoint ComputeAmountThatFits(MyDefinitionId contentId)
        {
            if (!MyPerGameSettings.ConstrainInventory())
                return MyFixedPoint.MaxValue;
            float volume, mass;
            if (!GetVolumeAndMass(ref contentId, out volume, out mass))
                return 0;
            var amountThatFitsVolume = MyFixedPoint.Max((MyFixedPoint)((float)m_maxVolume - (float)m_currentVolume) * (1.0f / (float)volume), 0);
            var amountThatFitsMass = MyFixedPoint.Max((MyFixedPoint)(((float)m_maxMass - (float)m_currentMass) * (1.0f / (float)mass)), 0);
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

            var amountThatFits = (m_maxVolume - m_currentVolume) * (1.0f / blueprint.OutputVolume);
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
            return ContainItems(amount, ob.GetObjectId());
        }

        public MyFixedPoint GetItemAmount(MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None)
        {
            MyFixedPoint amount = 0;
            foreach (var item in m_items)
            {
                if (item.Content.GetObjectId() == contentId &&
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
            int? itemPos = FindFirstPositionOfType(contentId, flags);
            if (!itemPos.HasValue)
                return false;

            int i = itemPos.Value;
            MyFixedPoint amountPresent = m_items[i].Amount;
            if (!amount.HasValue || amountPresent >= amount)
                return true;

            for (i++; i < m_items.Count; ++i)
            {
                var content = m_items[i].Content;
                if (contentId == content.GetObjectId() &&
                    flags == content.Flags)
                {
                    amountPresent += m_items[i].Amount;
                }
            }

            return amountPresent >= amount;
        }

        public void TakeFloatingObject(MyFloatingObject obj)
        {
            MyFixedPoint amount = obj.Item.Amount;
            if (MyPerGameSettings.ConstrainInventory())
                amount = MyFixedPoint.Min(ComputeAmountThatFits(obj.Item.Content.GetObjectId()), amount);
            if (amount > 0)
            {
                if (Sync.IsServer)
                {
                    if (obj.MarkedForClose)
                        return;
                    MyFloatingObjects.RemoveFloatingObject(obj, amount);
                    AddItemsInternal(amount, obj.Item.Content);
                    SyncObject.SendAddItemsAnnounce(this, amount, obj.Item.Content);
                }
                else
                    SyncObject.TakeFloatingObjectRequest(this, obj);
            }
        }

        public bool AddGrid(MyCubeGrid grid)
        {
            HashSet<Sandbox.Game.Entities.Cube.MySlimBlock> lst = new HashSet<Sandbox.Game.Entities.Cube.MySlimBlock>();
            foreach(var b in grid.GetBlocks())
            {
                if(b.FatBlock is MyCompoundCubeBlock)
                {
                    foreach(var subb in (b.FatBlock as MyCompoundCubeBlock).GetBlocks())
                    {
                        if (AddBlock(subb))
                            lst.Add(b);
                    }
                }
                else
                {
                    if (AddBlock(b))
                        lst.Add(b);
                }
            }
            foreach(var b in lst)
            {
                grid.RemoveBlock(b, true);
            }
            return lst.Count > 0;
            //grid.Close();
        }

        private bool AddBlock(Entities.Cube.MySlimBlock block)
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
        public bool AddItems(MyFixedPoint amount, MyObjectBuilder_Base objectBuilder, int index = -1)
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
                AddItemsInternal(amount, physicalObjectBuilder, index);
                SyncObject.SendAddItemsAnnounce(this, amount, physicalObjectBuilder, index);
            }
            else
                SyncObject.SendAddItemsRequest(this, index, amount, physicalObjectBuilder);
            return true;
        }

        private void AffectAddBySurvival(ref MyFixedPoint amount, MyObjectBuilder_PhysicalObject objectBuilder)
        {
            Debug.Assert(Sync.IsServer);
            MyFixedPoint space = ComputeAmountThatFits(objectBuilder.GetObjectId());
            if (space < amount)
            {
                if (Owner.InventoryOwnerType == MyInventoryOwnerTypeEnum.Character)
                {
                    MyCharacter c = (Owner as MyCharacter);
                    Matrix m = c.GetHeadMatrix(true);
                    MyEntity entity = MyFloatingObjects.Spawn(new MyPhysicalInventoryItem(amount - space, objectBuilder), m.Translation, m.Forward, m.Up, c.Physics);
                    entity.Physics.ApplyImpulse(m.Forward.Cross(m.Up), c.PositionComp.GetPosition());
                }
                amount = space;
            }
        }

        public void AddItemsInternal(MyFixedPoint amount, MyObjectBuilder_PhysicalObject objectBuilder, int index = -1, uint? itemId = null)
        {
            Debug.Assert(amount > 0, "Adding 0 amount of item.");

            var newItem = new MyPhysicalInventoryItem() { Amount = amount, Content = objectBuilder };

            if (index >= 0 && index < m_items.Count)
            {
                if (m_items[index].Content.CanStack(objectBuilder))
                {
                    newItem.Amount += m_items[index].Amount;
                    newItem.ItemId = m_items[index].ItemId;
                    m_items[index] = newItem;
                }
                else
                {
                    newItem.ItemId = NextItemID;
                    m_items.Insert(index, newItem);
                }
            }
            else
            {
                bool add = true;
                bool canStackWithItself = newItem.Content.CanStack(newItem.Content);
                if (index < 0 && canStackWithItself)
                {
                    int? itemPos = FindFirstStackablePosition(objectBuilder);
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
                    if (canStackWithItself)
                    {
                        newItem.ItemId = itemId.HasValue ? itemId.Value : NextItemID;
                        m_items.Add(newItem);
                    }
                    else
                    {
                        var targetAmount = newItem.Amount;
                        newItem.Amount = 1;
                        for (MyFixedPoint addedAmount = 0; addedAmount < targetAmount; addedAmount += 1)
                        {
                            newItem.ItemId = itemId.HasValue ? itemId.Value : NextItemID;
                            itemId = null; // so we use NextItemID next time
                            m_items.Add(newItem);
                            newItem.Content = newItem.Content.Clone() as MyObjectBuilder_PhysicalObject;
                            Debug.Assert(newItem.Content != null);
                        }
                    }
                }
            }

            RefreshVolumeAndMass();

            VerifyIntegrity();

            if (ContentsChanged != null)
                ContentsChanged(this);
        }

        public void RemoveItemsOfType(MyFixedPoint amount, MyObjectBuilder_PhysicalObject objectBuilder, bool spawn = false)
        {
            TransferOrRemove(this, amount, objectBuilder.GetObjectId(), objectBuilder.Flags, null, spawn);
        }

        public void RemoveItemsOfType(MyFixedPoint amount, MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None, bool spawn = false)
        {
            TransferOrRemove(this, amount, contentId, flags, null, spawn);
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
            var am = amount.HasValue ? amount.Value : (item.HasValue? item.Value.Amount : 1);
            MyEntity spawned = null;
            if (Sync.IsServer)
            {
                
                if (item.HasValue && RemoveItemsInternal(itemId, am, sendEvent))
                {
                    if (spawn)
                    {
                        var owner = Owner as MyEntity;
                        if(!spawnPos.HasValue)
                            spawnPos = MatrixD.CreateWorld(owner.PositionComp.GetPosition() + owner.PositionComp.WorldMatrix.Forward + owner.PositionComp.WorldMatrix.Up, owner.PositionComp.WorldMatrix.Forward, owner.PositionComp.WorldMatrix.Up);
                        spawned = item.Value.Spawn(am, spawnPos.Value, owner);
                    }
                    SyncObject.SendRemoveItemsAnnounce(this, am, itemId);
                }
            }
            else
                SyncObject.SendRemoveItemsRequest(this, am, itemId, spawn);
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

            if (sendEvent && ContentsChanged != null)
                ContentsChanged(this);
            return true;
        }



        public List<MyPhysicalInventoryItem> GetItems()
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

        private static void TransferOrRemove(MyInventory src, MyFixedPoint? amount, MyDefinitionId contentId, MyItemFlags flags = MyItemFlags.None, MyInventory dst = null, bool spawn = false)
        {
            //Debug.Assert(!amount.HasValue || amount.Value > 0, "Transfering 0 amount of item.");
            if (src.ContainItems(amount, contentId, flags))
            {
                bool transferAll = !amount.HasValue;
                MyFixedPoint remainingAmount = transferAll ? 0 : amount.Value;
                
                //TODO(AF) Remove oxygen specific code from inventory.
                //Will be fixed once MyInventory will support Entities.
                // If the requested item is an oxygen container, do a preliminary loop to pull any non-full items first.
                if (contentId.TypeId == typeof(MyObjectBuilder_OxygenContainerObject))
                {
                    int k = 0;
                    while (k < src.m_items.Count)
                    {
                        if (!transferAll && remainingAmount == 0)
                            break;

                        MyPhysicalInventoryItem item = src.m_items[k];
                        
                        // Skip full oxygen bottles in this loop.  They will not be skipped in the next one.
                        var oxygenBottle = item.Content as MyObjectBuilder_OxygenContainerObject;
                        if (oxygenBottle != null && oxygenBottle.OxygenLevel == 1f)
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
                            remainingAmount -= item.Amount;
                            Transfer(src, dst, item.ItemId, -1, spawn: spawn);
                        }
                        else
                        {
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

                    if (item.Content.GetObjectId() != contentId)
                    {
                        i++;
                        continue;
                    }

                    if (transferAll || remainingAmount >= item.Amount)
                    {
                        remainingAmount -= item.Amount;
                        Transfer(src, dst, item.ItemId, -1, spawn: spawn);
                    }
                    else
                    {
                        Transfer(src, dst, item.ItemId, -1, remainingAmount, spawn);
                        remainingAmount = 0;
                    }
                }
            }
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

        public static void Transfer(MyInventory src, MyInventory dst, uint srcItemId, int dstIdx = -1, MyFixedPoint? amount = null, bool spawn = false)
        {
            var itemNullable = src.GetItemByID(srcItemId);
            if (!itemNullable.HasValue)
                return;

            var item = itemNullable.Value;
            if (dst != null && !dst.CheckConstraint(item.Content.GetObjectId()))
                return;
            var transferAmount = amount ?? item.Amount;

            SyncObject.TransferItems(src, transferAmount, srcItemId, dst, dstIdx, spawn);
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

            if (somethingRemoved && ContentsChanged != null)
                ContentsChanged(this);

            return somethingRemoved;
        }

        public bool IsItemAt(int position)
        {
            return m_items.IsValidIndex(position);
        }
        #endregion

        #region Serialization

        public MyObjectBuilder_Inventory GetObjectBuilder()
        {
            var objBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Inventory>();
            objBuilder.Items.Clear();

            objBuilder.nextItemId = m_nextItemID;

            foreach (var item in m_items)
                objBuilder.Items.Add(item.GetObjectBuilder());

            return objBuilder;
        }

        public void Init(MyObjectBuilder_Inventory objectBuilder)
        {
            Clear();

            if (objectBuilder == null)
                return;
            if (!Sync.IsServer)
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

                var contentId = item.PhysicalContent.GetObjectId();

                if (!CanItemsBeAdded(item.Amount, contentId))
                    continue;

                var canStackWithItself = item.PhysicalContent.CanStack(item.PhysicalContent);
                if (!canStackWithItself)
                {
                    MyFixedPoint added = 0;
                    while (added < item.Amount)
                    {
                        AddItemsInternal(1, item.PhysicalContent, i, itemId: (Sync.IsServer) ? null : (uint?)item.ItemId);
                        added += 1;
                        ++i;
                    }
                }
                else
                {
                    if (Sync.IsServer)
                        AddItemsInternal(item.Amount, item.PhysicalContent, i);
                    else
                        //Building from information recieved from server - dont send msg about adding this
                        AddItemsInternal(item.Amount, item.PhysicalContent, i, itemId: item.ItemId);
                }
                i++;
            }
            VerifyIntegrity();
        }

        #endregion

        public event Action<MyInventory> ContentsChanged;
        public event Action<IMyComponentInventory, IMyInventoryOwner> OwnerChanged;
        private Action<IMyComponentInventory> ComponentContentsChanged;

        private void RefreshVolumeAndMass()
        {
            m_currentMass = 0;
            m_currentVolume = 0;
            foreach (var item in m_items)
            {
                Debug.Assert(item.Amount > 0);
                float volume, mass;
                var id = item.Content.GetObjectId();
                if (!GetVolumeAndMass(ref id, out volume, out mass))
                {
                    Debug.Assert(item.Content.SubtypeName == "Potassium" || item.Content.SubtypeName == "Ice_01", string.Format("Missing definition for '{0}/{1}'", item.Content.TypeId, item.Content.SubtypeName));
                    continue;
                }

                m_currentVolume += volume * item.Amount;
                m_currentMass += mass * item.Amount;
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
        public void UpdateOxygenAmount()
        {
            RefreshVolumeAndMass();

            if (ContentsChanged != null)
                ContentsChanged(this);
        }

        internal void SyncOxygenContainerLevel(uint itemId, float level)
        {
            SyncObject.UpdateOxygenLevel(this, level, itemId);
        }
        #endregion

    }
}
