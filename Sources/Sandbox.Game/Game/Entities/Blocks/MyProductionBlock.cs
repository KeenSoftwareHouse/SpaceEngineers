using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Havok;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;

using VRageMath;
using Sandbox.Game.World;
using Sandbox.Game.Multiplayer;
using Sandbox.Engine.Multiplayer;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System.Reflection;
using Sandbox.Common;
using Sandbox.Game.GameSystems.Conveyors;
using VRage;
using SteamSDK;
using Sandbox.Game.Localization;

namespace Sandbox.Game.Entities.Cube
{
    /// <summary>
    /// Common base for Assembler and Refinery blocks
    /// </summary>
    abstract class MyProductionBlock : MyFunctionalBlock, IMyInventoryOwner, IMyPowerConsumer, IMyConveyorEndpointBlock, Sandbox.ModAPI.Ingame.IMyProductionBlock
    {
        #region Sync class
        [PreloadRequired]
        public class ProductionBlockSync : MySyncEntity
        {
            MyProductionBlock Block;

            static ProductionBlockSync()
            {
                MySyncLayer.RegisterEntityMessage<ProductionBlockSync, AddQueueItemMsg>(OnAddQueueItemRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
                MySyncLayer.RegisterEntityMessage<ProductionBlockSync, AddQueueItemMsg>(OnAddQueueItemSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
                MySyncLayer.RegisterEntityMessage<ProductionBlockSync, RemoveQueueItemMsg>(OnRemoveQueueItemRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
                MySyncLayer.RegisterEntityMessage<ProductionBlockSync, RemoveQueueItemMsg>(OnRemoveQueueItemSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
                MySyncLayer.RegisterEntityMessage<ProductionBlockSync, MoveQueueItemMsg>(OnMoveQueueItemRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
                MySyncLayer.RegisterEntityMessage<ProductionBlockSync, MoveQueueItemMsg>(OnMoveQueueItemSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            }

            public ProductionBlockSync(MyProductionBlock block)
                : base(block)
            {
                Block = block;
            }

            [ProtoContract]
            [MessageId(2477, P2PMessageEnum.Reliable)]
            struct AddQueueItemMsg : IEntityMessage
            {
                [ProtoMember]
                public long ProductionEntityId;
                public long GetEntityId() { return ProductionEntityId; }
                [ProtoMember]
                public SerializableDefinitionId Blueprint;
                [ProtoMember]
                public MyFixedPoint Amount;
                [ProtoMember]
                public int Idx;
            }

            [MessageId(2478, P2PMessageEnum.Reliable)]
            struct RemoveQueueItemMsg : IEntityMessage
            {
                public long ProductionEntityId;
                public long GetEntityId() { return ProductionEntityId; }
                public int Idx;
                public MyFixedPoint Amount;
                public float CurrentProgress;
            }

            [MessageId(2479, P2PMessageEnum.Reliable)]
            struct MoveQueueItemMsg : IEntityMessage
            {
                public long ProductionEntityId;
                public long GetEntityId() { return ProductionEntityId; }
                public uint SrcItemId;
                public int DstIdx;
            }

            [MessageId(2490, P2PMessageEnum.Reliable)]
            struct ClearQueue : IEntityMessage
            {
                public long ProductionEntityId;
                public long GetEntityId() { return ProductionEntityId; }
            }

            public void MoveQueueItemAnnounce(uint srcItemId, int dstIdx)
            {
                Debug.Assert(Sync.IsServer);

                var msg = new MoveQueueItemMsg();
                msg.SrcItemId = srcItemId;
                msg.DstIdx = dstIdx;
                msg.ProductionEntityId = Block.EntityId;

                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
            }

            public void MoveQueueItemRequest(uint srcItemId, int dstIdx)
            {
                Debug.Assert(!Sync.IsServer);

                var msg = new MoveQueueItemMsg();
                msg.SrcItemId = srcItemId;
                msg.DstIdx = dstIdx;
                msg.ProductionEntityId = Block.EntityId;

                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            }

            static void OnMoveQueueItemRequest(ProductionBlockSync sync, ref MoveQueueItemMsg msg, MyNetworkClient sender)
            {
                sync.Block.MoveQueueItemRequest(msg.SrcItemId, msg.DstIdx);
            }

            static void OnMoveQueueItemSuccess(ProductionBlockSync sync, ref MoveQueueItemMsg msg, MyNetworkClient sender)
            {
                 sync.Block.MoveQueueItem(msg.SrcItemId, msg.DstIdx);
            }

            /// <summary>
            /// idx - index to insert (-1 = last)
            /// </summary>
            public void AddQueueItemRequest(MyBlueprintDefinitionBase blueprint, MyFixedPoint amount, int idx = -1)
            {
                Debug.Assert(idx != -2, "No longer supported.");
                var msg = new AddQueueItemMsg();
                msg.Idx = idx;
                msg.Blueprint = blueprint.Id;
                msg.Amount = amount;
                msg.ProductionEntityId = Block.EntityId;
                
                Sync.Layer.SendMessageToServer(ref msg);
            }

            public void AddQueueItemAnnounce(MyBlueprintDefinitionBase blueprint, MyFixedPoint amount, int idx = -1)
            {
                Debug.Assert(idx != -2, "No longer supported.");
                Debug.Assert(Sync.IsServer);
                var msg = new AddQueueItemMsg();
                msg.Idx = idx;
                msg.Blueprint = blueprint.Id;
                msg.Amount = amount;
                msg.ProductionEntityId = Block.EntityId;

                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
            }

            static void OnAddQueueItemRequest(ProductionBlockSync sync, ref AddQueueItemMsg msg, MyNetworkClient sender)
            {
                var blueprint = MyDefinitionManager.Static.GetBlueprintDefinition(msg.Blueprint);
                Debug.Assert(blueprint != null, "Blueprint not present in the dictionary.");
                if (blueprint != null)
                {
                    sync.Block.InsertQueueItem(msg.Idx, blueprint, msg.Amount);
                    Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
                }
            }

            static void OnAddQueueItemSuccess(ProductionBlockSync sync, ref AddQueueItemMsg msg, MyNetworkClient sender)
            {
                sync.Block.InsertQueueItem(msg.Idx, MyDefinitionManager.Static.GetBlueprintDefinition(msg.Blueprint), msg.Amount);
            }

            public void RemoveQueueItemRequest(int idx, MyFixedPoint amount)
            {
                Debug.Assert(idx != -2, "No longer supported.");
                var msg = new RemoveQueueItemMsg();
                msg.Idx = idx;
                msg.Amount = amount;
                msg.ProductionEntityId = Block.EntityId;

                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            }

            /// <summary>
            /// idx - index to insert to (-1 = last)
            /// </summary>
            public void RemoveQueueItemAnnounce(int idx, MyFixedPoint amount, float progress = 0f)
            {
                Debug.Assert(idx != -2, "No longer supported.");
                var msg = new RemoveQueueItemMsg();
                msg.Idx = idx;
                msg.Amount = amount;
                msg.CurrentProgress = progress;
                msg.ProductionEntityId = Block.EntityId;

                Sync.Layer.SendMessageToAll(msg, MyTransportMessageEnum.Success);
            }

            static void OnRemoveQueueItemRequest(ProductionBlockSync sync, ref RemoveQueueItemMsg msg, MyNetworkClient sender)
            {
                if (!RemoveQueueItemTests(sync, msg)) return;
                OnRemoveQueueItemInternal(sync, msg);

                Sync.Layer.SendMessageToAll(msg, MyTransportMessageEnum.Success);
            }

            static void OnRemoveQueueItemSuccess(ProductionBlockSync sync, ref RemoveQueueItemMsg msg, MyNetworkClient sender)
            {
                if (!RemoveQueueItemTests(sync, msg)) return;
                OnRemoveQueueItemInternal(sync, msg);
            }

            private static bool RemoveQueueItemTests(ProductionBlockSync sync, RemoveQueueItemMsg msg)
            {
                Debug.Assert(msg.Idx != -2, "No longer supported.");

                if (sync == null)
                {
                    MySandboxGame.Log.WriteLine("Queue sync object is null!");
                    return false;
                }
                if (sync != null && sync.Block == null)
                {
                    MySandboxGame.Log.WriteLine("Block of queue sync object is null!");
                    return false;
                }
                if (!sync.Block.m_queue.IsValidIndex(msg.Idx) && msg.Idx != -1)
                {
                    MySandboxGame.Log.WriteLine("Invalid queue index in the remove item message!");
                    return false;
                }

                return true;
            }

            private static void OnRemoveQueueItemInternal(ProductionBlockSync sync, RemoveQueueItemMsg msg)
            {
                if (msg.Idx >= 0)
                    sync.Block.RemoveQueueItem(msg.Idx);
                else
                    sync.Block.RemoveFirstQueueItem(msg.Amount, msg.CurrentProgress);
            }
        }
        #endregion

        protected MySoundPair m_processSound = new MySoundPair();

        public struct QueueItem
        {
            public MyFixedPoint Amount;
            public MyBlueprintDefinitionBase Blueprint;
            public uint ItemId;
        }

        #region Fields

        protected List<QueueItem> m_queue;
        private MyProductionBlockDefinition ProductionBlockDefinition
        {
            get { return (MyProductionBlockDefinition)base.BlockDefinition; }
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
                    UpdatePower();
                    if (value)
                        OnStartProducing();
                    else
                        OnStopProducing();
                }
            }
        }

        protected virtual void UpdatePower()
        {
            PowerReceiver.Update();
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
            return PowerReceiver.IsPowered && base.CheckIsWorking();
        }

        public MyPowerReceiver PowerReceiver
        {
            get;
            private set;
        }

        static MyProductionBlock()
        {
            var useConveyorSystem = new MyTerminalControlOnOffSwitch<MyProductionBlock>("UseConveyor", MySpaceTexts.Terminal_UseConveyorSystem);
            useConveyorSystem.Getter = (x) => (x as IMyInventoryOwner).UseConveyorSystem;
            useConveyorSystem.Setter = (x, v) => MySyncConveyors.SendChangeUseConveyorSystemRequest(x.EntityId, v);
            useConveyorSystem.EnableToggleAction();
            MyTerminalControlFactory.AddControl(useConveyorSystem);
        }

        #endregion Properties

        public event Action<MyProductionBlock> QueueChanged;
        private string m_string;
        protected bool m_useConveyorSystem;
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
            m_soundEmitter = new MyEntity3DSoundEmitter(this);
            m_queue = new List<QueueItem>();

            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;

            IsProducing = false;
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;
            base.Init(objectBuilder, cubeGrid);

            m_inputInventory = new MyInventory(
                ProductionBlockDefinition.InventoryMaxVolume,
                ProductionBlockDefinition.InventorySize,
                MyInventoryFlags.CanReceive,
                this);

            m_outputInventory = new MyInventory(
                ProductionBlockDefinition.InventoryMaxVolume,
                ProductionBlockDefinition.InventorySize,
                MyInventoryFlags.CanSend,
                this);

            var ob = (MyObjectBuilder_ProductionBlock)objectBuilder;
            if (ob.InputInventory != null)
                InputInventory.Init(ob.InputInventory);
            if (ob.OutputInventory != null)
                OutputInventory.Init(ob.OutputInventory);

            m_nextItemId = ob.NextItemId;
            bool nextIdWasZero = m_nextItemId == 0;

            base.IsWorkingChanged += CubeBlock_IsWorkingChanged;

            PowerReceiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Factory,
                false,
                ProductionBlockDefinition.OperationalPowerConsumption,
                ComputeRequiredPower);
            PowerReceiver.IsPoweredChanged += Receiver_IsPoweredChanged;
            PowerReceiver.Update();
            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawPowerReciever(PowerReceiver,this));

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
                        MySandboxGame.Log.WriteLine(string.Format("Could not add item into production block's queue: Blueprint {0} was not found.", item.Id));
                    }
                }

