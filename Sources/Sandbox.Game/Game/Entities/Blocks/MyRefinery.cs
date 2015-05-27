using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.World;

using VRage.Trace;
using VRageMath;
using Sandbox.Game.Multiplayer;
using VRage.Utils;
using Sandbox.Game.Screens;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.GameSystems;
using VRage;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.Localization;
using Sandbox.Game.Entities.Interfaces;

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Refinery))]
    class MyRefinery : MyProductionBlock, IMyRefinery
    {
        private MyEntity m_currentUser;
        private MyRefineryDefinition m_refineryDef;
        private bool m_queueNeedsRebuild;
        private bool m_processingLock; // Signal to ignore all inventory contents changed events.

        private List<KeyValuePair<int, MyBlueprintDefinitionBase>> m_tmpSortedBlueprints = new List<KeyValuePair<int, MyBlueprintDefinitionBase>>();

        public MyRefinery() :
            base()
        {
            m_baseIdleSound.Init("BlockRafinery");
            m_processSound.Init("BlockRafineryProcess");
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            MyDebug.AssertDebug(BlockDefinition is MyRefineryDefinition);
            m_refineryDef = BlockDefinition as MyRefineryDefinition;

            InputInventory.Constraint = m_refineryDef.InputInventoryConstraint;
            bool removed = InputInventory.FilterItemsUsingConstraint();
            Debug.Assert(!removed, "Inventory filter removed items which were present in the object builder.");
            InputInventory.ContentsChanged += inventory_OnContentsChanged;

            OutputInventory.Constraint = m_refineryDef.OutputInventoryConstraint;
            removed = OutputInventory.FilterItemsUsingConstraint();
            Debug.Assert(!removed, "Inventory filter removed items which were present in the object builder.");
            OutputInventory.ContentsChanged += inventory_OnContentsChanged;

            m_queueNeedsRebuild = true;

            UpgradeValues.Add("Productivity", 0f);
            UpgradeValues.Add("Effectiveness", 1f);
            UpgradeValues.Add("PowerEfficiency", 1f);

            PowerReceiver.RequiredInputChanged += PowerReceiver_RequiredInputChanged;
            OnUpgradeValuesChanged += UpdateDetailedInfo;

            UpdateDetailedInfo();
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();
            if (Sync.IsServer && IsWorking && m_useConveyorSystem)
            {
                if (InputInventory.VolumeFillFactor < 0.6f)
                {
                    MyGridConveyorSystem.PullAllRequest(this, InputInventory, OwnerId, InputInventory.Constraint);
                }
                if (OutputInventory.VolumeFillFactor > 0.75f)
                {
                    Debug.Assert(OutputInventory.GetItems().Count > 0);
                    MyGridConveyorSystem.PushAnyRequest(this, OutputInventory, OwnerId);
                }
            }
        }

        void PowerReceiver_RequiredInputChanged(GameSystems.Electricity.MyPowerReceiver receiver, float oldRequirement, float newRequirement)
        {
            UpdateDetailedInfo();
        }
        
        private void inventory_OnContentsChanged(MyInventory inv)
        {
            if (m_processingLock)
                return;

            if (Sync.IsServer)
                m_queueNeedsRebuild = true;
        }

        private void RebuildQueue()
        {
            Debug.Assert(Sync.IsServer || !MyFakes.ENABLE_PRODUCTION_SYNC);

            m_queueNeedsRebuild = false;
            ClearQueue(false);

            InitializeInventoryCounts(inputInventory: true);

            // Find all blueprints that contain as a prerequisite any item from the input inventory and sort them by the input inventory
            // index of the first item found.
            m_tmpSortedBlueprints.Clear();
            var inputItems = InputInventory.GetItems();
            for (int i = 0; i < m_refineryDef.BlueprintClasses.Count; ++i)
            {
                foreach (var blueprint in m_refineryDef.BlueprintClasses[i])
                {
                    int firstRequirementIndex = 0;
                    bool found = false;
                    while (firstRequirementIndex < inputItems.Count)
                    {
                        MyDefinitionId inputItemId = new MyDefinitionId(inputItems[firstRequirementIndex].Content.TypeId, inputItems[firstRequirementIndex].Content.SubtypeId);
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
                            m_tmpSortedBlueprints.Add(new KeyValuePair<int, MyBlueprintDefinitionBase>(firstRequirementIndex, blueprint));
                            break;
                        }
                        firstRequirementIndex++;
                    }
                }
            }
            m_tmpSortedBlueprints.Sort((pair1, pair2) => pair1.Key - pair2.Key);


            MyFixedPoint buildAmount, remainingAmount;
            foreach (var pair in m_tmpSortedBlueprints)
            {
                var blueprint = pair.Value;

                buildAmount = MyFixedPoint.MaxValue;
                foreach (var requirement in blueprint.Prerequisites)
                {
                    remainingAmount = 0;
                    m_tmpInventoryCounts.TryGetValue(requirement.Id, out remainingAmount);
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
                    foreach (var prerequisite in blueprint.Prerequisites)
                    {
                        m_tmpInventoryCounts.TryGetValue(prerequisite.Id, out remainingAmount);
                        remainingAmount -= prerequisite.Amount * buildAmount;
                        //Debug.Assert(remainingAmount >= 0);
                        if (remainingAmount == 0)
                            m_tmpInventoryCounts.Remove(prerequisite.Id);
                        else
                            m_tmpInventoryCounts[prerequisite.Id] = remainingAmount;
                    }
                }
            }

            m_tmpSortedBlueprints.Clear();
            m_tmpInventoryCounts.Clear();
        }

        private void UpdateDetailedInfo()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.AppendFormat("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(GetOperationalPowerConsumption(), DetailedInfo);
            DetailedInfo.AppendFormat("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_RequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(PowerReceiver.RequiredInput, DetailedInfo);

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
            if (!MyFakes.OCTOBER_RELEASE_REFINERY_ENABLED)
                return;

            if (m_queueNeedsRebuild && (Sync.IsServer || !MyFakes.ENABLE_PRODUCTION_SYNC))
                RebuildQueue();

            IsProducing = IsWorking && !IsQueueEmpty && !OutputInventory.IsFull;
            if (IsProducing)
                ProcessQueueItems(timeDelta);
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

                    Debug.Assert(blueprintsProcessed > 0, "No items in inventory but there are blueprints in the queue!");
                    if (blueprintsProcessed == 0)
                    {
                        MySandboxGame.Log.WriteLine("MyRefinery.ProcessQueueItems: Inventory empty while there are still blueprints in the queue!");
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

            if (!MySession.Static.CreativeMode)
            {
                blueprintAmount = MyFixedPoint.Min(OutputInventory.ComputeAmountThatFits(queueItem), blueprintAmount);
            }
            if (blueprintAmount == 0)
                return;

            foreach (var prerequisite in queueItem.Prerequisites)
            {
                var obPrerequisite = (MyObjectBuilder_PhysicalObject)Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.CreateNewObject(prerequisite.Id);
                var prerequisiteAmount = blueprintAmount * prerequisite.Amount;
                InputInventory.RemoveItemsOfType(prerequisiteAmount, obPrerequisite);
            }

            foreach (var result in queueItem.Results)
            {
                var resultId = result.Id;
                var obResult = (MyObjectBuilder_PhysicalObject)Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.CreateNewObject(resultId);

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
