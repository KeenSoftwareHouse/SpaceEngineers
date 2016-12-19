using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;

using VRageMath;
using Sandbox.Game.World;
using Sandbox.Game.Multiplayer;
using Sandbox.Engine.Multiplayer;
using ProtoBuf;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems.Conveyors;
using VRage;
using SteamSDK;
using Sandbox.Game.Localization;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using Sandbox.ModAPI;
using Sandbox.Game.Entities.Inventory;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Ingame;
using VRage.Network;
using VRage.Profiler;
using VRage.Sync;
using VRage.Utils;

namespace Sandbox.Game.Entities.Cube
{
    /// <summary>
    /// Common base for Assembler and Refinery blocks
    /// </summary>
    public abstract class MyProductionBlock : MyFunctionalBlock, IMyConveyorEndpointBlock, IMyProductionBlock, IMyInventoryOwner
    {
        protected MySoundPair m_processSound = new MySoundPair();

        public struct QueueItem
        {
            public MyFixedPoint Amount;
            public MyBlueprintDefinitionBase Blueprint;
            public uint ItemId;
        }

	    #region Fields

        protected List<QueueItem> m_queue;
        protected MyProductionBlockDefinition ProductionBlockDefinition
        {
            get { return (MyProductionBlockDefinition)base.BlockDefinition; }
        }

        private MyInventoryAggregate m_inventoryAggregate;
        public MyInventoryAggregate InventoryAggregate
        {
            get
            {                          
                return m_inventoryAggregate;
            }           
            set
            {
                if (value != null)
                {
                    Components.Remove<MyInventoryBase>();
                    Components.Add<MyInventoryBase>(value);
                    Debug.Assert(m_inventoryAggregate == value, "Aggregate wasn't added");
                }
                else
                {
                    Debug.Fail("Null value passed!");
                }
            }
        }

        public  MyInventory InputInventory
        {
            get
            {
                return m_inputInventory;
            }
            protected set
            {
                if (!InventoryAggregate.ChildList.Contains(value))
                {
                    if (m_inputInventory != null)
                    {
                        Debug.Assert(InventoryAggregate.ChildList.Contains(m_inputInventory), "Input inventory got lost! It is not in the inventory aggregate!");
                        InventoryAggregate.ChildList.RemoveComponent(m_inputInventory);
                    }
                    InventoryAggregate.AddComponent(value);
                }
                Debug.Assert(InventoryCount <= 2, "Invalid insertion - too many inventories in aggregate");
                Debug.Assert(m_inputInventory != null, "Input inventory wasn't set!");
            }
        }

        public MyInventory OutputInventory
        {
            get
            {
                return m_outputInventory;
            }
            protected set
            {
                if (!InventoryAggregate.ChildList.Contains(value))
                {
                    if (m_outputInventory != null)
                    {
                        Debug.Assert(InventoryAggregate.ChildList.Contains(m_outputInventory), "Output inventory got lost! It is not in the inventory aggregate!");
                        InventoryAggregate.ChildList.RemoveComponent(m_outputInventory);
                    }
                    InventoryAggregate.AddComponent(value);
                }                
                Debug.Assert(InventoryCount <= 2, "Invalid insertion - too many inventories in aggregate");
                Debug.Assert(m_outputInventory != null, "Output inventory wasn't set!");
            }
        }

        private MyInventory m_inputInventory;
        private MyInventory m_outputInventory;
        private int m_lastUpdateTime;
        private bool m_isProducing;

        // A helper variable used by children for rebuilding the queues
        protected static Dictionary<MyDefinitionId, MyFixedPoint> m_tmpInventoryCounts = new Dictionary<MyDefinitionId, MyFixedPoint>();

        #endregion Fields

        #region Properties

        public event Action StartedProducing;

        public event Action StoppedProducing;

        public bool IsQueueEmpty
        {
            get { return m_queue.Count == 0; }
        }