                UpdatePower();
            }

            m_useConveyorSystem = ob.UseConveyorSystem;

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
            PowerReceiver.Update();
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

        public new ProductionBlockSync SyncObject
        {
            get { return (ProductionBlockSync)base.SyncObject; }
        }

        protected override MySyncEntity OnCreateSync()
        {
            return new ProductionBlockSync(this);
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

            if (MyFakes.ENABLE_PRODUCTION_SYNC)
            {
                RemoveFirstQueueItem(amount, progress);
                SyncObject.RemoveQueueItemAnnounce(-1, amount, progress);
            }
            else
                RemoveFirstQueueItem(amount);
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
            if (MyFakes.ENABLE_PRODUCTION_SYNC)
            {
                if (Sync.IsServer)
                {
                    InsertQueueItem(idx, blueprint, amount);
                    SyncObject.AddQueueItemAnnounce(blueprint, amount, idx);
                }
                else
                    SyncObject.AddQueueItemRequest(blueprint, amount, idx);
            }
            else
            {
                InsertQueueItem(idx, blueprint, amount);
            }
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

        public void RemoveQueueItemRequest(int itemIdx)
        {
            if (MyFakes.ENABLE_PRODUCTION_SYNC)
            {
                if (Sync.IsServer)
                {
                    RemoveQueueItem(itemIdx);
                    SyncObject.RemoveQueueItemAnnounce(itemIdx, 1);
                }
                else
                    SyncObject.RemoveQueueItemRequest(itemIdx, 1);
            }
            else
                RemoveQueueItem(itemIdx);
        }

        protected virtual void RemoveQueueItem(int itemIdx)
        {
            m_queue.RemoveAt(itemIdx);

            UpdatePower();

            OnQueueChanged();
        }

        public void MoveQueueItemRequest(uint queueItemId, int targetIdx)
        {
            if (MyFakes.ENABLE_PRODUCTION_SYNC)
            {
                if (Sync.IsServer)
                {
                    MoveQueueItem(queueItemId, targetIdx);
                    SyncObject.MoveQueueItemAnnounce(queueItemId, targetIdx);
                }
                else
                    SyncObject.MoveQueueItemRequest(queueItemId, targetIdx);
            }
            else
                MoveQueueItem(queueItemId, targetIdx);
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

            for (int i = m_queue.Count - 1; i >= 0; i--)
            {
                RemoveQueueItemRequest(i);
            }

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
            if (!MyFakes.OCTOBER_RELEASE_REFINERY_ENABLED && !MyFakes.OCTOBER_RELEASE_ASSEMBLER_ENABLED)
                return;

            int currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            if (PowerReceiver.IsPowered)
            {
                UpdateProduction(currentTime - m_lastUpdateTime);
            }
            else
            {
                IsProducing = false;
            }
            m_lastUpdateTime = currentTime;
        }

        protected override void Closing()
        {
            m_soundEmitter.StopSound(true);
            base.Closing();
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();
            UpdateProduction();
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();
            m_soundEmitter.Update();
        }

        #region IMyInventoryOwner

        String IMyInventoryOwner.DisplayNameText
        {
            get { return CustomName.ToString(); }
        }

        public int InventoryCount 
        { 
            get 
            { 
                return (MyFakes.OCTOBER_RELEASE_ASSEMBLER_ENABLED || MyFakes.OCTOBER_RELEASE_REFINERY_ENABLED) ? 2 : 1;
            }
        }

        public MyInventory GetInventory(int index = 0)
        {
            switch (index)
            {
                case 0: return m_inputInventory;
                case 1: return m_outputInventory;
                default:
                    throw new InvalidBranchException();
            }
        }

        public override void OnRemovedByCubeBuilder()
        {
            ReleaseInventory(m_inputInventory);
            ReleaseInventory(m_outputInventory);
            base.OnRemovedByCubeBuilder();
        }

        public override void OnDestroy()
        {
            ReleaseInventory(m_inputInventory,true);
            ReleaseInventory(m_outputInventory,true);
            base.OnDestroy();
        }

        public MyInventoryOwnerTypeEnum InventoryOwnerType
        {
            get { return MyInventoryOwnerTypeEnum.System; }
        }

        bool IMyInventoryOwner.UseConveyorSystem
        {
            get
            {
                return m_useConveyorSystem;
            }
            set
            {
                m_useConveyorSystem = value;
            }
        }

        Sandbox.ModAPI.Interfaces.IMyInventory Sandbox.ModAPI.Interfaces.IMyInventoryOwner.GetInventory(int index)
        {
           return GetInventory(index);
        }

        bool ModAPI.Interfaces.IMyInventoryOwner.UseConveyorSystem
        {
            get
            {
                return (this as IMyInventoryOwner).UseConveyorSystem;
            }
            set
            {
                (this as IMyInventoryOwner).UseConveyorSystem = value;
            }
        }
        #endregion IMyInventoryObject

        protected override void OnEnabledChanged()
        {
            PowerReceiver.Update();
            base.OnEnabledChanged();

            if (IsWorking && IsProducing)
                OnStartProducing();
        }

        public MyInventory InputInventory { get { return m_inputInventory; } }

        public MyInventory OutputInventory { get { return m_outputInventory; } }

        private float ComputeRequiredPower()
        {
            return (Enabled && IsFunctional) ? (IsProducing) ? GetOperationalPowerConsumption()
                                                             : ProductionBlockDefinition.StandbyPowerConsumption
                                             : 0.0f;
        }

        protected virtual float GetOperationalPowerConsumption()
        {
            return ProductionBlockDefinition.OperationalPowerConsumption;
        }

        private void Receiver_IsPoweredChanged()
        {
            if (!PowerReceiver.IsPowered)
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
            m_soundEmitter.PlaySound(m_processSound, false);
            var handle = StartedProducing;
            if (handle != null) handle();
        }

        protected void OnStopProducing()
        {
            m_soundEmitter.StopSound(false);
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
                return (this as IMyInventoryOwner).UseConveyorSystem;
            }
        }
    }
}