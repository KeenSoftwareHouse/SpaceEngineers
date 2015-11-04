using System.Collections.Generic;
using System.Linq;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Conveyors;
using VRage.Import;
using VRageMath;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRageRender;
using System.Diagnostics;
using Sandbox.Game.EntityComponents;
using VRage.ModAPI;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_UpgradeModule))]
    class MyUpgradeModule : MyFunctionalBlock, ModAPI.IMyUpgradeModule
    {
        private ConveyorLinePosition[] m_connectionPositions;
        private Dictionary<ConveyorLinePosition, MyCubeBlock> m_connectedBlocks;
        private MyUpgradeModuleInfo[] m_upgrades;

        /// <summary>
        /// These are sorted so that dummy index and emissivity index match
        /// </summary>
        SortedDictionary<string, MyModelDummy> m_dummies;
        private bool m_needsRefresh;

        private MyResourceStateEnum m_oldResourceState = MyResourceStateEnum.NoPower;

        private new MyUpgradeModuleDefinition BlockDefinition
        {
            get { return (MyUpgradeModuleDefinition)base.BlockDefinition; }
        }

        public override void Init(Common.ObjectBuilders.MyObjectBuilder_CubeBlock builder, MyCubeGrid cubeGrid)
        {
            base.Init(builder, cubeGrid);

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;

            m_connectedBlocks = new Dictionary<ConveyorLinePosition, MyCubeBlock>();
            m_dummies = new SortedDictionary<string,MyModelDummy>(Engine.Models.MyModels.GetModelOnlyDummies(BlockDefinition.Model).Dummies);

            InitDummies();

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
            IsWorkingChanged += MyUpgradeModule_IsWorkingChanged;

            m_upgrades = BlockDefinition.Upgrades;
            UpdateIsWorking();
        }

        void MyUpgradeModule_IsWorkingChanged(MyCubeBlock obj)
        {
            RefreshEffects();
            UpdateEmissivity();
        }

        private void ComponentStack_IsFunctionalChanged()
        {
            UpdateIsWorking();
        }

        void CubeGrid_OnBlockRemoved(MySlimBlock obj)
        {
            if (obj != SlimBlock)
            {
                m_needsRefresh = true;
            }
        }

        void CubeGrid_OnBlockAdded(MySlimBlock obj)
        {
            if (obj != SlimBlock)
            {
                m_needsRefresh = true;
            }
        }

        public override void OnRegisteredToGridSystems()
        {
            base.OnRegisteredToGridSystems();

            InitDummies();

            m_needsRefresh = true;
            UpdateEmissivity();

            CubeGrid.OnBlockAdded += CubeGrid_OnBlockAdded;
            CubeGrid.OnBlockRemoved += CubeGrid_OnBlockRemoved;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (m_needsRefresh)
            {
                RefreshConnections();
                m_needsRefresh = false;
            }
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();
            if (CubeGrid.GridSystems.ResourceDistributor.ResourceState != m_oldResourceState)
            {
                m_oldResourceState = CubeGrid.GridSystems.ResourceDistributor.ResourceState;
                UpdateEmissivity();
            }

            m_oldResourceState = CubeGrid.GridSystems.ResourceDistributor.ResourceState;
        }

        private void InitDummies()
        {
            m_connectedBlocks.Clear();
            m_connectionPositions = MyMultilineConveyorEndpoint.GetLinePositions(this, m_dummies, "detector_upgrade");
            for (int i = 0; i < m_connectionPositions.Count(); i++)
            {
                m_connectionPositions[i] = MyMultilineConveyorEndpoint.PositionToGridCoords(m_connectionPositions[i], this);
                m_connectedBlocks.Add(m_connectionPositions[i], null);
            }
        }

        public override void OnModelChange()
        {
            base.OnModelChange();
            UpdateEmissivity();
        }

        private void RefreshConnections()
        {
            foreach (var connectorPos in m_connectionPositions)
            {
                var connectingPos = connectorPos.GetConnectingPosition();

                var slimBlock = CubeGrid.GetCubeBlock(connectingPos.LocalGridPosition);
                if (slimBlock != null && slimBlock.FatBlock != null)
                {
                    var newBlock = slimBlock.FatBlock;

                    MyCubeBlock connectedBlock = null;
                    m_connectedBlocks.TryGetValue(connectorPos, out connectedBlock);

                    if (newBlock != null && !newBlock.GetComponent().ConnectionPositions.Contains(connectingPos))
                    {
                        newBlock = null;
                    }

                    if (newBlock != null && !CanAffectBlock(newBlock))
                    {
                        newBlock = null;
                    }

                    if (newBlock != connectedBlock)
                    {
                        if (IsWorking)
                        {
                            if (connectedBlock != null)
                            {
                                RemoveEffectFromBlock(connectedBlock);
                            }
                            if (newBlock != null)
                            {
                                AddEffectToBlock(newBlock);
                            }
                        }

                        m_connectedBlocks[connectorPos] = newBlock;
                    }
                }
                else
                {
                    MyCubeBlock connectedBlock = null;
                    m_connectedBlocks.TryGetValue(connectorPos, out connectedBlock);

                    if (connectedBlock != null)
                    {
                        if (IsWorking)
                        {
                            RemoveEffectFromBlock(connectedBlock);
                        }

                        m_connectedBlocks[connectorPos] = null;
                    }
                }
            }
            UpdateEmissivity();
        }

        private void RefreshEffects()
        {
            foreach (var block in m_connectedBlocks.Values)
            {
                if (block == null)
                {
                    continue;
                }

                if (IsWorking)
                {
                    AddEffectToBlock(block);
                }
                else
                {
                    RemoveEffectFromBlock(block);
                }
            }
        }

        private bool CanAffectBlock(MyCubeBlock block)
        {
            foreach (var upgrade in m_upgrades)
            {
                if (block.UpgradeValues.ContainsKey(upgrade.UpgradeType))
                {
                    return true;
                }
            }

            return false;
        }

        private void RemoveEffectFromBlock(MyCubeBlock block)
        {
            foreach (var upgrade in m_upgrades)
            {
                float val;
                if (block.UpgradeValues.TryGetValue(upgrade.UpgradeType, out val))
                {
                    if (upgrade.ModifierType == MyUpgradeModifierType.Additive)
                    {
                        val -= upgrade.Modifier;

                        if (val < 0f)
                        {
                            val = 0f;
                            Debug.Fail("Additive modifier cannot be negative!");
                        }
                    }
                    else
                    {
                        val /= upgrade.Modifier;
                        if (val < 1f)
                        {
                            val = 1f;
                            Debug.Fail("Multiplicative modifier cannot be < 1.0f!");
                        }
                    }
                    block.UpgradeValues[upgrade.UpgradeType] = val;
                }
            }

            block.CommitUpgradeValues();
        }

        private void AddEffectToBlock(MyCubeBlock block)
        {
            foreach (var upgrade in m_upgrades)
            {
                float val;
                if (block.UpgradeValues.TryGetValue(upgrade.UpgradeType, out val))
                {
                    if (upgrade.ModifierType == MyUpgradeModifierType.Additive)
                    {
                        val += upgrade.Modifier;
                    }
                    else
                    {
                        val *= upgrade.Modifier;
                    }
                    block.UpgradeValues[upgrade.UpgradeType] = val;
                }
            }

            block.CommitUpgradeValues();
        }

        private void UpdateEmissivity()
        {
            if (m_connectedBlocks == null)
            {
                return;
            }

            for (int i = 0; i < m_connectionPositions.Length; i++)
            {
                string emissiveName = "Emissive" + i.ToString();
                Color color = Color.Green;

                MyCubeBlock connectedBlock = null;
                m_connectedBlocks.TryGetValue(m_connectionPositions[i], out connectedBlock);
                if (connectedBlock == null)
                {
                    color = Color.Red;
                }

                float emissivity = 1f;
                if (!IsWorking)
                {
                    emissivity = 0f;
                }

                if (m_oldResourceState != MyResourceStateEnum.Ok)
                {
                    emissivity = 0f;
                }

                if (Render.RenderObjectIDs[0] != MyRenderProxy.RENDER_ID_UNASSIGNED)
                {
                    VRageRender.MyRenderProxy.UpdateColorEmissivity(Render.RenderObjectIDs[0], 0, emissiveName, color, emissivity);
                }
            }
        }

        public override void OnUnregisteredFromGridSystems()
        {
            base.OnUnregisteredFromGridSystems();

            CubeGrid.OnBlockAdded -= CubeGrid_OnBlockAdded;
            CubeGrid.OnBlockRemoved -= CubeGrid_OnBlockRemoved;

            SlimBlock.ComponentStack.IsFunctionalChanged -= ComponentStack_IsFunctionalChanged;

            ClearConnectedBlocks();
        }

        private void ClearConnectedBlocks()
        {
            foreach (var connectedBlock in m_connectedBlocks.Values)
            {
                if (connectedBlock != null && IsWorking)
                {
                    RemoveEffectFromBlock(connectedBlock);
                }
            }

            m_connectedBlocks.Clear();
        }

        protected int GetBlockConnectionCount(MyCubeBlock cubeBlock)
        {
            int count = 0;
            foreach (var value in m_connectedBlocks.Values)
            {
                if (value == cubeBlock)
                {
                    count++;
                }
            }

            return count;
        }

        void ModAPI.Ingame.IMyUpgradeModule.GetUpgradeList(out List<MyUpgradeModuleInfo> upgradelist)
        {
            upgradelist = new List<MyUpgradeModuleInfo>();
            foreach (var value in m_upgrades)
                upgradelist.Add(value);
        }

        uint ModAPI.Ingame.IMyUpgradeModule.UpgradeCount
        {
            get
            {
                return (uint)m_upgrades.Count();
            }
        }

        uint ModAPI.Ingame.IMyUpgradeModule.Connections
        {
            get
            {
                uint count = 0;
                MyCubeBlock lastblock = null;
                foreach (var value in m_connectedBlocks.Values)
                {
                    if (lastblock == value)
                        continue;
                    if (value != null)
                    {
                        count++;
                        lastblock = value;
                    }
                }
                return count;
            }
        }
    }
}