        /// <summary>
        /// Set by subclasses indicating whether they are working on something or not.
        /// Determines required power input.
        /// </summary>
        public bool IsProducing
        {
            get { return m_isProducing; }
            protected set
            {
                if (m_isProducing != value)
                {
                    m_isProducing = value;
                    if (value)
                        OnStartProducing();
                    else
                        OnStopProducing();
                    UpdatePower();
                }
            }
        }

        protected virtual void UpdatePower()
        {
			ResourceSink.Update();
        }

        /// <summary>
        /// Do not make changes to values in this enumerable. Consider it const reference.
        /// </summary>
        public IEnumerable<QueueItem> Queue
        {
            get { return m_queue; }
        }

        protected override bool CheckIsWorking()
        {
            return ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) && base.CheckIsWorking();
        }

        #endregion Properties

        public event Action<MyProductionBlock> QueueChanged;
        private string m_string;
        protected readonly Sync<bool> m_useConveyorSystem;
        private IMyConveyorEndpoint m_multilineConveyorEndpoint;

        //Use NextItemID
        private uint m_nextItemId = 0;
        //Autoincrements
        public uint NextItemId
        {
            get { return m_nextItemId++; }
        }

        #region Init

        public MyProductionBlock()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_useConveyorSystem = SyncType.CreateAndAddProp<bool>();
#endif // XB1
            CreateTerminalControls();

            m_soundEmitter = new MyEntity3DSoundEmitter(this, true);
            m_queue = new List<QueueItem>();

            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;

