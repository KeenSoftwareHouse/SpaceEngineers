#region Using

using System;
using System.Diagnostics;
using System.Text;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;

using VRage.Trace;
using VRageMath;
using Sandbox.Game.World;
using Sandbox.Game.Multiplayer;
using System.Collections.Generic;
using SteamSDK;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Screens;
using Sandbox.Graphics;
using VRage;
using Sandbox.Game.GameSystems;
using VRage.Utils;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.ModAPI;
using Sandbox.Game.Localization;
using VRage;
using Sandbox.Game.Entities.Interfaces;
using Sandbox.Game.EntityComponents;
using VRage.ObjectBuilders;
using Sandbox.ModAPI.Interfaces;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Network;

#endregion

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Assembler))]
    public class MyAssembler : MyProductionBlock, IMyAssembler, IMyEventProxy
    {

        public enum StateEnum
        {
            Ok,
            Disabled,
            NotWorking,
            NotEnoughPower,
            MissingItems,
            InventoryFull,
        }

        private MyEntity m_currentUser;
        private MyAssemblerDefinition m_assemblerDef;
        private float m_currentProgress;
        private MyBlueprintDefinitionBase m_currentBlueprint;
        private StateEnum m_currentState;
        private bool m_slave = false;
        private bool m_repeatDisassembleEnabled;
        private bool m_repeatAssembleEnabled;
        private bool m_disassembleEnabled;
        private List<MyEntity> m_inventoryOwners = new List<MyEntity>();
        private List<MyBlueprintDefinitionBase.Item> m_requiredComponents = new List<MyBlueprintDefinitionBase.Item>(); 

        private const float TIME_IN_ADVANCE = 5;

        private bool m_isProcessing = false;
        private bool m_soundStartedFromInventory = false;
        private List<QueueItem> m_otherQueue;
        private List<MyAssembler> m_assemblers = new List<MyAssembler>();
        private int m_assemblerKeyCounter;
        private MyCubeGrid m_cubeGrid;
        private bool m_inventoryOwnersDirty = true;

        public bool InventoryOwnersDirty
        {
            get { return m_inventoryOwnersDirty; }
            set { m_inventoryOwnersDirty = value; }
        }

        public bool IsSlave
        {
            get { return m_slave; }
            private set { m_slave = value; }
        }

        /// <summary>
        /// Progress of currently built item in % (range 0 to 1).
        /// </summary>
        public float CurrentProgress
        {
            get { return m_currentProgress; }
            set
            {
                if (m_currentProgress != value)
                {
                    m_currentProgress = value;
                    if (CurrentProgressChanged != null) CurrentProgressChanged(this);
                }
            }
        }

        public StateEnum CurrentState
        {
            get { return m_currentState; }
            private set
            {
                if (m_currentState != value)
                {
                    m_currentState = value;
                    if (CurrentStateChanged != null)
                        CurrentStateChanged(this);
                }
            }
        }

        public event Action<MyAssembler> CurrentProgressChanged;
        public event Action<MyAssembler> CurrentStateChanged;
        public event Action<MyAssembler> CurrentModeChanged;

        public MyAssembler() :
            base()
        {
            CreateTerminalControls();

            m_otherQueue = new List<QueueItem>();
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyAssembler>())
                return;
            base.CreateTerminalControls();
            var slaveCheck = new MyTerminalControlCheckbox<MyAssembler>("slaveMode", MySpaceTexts.Assembler_SlaveMode, MySpaceTexts.Assembler_SlaveMode);
            slaveCheck.Getter = (x) => x.IsSlave;
            slaveCheck.Setter = (x, v) =>
            {
                if (x.RepeatEnabled)
                {
                    x.RequestRepeatEnabled(false);
                }
                x.RequestSlaveEnabled(v);

            };
            slaveCheck.EnableAction();
            MyTerminalControlFactory.AddControl(slaveCheck);
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            UpgradeValues.Add("Productivity", 0f);
            UpgradeValues.Add("PowerEfficiency", 1f);

            base.Init(objectBuilder, cubeGrid);
            m_cubeGrid = cubeGrid;
            NeedsUpdate |= VRage.ModAPI.MyEntityUpdateEnum.EACH_100TH_FRAME;

            MyDebug.AssertDebug(BlockDefinition is MyAssemblerDefinition);
            m_assemblerDef = BlockDefinition as MyAssemblerDefinition;


            if (InventoryAggregate.InventoryCount > 2)
            {
                Debug.Fail("Inventory aggregate has to many inventories, probably wrong save. If you continue the unused inventories will be removed. Save the world to correct it. Please report this is if problem prevail.");

                FixInputOutputInventories(m_assemblerDef.InputInventoryConstraint, m_assemblerDef.OutputInventoryConstraint);
            }

            InputInventory.Constraint = m_assemblerDef.InputInventoryConstraint;
            OutputInventory.Constraint = m_assemblerDef.OutputInventoryConstraint;

            bool removed = InputInventory.FilterItemsUsingConstraint();
            Debug.Assert(!removed, "Inventory filter removed items which were present in the object builder.");

            var builder = (MyObjectBuilder_Assembler)objectBuilder;
            if (builder.OtherQueue != null)
            {
                m_otherQueue.Clear();
                if (m_otherQueue.Capacity < builder.OtherQueue.Length)
                    m_otherQueue.Capacity = builder.OtherQueue.Length;
                for (int i = 0; i < builder.OtherQueue.Length; ++i)
                {
                    var item = builder.OtherQueue[i];

                    var blueprint = MyDefinitionManager.Static.TryGetBlueprintDefinitionByResultId(item.Id);
                    if (blueprint != null)
                    {
                        m_otherQueue.Add(new QueueItem()
                        {
                            Blueprint = blueprint,
                            Amount = item.Amount,
                        });
                    }
                    else
                    {
                        MySandboxGame.Log.WriteLine(string.Format("No blueprint that produces a single result with Id '{0}'", item.Id));
                    }
                }
            }
            CurrentProgress = builder.CurrentProgress;
            m_disassembleEnabled = builder.DisassembleEnabled;
            m_repeatAssembleEnabled = builder.RepeatAssembleEnabled;
            m_repeatDisassembleEnabled = builder.RepeatDisassembleEnabled;
            m_slave = builder.SlaveEnabled;
            UpdateInventoryFlags();

            m_baseIdleSound = BlockDefinition.PrimarySound;
            m_processSound = BlockDefinition.ActionSound;

            OnUpgradeValuesChanged += UpdateDetailedInfo;

            ResourceSink.RequiredInputChanged += PowerReceiver_RequiredInputChanged;
            UpdateDetailedInfo();
        }


        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = (MyObjectBuilder_Assembler)base.GetObjectBuilderCubeBlock(copy);

            ob.CurrentProgress = CurrentProgress;
            ob.DisassembleEnabled = m_disassembleEnabled;
            ob.RepeatAssembleEnabled = m_repeatAssembleEnabled;
            ob.RepeatDisassembleEnabled = m_repeatDisassembleEnabled;
            ob.SlaveEnabled = m_slave;

            if (m_otherQueue.Count > 0)
            {
                ob.OtherQueue = new MyObjectBuilder_ProductionBlock.QueueItem[m_otherQueue.Count];
                for (int i = 0; i < m_otherQueue.Count; ++i)
                {
                    ob.OtherQueue[i] = new MyObjectBuilder_ProductionBlock.QueueItem()
                    {
                        Amount = m_otherQueue[i].Amount,
                        Id = m_otherQueue[i].Blueprint.Id
                    };
                }
            }
            else
                ob.OtherQueue = null;

            return ob;
        }

        private void UpdateDetailedInfo()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MyCommonTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.AppendFormat("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(GetOperationalPowerConsumption(), DetailedInfo);
            DetailedInfo.AppendFormat("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_RequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(ResourceSink.RequiredInputByType(MyResourceDistributorComponent.ElectricityId), DetailedInfo);


            DetailedInfo.AppendFormat("\n\n");
            DetailedInfo.Append("Productivity: ");
            DetailedInfo.Append(((UpgradeValues["Productivity"] + 1f) * 100f).ToString("F0"));
            DetailedInfo.Append("%\n");
            DetailedInfo.Append("Power Efficiency: ");
            DetailedInfo.Append(((UpgradeValues["PowerEfficiency"]) * 100f).ToString("F0"));
            DetailedInfo.Append("%\n");

            RaisePropertiesChanged();
        }

        void PowerReceiver_RequiredInputChanged(MyDefinitionId resourceTypeId, MyResourceSinkComponent receiver, float oldRequirement, float newRequirement)
        {
            UpdateDetailedInfo();
        }

        private static List<IMyConveyorEndpoint> m_conveyorEndpoints = new List<IMyConveyorEndpoint>();
        private static MyAssembler m_assemblerForPathfinding;

        private static Predicate<IMyConveyorEndpoint> m_vertexPredicate = VertexRules;
        private static bool VertexRules(IMyConveyorEndpoint vertex)
        {
            return vertex.CubeBlock is MyAssembler && vertex.CubeBlock != m_assemblerForPathfinding;
        }

        private static Predicate<IMyConveyorEndpoint> m_edgePredicate = EdgeRules;
        private static bool EdgeRules(IMyConveyorEndpoint edge)
        {
            if (edge.CubeBlock.OwnerId == 0)
                return true;

            return m_assemblerForPathfinding.FriendlyWithBlock(edge.CubeBlock);
        }

        private MyAssembler GetMasterAssembler()
        {
            m_conveyorEndpoints.Clear();
            
            m_assemblerForPathfinding = this;

            MyGridConveyorSystem.FindReachable(this.ConveyorEndpoint, m_conveyorEndpoints, m_vertexPredicate, m_edgePredicate);

            MyUtils.ShuffleList<IMyConveyorEndpoint>(m_conveyorEndpoints);

            foreach (var ep in m_conveyorEndpoints)
            {
                var ass = ep.CubeBlock as MyAssembler;
                if (ass != null && !ass.DisassembleEnabled && !ass.IsSlave && ass.m_queue.Count > 0)
                    return ass;
            }
            return null;
        }

        private void GetItemFromOtherAssemblers(float remainingTime)
        {
            var factor = MySession.Static.AssemblerSpeedMultiplier * (((MyAssemblerDefinition)BlockDefinition).AssemblySpeed + UpgradeValues["Productivity"]);

            var masterAssembler = GetMasterAssembler();
            if (masterAssembler != null)
            {
                if (masterAssembler.m_repeatAssembleEnabled)
                {
                    if (m_queue.Count == 0)
                    {
                        while (remainingTime > 0)
                        {
                            foreach (var qItem in masterAssembler.m_queue)
                            {
                                remainingTime -= (float)((qItem.Blueprint.BaseProductionTimeInSeconds / factor) * qItem.Amount);
                                InsertQueueItemRequest(m_queue.Count, qItem.Blueprint, qItem.Amount);
                            }
                        }
                    }
                }
                else if (masterAssembler.m_queue.Count > 0)
                {
                    var item = masterAssembler.TryGetQueueItem(0);
                    if (item != null && item.Value.Amount > 1)
                    {
                        var itemAmount = Math.Min((int)item.Value.Amount - 1, Convert.ToInt32(Math.Ceiling(remainingTime / (item.Value.Blueprint.BaseProductionTimeInSeconds / factor))));
                        if (itemAmount > 0)
                        {
                            masterAssembler.RemoveFirstQueueItemAnnounce(itemAmount, masterAssembler.CurrentProgress);
                            InsertQueueItemRequest(m_queue.Count, item.Value.Blueprint, itemAmount);
                        }
                    }
                }
            }
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();

            if (m_inventoryOwnersDirty)
            {
                GetCoveyorInventoryOwners();
            }

            if (Sync.IsServer && IsWorking && m_useConveyorSystem)
            {
                if (DisassembleEnabled) // Dissasembling
                {
                    if (OutputInventory.VolumeFillFactor < 0.99f)
                    {
                        //MyGridConveyorSystem.PullAllRequest(this, OutputInventory, OwnerId, OutputInventory.Constraint);
                        var item = TryGetFirstQueueItem();
                        if (item != null)
                        {
                            if (!OutputInventory.ContainItems(null, item.Value.Blueprint.Results[0].Id))
                            {
                                MyGridConveyorSystem.ItemPullRequest(this, OutputInventory, OwnerId, item.Value.Blueprint.Results[0].Id, item.Value.Amount);
                            }
                        }
                    }
                    if (InputInventory.VolumeFillFactor > 0.75f)
                    {
                        Debug.Assert(InputInventory.GetItems().Count > 0);
                        MyGridConveyorSystem.PushAnyRequest(this, InputInventory, OwnerId);
                    }
                }
                else // Assembling
                {
                    //if (IsSlave && m_queue.Count < 1 && MyFakes.ENABLE_ASSEMBLER_COOPERATION && !RepeatEnabled) 
                    //{
                    //    GetItemFromOtherAssemblers(TIME_IN_ADVANCE);
                    //}
                    if (InputInventory.VolumeFillFactor < 0.99f)
                    {
                        m_requiredComponents.Clear();

                        var next = false;
                        int i = 0;
                        var time = 0f;
                        do
                        {
                            var item = TryGetQueueItem(i);
                            var remainingTime = TIME_IN_ADVANCE - time;
                            if (item.HasValue)
                            {
                                var productivity = (((MyAssemblerDefinition)BlockDefinition).AssemblySpeed + UpgradeValues["Productivity"]);
                                var factor = MySession.Static.AssemblerSpeedMultiplier * productivity;
                                var itemAmount = 1;
                                if (item.Value.Blueprint.BaseProductionTimeInSeconds / factor < remainingTime)
                                {
                                    itemAmount = Math.Min((int)item.Value.Amount, Convert.ToInt32(Math.Ceiling(remainingTime / (item.Value.Blueprint.BaseProductionTimeInSeconds / factor))));
                                }
                                time += itemAmount * item.Value.Blueprint.BaseProductionTimeInSeconds / factor;
                                if (time < TIME_IN_ADVANCE)
                                {
                                    next = true;
                                }
                                var amountMult = (MyFixedPoint)(1.0f / MySession.Static.AssemblerEfficiencyMultiplier);
                                foreach (var component in item.Value.Blueprint.Prerequisites)
                                {
                                    var requiredAmount = component.Amount * itemAmount * amountMult;

                                    bool found = false;
                                    for (int j = 0; j < m_requiredComponents.Count; j++)
                                    {
                                        if (m_requiredComponents[j].Id == component.Id)
                                        {
                                            m_requiredComponents[j] = new MyBlueprintDefinitionBase.Item
                                            {
                                                Amount = m_requiredComponents[j].Amount + requiredAmount,
                                                Id = component.Id
                                            };
                                            found = true;
                                            break;
                                        }
                                    }
                                    if (!found)
                                    {
                                        m_requiredComponents.Add(new MyBlueprintDefinitionBase.Item
                                        {
                                            Amount = requiredAmount,
                                            Id = component.Id
                                        });
                                    }
                                }
                            }

                            i++;
                            if (i >= m_queue.Count)
                                next = false;
                        } while (next);

                        foreach (var component in m_requiredComponents)
                        {
                            var availableAmount = InputInventory.GetItemAmount(component.Id);
                            var neededAmount = component.Amount - availableAmount;
                            if (neededAmount <= 0) continue;

                            MyGridConveyorSystem.ItemPullRequest(this, InputInventory, OwnerId, component.Id, neededAmount);                            
                        }

                        if (IsSlave && !RepeatEnabled)
                        {
                            var remainingTime = TIME_IN_ADVANCE - time;
                            if (remainingTime > 0)
                                GetItemFromOtherAssemblers(remainingTime);
                        }
                    }

                    if (OutputInventory.VolumeFillFactor > 0.75f)
                    {
                        Debug.Assert(OutputInventory.GetItems().Count > 0);
                        MyGridConveyorSystem.PushAnyRequest(this, OutputInventory, OwnerId);
                    }
                }
            }
        }

        protected override void UpdateProduction(int timeDelta)
        {
            if (!Enabled)
            {
                CurrentState = StateEnum.Disabled;
                return;
            }

            if (!ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) || ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId) < GetOperationalPowerConsumption())
            {
                if (!ResourceSink.IsPowerAvailable(MyResourceDistributorComponent.ElectricityId, GetOperationalPowerConsumption()))
                {
                    CurrentState = StateEnum.NotEnoughPower;
                    return;
                }
            }

            if (!IsWorking)
            {
                CurrentState = StateEnum.NotWorking;
                return;
            }

            if (IsQueueEmpty)
            {
                return;
            }

            var firstQueueItem = TryGetFirstQueueItem();
            while (timeDelta > 0)
            {
                if (!firstQueueItem.HasValue)
                {
                    CurrentProgress = 0f;
                    if (IsQueueEmpty)
                    {
                        IsProducing = false;
                        return;
                    }

                    if (!Sync.IsServer)
                        break;

                    firstQueueItem = TryGetFirstQueueItem();
                }

                var currentBlueprint = firstQueueItem.Value.Blueprint;
                CurrentState = CheckInventory(currentBlueprint);
                if (CurrentState != StateEnum.Ok)
                {
                    IsProducing = false;
                    return;
                }
                var remainingTime = calculateBlueprintProductionTime(currentBlueprint) - CurrentProgress * calculateBlueprintProductionTime(currentBlueprint);

                if (timeDelta >= remainingTime)
                {
                    if (Sync.IsServer)
                    {
                        if (DisassembleEnabled)
                            FinishDisassembling(currentBlueprint);
                        else
                        {
                            if (RepeatEnabled)
                                InsertQueueItemRequest(-1, currentBlueprint);
                            FinishAssembling(currentBlueprint);
                        }

                        RemoveFirstQueueItemAnnounce(1);
                    }
                    timeDelta -= (int)Math.Ceiling(remainingTime);
                    CurrentProgress = 0;
                    firstQueueItem = null;
                }
                else
                {
                    CurrentProgress += timeDelta / calculateBlueprintProductionTime(currentBlueprint);
                    timeDelta = 0;
                }
            }
            IsProducing = IsWorking && !IsQueueEmpty;
        }

        private float calculateBlueprintProductionTime(MyBlueprintDefinitionBase currentBlueprint)
        {
            return currentBlueprint.BaseProductionTimeInSeconds * 1000 / (MySession.Static.AssemblerSpeedMultiplier * ((MyAssemblerDefinition)BlockDefinition).AssemblySpeed + UpgradeValues["Productivity"]);
        }

        private void FinishAssembling(MyBlueprintDefinitionBase blueprint)
        {
            var amountMult = (MyFixedPoint)(1.0f / MySession.Static.AssemblerEfficiencyMultiplier);
            for (int i = 0; i < blueprint.Prerequisites.Length; ++i)
            {
                var item = blueprint.Prerequisites[i];
                InputInventory.RemoveItemsOfType(item.Amount * amountMult, item.Id);
            }

            foreach (var res in blueprint.Results)
            {
                MyObjectBuilder_PhysicalObject resOb = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(res.Id.TypeId, res.Id.SubtypeName);
                OutputInventory.AddItems(res.Amount, resOb);
            }
        }

        private void FinishDisassembling(MyBlueprintDefinitionBase blueprint)
        {
            if (RepeatEnabled && Sync.IsServer) OutputInventory.ContentsChanged -= OutputInventory_ContentsChanged;
            foreach (var res in blueprint.Results)
            {
                OutputInventory.RemoveItemsOfType(res.Amount, res.Id);
            }
            if (RepeatEnabled && Sync.IsServer) OutputInventory.ContentsChanged += OutputInventory_ContentsChanged;

            var amountMult = (MyFixedPoint)(1.0f / MySession.Static.AssemblerEfficiencyMultiplier);
            for (int i = 0; i < blueprint.Prerequisites.Length; ++i)
            {
                var item = blueprint.Prerequisites[i];
                var itemOb = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(item.Id.TypeId, item.Id.SubtypeName);
                InputInventory.AddItems(item.Amount * amountMult, itemOb);
            }
        }

        private StateEnum CheckInventory(MyBlueprintDefinitionBase blueprint)
        {
            var amountMult = (MyFixedPoint)(1.0f / MySession.Static.AssemblerEfficiencyMultiplier);
            if (DisassembleEnabled)
            {
                if (!CheckInventoryCapacity(InputInventory, blueprint.Prerequisites, amountMult))
                    return StateEnum.InventoryFull;

                if (!CheckInventoryContents(OutputInventory, blueprint.Results, 1))
                    return StateEnum.MissingItems;
            }
            else
            {
                if (!CheckInventoryCapacity(OutputInventory, blueprint.Results, 1))
                    return StateEnum.InventoryFull;

                if (!CheckInventoryContents(InputInventory, blueprint.Prerequisites, amountMult))
                    return StateEnum.MissingItems;
            }

            return StateEnum.Ok;
        }

        private bool CheckInventoryCapacity(MyInventory inventory, MyBlueprintDefinitionBase.Item item, MyFixedPoint amountMultiplier)
        {
            return inventory.CanItemsBeAdded(item.Amount * amountMultiplier, item.Id);
        }

        private bool CheckInventoryCapacity(MyInventory inventory, MyBlueprintDefinitionBase.Item[] items, MyFixedPoint amountMultiplier)
        {
            if (MySession.Static.CreativeMode)
                return true;

            MyFixedPoint resultVolume = 0;
            foreach (var item in items)
            {
                var def = MyDefinitionManager.Static.GetPhysicalItemDefinition(item.Id);
                resultVolume += (MyFixedPoint)def.Volume * item.Amount * amountMultiplier;
            }
            return inventory.CurrentVolume + resultVolume <= inventory.MaxVolume;
        }

        private bool CheckInventoryContents(MyInventory inventory, MyBlueprintDefinitionBase.Item item, MyFixedPoint amountMultiplier)
        {
            return inventory.ContainItems(item.Amount * amountMultiplier, item.Id);
        }

        private bool CheckInventoryContents(MyInventory inventory, MyBlueprintDefinitionBase.Item[] item, MyFixedPoint amountMultiplier)
        {
            for (int i = 0; i < item.Length; ++i)
            {
                if (!inventory.ContainItems(item[i].Amount * amountMultiplier, item[i].Id))
                    return false;
            }
            return true;
        }

        protected override void OnQueueChanged()
        {
            if (CurrentState == StateEnum.MissingItems && IsQueueEmpty)
            {
                CurrentState = (!Enabled) ? StateEnum.Disabled :
                               (!ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId)) ? StateEnum.NotEnoughPower :
                               (!IsFunctional) ? StateEnum.NotWorking :
                               StateEnum.Ok;
            }
            IsProducing = IsWorking && !IsQueueEmpty;
            base.OnQueueChanged();
        }

        protected override void RemoveFirstQueueItem(MyFixedPoint amount, float progress = 0f)
        {
            CurrentProgress = progress;

            base.RemoveFirstQueueItem(amount);
        }

        protected override void RemoveQueueItem(int itemIdx)
        {
            if (itemIdx == 0)
                CurrentProgress = 0f;
            base.RemoveQueueItem(itemIdx);
        }

        protected override void InsertQueueItem(int idx, MyBlueprintDefinitionBase blueprint, MyFixedPoint amount)
        {
            if (idx == 0)
            {
                var queueItem = TryGetFirstQueueItem();
                if (queueItem.HasValue && queueItem.Value.Blueprint != blueprint)
                    CurrentProgress = 0f;
            }
            base.InsertQueueItem(idx, blueprint, amount);
        }

        protected override void MoveQueueItem(uint queueItemId, int targetIdx)
        {
            if (targetIdx == 0)
            {
                var queueItem = TryGetQueueItemById(queueItemId);
                var firstItem = TryGetFirstQueueItem();

                if (queueItem.HasValue && firstItem.HasValue)
                {
                    if (queueItem.Value.Blueprint != firstItem.Value.Blueprint)
                        CurrentProgress = 0f;
                }
            }

            base.MoveQueueItem(queueItemId, targetIdx);
        }

        public bool RepeatEnabled
        {
            get { return (m_disassembleEnabled) ? m_repeatDisassembleEnabled : m_repeatAssembleEnabled; }
            private set
            {
                if (m_disassembleEnabled)
                    SetRepeat(ref m_repeatDisassembleEnabled, value);
                else
                    SetRepeat(ref m_repeatAssembleEnabled, value);
            }
        }

        private void SetRepeat(ref bool currentValue, bool newValue)
        {
            if (currentValue != newValue)
            {
                currentValue = newValue;
                RebuildQueueInRepeatDisassembling();
                if (CurrentModeChanged != null)
                    CurrentModeChanged(this);
            }
        }

        private void SetSlave()
        {
            if (CurrentModeChanged != null)
            {
                CurrentModeChanged(this);
            }
        }

        public bool DisassembleEnabled
        {
            get { return m_disassembleEnabled; }
            private set
            {
                if (m_disassembleEnabled != value)
                {
                    CurrentProgress = 0f;

                    m_disassembleEnabled = value;
                    SwapQueue(ref m_otherQueue);
                    RebuildQueueInRepeatDisassembling();
                    UpdateInventoryFlags();
                    m_currentState = StateEnum.Ok;
                    if (CurrentModeChanged != null)
                        CurrentModeChanged(this);
                    if (CurrentStateChanged != null)
                        CurrentStateChanged(this);
                }
            }
        }

        private void OutputInventory_ContentsChanged(MyInventoryBase inventory)
        {
            if (DisassembleEnabled && RepeatEnabled && Sync.IsServer)
                RebuildQueueInRepeatDisassembling();
        }

        #region Multiplayer Callbacks

        public void RequestDisassembleEnabled(bool newDisassembleEnabled)
        {
            if (newDisassembleEnabled != DisassembleEnabled)
                MyMultiplayer.RaiseEvent(this, x => x.ModeSwitchCallback, newDisassembleEnabled);
        }

        [Event, Reliable, Server, Broadcast]
        private void ModeSwitchCallback(bool disassembleEnabled)
        {
            this.DisassembleEnabled = disassembleEnabled;
        }

        public void RequestRepeatEnabled(bool newRepeatEnable)
        {
            if (newRepeatEnable != RepeatEnabled)
                MyMultiplayer.RaiseEvent(this, x => x.RepeatEnabledCallback, this.DisassembleEnabled, newRepeatEnable);
        }

        [Event, Reliable, Server, Broadcast]
        private void RepeatEnabledCallback(bool disassembleEnabled, bool repeatEnable)
        {
            this.RepeatEnabledSuccess(disassembleEnabled, repeatEnable);
        }

        public void RequestSlaveEnabled(bool slaveModeEnable)
        {
            if (slaveModeEnable != IsSlave)
                MyMultiplayer.RaiseEvent(this, x => x.SlaveSwitchCallback, slaveModeEnable);
        }

        [Event, Reliable, Server, Broadcast]
        private void SlaveSwitchCallback(bool slaveModeEnabled)
        {
            this.IsSlave = slaveModeEnabled;
            this.SetSlave();
        }

        public void RequestDisassembleAll()
        {
            if (DisassembleEnabled && !RepeatEnabled)
                MyMultiplayer.RaiseEvent(this, x => x.DisassembleAllCallback);
        }

        [Event, Reliable, Server]
        private void DisassembleAllCallback()
        {
            this.DisassembleAllInOutput();
        }

        #endregion

        private void RepeatEnabledSuccess(bool disassembleMode, bool repeatEnabled)
        {
            if (disassembleMode)
                SetRepeat(ref m_repeatDisassembleEnabled, repeatEnabled);
            else
                SetRepeat(ref m_repeatAssembleEnabled, repeatEnabled);
        }

        private void RebuildQueueInRepeatDisassembling()
        {
            if (!DisassembleEnabled || !RepeatEnabled)
                return;

            DisassembleAllInOutput();
        }

        private void UpdateInventoryFlags()
        {
            OutputInventory.SetFlags(DisassembleEnabled ? MyInventoryFlags.CanReceive : MyInventoryFlags.CanSend);
            InputInventory.SetFlags(DisassembleEnabled ? MyInventoryFlags.CanSend : MyInventoryFlags.CanReceive);
        }

        private void DisassembleAllInOutput()
        {
            ClearQueue(sendEvent: false);

            var items = OutputInventory.GetItems();
            var toAdd = new List<Tuple<MyBlueprintDefinitionBase, MyFixedPoint>>();
            bool add = true;

            foreach (var item in items)
            {
                var blueprint = MyDefinitionManager.Static.TryGetBlueprintDefinitionByResultId(item.Content.GetId());
                if (blueprint != null)
                {
                    var tuple = Tuple.Create(blueprint, item.Amount);
                    toAdd.Add(tuple);
                }
                else
                {
                    add = false;
                    toAdd.Clear();
                    break;
                }
            }
            if (add)
            {
                foreach (var bp in toAdd)
                {
                    InsertQueueItemRequest(-1, bp.Item1, bp.Item2);
                }
                return;
            }

            InitializeInventoryCounts(inputInventory: false);

            MyFixedPoint disassembleAmount, remainingAmount;
            for (int i = 0; i < m_assemblerDef.BlueprintClasses.Count; ++i)
            {
                foreach (var blueprint in m_assemblerDef.BlueprintClasses[i])
                {
                    disassembleAmount = MyFixedPoint.MaxValue;
                    foreach (var result in blueprint.Results)
                    {
                        remainingAmount = 0;
                        m_tmpInventoryCounts.TryGetValue(result.Id, out remainingAmount);
                        if (remainingAmount == 0)
                        {
                            disassembleAmount = 0;
                            break;
                        }
                        disassembleAmount = MyFixedPoint.Min((MyFixedPoint)((double)remainingAmount / (double)result.Amount), disassembleAmount);
                    }

                    if (blueprint.Atomic) disassembleAmount = MyFixedPoint.Floor(disassembleAmount);

                    if (disassembleAmount > 0)
                    {
                        InsertQueueItemRequest(-1, blueprint, disassembleAmount);
                        foreach (var result in blueprint.Results)
                        {
                            m_tmpInventoryCounts.TryGetValue(result.Id, out remainingAmount);
                            remainingAmount -= result.Amount * disassembleAmount;
                            Debug.Assert(remainingAmount >= 0);
                            if (remainingAmount == 0)
                                m_tmpInventoryCounts.Remove(result.Id);
                            else
                                m_tmpInventoryCounts[result.Id] = remainingAmount;
                        }
                    }
                }
            }

            m_tmpInventoryCounts.Clear();
        }

        public void GetCoveyorInventoryOwners()
        {
            m_inventoryOwners.Clear();
            List<IMyConveyorEndpoint> reachableVertices = new List<IMyConveyorEndpoint>();
            MyGridConveyorSystem.FindReachable(this.ConveyorEndpoint, reachableVertices, (vertex) => vertex.CubeBlock != null && FriendlyWithBlock(vertex.CubeBlock) && vertex.CubeBlock.HasInventory);

            foreach (var vertex in reachableVertices)
            {
                m_inventoryOwners.Add(vertex.CubeBlock);
            }
            m_inventoryOwnersDirty = false;
        }

        public bool CheckConveyorResources(MyFixedPoint? amount, MyDefinitionId contentId)
        {
            foreach (var inv in m_inventoryOwners)
            {
                if (inv != null)
                {
                    var cargo = inv as MyEntity;

                    if (cargo != null && cargo.HasInventory)
                    {
                        System.Diagnostics.Debug.Assert((cargo.GetInventory(0) as MyInventory) != null, "Null or other inventory type!");

                        var flags = (cargo.GetInventory(0) as MyInventory).GetFlags();
                        var flag = MyInventoryFlags.CanSend | MyInventoryFlags.CanReceive;
                        List<MyInventory> inventories = new List<MyInventory>();

                        for (int i = 0; i < cargo.InventoryCount; i++)
                        {
                            System.Diagnostics.Debug.Assert((cargo.GetInventory(i) as MyInventory) != null, "Null or other inventory type!");
                            inventories.Add(cargo.GetInventory(i) as MyInventory);
                        }

                        foreach (var inventory in inventories)
                        {
                            if (inventory.ContainItems(amount, contentId) && ((flags == flag || flags == MyInventoryFlags.CanSend) || cargo == this))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        bool Sandbox.ModAPI.Ingame.IMyAssembler.DisassembleEnabled
        {
            get { return DisassembleEnabled; }
        }

        protected override float GetOperationalPowerConsumption()
        {
            return base.GetOperationalPowerConsumption() * (1f + UpgradeValues["Productivity"]) * (1f / UpgradeValues["PowerEfficiency"]);
        }

        protected override void OnInventoryAddedToAggregate(Inventory.MyInventoryAggregate aggregate, MyInventoryBase inventory)
        {
            base.OnInventoryAddedToAggregate(aggregate, inventory);

            if (inventory == OutputInventory)
            {
                if (Sync.IsServer)
                    OutputInventory.ContentsChanged += OutputInventory_ContentsChanged;
            }
        }

        protected override void OnBeforeInventoryRemovedFromAggregate(Inventory.MyInventoryAggregate aggregate, MyInventoryBase inventory)
        {
            base.OnBeforeInventoryRemovedFromAggregate(aggregate, inventory);
            if (inventory == OutputInventory)
            {
                if (Sync.IsServer)
                    OutputInventory.ContentsChanged -= OutputInventory_ContentsChanged;
            }
        }
    }
}
