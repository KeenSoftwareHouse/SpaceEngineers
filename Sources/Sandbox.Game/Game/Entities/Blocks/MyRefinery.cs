using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.World;
using Sandbox.Game.Multiplayer;
using VRage.Utils;
using Sandbox.Game.GameSystems;
using VRage;
using Sandbox.ModAPI;
using Sandbox.Game.Localization;
using VRage.ObjectBuilders;
using System;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Profiler;

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Refinery))]
    public class MyRefinery : MyProductionBlock, IMyRefinery
    {
        private MyEntity m_currentUser;
        private MyRefineryDefinition m_refineryDef;
        private bool m_queueNeedsRebuild;
        private bool m_processingLock; // Signal to ignore all inventory contents changed events.

        private readonly List<KeyValuePair<int, MyBlueprintDefinitionBase>> m_tmpSortedBlueprints = new List<KeyValuePair<int, MyBlueprintDefinitionBase>>();

        public MyRefinery()
        {
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            // Need to be initialized before base.Init because when loading world with producting refinery
            // it will be missing when recompute power and cause disappearing of refinery.
            UpgradeValues.Add("Productivity", 0f);
            UpgradeValues.Add("Effectiveness", 1f);
            UpgradeValues.Add("PowerEfficiency", 1f);

            base.Init(objectBuilder, cubeGrid);

            MyDebug.AssertDebug(BlockDefinition is MyRefineryDefinition);
            m_refineryDef = BlockDefinition as MyRefineryDefinition;

            if (InventoryAggregate.InventoryCount > 2)
            {
                Debug.Fail("Inventory aggregate has to many inventories, probably wrong save. If you continue the unused inventories will be removed. Save the world to correct it. Please report this is if problem prevail.");

                FixInputOutputInventories(m_refineryDef.InputInventoryConstraint, m_refineryDef.OutputInventoryConstraint);
            }

            InputInventory.Constraint = m_refineryDef.InputInventoryConstraint;
            bool removed = InputInventory.FilterItemsUsingConstraint();
            Debug.Assert(!removed, "Inventory filter removed items which were present in the object builder.");

            OutputInventory.Constraint = m_refineryDef.OutputInventoryConstraint;
            removed = OutputInventory.FilterItemsUsingConstraint();
            Debug.Assert(!removed, "Inventory filter removed items which were present in the object builder.");

            m_queueNeedsRebuild = true;

            m_baseIdleSound = BlockDefinition.PrimarySound;
            m_processSound = BlockDefinition.ActionSound;

            ResourceSink.RequiredInputChanged += PowerReceiver_RequiredInputChanged;
            OnUpgradeValuesChanged += UpdateDetailedInfo;

            UpdateDetailedInfo();
            NeedsUpdate |= VRage.ModAPI.MyEntityUpdateEnum.EACH_100TH_FRAME;
        }       

        protected override void OnBeforeInventoryRemovedFromAggregate(Inventory.MyInventoryAggregate aggregate, MyInventoryBase inventory)
        {                        
            if (inventory == InputInventory)
            {
                InputInventory.ContentsChanged += inventory_OnContentsChanged;
            }
            else if (inventory == OutputInventory)
            {
                OutputInventory.ContentsChanged += inventory_OnContentsChanged;
            }
            else
            {
                Debug.Fail("Added inventory to aggregate, but not input or output invenoty?! This shouldn't happen.");
            }
            base.OnBeforeInventoryRemovedFromAggregate(aggregate, inventory); // Base method needs to be called here, cuz it removes the inventories from properties
        }

        protected override void OnInventoryAddedToAggregate(Inventory.MyInventoryAggregate aggregate, MyInventoryBase inventory)
        {
            base.OnInventoryAddedToAggregate(aggregate, inventory);
            if (inventory == InputInventory)
            {
                InputInventory.ContentsChanged += inventory_OnContentsChanged;
            }
            else if (inventory == OutputInventory)
            {
                OutputInventory.ContentsChanged += inventory_OnContentsChanged;
            }
            else
            {
                Debug.Fail("Added inventory to aggregate, but not input or output invenoty?! This shouldn't happen.");
            }
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();
            if (Sync.IsServer && IsWorking && m_useConveyorSystem)
            {
                if (InputInventory.VolumeFillFactor < 0.6f)
                {
                    if(MyGridConveyorSystem.PullAllRequest(this, InputInventory, OwnerId, InputInventory.Constraint))
                        m_queueNeedsRebuild = true;
                }
                if (OutputInventory.VolumeFillFactor > 0.75f)
                {
                    Debug.Assert(OutputInventory.GetItems().Count > 0);
                    MyGridConveyorSystem.PushAnyRequest(this, OutputInventory, OwnerId);
                }
            }
        }

        void PowerReceiver_RequiredInputChanged(MyDefinitionId resourceTypeId, MyResourceSinkComponent receiver, float oldRequirement, float newRequirement)
        {
            UpdateDetailedInfo();
        }

        private void inventory_OnContentsChanged(MyInventoryBase inv)
        {
            if (m_processingLock)
                return;

            if (Sync.IsServer)
                m_queueNeedsRebuild = true;
        }

        private void RebuildQueue()
        {
            Debug.Assert(Sync.IsServer);

            m_queueNeedsRebuild = false;
            ClearQueue(false);

            //Changed by Gregory: Allow for duplicate blueprints cause it should be a supported functionality to add resources of the same type more than once
            //So now the index is essentially given by input items of inventory. Maybe try something more efficient?
            m_tmpSortedBlueprints.Clear();
            var inputItems = InputInventory.GetItems();
            for (int indx = 0; indx < inputItems.Count; indx++)
            {
                for (int i = 0; i < m_refineryDef.BlueprintClasses.Count; ++i)
                {
                    foreach (var blueprint in m_refineryDef.BlueprintClasses[i])
                    {
                        bool found = false;
                        MyDefinitionId inputItemId = new MyDefinitionId(inputItems[indx].Content.TypeId, inputItems[indx].Content.SubtypeId);
                        for (int j = 0; j < blueprint.Prerequisites.Length; ++j)
                        {
                            if (blueprint.Prerequisites[j].Id.Equals(inputItemId))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found)
                        {
                            m_tmpSortedBlueprints.Add(new KeyValuePair<int, MyBlueprintDefinitionBase>(indx, blueprint));
                            break;
                        }
                    }
                }
            }


            MyFixedPoint buildAmount, remainingAmount;
            for (int i = 0; i < m_tmpSortedBlueprints.Count; i++)
            {
                var blueprint = m_tmpSortedBlueprints[i].Value;

                buildAmount = MyFixedPoint.MaxValue;
                foreach (var requirement in blueprint.Prerequisites)
                {
                    remainingAmount = inputItems[i].Amount;
                    if (remainingAmount == 0)
                    {
                        buildAmount = 0;
                        break;
                    }
                    buildAmount = MyFixedPoint.Min((remainingAmount * (1f / (float)requirement.Amount)), buildAmount);
                }

                if (blueprint.Atomic) buildAmount = MyFixedPoint.Floor(buildAmount);

                if (buildAmount > 0 && buildAmount != MyFixedPoint.MaxValue)
                {
                    InsertQueueItemRequest(-1, blueprint, buildAmount);
                }
            }

            m_tmpSortedBlueprints.Clear();
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
            DetailedInfo.Append("Effectiveness: ");
            DetailedInfo.Append(((UpgradeValues["Effectiveness"]) * 100f).ToString("F0"));
            DetailedInfo.Append("%\n");
            DetailedInfo.Append("Power Efficiency: ");
            DetailedInfo.Append(((UpgradeValues["PowerEfficiency"]) * 100f).ToString("F0"));
            DetailedInfo.Append("%\n");

            RaisePropertiesChanged();
        }

        protected override void UpdateProduction(int timeDelta)
        {
            ProfilerShort.Begin("Rebuild Queue");
            if (m_queueNeedsRebuild && (Sync.IsServer))
                RebuildQueue();

            ProfilerShort.BeginNextBlock("ProcessQueueItems");
            IsProducing = IsWorking && !IsQueueEmpty && !OutputInventory.IsFull;
            if (IsProducing)
                ProcessQueueItems(timeDelta);
            ProfilerShort.End();
        }

        private void ProcessQueueItems(int timeDelta)
        {
            // Prevent refreshing of queue.
            m_processingLock = true;

            if (Sync.IsServer)
            {
                while (!IsQueueEmpty && timeDelta > 0)
                {
                    var wrappedItem = TryGetFirstQueueItem();
                    Debug.Assert(wrappedItem.HasValue);
                    QueueItem queueItem = wrappedItem.Value;

                    // Compute how much blueprints could be processed in remaining time.
                    MyFixedPoint blueprintsProcessed = (MyFixedPoint)((timeDelta * (m_refineryDef.RefineSpeed + UpgradeValues["Productivity"]) * MySession.Static.RefinerySpeedMultiplier) / (queueItem.Blueprint.BaseProductionTimeInSeconds * 1000));

                    // Fix that number according to how much items are in the inventory
                    foreach (var prerequisite in queueItem.Blueprint.Prerequisites)
                    {
                        MyFixedPoint oreAvailable = InputInventory.GetItemAmount(prerequisite.Id);
                        MyFixedPoint oreProcessed = blueprintsProcessed * prerequisite.Amount;
                        if (oreAvailable < oreProcessed)
                        {
                            blueprintsProcessed = oreAvailable * (1f / (float)prerequisite.Amount);
                        }
                    }

                    //GR: This assertion happens on last item to be removed when allowing duplicate blueprints. The queue is emptied but with small delay. Synchronization needed?
                    //Debug.Assert(blueprintsProcessed > 0, "No items in inventory but there are blueprints in the queue!");
                    if (blueprintsProcessed == 0)
                    {
                        //GR: For now comment out bcause it spams the log on servers on occasions
                        //MySandboxGame.Log.WriteLine("MyRefinery.ProcessQueueItems: Inventory empty while there are still blueprints in the queue!");
                        m_queueNeedsRebuild = true;
                        break;
                    }

                    // Math.Max has to be here to avoid situations in which timeDelta wouldn't change at all.
                    // Basically, this means that the refinery cannot spend less time processing items than 1ms, which is OK, I guess
                    timeDelta = timeDelta - System.Math.Max(1, (int)((float)blueprintsProcessed * queueItem.Blueprint.BaseProductionTimeInSeconds / m_refineryDef.RefineSpeed * 1000));
                    if (timeDelta < 0) timeDelta = 0;

                    ChangeRequirementsToResults(queueItem.Blueprint, blueprintsProcessed);
                }
            }

            IsProducing = !IsQueueEmpty;
            m_processingLock = false;
        }

        private void ChangeRequirementsToResults(MyBlueprintDefinitionBase queueItem, MyFixedPoint blueprintAmount)
        {
            Debug.Assert(Sync.IsServer);

            Debug.Assert(m_refineryDef != null, "m_refineryDef shouldn't be null!!!");
            if (m_refineryDef == null)
            {
                MyLog.Default.WriteLine("m_refineryDef shouldn't be null!!!" + this);
                return;
            }

            if(Sync.IsServer == false)
            {
                return;
            }

            if (MySession.Static == null || queueItem == null || queueItem.Prerequisites == null || OutputInventory == null || InputInventory == null || queueItem.Results == null || m_refineryDef == null) 
            {
                return;
            }

            if (!MySession.Static.CreativeMode)
            {
                blueprintAmount = MyFixedPoint.Min(OutputInventory.ComputeAmountThatFits(queueItem), blueprintAmount);
            }

            if (blueprintAmount == 0)
                return;

            foreach (var prerequisite in queueItem.Prerequisites)
            {
                MyObjectBuilder_PhysicalObject obPrerequisite = MyObjectBuilderSerializer.CreateNewObject(prerequisite.Id) as MyObjectBuilder_PhysicalObject;
                if (obPrerequisite == null)
                {
                    Debug.Fail("obPrerequisite shouldn't be null!!!");
                    MyLog.Default.WriteLine("obPrerequisite shouldn't be null!!! " + this);
                    continue;
                }
                var prerequisiteAmount = blueprintAmount * prerequisite.Amount;
                InputInventory.RemoveItemsOfType(prerequisiteAmount, obPrerequisite);
            }

            foreach (var result in queueItem.Results)
            {
                var resultId = result.Id;
                MyObjectBuilder_PhysicalObject obResult = MyObjectBuilderSerializer.CreateNewObject(resultId) as MyObjectBuilder_PhysicalObject;
                if (obResult == null)
                {
                    Debug.Fail("obResult shouldn't be null!!!");
                    MyLog.Default.WriteLine("obResult shouldn't be null!!! " + this);
                    continue;
                }
                var conversionRatio = result.Amount * m_refineryDef.MaterialEfficiency * UpgradeValues["Effectiveness"];
                if (conversionRatio > (MyFixedPoint)1.0f)
                {
                    conversionRatio = (MyFixedPoint)1.0f;
                }

                var resultAmount = blueprintAmount * conversionRatio;
                OutputInventory.AddItems(resultAmount, obResult);
            }

            RemoveFirstQueueItemAnnounce(blueprintAmount);
        }

        protected override float GetOperationalPowerConsumption()
        {
            return base.GetOperationalPowerConsumption() * (1f + UpgradeValues["Productivity"]) * (1f / UpgradeValues["PowerEfficiency"]);
        }
    }
}