            IsProducing = false;
            Components.ComponentAdded += OnComponentAdded;
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyProductionBlock>())
                return;
            base.CreateTerminalControls();
            var useConveyorSystem = new MyTerminalControlOnOffSwitch<MyProductionBlock>("UseConveyor", MySpaceTexts.Terminal_UseConveyorSystem);
            useConveyorSystem.Getter = (x) => x.UseConveyorSystem;
            useConveyorSystem.Setter = (x, v) => x.UseConveyorSystem = v;
            useConveyorSystem.EnableToggleAction();
            MyTerminalControlFactory.AddControl(useConveyorSystem);
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;

            var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(ProductionBlockDefinition.ResourceSinkGroup, ProductionBlockDefinition.OperationalPowerConsumption, ComputeRequiredPower);
            sinkComp.IsPoweredChanged += Receiver_IsPoweredChanged;
            ResourceSink = sinkComp;

            base.Init(objectBuilder, cubeGrid);

            var ob = (MyObjectBuilder_ProductionBlock)objectBuilder;

            if (InventoryAggregate == null)
            {
                InventoryAggregate = new MyInventoryAggregate();
            }

            if (InputInventory == null)
            {
                InputInventory = new MyInventory(
                    ProductionBlockDefinition.InventoryMaxVolume,
                    ProductionBlockDefinition.InventorySize,
                    MyInventoryFlags.CanReceive);
                if (ob.InputInventory != null)
                    InputInventory.Init(ob.InputInventory);                
            }

            Debug.Assert(InputInventory.Owner == this, "Ownership was not set!");

            if (OutputInventory == null)
            {
                OutputInventory = new MyInventory(
                    ProductionBlockDefinition.InventoryMaxVolume,
                    ProductionBlockDefinition.InventorySize,
                    MyInventoryFlags.CanSend);
                if (ob.OutputInventory != null)
                    OutputInventory.Init(ob.OutputInventory);
            }

            Debug.Assert(OutputInventory.Owner == this, "Ownership was not set!");

            m_nextItemId = ob.NextItemId;
            bool nextIdWasZero = m_nextItemId == 0;

            base.IsWorkingChanged += CubeBlock_IsWorkingChanged;

			
			ResourceSink.Update();

			AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawPowerReciever(ResourceSink, this));

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            if (ob.Queue != null)
            {
                m_queue.Clear();
                if (m_queue.Capacity < ob.Queue.Length)
                    m_queue.Capacity = ob.Queue.Length;
                for (int i = 0; i < ob.Queue.Length; ++i)
                {
                    var item = ob.Queue[i];
                    Debug.Assert(item.ItemId != null || nextIdWasZero, "Item index was null while next id for production block was given non-zero. This is inconsistency.");

                    var deserializedItem = DeserializeQueueItem(item);
                    Debug.Assert(deserializedItem.Blueprint != null, "Could not add item into production block's queue: Blueprint was not found.");
                    if (deserializedItem.Blueprint != null)
                    {
                        m_queue.Add(deserializedItem);
                    }
                    else
                    {
                        MySandboxGame.Log.WriteLine(string.Format("Could not add item into production block's queue: Blueprint {0} was not found.", item.Id));
                    }
                }

                UpdatePower();
            }

            m_useConveyorSystem.Value = ob.UseConveyorSystem;

            m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = (MyObjectBuilder_ProductionBlock)base.GetObjectBuilderCubeBlock(copy);

            ob.InputInventory  = InputInventory.GetObjectBuilder();
            ob.OutputInventory = OutputInventory.GetObjectBuilder();
            ob.UseConveyorSystem = m_useConveyorSystem;
            ob.NextItemId = m_nextItemId;

            if (m_queue.Count > 0)
            {
                ob.Queue = new MyObjectBuilder_ProductionBlock.QueueItem[m_queue.Count];
                for (int i = 0; i < m_queue.Count; ++i)
                {
                    ob.Queue[i].Id     = m_queue[i].Blueprint.Id;
                    ob.Queue[i].Amount = m_queue[i].Amount;
                    ob.Queue[i].ItemId = m_queue[i].ItemId;
                }
            }
            else
                ob.Queue = null;
            return ob;
        }

        void ComponentStack_IsFunctionalChanged()
        {
			ResourceSink.Update();
        }

        #endregion Init

        public bool CanUseBlueprint(MyBlueprintDefinitionBase blueprint)
        {
            foreach (var blueprintClass in ProductionBlockDefinition.BlueprintClasses)
                if (blueprintClass.ContainsBlueprint(blueprint)) return true;
            return false;
        }

        protected void InitializeInventoryCounts(bool inputInventory = true)
        {
            m_tmpInventoryCounts.Clear();
            foreach (var item in inputInventory ? InputInventory.GetItems() : OutputInventory.GetItems())
            {
                MyFixedPoint count = 0;
                MyDefinitionId itemId = new MyDefinitionId(item.Content.TypeId, item.Content.SubtypeId);
                m_tmpInventoryCounts.TryGetValue(itemId, out count);
                m_tmpInventoryCounts[itemId] = count + item.Amount;
            }
        }

        #region Multiplayer

        /// <summary>
        /// Sends request to server to add item to queue. (Can be also called on server. In that case it will be local)
        /// </summary>
        /// <param name="blueprint"></param>
        /// <param name="ammount"></param>
        /// <param name="idx">idx - index to insert (-1 = last).</param>
        public void AddQueueItemRequest(MyBlueprintDefinitionBase blueprint, MyFixedPoint ammount, int idx = -1)
        {

            SerializableDefinitionId serializableId = blueprint.Id;

            MyMultiplayer.RaiseEvent(this, x => x.OnAddQueueItemRequest, idx, serializableId, ammount);

        }

        [Event, Reliable, Server]
        private void OnAddQueueItemRequest(int idx, SerializableDefinitionId defId, MyFixedPoint ammount)
        {
            var blueprint = MyDefinitionManager.Static.GetBlueprintDefinition(defId);
            Debug.Assert(blueprint != null, "Blueprint not present in the dictionary.");
            if (blueprint != null)
            {
                this.InsertQueueItem(idx, blueprint, ammount);
                MyMultiplayer.RaiseEvent(this, x => x.OnAddQueueItemSuccess, idx, defId, ammount);
                
            }
        }

        [Event, Reliable, Broadcast]
        private void OnAddQueueItemSuccess(int idx, SerializableDefinitionId defId, MyFixedPoint ammount)
        {
            this.InsertQueueItem(idx, MyDefinitionManager.Static.GetBlueprintDefinition(defId), ammount);
        }


        /// <summary>
        /// Sends request to server to move queue item. (Can be also called on server. In that case it will be local)
        /// </summary>
        /// <param name="srcItemId"></param>
        /// <param name="dstIdx"></param>
        public void MoveQueueItemRequest(uint srcItemId, int dstIdx)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnMoveQueueItemCallback, srcItemId, dstIdx);
        }

        [Event, Reliable, Server, Broadcast]
        private void OnMoveQueueItemCallback(uint srcItemId, int dstIdx)
        {
            this.MoveQueueItem(srcItemId, dstIdx);
        }

        [Event, Reliable, Server]
        protected void ClearQueueRequest()
        {
            for (int i = m_queue.Count - 1; i >= 0; i--)
            {
                if (!RemoveQueueItemTests(i))
                    continue;

                MyFixedPoint ammount = 1;
                MyMultiplayer.RaiseEvent(this, x => x.OnRemoveQueueItem, i, ammount, 0f);
            }
        }

        /// <summary>
        /// Sends request to server to remove item from queue. (Can be also called on server. In that case it will be local)
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="amount"></param>
        /// <param name="progress"></param>
        public void RemoveQueueItemRequest(int idx, MyFixedPoint amount, float progress = 0f)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnRemoveQueueItemRequest, idx, amount, progress);
        }

        [Event, Reliable, Server]
        private void OnRemoveQueueItemRequest(int idx, MyFixedPoint amount, float progress)
        {
            if (!RemoveQueueItemTests(idx)) 
                return;

            MyMultiplayer.RaiseEvent(this, x => x.OnRemoveQueueItem, idx, amount, progress);

        }

        private bool RemoveQueueItemTests(int idx)
        {
            Debug.Assert(idx != -2, "No longer supported.");

            if (!this.m_queue.IsValidIndex(idx) && idx != -1)
            {
                MySandboxGame.Log.WriteLine("Invalid queue index in the remove item message!");
                return false;
            }

            return true;
        }

        [Event, Reliable, Server, Broadcast]
        private void OnRemoveQueueItem(int idx, MyFixedPoint amount, float progress)
        {
            if (idx >= 0)
                this.RemoveQueueItem(idx);
            else
                this.RemoveFirstQueueItem(amount, progress);
        }

        #endregion

        #region Queue manipulation

        protected virtual void OnQueueChanged()
        {
            if (QueueChanged != null)
                QueueChanged(this);
        }

        private QueueItem DeserializeQueueItem(MyObjectBuilder_ProductionBlock.QueueItem itemOb)
        {
            QueueItem item = new QueueItem();
            
            item.Amount = itemOb.Amount;
            // Try to get the blueprint by its definition ID
            if (MyDefinitionManager.Static.HasBlueprint(itemOb.Id))
            {
                item.Blueprint = MyDefinitionManager.Static.GetBlueprintDefinition(itemOb.Id);
            }
            // Otherwise, we're dealing with an old save, so we get the blueprint by the result's definition ID
            else
            {
                item.Blueprint = MyDefinitionManager.Static.TryGetBlueprintDefinitionByResultId(itemOb.Id);
            }
            item.ItemId = itemOb.ItemId.HasValue ? itemOb.ItemId.Value : NextItemId;
            return item;
        }

        /// <summary>
        /// Removes specified amount from the first item in the queue. This is called by 
        /// subclasses (eg. assembler puts an item into its current production, refinery
        /// processes some ore, etc.) in their own specific situations.
        /// </summary>
        /// <param name="amount">Amount to be removed.</param>
        /// <param name="progress">Current progress of he processed item.</param>
        protected void RemoveFirstQueueItemAnnounce(MyFixedPoint amount, float progress = 0f)
        {
            Debug.Assert(Sync.IsServer);
            this.RemoveQueueItemRequest(-1, amount, progress);
        }

        protected virtual void RemoveFirstQueueItem(MyFixedPoint amount, float progress = 0f)
        {
        //    Debug.Assert(m_queue.IsValidIndex(0), "Index out of bounds.");

            if (!m_queue.IsValidIndex(0))
                return;

            QueueItem queueItem = m_queue[0];
            amount = MathHelper.Clamp(amount, 0, queueItem.Amount);

            // Cannot use RemoveQueueItem() because it calls UpdateProduction(), which may
            // in turn call RemoveFinishedQueueItem().
            queueItem.Amount -= amount;
            m_queue[0] = queueItem;

            Debug.Assert(queueItem.Amount >= 0, "Amount of queue item went below 0");
            if (queueItem.Amount <= 0)
            {
                //possibly not needed?
                var ass = this as MyAssembler;
                if (ass != null)
                {
                    ass.CurrentProgress = 0f;
                }
                m_queue.RemoveAt(0);
            }

            UpdatePower();

            OnQueueChanged();
        }

        public void InsertQueueItemRequest(int idx, MyBlueprintDefinitionBase blueprint)
        {
            InsertQueueItemRequest(idx, blueprint, 1);
        }

        public void InsertQueueItemRequest(int idx, MyBlueprintDefinitionBase blueprint, MyFixedPoint amount)
        {
            this.AddQueueItemRequest(blueprint, amount, idx); 
        }

        protected virtual void InsertQueueItem(int idx, MyBlueprintDefinitionBase blueprint, MyFixedPoint amount)
        {
            Debug.Assert(idx <= m_queue.Count);
            Debug.Assert(CanUseBlueprint(blueprint));
            Debug.Assert(amount > 0);

            QueueItem item = new QueueItem();
            item.Amount = amount;
            item.Blueprint = blueprint;

            if (CanUseBlueprint(item.Blueprint))
            {
                if (m_queue.IsValidIndex(idx) && m_queue[idx].Blueprint == item.Blueprint)
                {
                    // Increase amount if there is same kind of item at this index.
                    item.Amount += m_queue[idx].Amount;
                    item.ItemId = m_queue[idx].ItemId;
                    m_queue[idx] = item;
                }
                else if (m_queue.Count > 0 && (idx >= m_queue.Count || idx == -1) && m_queue[m_queue.Count - 1].Blueprint == item.Blueprint)
                {
                    // Add to the last item in the queue if it is the same.
                    item.Amount += m_queue[m_queue.Count - 1].Amount;
                    item.ItemId = m_queue[m_queue.Count - 1].ItemId;
                    m_queue[m_queue.Count - 1] = item;
                }
                else
                {
                    if (idx == -1)
                        idx = m_queue.Count;

                    if (idx > m_queue.Count)
                    {
                        MyLog.Default.WriteLine("Production block.InsertQueueItem: Index out of bounds, desync!");
                        idx = m_queue.Count;
                    }

                    // Reset timer when adding first item. Otherwise we might produce it faster than possible.
                    if (m_queue.Count == 0)
                        m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

                    // Put new item into the queue with given amount and type.
                    item.ItemId = NextItemId;
                    m_queue.Insert(idx, item);
                }

                UpdatePower();

                OnQueueChanged();
            }
        }

        public QueueItem GetQueueItem(int idx)
        {
            return m_queue[idx];
        }

        public QueueItem? TryGetQueueItem(int idx)
        {
            return m_queue.IsValidIndex(idx) ? m_queue[idx] : (QueueItem?)null;
        }

        public QueueItem? TryGetQueueItemById(uint itemId)
        {
            for (int i = 0; i < m_queue.Count; ++i)
            {
                if (m_queue[i].ItemId == itemId)
                {
                    return m_queue[i];
                }
            }

            return null;
        }

        protected virtual void RemoveQueueItem(int itemIdx)
        {
            if(itemIdx >= m_queue.Count)
            {
                Debug.Fail("Index out of bounds!");
                VRage.Utils.MyLog.Default.WriteLine("Production block.RemoveQueueItem: Index out of bounds!");
                return;
            }
            m_queue.RemoveAt(itemIdx);

            UpdatePower();

            OnQueueChanged();
        }

        protected virtual void MoveQueueItem(uint queueItemId, int targetIdx)
        {
            Debug.Assert(targetIdx >= 0);

            for (int i = 0; i < m_queue.Count; ++i)
            {
                if (m_queue[i].ItemId == queueItemId)
                {
                    QueueItem item = m_queue[i];
                    targetIdx = Math.Min(m_queue.Count - 1, targetIdx);

                    if (i == targetIdx)
                        return;
                        
                    m_queue.RemoveAt(i);

                    int mergingIndex = -1;
                    if (m_queue.IsValidIndex(targetIdx - 1) && m_queue[targetIdx - 1].Blueprint == item.Blueprint)
                        mergingIndex = targetIdx - 1;
                    if (m_queue.IsValidIndex(targetIdx) && m_queue[targetIdx].Blueprint == item.Blueprint)
                        mergingIndex = targetIdx;

                    if (mergingIndex != -1)
                    {
                        QueueItem item2 = m_queue[mergingIndex];
                        item2.Amount += item.Amount;
                        m_queue[mergingIndex] = item2;
                    }
                    else
                    {
                        m_queue.Insert(targetIdx, item);
                    }
                    break;
                }
            }

            OnQueueChanged();
        }

        public QueueItem? TryGetFirstQueueItem()
        {
            return TryGetQueueItem(0);
        }

        public void ClearQueue(bool sendEvent = true)
        {
            if (!Sync.IsServer)
                return;

            this.ClearQueueRequest();

            //while (m_queue.Count > 0)
                //RemoveFirstQueueItemRequest(m_queue[0].Amount);
            if (sendEvent)
                OnQueueChanged();
        }

        /// <summary>
        /// Swaps this and other queue. This operation is not synchronized. Those using
        /// this method should take care of proper synchronization.
        /// </summary>
        protected void SwapQueue(ref List<QueueItem> otherQueue)
        {
            var tmp = m_queue;
            m_queue = otherQueue;
            otherQueue = tmp;
            OnQueueChanged();
        }

        #endregion Queue manipulation

        /// <summary>
        /// Note: The child production block should update the IsProducing flag in this method
        /// </summary>
        protected abstract void UpdateProduction(int timeDelta);

        public void UpdateProduction()
        {
            ProfilerShort.Begin("UpdateProduction");
            int currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            if (ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId))
            {
                UpdateProduction(currentTime - m_lastUpdateTime);
            }
            else
            {
                IsProducing = false;
            }
            m_lastUpdateTime = currentTime;
            ProfilerShort.End();
        }

        protected override void Closing()
        {
            if (m_soundEmitter != null)
                m_soundEmitter.StopSound(true);
            base.Closing();
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();
            UpdateProduction();
        }

        #region Inventory

        public override MyInventoryBase GetInventoryBase(int index = 0)
        {
            switch (index)
            {
                case 0: return InputInventory;
                case 1: return OutputInventory;
                default:
                    throw new InvalidBranchException();
            }
        }        

        public override void OnRemovedByCubeBuilder()
        {
            ReleaseInventory(InputInventory);
            ReleaseInventory(OutputInventory);
            base.OnRemovedByCubeBuilder();
        }

        public override void OnDestroy()
        {
            ReleaseInventory(InputInventory,true);
            ReleaseInventory(OutputInventory,true);
            base.OnDestroy();
        }

        bool UseConveyorSystem
        {
            get
            {
                return m_useConveyorSystem;
            }
            set
            {
                m_useConveyorSystem.Value = value;
            }
        }

        private void OnComponentAdded(Type type, VRage.Game.Components.MyEntityComponentBase component)
        {
            var aggregate = component as MyInventoryAggregate;
            if (aggregate != null)
            {                
                m_inventoryAggregate = aggregate;
                m_inventoryAggregate.BeforeRemovedFromContainer += OnInventoryAggregateRemoved;
                m_inventoryAggregate.OnAfterComponentAdd += OnInventoryAddedToAggregate;
                m_inventoryAggregate.OnBeforeComponentRemove += OnBeforeInventoryRemovedFromAggregate;

                foreach (var inventory in m_inventoryAggregate.ChildList.Reader)
                {
                    MyInventory inv = inventory as MyInventory;
                    OnInventoryAddedToAggregate(aggregate, inv);
                }
            }
        }       

        protected virtual void OnInventoryAddedToAggregate(MyInventoryAggregate aggregate, MyInventoryBase inventory)
        {
            if (m_inputInventory == null)
            {
                m_inputInventory = inventory as MyInventory;
            }
            else if (m_outputInventory == null)
            {
                m_outputInventory = inventory as MyInventory;
            }
            else
            {
                Debug.Fail("Adding inventory to aggregate, but input and output inventory is already set!");
            }
        }

        private void OnInventoryAggregateRemoved(MyEntityComponentBase component)
        {
            m_inputInventory = null;
            m_outputInventory = null;
            m_inventoryAggregate.BeforeRemovedFromContainer -= OnInventoryAggregateRemoved;
            m_inventoryAggregate.OnAfterComponentAdd -= OnInventoryAddedToAggregate;
            m_inventoryAggregate.OnBeforeComponentRemove -= OnBeforeInventoryRemovedFromAggregate;
            m_inventoryAggregate = null;
        }   

        protected virtual void OnBeforeInventoryRemovedFromAggregate(MyInventoryAggregate aggregate, MyInventoryBase inventory)
        {
            if (inventory == m_inputInventory)
            {
                m_inputInventory = null;
            }
            else if (inventory == m_outputInventory)
            {
                m_outputInventory = null;
            }
            else
            {
                Debug.Fail("Removing inventory from aggregate, but isn't neither input nor output! This shouldn't happend.");
            }
        }

        #endregion Inventory

        protected override void OnEnabledChanged()
        {
            UpdatePower();
            base.OnEnabledChanged();

            if (IsWorking && IsProducing)
                OnStartProducing();
        }
     
        private float ComputeRequiredPower()
        {
            return (Enabled && IsFunctional) ? (IsProducing || !IsQueueEmpty) ? GetOperationalPowerConsumption()
                                                             : ProductionBlockDefinition.StandbyPowerConsumption
                                             : 0.0f;
        }

        protected virtual float GetOperationalPowerConsumption()
        {
            return ProductionBlockDefinition.OperationalPowerConsumption;
        }

        private void Receiver_IsPoweredChanged()
        {
            if (!ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId))
                IsProducing = false;
            UpdateIsWorking();
        }

        private void CubeBlock_IsWorkingChanged(MyCubeBlock block)
        {
            if (IsWorking)
            {
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Green, Color.White);
                if (IsProducing)
                    OnStartProducing();
            }
            else
            {
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 0.0f, Color.Red, Color.White);
            }
        }

        protected void OnStartProducing()
        {
            if(m_soundEmitter != null)
                m_soundEmitter.PlaySound(m_processSound, true);
            var handle = StartedProducing;
            if (handle != null) handle();
        }

        protected void OnStopProducing()
        {
            if (m_soundEmitter != null)
            {
                if (IsWorking)
                {
                    m_soundEmitter.StopSound(false);
                    m_soundEmitter.PlaySound(m_baseIdleSound, false, true);
                }
                else
                    m_soundEmitter.StopSound(false);
            }
            var handle = StoppedProducing;
            if (handle != null) handle();
        }

        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get { return m_multilineConveyorEndpoint; }
        }

        public void InitializeConveyorEndpoint()
        {
            m_multilineConveyorEndpoint = new MyMultilineConveyorEndpoint(this);
            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_multilineConveyorEndpoint));
        }

        bool Sandbox.ModAPI.Ingame.IMyProductionBlock.UseConveyorSystem 
        { 
            get 
            { 
                return UseConveyorSystem;
            }
        }

        #region IMyInventoryOwner implementation

        int IMyInventoryOwner.InventoryCount
        {
            get { return InventoryCount; }
        }

        long IMyInventoryOwner.EntityId
        {
            get { return EntityId; }
        }

        bool IMyInventoryOwner.HasInventory
        {
            get { return HasInventory; }
        }

        bool IMyInventoryOwner.UseConveyorSystem
        {
            get
            {
                return UseConveyorSystem;
            }
            set
            {
                UseConveyorSystem = value;
            }
        }

        IMyInventory IMyInventoryOwner.GetInventory(int index)
        {
            return this.GetInventory(index);
        }

        #endregion

        #region Fixing inventory

        public void FixInputOutputInventories(MyInventoryConstraint inputInventoryConstraint, MyInventoryConstraint outputInventoryConstraint)
        {
            if (m_inventoryAggregate.InventoryCount == 2)
            {
                return;
            }

            var fixedAggregate = MyInventoryAggregate.FixInputOutputInventories(m_inventoryAggregate, inputInventoryConstraint, outputInventoryConstraint);
            Components.Remove<MyInventoryBase>();
            m_outputInventory = null;
            m_inputInventory = null;
            Components.Add<MyInventoryBase>(fixedAggregate);
        }

        #endregion

        #region IMyConveyorEndpointBlock implementation

        public virtual Sandbox.Game.GameSystems.Conveyors.PullInformation GetPullInformation()
        {
            Sandbox.Game.GameSystems.Conveyors.PullInformation pullInformation = new Sandbox.Game.GameSystems.Conveyors.PullInformation();
            pullInformation.Inventory = InputInventory;
            pullInformation.OwnerID = OwnerId;
            pullInformation.Constraint = InputInventory.Constraint;
            return pullInformation;
        }

        public virtual Sandbox.Game.GameSystems.Conveyors.PullInformation GetPushInformation()
        {
            Sandbox.Game.GameSystems.Conveyors.PullInformation pullInformation = new Sandbox.Game.GameSystems.Conveyors.PullInformation();
            pullInformation.Inventory = OutputInventory;
            pullInformation.OwnerID = OwnerId;
            pullInformation.Constraint = OutputInventory.Constraint;
            return pullInformation;
        }

        bool IMyProductionBlock.CanUseBlueprint(MyDefinitionBase blueprint)
        {
            return CanUseBlueprint(blueprint as MyBlueprintDefinition);
        }

        void IMyProductionBlock.AddQueueItem(MyDefinitionBase blueprint, MyFixedPoint amount)
        {
            AddQueueItemRequest(blueprint as MyBlueprintDefinition, amount);
        }

        void IMyProductionBlock.InsertQueueItem(int idx, MyDefinitionBase blueprint, MyFixedPoint amount)
        {
            InsertQueueItemRequest(idx, blueprint as MyBlueprintDefinition, amount);
        }

        void Sandbox.ModAPI.IMyProductionBlock.RemoveQueueItem(int idx, MyFixedPoint amount)
        {
            RemoveQueueItemRequest(idx, amount);
        }

        void Sandbox.ModAPI.IMyProductionBlock.ClearQueue()
        {
            ClearQueueRequest();
        }

        List<MyProductionQueueItem> IMyProductionBlock.GetQueue()
        {
            List<MyProductionQueueItem> result = new List<MyProductionQueueItem>(m_queue.Count);
            foreach (var item in m_queue)
            {
                MyProductionQueueItem newItem = new MyProductionQueueItem();
                newItem.Amount = item.Amount;
                newItem.Blueprint = item.Blueprint;
                newItem.ItemId = item.ItemId;
                result.Add(newItem);
            }
            return result;
        }

        #endregion
    }
}