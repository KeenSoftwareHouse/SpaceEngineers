using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sandbox.Game.EntityComponents;
using VRage;
using VRage.Algorithms;
using VRage.Collections;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRageRender;
using Sandbox.ModAPI.Interfaces;
using VRage.Game;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.Game.ModAPI.Ingame;
using Sandbox.Game.Localization;
using VRage.Profiler;
using Sandbox.Engine.Utils;

namespace Sandbox.Game.GameSystems
{
    public class MyGridConveyorSystem
    {
        private static readonly float CONVEYOR_SYSTEM_CONSUMPTION = 0.005f;

        readonly HashSet<MyCubeBlock> m_inventoryBlocks = new HashSet<MyCubeBlock>();
        readonly HashSet<IMyConveyorEndpointBlock> m_conveyorEndpointBlocks = new HashSet<IMyConveyorEndpointBlock>();
        readonly HashSet<MyConveyorLine> m_lines = new HashSet<MyConveyorLine>();

        readonly HashSet<MyShipConnector> m_connectors = new HashSet<MyShipConnector>();

        public event Action<MyCubeBlock> BlockAdded;
        public event Action<MyCubeBlock> BlockRemoved;

        public event Action<IMyConveyorEndpointBlock> OnBeforeRemoveEndpointBlock;
        public event Action<IMyConveyorSegmentBlock> OnBeforeRemoveSegmentBlock;

        private MyCubeGrid m_grid;

        private bool m_needsRecomputation = true;
        private HashSet<MyCubeGrid> m_tmpConnectedGrids = new HashSet<MyCubeGrid>();

        [ThreadStatic]
        private static List<ConveyorLinePosition> m_tmpConveyorPositionListPerThread = new List<ConveyorLinePosition>(6);
        private static List<ConveyorLinePosition> m_tmpConveyorPositionList { get { if (m_tmpConveyorPositionListPerThread == null) m_tmpConveyorPositionListPerThread = new List<ConveyorLinePosition>(6); return m_tmpConveyorPositionListPerThread; } }

        [ThreadStatic]
        private static List<MyPhysicalInventoryItem> m_tmpInventoryItemsPerThread;
        private static List<MyPhysicalInventoryItem> m_tmpInventoryItems { get { if (m_tmpInventoryItemsPerThread == null) m_tmpInventoryItemsPerThread = new List<MyPhysicalInventoryItem>(); return m_tmpInventoryItemsPerThread; } }

        [ThreadStatic]
        private static List<MyTuple<IMyConveyorEndpointBlock, MyPhysicalInventoryItem>> m_tmpPullRequestsPerThread;
        private static List<MyTuple<IMyConveyorEndpointBlock, MyPhysicalInventoryItem>> m_tmpPullRequests { get { if (m_tmpPullRequestsPerThread == null) m_tmpPullRequestsPerThread = new List<MyTuple<IMyConveyorEndpointBlock, MyPhysicalInventoryItem>>(); return m_tmpPullRequestsPerThread; } }

        [ThreadStatic]
        private static PullRequestItemSet m_tmpRequestedItemSetPerThread;
        private static PullRequestItemSet m_tmpRequestedItemSet { get { if (m_tmpRequestedItemSetPerThread == null) m_tmpRequestedItemSetPerThread = new PullRequestItemSet(); return m_tmpRequestedItemSetPerThread; } }

        [ThreadStatic]
        private static MyPathFindingSystem<IMyConveyorEndpoint> m_pathfinding = new MyPathFindingSystem<IMyConveyorEndpoint>();
        private static MyPathFindingSystem<IMyConveyorEndpoint> Pathfinding
        {
            get
            {
                if (m_pathfinding == null)
                    m_pathfinding = new MyPathFindingSystem<IMyConveyorEndpoint>();
                return m_pathfinding;
            }
        }

        private static Dictionary<Tuple<IMyConveyorEndpointBlock, IMyConveyorEndpointBlock>, ParallelTasks.Task> m_currentTransferComputationTasks = new Dictionary<Tuple<IMyConveyorEndpointBlock, IMyConveyorEndpointBlock>, ParallelTasks.Task>();

        private Dictionary<ConveyorLinePosition, MyConveyorLine> m_lineEndpoints;
        private Dictionary<Vector3I, MyConveyorLine> m_linePoints;
        private HashSet<MyConveyorLine> m_deserializedLines;

        public bool IsClosing = false;

        public MyStringId HudMessage = MyStringId.NullOrEmpty;

        public MyResourceSinkComponent ResourceSink
        {
            get;
            private set;
        }

        public bool IsInteractionPossible
        {
            get
            {
                bool inConstraint = false;
                foreach (var connector in m_connectors)
                {
                    inConstraint |= connector.InConstraint;
                }
                return inConstraint;
            }
        }

        public bool Connected
        {
            get
            {
                bool connected = false;
                foreach (var connector in m_connectors)
                {
                    connected |= connector.Connected;
                }
                return connected;
            }
        }

        public MyGridConveyorSystem(MyCubeGrid grid)
        {
            m_grid = grid;

            m_lineEndpoints = null;
            m_linePoints = null;
            m_deserializedLines = null;

            ResourceSink = new MyResourceSinkComponent();
            ResourceSink.Init(
                MyStringHash.GetOrCompute("Conveyors"),
                CONVEYOR_SYSTEM_CONSUMPTION,
                CalculateConsumption);

            ResourceSink.IsPoweredChanged += Receiver_IsPoweredChanged;
            ResourceSink.Update();
        }

        public void BeforeBlockDeserialization(List<MyObjectBuilder_ConveyorLine> lines)
        {
            ProfilerShort.Begin("ConveyorSystem.BeforeBlockDeserialization()");
            if (lines == null)
            {
                ProfilerShort.End();
                return;
            }

            m_lineEndpoints = new Dictionary<ConveyorLinePosition, MyConveyorLine>(lines.Count * 2);
            m_linePoints = new Dictionary<Vector3I, MyConveyorLine>(lines.Count * 4);
            m_deserializedLines = new HashSet<MyConveyorLine>();

            foreach (var lineBuilder in lines)
            {
                MyConveyorLine line = new MyConveyorLine();
                line.Init(lineBuilder, m_grid);
                if (!line.CheckSectionConsistency()) continue;

                ConveyorLinePosition start = new ConveyorLinePosition(lineBuilder.StartPosition, lineBuilder.StartDirection);
                ConveyorLinePosition end = new ConveyorLinePosition(lineBuilder.EndPosition, lineBuilder.EndDirection);

                try
                {
                    m_lineEndpoints.Add(start, line);
                    m_lineEndpoints.Add(end, line);

                    foreach (var position in line)
                    {
                        m_linePoints.Add(position, line);
                    }

                    m_deserializedLines.Add(line);
                    m_lines.Add(line);
                }
                catch (ArgumentException)
                {
                    // Something was wrong in the conveyor line serialization. Display an assert, but don't crash.
                    Debug.Assert(false, "Problem with deserializing lines. Recalculating all lines from scratch...");
                    // Reset the deserialization structures and rebuild the conveyor lines anew
                    m_lineEndpoints = null;
                    m_deserializedLines = null;
                    m_linePoints = null;
                    m_lines.Clear();
                    break;
                }
            }
            ProfilerShort.End();
        }

        public MyConveyorLine GetDeserializingLine(ConveyorLinePosition position)
        {
            if (m_lineEndpoints == null) return null;

            MyConveyorLine retval;
            m_lineEndpoints.TryGetValue(position, out retval);
            return retval;
        }

        public MyConveyorLine GetDeserializingLine(Vector3I position)
        {
            if (m_linePoints == null) return null;

            MyConveyorLine retval;
            m_linePoints.TryGetValue(position, out retval);
            return retval;
        }

        public void AfterBlockDeserialization()
        {
            ProfilerShort.Begin("ConveyorSystem.AfterBlockDeserialization()");
            m_lineEndpoints = null;
            m_linePoints = null;
            m_deserializedLines = null;

            foreach (var line in m_lines)
            {
                line.UpdateIsFunctional();
            }
            ProfilerShort.End();
        }

        public void SerializeLines(List<MyObjectBuilder_ConveyorLine> resultList)
        {
            foreach (var line in m_lines)
            {
                // Empty disconnected lines of length 1 can be created anew during deserialization
                if (line.IsEmpty && line.IsDisconnected && line.Length == 1)
                    continue;

                resultList.Add(line.GetObjectBuilder());
            }
        }

        public void AfterGridClose()
        {
            m_lines.Clear();
        }

        public void Add(MyCubeBlock block)
        {
            bool added = m_inventoryBlocks.Add(block);
            System.Diagnostics.Debug.Assert(added, "Double add");

            var handler = BlockAdded;
            if (handler != null) handler(block);
        }

        public void Remove(MyCubeBlock block)
        {
            bool removed = m_inventoryBlocks.Remove(block);
            System.Diagnostics.Debug.Assert(removed, "Double remove or removing something not added");

            var handler = BlockRemoved;
            if (handler != null) handler(block);
        }

        public void AddConveyorBlock(IMyConveyorEndpointBlock endpointBlock)
        {
            // Invalidate iterator and add block
            m_endpointIterator = null;
            m_conveyorEndpointBlocks.Add(endpointBlock);

            if (endpointBlock is MyShipConnector)
                m_connectors.Add(endpointBlock as MyShipConnector);

            m_tmpConveyorPositionList.Clear();
            var endpoint = endpointBlock.ConveyorEndpoint;

            for (int i = 0; i < endpoint.GetLineCount(); ++i)
            {
                var position = endpoint.GetPosition(i);
                var line = endpoint.GetConveyorLine(i);

                if (m_deserializedLines != null && m_deserializedLines.Contains(line))
                    continue;

                var otherBlock = m_grid.GetCubeBlock(position.NeighbourGridPosition);
                if (otherBlock == null)
                {
                    m_lines.Add(line);
                    continue;
                }

                var otherEndpointBlock = otherBlock.FatBlock as IMyConveyorEndpointBlock;
                var otherSegmentBlock = otherBlock.FatBlock as IMyConveyorSegmentBlock;

                if (otherSegmentBlock != null)
                {
                    if (!TryMergeEndpointSegment(endpointBlock, otherSegmentBlock, position))
                    {
                        m_lines.Add(line);
                    }
                }
                else if (otherEndpointBlock != null)
                {
                    if (!TryMergeEndpointEndpoint(endpointBlock, otherEndpointBlock, position, position.GetConnectingPosition()))
                    {
                        m_lines.Add(line);
                    }
                }
                else
                {
                    m_lines.Add(line);
                }
            }
            m_tmpConveyorPositionList.Clear();
        }

        public void DebugDraw(MyCubeGrid grid)
        {
            foreach (var line in m_lines)
            {
                line.DebugDraw(grid);
            }

            MyRenderProxy.DebugDrawText2D(new Vector2(1.0f, 1.0f), "Conveyor lines: " + m_lines.Count, Color.Red, 1.0f);
        }

        public void DebugDrawLinePackets()
        {
            foreach (var line in m_lines)
            {
                line.DebugDrawPackets();
            }
        }

        public void PrepareForDraw()
        {
            if (MySandboxGame.IsPaused) return;

            foreach (var line in m_lines)
            {
                line.PrepareForDraw(m_grid);
            }
        }

        public void UpdateBeforeSimulation()
        {
            MySimpleProfiler.Begin("Conveyor");
            foreach (var line in m_lines)
            {
                if (!line.IsEmpty)
                    line.Update();
            }
            MySimpleProfiler.End("Conveyor");
        }

        public void UpdateBeforeSimulation10()
        {
            MySimpleProfiler.Begin("Conveyor");
            ResourceSink.Update();
            MySimpleProfiler.End("Conveyor");
        }

        public void FlagForRecomputation()
        {
            // Get all connected grids
            m_tmpConnectedGrids.Clear();
            if (m_grid != null && m_grid.GridSystems != null && m_grid.GridSystems.TerminalSystem != null)
            {
                foreach (var block in m_grid.GridSystems.TerminalSystem.Blocks)
                {
                    if (!m_tmpConnectedGrids.Contains(block.CubeGrid))
                        m_tmpConnectedGrids.Add(block.CubeGrid);
                }
            }

            // Flag all of them for recomputation
            m_needsRecomputation = true;
            foreach (MyCubeGrid grid in m_tmpConnectedGrids)
            {
                if (grid.GridSystems != null && grid.GridSystems.ConveyorSystem != null)
                    grid.GridSystems.ConveyorSystem.m_needsRecomputation = true;
            }
        }

        public void UpdateAfterSimulation100()
        {
            if (m_needsRecomputation)
            {
                MySimpleProfiler.Begin("Conveyor");
                RecomputeConveyorEndpoints();
                m_needsRecomputation = false;
                MySimpleProfiler.End("Conveyor");
            }
        }

        void Receiver_IsPoweredChanged()
        {
            FlagForRecomputation();
            foreach (var line in m_lines)
            {
                line.UpdateIsWorking();
            }
        }

        public float CalculateConsumption()
        {
            float consumption = 0.0f;

            foreach (var line in m_lines)
            {
                if (!line.IsFunctional)
                    continue;

                consumption += line.Length;
            }

            consumption *= MyEnergyConstants.REQUIRED_INPUT_CONVEYOR_LINE;

            return consumption;
        }

        public void RemoveConveyorBlock(IMyConveyorEndpointBlock block)
        {
            // Invalidate iterator and remove block
            m_endpointIterator = null;
            m_conveyorEndpointBlocks.Remove(block);

            if (block is MyShipConnector)
                m_connectors.Remove(block as MyShipConnector);

            if (IsClosing) return;

            if (OnBeforeRemoveEndpointBlock != null)
                OnBeforeRemoveEndpointBlock(block);

            for (int i = 0; i < block.ConveyorEndpoint.GetLineCount(); ++i)
            {
                MyConveyorLine line = block.ConveyorEndpoint.GetConveyorLine(i);
                line.DisconnectEndpoint(block.ConveyorEndpoint);

                if (line.IsDegenerate)
                    m_lines.Remove(line);
            }
        }

        public void AddSegmentBlock(IMyConveyorSegmentBlock segmentBlock)
        {
            // Old line unregistering is done in the internal functions
            AddSegmentBlockInternal(segmentBlock, segmentBlock.ConveyorSegment.ConnectingPosition1);
            AddSegmentBlockInternal(segmentBlock, segmentBlock.ConveyorSegment.ConnectingPosition2);

            // Registering of the new line, if needed (the line can already be registered, if it was created by merging)
            if (!m_lines.Contains(segmentBlock.ConveyorSegment.ConveyorLine) && segmentBlock.ConveyorSegment.ConveyorLine != null)
            {
                m_lines.Add(segmentBlock.ConveyorSegment.ConveyorLine);
            }
        }

        public void RemoveSegmentBlock(IMyConveyorSegmentBlock segmentBlock)
        {
            if (IsClosing) return;

            if (OnBeforeRemoveSegmentBlock != null)
                OnBeforeRemoveSegmentBlock(segmentBlock);

            MyConveyorLine oldLine = segmentBlock.ConveyorSegment.ConveyorLine;
            MyConveyorLine newLine = segmentBlock.ConveyorSegment.ConveyorLine.RemovePortion(segmentBlock.ConveyorSegment.ConnectingPosition1.NeighbourGridPosition, segmentBlock.ConveyorSegment.ConnectingPosition2.NeighbourGridPosition);

            // Old line or new line can be empty after splitting. If the new line would be empty, Split(...) will return null
            // We have to unregister only old line
            if (oldLine.IsDegenerate)
                m_lines.Remove(oldLine);
            if (newLine == null)
                return;

            UpdateLineReferences(newLine, newLine);

            // The new line will always need to be registered
            Debug.Assert(!newLine.IsDegenerate);
            m_lines.Add(newLine);
        }

        private void AddSegmentBlockInternal(IMyConveyorSegmentBlock segmentBlock, ConveyorLinePosition connectingPosition)
        {
            var otherBlock = m_grid.GetCubeBlock(connectingPosition.LocalGridPosition);
            if (otherBlock != null)
            {
                if (m_deserializedLines != null && m_deserializedLines.Contains(segmentBlock.ConveyorSegment.ConveyorLine))
                    return;

                var otherConveyorBlock = otherBlock.FatBlock as IMyConveyorEndpointBlock;
                var otherSegmentBlock = otherBlock.FatBlock as IMyConveyorSegmentBlock;

                if (otherSegmentBlock != null)
                {
                    var oldLine = segmentBlock.ConveyorSegment.ConveyorLine;
                    if (m_lines.Contains(oldLine))
                    {
                        m_lines.Remove(oldLine);
                    }

                    if (otherSegmentBlock.ConveyorSegment.CanConnectTo(connectingPosition, segmentBlock.ConveyorSegment.ConveyorLine.Type))
                        MergeSegmentSegment(segmentBlock, otherSegmentBlock);
                }
                if (otherConveyorBlock != null)
                {
                    var oldLine = otherConveyorBlock.ConveyorEndpoint.GetConveyorLine(connectingPosition);
                    if (TryMergeEndpointSegment(otherConveyorBlock, segmentBlock, connectingPosition))
                    {
                        m_lines.Remove(oldLine);
                    }
                }
            }
        }

        /// <summary>
        /// Tries to merge the conveyor lines of a conveyor block and segment block.
        /// Also changes the reference in the endpoint block to the correct line.
        /// </summary>
        private bool TryMergeEndpointSegment(IMyConveyorEndpointBlock endpoint, IMyConveyorSegmentBlock segmentBlock, ConveyorLinePosition endpointPosition)
        {
            MyConveyorLine endpointLine = endpoint.ConveyorEndpoint.GetConveyorLine(endpointPosition);
            if (endpointLine == null) return false;

            // The conveyor segment cannot merge with the given endpoint
            if (!segmentBlock.ConveyorSegment.CanConnectTo(endpointPosition.GetConnectingPosition(), endpointLine.Type))
                return false;

            MyConveyorLine segmentLine = segmentBlock.ConveyorSegment.ConveyorLine;

            segmentLine.Merge(endpointLine, segmentBlock);
            endpoint.ConveyorEndpoint.SetConveyorLine(endpointPosition, segmentLine);
            endpointLine.RecalculateConductivity();
            segmentLine.RecalculateConductivity();
            return true;
        }

        private bool TryMergeEndpointEndpoint(IMyConveyorEndpointBlock endpointBlock1, IMyConveyorEndpointBlock endpointBlock2, ConveyorLinePosition pos1, ConveyorLinePosition pos2)
        {
            MyConveyorLine line1 = endpointBlock1.ConveyorEndpoint.GetConveyorLine(pos1);
            if (line1 == null)
                return false;

            MyConveyorLine line2 = endpointBlock2.ConveyorEndpoint.GetConveyorLine(pos2);
            if (line2 == null)
                return false;

            if (line1.Type != line2.Type)
                return false;

            if (line1.GetEndpoint(1) == null)
                line1.Reverse();
            Debug.Assert(line1.GetEndpoint(1) != null);
            if (line2.GetEndpoint(0) == null)
                line2.Reverse();
            Debug.Assert(line2.GetEndpoint(0) != null);

            line2.Merge(line1);
            endpointBlock1.ConveyorEndpoint.SetConveyorLine(pos1, line2);
            line1.RecalculateConductivity();
            line2.RecalculateConductivity();

            return true;
        }

        private void MergeSegmentSegment(IMyConveyorSegmentBlock newSegmentBlock, IMyConveyorSegmentBlock oldSegmentBlock)
        {
            MyConveyorLine line1 = newSegmentBlock.ConveyorSegment.ConveyorLine;
            MyConveyorLine line2 = oldSegmentBlock.ConveyorSegment.ConveyorLine;

            // Creating a cycle - no need to merge anything
            if (line1 != line2)
            {
                line2.Merge(line1, newSegmentBlock);
            }

            UpdateLineReferences(line1, line2);

            // newSegmentBlock is a newly created block, so we have to update its line reference as well
            newSegmentBlock.ConveyorSegment.SetConveyorLine(line2);
        }

        private void UpdateLineReferences(MyConveyorLine oldLine, MyConveyorLine newLine)
        {
            for (int i = 0; i < 2; ++i)
            {
                if (oldLine.GetEndpoint(i) != null)
                {
                    oldLine.GetEndpoint(i).SetConveyorLine(oldLine.GetEndpointPosition(i), newLine);
                }
            }

            foreach (var position in oldLine)
            {
                var block = m_grid.GetCubeBlock(position);
                if (block == null)
                    continue;

                var segmentBlock = block.FatBlock as IMyConveyorSegmentBlock;
                Debug.Assert(segmentBlock != null, "Conveyor line was going through a non-segment block");
                if (segmentBlock == null)
                    continue;

                segmentBlock.ConveyorSegment.SetConveyorLine(newLine);
            }
            oldLine.RecalculateConductivity();
            newLine.RecalculateConductivity();
        }

        public void ToggleConnectors()
        {
            bool connected = false;
            foreach (var connector in m_connectors)
            {
                connected |= connector.Connected;
            }

            foreach (var connector in m_connectors)
            {
                if (connector.GetPlayerRelationToOwner() == VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies) continue;

                if (connected && connector.Connected)
                {
                    connector.TryDisconnect();
                    HudMessage = MySpaceTexts.NotificationConnectorsDisabled;
                }
                if (!connected)
                {
                    connector.TryConnect();

                    if (connector.InConstraint)
                        HudMessage = MySpaceTexts.NotificationConnectorsEnabled;
                    else
                        HudMessage = MyStringId.NullOrEmpty;
                }
            }
        }

        [ThreadStatic]
        private static long m_playerIdForAccessiblePredicate;
        private static void SetTraversalPlayerId(long playerId)
        {
            m_playerIdForAccessiblePredicate = playerId;
        }

        [ThreadStatic]
        private static MyDefinitionId m_inventoryItemDefinitionId;
        private static void SetTraversalInventoryItemDefinitionId(MyDefinitionId item = new MyDefinitionId())
        {
            m_inventoryItemDefinitionId = item;
        }

        [ThreadStatic]
        private static IMyConveyorEndpoint m_startingEndpoint;

        private static Predicate<IMyConveyorEndpoint> IsAccessAllowedPredicate = IsAccessAllowed;
        private static bool IsAccessAllowed(IMyConveyorEndpoint endpoint)
        {
            var relation = endpoint.CubeBlock.GetUserRelationToOwner(m_playerIdForAccessiblePredicate);
            var isEnemy = relation == VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies;
            if (isEnemy)
            {
                return false;
            }

            var conveyorSorter = endpoint.CubeBlock as MyConveyorSorter;

            if (conveyorSorter != null)
            {
                if (m_inventoryItemDefinitionId != new MyDefinitionId())
                    return conveyorSorter.IsAllowed(m_inventoryItemDefinitionId);
            }

            return true;
        }

        private static Predicate<IMyPathEdge<IMyConveyorEndpoint>> IsConveyorLargePredicate = IsConveyorLarge;
        private static bool IsConveyorLarge(IMyPathEdge<IMyConveyorEndpoint> conveyorLine)
        {
            return !(conveyorLine is MyConveyorLine) || (conveyorLine as MyConveyorLine).Type == MyObjectBuilder_ConveyorLine.LineType.LARGE_LINE;
        }

        private static Predicate<IMyPathEdge<IMyConveyorEndpoint>> IsConveyorSmallPredicate = IsConveyorSmall;
        private static bool IsConveyorSmall(IMyPathEdge<IMyConveyorEndpoint> conveyorLine)
        {
            return !(conveyorLine is MyConveyorLine) || (conveyorLine as MyConveyorLine).Type == MyObjectBuilder_ConveyorLine.LineType.SMALL_LINE;
        }

        private static bool NeedsLargeTube(MyDefinitionId itemDefinitionId)
        {
            MyPhysicalItemDefinition itemDef = MyDefinitionManager.Static.GetPhysicalItemDefinition(itemDefinitionId);

            if (itemDef == null)
                return true;

            // A bit hacky but in this case better than adding something to the definitions
            if (itemDefinitionId.TypeId == typeof(MyObjectBuilder_PhysicalGunObject))
                return false;

            return itemDef.Size.AbsMax() > 0.25f;
        }

        public static void AppendReachableEndpoints(IMyConveyorEndpoint source, long playerId, List<IMyConveyorEndpoint> reachable, MyPhysicalInventoryItem item, Predicate<IMyConveyorEndpoint> endpointFilter = null)
        {
            IMyConveyorEndpointBlock block = source.CubeBlock as IMyConveyorEndpointBlock;
            Debug.Assert(block != null);
            if (block == null)
                return;

            lock (Pathfinding)
            {
                SetTraversalPlayerId(playerId);
                var itemId = item.Content.GetId();
                SetTraversalInventoryItemDefinitionId(itemId);

                Pathfinding.FindReachable(block.ConveyorEndpoint, reachable, endpointFilter, IsAccessAllowedPredicate, NeedsLargeTube(itemId) ? IsConveyorLargePredicate : null);
            }
        }

        public HashSetReader<MyCubeBlock> InventoryBlocks
        {
            get
            {
                return new HashSetReader<MyCubeBlock>(m_inventoryBlocks);
            }
        }

        // Wrapper for various sets of inventory items for the pull requests
        private class PullRequestItemSet
        {
            private bool m_all;
            private MyObjectBuilderType? m_obType;
            private MyInventoryConstraint m_constraint;

            public void Clear()
            {
                m_all = false;
                m_obType = null;
                m_constraint = null;
            }

            public void Set(bool all)
            {
                Clear();
                m_all = all;
            }

            public void Set(MyObjectBuilderType? itemTypeId)
            {
                Clear();
                m_obType = itemTypeId;
            }

            public void Set(MyInventoryConstraint inventoryConstraint)
            {
                Clear();
                m_constraint = inventoryConstraint;
            }

            public bool Contains(MyDefinitionId itemId)
            {
                if (m_all)
                    return true;

                if (m_obType.HasValue && m_obType.Value == itemId.TypeId)
                    return true;

                if (m_constraint != null && m_constraint.Check(itemId))
                    return true;

                return false;
            }
        }

        public static bool PullAllRequest(IMyConveyorEndpointBlock start, MyInventory destinationInventory, long playerId, MyInventoryConstraint requestedTypeIds)
        {
            MyCubeBlock startingBlock = start as MyCubeBlock;
            if (startingBlock == null) return false;

            m_tmpRequestedItemSet.Set(requestedTypeIds);

            // Try and get the block from the cache
            MyGridConveyorSystem conveyorSystem = startingBlock.CubeGrid.GridSystems.ConveyorSystem;
            MyGridConveyorSystem.ConveyorEndpointMapping endpoints = conveyorSystem.GetConveyorEndpointMapping(start);
            if (endpoints.pullElements != null)
            {
                bool didTransfer = false;

                // Iterate to the other elements, see if we can collect some amount of items to pull
                for (int i = 0; i < endpoints.pullElements.Count; i++)
                {
                    MyCubeBlock sourceBlock = endpoints.pullElements[i] as MyCubeBlock;
                    if (sourceBlock == null) continue;

                    int inventoryCount = sourceBlock.InventoryCount;
                    for (int inventoryIndex = 0; inventoryIndex < inventoryCount; inventoryIndex++)
                    {
                        MyInventory inventory = sourceBlock.GetInventory(inventoryIndex);
                        if ((inventory.GetFlags() & MyInventoryFlags.CanSend) == 0)
                            continue;

                        if (inventory == destinationInventory)
                            continue;

                        m_tmpInventoryItems.Clear();
                        foreach (var item in inventory.GetItems())
                        {
                            m_tmpInventoryItems.Add(item);
                        }

                        foreach (var item in m_tmpInventoryItems)
                        {
                            if (destinationInventory.VolumeFillFactor >= 1.0f)
                            {
                                m_tmpInventoryItems.Clear();
                                return true;
                            }

                            var itemId = item.Content.GetId();

                            if (requestedTypeIds != null && !m_tmpRequestedItemSet.Contains(itemId))
                                continue;

                            // Verify that this item can, in fact, make it past sorters, etc
                            if (!CanTransfer(start, endpoints.pullElements[i], itemId, false))
                                continue;

                            var transferedAmount = item.Amount;

                            var oxygenBottle = item.Content as Sandbox.Common.ObjectBuilders.Definitions.MyObjectBuilder_GasContainerObject;
                            if (oxygenBottle != null && oxygenBottle.GasLevel >= 1f)
                                continue;

                            if (!MySession.Static.CreativeMode)
                            {
                                var fittingAmount = destinationInventory.ComputeAmountThatFits(item.Content.GetId());
                                if (item.Content.TypeId != typeof(MyObjectBuilder_Ore) &&
                                    item.Content.TypeId != typeof(MyObjectBuilder_Ingot))
                                {
                                    fittingAmount = MyFixedPoint.Floor(fittingAmount);
                                }
                                transferedAmount = MyFixedPoint.Min(fittingAmount, transferedAmount);
                            }
                            if (transferedAmount == 0)
                                continue;

                            didTransfer = true;
                            MyInventory.Transfer(inventory, destinationInventory, item.Content.GetId(), MyItemFlags.None, transferedAmount);

                            if (destinationInventory.CargoPercentage >= 0.99f)
                                break;
                        }

                        if (destinationInventory.CargoPercentage >= 0.99f)
                            break;
                    }

                    if (destinationInventory.CargoPercentage >= 0.99f)
                        break;
                }

                return didTransfer;
            }

            else
            {
                // Cache may need to be recomputed
                if (!conveyorSystem.m_isRecomputingGraph)
                    conveyorSystem.RecomputeConveyorEndpoints();
            }

            return false;
        }

        public static bool PullAllRequest(IMyConveyorEndpointBlock start, MyInventory destinationInventory, long playerId, MyObjectBuilderType? typeId = null)
        {
            SetTraversalPlayerId(playerId);
            m_tmpRequestedItemSet.Set(typeId);
            bool ret = ItemPullAll(start, destinationInventory);
            m_tmpRequestedItemSet.Clear();
            return ret;
        }

        public static bool PullAllRequest(IMyConveyorEndpointBlock start, MyInventory destinationInventory, long playerId, bool all)
        {
            SetTraversalPlayerId(playerId);
            m_tmpRequestedItemSet.Set(all);
            bool ret = ItemPullAll(start, destinationInventory);
            m_tmpRequestedItemSet.Clear();
            return ret;
        }

        private static bool ItemPullAll(IMyConveyorEndpointBlock start, MyInventory destinationInventory)
        {
            MyCubeBlock startingBlock = start as MyCubeBlock;
            if (startingBlock == null) return false;

            bool itemsPulled = false;

            // Try and get the block from the cache
            MyGridConveyorSystem conveyorSystem = startingBlock.CubeGrid.GridSystems.ConveyorSystem;
            MyGridConveyorSystem.ConveyorEndpointMapping endpoints = conveyorSystem.GetConveyorEndpointMapping(start);
            if (endpoints.pullElements != null)
            {
                // Iterate to the other elements, see if we can collect some amount of items to pull
                for (int i = 0; i < endpoints.pullElements.Count; i++)
                {
                    MyCubeBlock sourceBlock = endpoints.pullElements[i] as MyCubeBlock;
                    if (sourceBlock == null) continue;

                    int inventoryCount = sourceBlock.InventoryCount;
                    for (int inventoryIndex = 0; inventoryIndex < inventoryCount; inventoryIndex++)
                    {
                        MyInventory inventory = sourceBlock.GetInventory(inventoryIndex);
                        if ((inventory.GetFlags() & MyInventoryFlags.CanSend) == 0)
                            continue;

                        if (inventory == destinationInventory)
                            continue;

                        var items = inventory.GetItems().ToArray();
                        for (int itemIndex = 0; itemIndex < items.Length; itemIndex++)
                        {
                            var item = items[itemIndex];
                            var itemId = item.GetDefinitionId();

                            var amountThatFits = destinationInventory.ComputeAmountThatFits(itemId);
                            if (amountThatFits <= 0)
                                continue;

                            // Verify that this item can, in fact, make it past sorters, etc
                            if (!CanTransfer(start, endpoints.pullElements[i], itemId, false))
                                continue;

                            var availableAmount = inventory.GetItemAmount(itemId);
                            var transferAmount = MyFixedPoint.Min(availableAmount, amountThatFits);

                            MyInventory.Transfer(inventory, destinationInventory, itemId, MyItemFlags.None, transferAmount);
                            itemsPulled = true;

                            if (destinationInventory.CargoPercentage >= 0.99f)
                                break;
                        }

                        if (destinationInventory.CargoPercentage >= 0.99f)
                            break;
                    }

                    if (destinationInventory.CargoPercentage >= 0.99f)
                        break;
                }
            }

            else
            {
                // Cache may need to be recomputed
                if (!conveyorSystem.m_isRecomputingGraph)
                    conveyorSystem.RecomputeConveyorEndpoints();
            }
            return itemsPulled;
        }

        public static void PrepareTraversal(
            IMyConveyorEndpoint startingVertex,
            Predicate<IMyConveyorEndpoint> vertexFilter = null,
            Predicate<IMyConveyorEndpoint> vertexTraversable = null,
            Predicate<IMyPathEdge<IMyConveyorEndpoint>> edgeTraversable = null)
        {
            m_startingEndpoint = startingVertex;
            lock (Pathfinding)
            {
                Pathfinding.PrepareTraversal(startingVertex, vertexFilter, vertexTraversable, edgeTraversable);
            }
        }

        private class TransferData : ParallelTasks.WorkData
        {
            public IMyConveyorEndpointBlock m_start = null;
            public IMyConveyorEndpointBlock m_endPoint = null;
            public MyDefinitionId m_itemId;
            public bool m_isPush = false;

            public bool m_canTransfer = false;

            public TransferData(IMyConveyorEndpointBlock start, IMyConveyorEndpointBlock endPoint, MyDefinitionId itemId, bool isPush)
            {
                m_start = start;
                m_endPoint = endPoint;
                m_itemId = itemId;
                m_isPush = isPush;
            }

            public void ComputeTransfer()
            {
                List<IMyConveyorEndpoint> reachable = new List<IMyConveyorEndpoint>();

                lock (Pathfinding)
                {
                    SetTraversalPlayerId(m_start.ConveyorEndpoint.CubeBlock.OwnerId);
                    SetTraversalInventoryItemDefinitionId(m_itemId);

                    if (m_isPush)
                    {
                        Pathfinding.FindReachable(m_start.ConveyorEndpoint, reachable, b => b != null && b.CubeBlock == m_endPoint, IsAccessAllowedPredicate, NeedsLargeTube(m_itemId) ? IsConveyorLargePredicate : null);
                    }
                    else
                    {
                        Pathfinding.FindReachable(m_endPoint.ConveyorEndpoint, reachable, b => b != null && b.CubeBlock == m_start, IsAccessAllowedPredicate, NeedsLargeTube(m_itemId) ? IsConveyorLargePredicate : null);
                    }
                }

                m_canTransfer = (reachable.Count != 0);
            }

            public void StoreTransferState()
            {
                MyGridConveyorSystem conveyorSystem = (m_start as MyCubeBlock).CubeGrid.GridSystems.ConveyorSystem;
                MyGridConveyorSystem.ConveyorEndpointMapping endpoints = conveyorSystem.GetConveyorEndpointMapping(m_start);

                endpoints.AddTransfer(m_endPoint, m_itemId, m_isPush, m_canTransfer);
            }
        }

        private static bool CanTransfer(IMyConveyorEndpointBlock start, IMyConveyorEndpointBlock endPoint, MyDefinitionId itemId, bool isPush)
        {
            MyGridConveyorSystem conveyorSystem = (start as MyCubeBlock).CubeGrid.GridSystems.ConveyorSystem;
            MyGridConveyorSystem.ConveyorEndpointMapping endpoints = conveyorSystem.GetConveyorEndpointMapping(start);

            // Verify that this item can, in fact, make it past sorters, etc
            bool canTransfer = true;
            if (endpoints.TryGetTransfer(endPoint, itemId, isPush, out canTransfer))
            {
                return canTransfer;
            }
            else
            {
                Tuple<IMyConveyorEndpointBlock, IMyConveyorEndpointBlock> tuple = new Tuple<IMyConveyorEndpointBlock, IMyConveyorEndpointBlock>(start, endPoint);
                lock (m_currentTransferComputationTasks)
                {
                    if (!m_currentTransferComputationTasks.ContainsKey(tuple))
                    {
                        TransferData transferData = new TransferData(start, endPoint, itemId, isPush);
                        ParallelTasks.Task task = ParallelTasks.Parallel.Start(ComputeTransferData, OnTransferDataComputed, transferData);
                        m_currentTransferComputationTasks.Add(tuple, task);
                    }
                }
                return false;
            }
        }

        private static void ComputeTransferData(ParallelTasks.WorkData workData)
        {
            TransferData transferData = workData as TransferData;
            if (transferData == null)
            {
                workData.FlagAsFailed();
                return;
            }

            transferData.ComputeTransfer();
        }

        private static void OnTransferDataComputed(ParallelTasks.WorkData workData)
        {
            TransferData transferData = workData as TransferData;
            if (transferData == null)
            {
                workData.FlagAsFailed();
                return;
            }

            transferData.StoreTransferState();

            Tuple<IMyConveyorEndpointBlock, IMyConveyorEndpointBlock> tuple = new Tuple<IMyConveyorEndpointBlock, IMyConveyorEndpointBlock>(transferData.m_start, transferData.m_endPoint);
            lock (m_currentTransferComputationTasks)
            {
                m_currentTransferComputationTasks.Remove(tuple);
            }
        }

        public static MyFixedPoint ItemPullRequest(IMyConveyorEndpointBlock start, MyInventory destinationInventory, long playerId, MyDefinitionId itemId, MyFixedPoint? amount = null, bool remove = false)
        {
            MyCubeBlock startingBlock = start as MyCubeBlock;
            if (startingBlock == null) return 0;

            MyFixedPoint transferred = 0;

            // Try and get the block from the cache
            MyGridConveyorSystem conveyorSystem = startingBlock.CubeGrid.GridSystems.ConveyorSystem;
            MyGridConveyorSystem.ConveyorEndpointMapping endpoints = conveyorSystem.GetConveyorEndpointMapping(start);
            if (endpoints.pullElements != null)
            {
                // Iterate to the other elements, see if we can collect some amount of items to pull
                for (int i = 0; i < endpoints.pullElements.Count; i++)
                {
                    MyCubeBlock sourceBlock = endpoints.pullElements[i] as MyCubeBlock;
                    if (sourceBlock == null) continue;

                    int inventoryCount = sourceBlock.InventoryCount;
                    for (int inventoryIndex = 0; inventoryIndex < inventoryCount; inventoryIndex++)
                    {
                        MyInventory inventory = sourceBlock.GetInventory(inventoryIndex);
                        if ((inventory.GetFlags() & MyInventoryFlags.CanSend) == 0)
                            continue;

                        if (inventory == destinationInventory)
                            continue;

                        // Verify that this item can, in fact, make it past sorters, etc
                        if (!CanTransfer(start, endpoints.pullElements[i], itemId, false))
                            continue;

                        var availableAmount = inventory.GetItemAmount(itemId);
                        if (amount.HasValue)
                        {
                            availableAmount = amount.HasValue ? MyFixedPoint.Min(availableAmount, amount.Value) : availableAmount;
                            if (availableAmount == 0)
                                continue;

                            if (remove)
                            {
                                transferred += inventory.RemoveItemsOfType(availableAmount, itemId);
                            }
                            else
                            {
                                transferred += MyInventory.Transfer(inventory, destinationInventory, itemId, MyItemFlags.None, availableAmount);
                            }

                            amount -= availableAmount;
                            if (amount.Value == 0)
                                return transferred;
                        }
                        else
                        {
                            if (remove)
                            {
                                transferred += inventory.RemoveItemsOfType(availableAmount, itemId);
                            }
                            else
                            {
                                transferred += MyInventory.Transfer(inventory, destinationInventory, itemId, MyItemFlags.None, availableAmount);
                            }
                        }

                        if (destinationInventory.CargoPercentage >= 0.99f)
                            break;
                    }

                    if (destinationInventory.CargoPercentage >= 0.99f)
                        break;
                }
            }

            else
            {
                // Cache may need to be recomputed
                if (!conveyorSystem.m_isRecomputingGraph)
                    conveyorSystem.RecomputeConveyorEndpoints();
            }
            return transferred;
        }

        public static MyFixedPoint ConveyorSystemItemAmount(IMyConveyorEndpointBlock start, MyInventory destinationInventory, long playerId, MyDefinitionId itemId)
        {
            MyFixedPoint amount = 0;
            using (var invertedConductivity = new MyConveyorLine.InvertedConductivity())
            {
                lock (Pathfinding)
                {
                    SetTraversalPlayerId(playerId);
                    SetTraversalInventoryItemDefinitionId(itemId);

                    PrepareTraversal(start.ConveyorEndpoint, null, IsAccessAllowedPredicate, NeedsLargeTube(itemId) ? IsConveyorLargePredicate : null);
                    foreach (var conveyorEndpoint in MyGridConveyorSystem.Pathfinding)
                    {
                        MyCubeBlock owner = (conveyorEndpoint.CubeBlock != null && conveyorEndpoint.CubeBlock.HasInventory) ? conveyorEndpoint.CubeBlock : null;
                        if (owner == null) continue;

                        for (int i = 0; i < owner.InventoryCount; ++i)
                        {
                            var inventory = owner.GetInventory(i) as MyInventory;
                            System.Diagnostics.Debug.Assert(inventory != null, "Null or other inventory type!");

                            if ((inventory.GetFlags() & MyInventoryFlags.CanSend) == 0)
                                continue;

                            if (inventory == destinationInventory)
                                continue;

                            amount += inventory.GetItemAmount(itemId);
                        }
                    }
                }
            }
            return amount;
        }

        public static void PushAnyRequest(IMyConveyorEndpointBlock start, MyInventory srcInventory, long playerId)
        {
            if (srcInventory.Empty())
                return;

            var itemArray = srcInventory.GetItems().ToArray();
            foreach (var item in itemArray)
            {
                ItemPushRequest(start, srcInventory, playerId, item);
            }
        }

        public static bool ItemPushRequest(IMyConveyorEndpointBlock start, MyInventory srcInventory, long playerId, MyPhysicalInventoryItem toSend, MyFixedPoint? amount = null)
        {
            MyCubeBlock startingBlock = start as MyCubeBlock;
            if (startingBlock == null) return false;

            bool success = false;

            // Try and get the block from the cache
            MyGridConveyorSystem conveyorSystem = startingBlock.CubeGrid.GridSystems.ConveyorSystem;
            MyGridConveyorSystem.ConveyorEndpointMapping endpoints = conveyorSystem.GetConveyorEndpointMapping(start);
            if (endpoints.pushElements != null)
            {
                var toSendContentId = toSend.Content.GetId();

                MyFixedPoint remainingAmount = toSend.Amount;
                if (amount.HasValue)
                {
                    remainingAmount = amount.Value;
                }

                for (int i = 0; i<endpoints.pushElements.Count; i++)
                {
                    MyCubeBlock targetBlock = endpoints.pushElements[i] as MyCubeBlock;
                    if (targetBlock == null) continue;

                    int inventoryCount = targetBlock.InventoryCount;
                    for (int inventoryIndex = 0; inventoryIndex < inventoryCount; inventoryIndex++)
                    {
                        MyInventory inventory = targetBlock.GetInventory(inventoryIndex);

                        if ((inventory.GetFlags() & MyInventoryFlags.CanReceive) == 0)
                            continue;

                        if (inventory == srcInventory)
                            continue;

                        var fittingAmount = inventory.ComputeAmountThatFits(toSendContentId);
                        fittingAmount = MyFixedPoint.Min(fittingAmount, remainingAmount);
                        if (!inventory.CheckConstraint(toSendContentId))
                            continue;
                        if (fittingAmount == 0)
                            continue;

                        // Verify that this item can, in fact, make it past sorters, etc
                        if (!CanTransfer(start, endpoints.pushElements[i], toSend.GetDefinitionId(), true))
                            continue;

                        MyInventory.Transfer(srcInventory, inventory, toSend.ItemId, -1, fittingAmount);
                        success = true;

                        remainingAmount -= fittingAmount;
                    }

                    if (remainingAmount <= 0)
                        break;
                }
            }

            else
            {
                // Cache may need to be recomputed
                if (!conveyorSystem.m_isRecomputingGraph)
                    conveyorSystem.RecomputeConveyorEndpoints();
            }

            return success;
        }


        public class ConveyorEndpointMapping
        {
            public List<IMyConveyorEndpointBlock> pullElements = null;
            public List<IMyConveyorEndpointBlock> pushElements = null;

            public Dictionary<Tuple<IMyConveyorEndpointBlock, MyDefinitionId, bool>, bool> testedTransfers = new Dictionary<Tuple<IMyConveyorEndpointBlock, MyDefinitionId, bool>, bool>();

            public void AddTransfer(IMyConveyorEndpointBlock block, MyDefinitionId itemId, bool isPush, bool canTransfer)
            {
                    var tuple = new Tuple<IMyConveyorEndpointBlock, MyDefinitionId, bool>(block, itemId, isPush);
                    testedTransfers[tuple] = canTransfer;
            }

            public bool TryGetTransfer(IMyConveyorEndpointBlock block, MyDefinitionId itemId, bool isPush, out bool canTransfer)
            {
                var tuple = new Tuple<IMyConveyorEndpointBlock, MyDefinitionId, bool>(block, itemId, isPush);
                return testedTransfers.TryGetValue(tuple, out canTransfer);
            }
        }

        private Dictionary<IMyConveyorEndpointBlock, ConveyorEndpointMapping> m_conveyorConnections = new Dictionary<IMyConveyorEndpointBlock, ConveyorEndpointMapping>();
        private bool m_isRecomputingGraph = false;
        private bool m_isRecomputationInterrupted = false;
        private bool m_isRecomputationIsAborted = false;
        private const double MAX_RECOMPUTE_DURATION_MILLISECONDS = 10;

        private Dictionary<IMyConveyorEndpointBlock, ConveyorEndpointMapping> m_conveyorConnectionsForThread = new Dictionary<IMyConveyorEndpointBlock, ConveyorEndpointMapping>();
        private IEnumerator<IMyConveyorEndpointBlock> m_endpointIterator = null;

        /// <summary>
        /// Starts the conveyor endpoint mapping recomputation, aborts the current process if needed.
        /// </summary>
        void RecomputeConveyorEndpoints()
        {
            // Clear out the data completely, then recompute.
            // If computation is already running, abort it, otherwise start
            m_conveyorConnections.Clear();

            if (m_isRecomputingGraph)
            {
                m_isRecomputationIsAborted = true;
                return;
            }

            StartRecomputationThread();
        }

        /// <summary>
        /// Starts the conveyor endpoint mapping recomputation.
        /// </summary>
        void StartRecomputationThread()
        {
            // Clear out data, reset thread state, begin iteration
            m_conveyorConnectionsForThread.Clear();

            m_isRecomputingGraph = true;
            m_isRecomputationIsAborted = false;
            m_isRecomputationInterrupted = false;

            m_endpointIterator = null;
            ParallelTasks.Parallel.Start(UpdateConveyorEndpointMapping, OnConveyorEndpointMappingUpdateCompleted);
        }

        public static void RecomputeMappingForBlock(IMyConveyorEndpointBlock processedBlock)
        {
            MyCubeBlock block = processedBlock as MyCubeBlock;
            if (block == null
                || block.CubeGrid == null
                || block.CubeGrid.GridSystems == null
                || block.CubeGrid.GridSystems.ConveyorSystem == null) return;

            ConveyorEndpointMapping endpointMap = block.CubeGrid.GridSystems.ConveyorSystem.ComputeMappingForBlock(processedBlock);

            // Update current mapping
            if (block.CubeGrid.GridSystems.ConveyorSystem.m_conveyorConnections.ContainsKey(processedBlock))
            {
                block.CubeGrid.GridSystems.ConveyorSystem.m_conveyorConnections[processedBlock] = endpointMap;
            }
            else
            {
                block.CubeGrid.GridSystems.ConveyorSystem.m_conveyorConnections.Add(processedBlock, endpointMap);
            }

            // Update future mapping if recomputation is in progress
            if (block.CubeGrid.GridSystems.ConveyorSystem.m_isRecomputingGraph)
            {
                if (block.CubeGrid.GridSystems.ConveyorSystem.m_conveyorConnectionsForThread.ContainsKey(processedBlock))
                {
                    block.CubeGrid.GridSystems.ConveyorSystem.m_conveyorConnectionsForThread[processedBlock] = endpointMap;
                }
                else
                {
                    block.CubeGrid.GridSystems.ConveyorSystem.m_conveyorConnectionsForThread.Add(processedBlock, endpointMap);
                }
            }
        }

        private ConveyorEndpointMapping ComputeMappingForBlock(IMyConveyorEndpointBlock processedBlock)
        {
            ConveyorEndpointMapping endpointMap = new ConveyorEndpointMapping();

            // Process pull mapping
            PullInformation pullInformation = processedBlock.GetPullInformation();
            if (pullInformation != null)
            {
                endpointMap.pullElements = new List<IMyConveyorEndpointBlock>();

                lock (Pathfinding)
                {
                    SetTraversalPlayerId(pullInformation.OwnerID);

                    // Pulling one specific item?
                    if (pullInformation.ItemDefinition != default(MyDefinitionId))
                    {
                        SetTraversalInventoryItemDefinitionId(pullInformation.ItemDefinition);

                        using (var invertedConductivity = new MyConveyorLine.InvertedConductivity())
                        {
                            PrepareTraversal(processedBlock.ConveyorEndpoint, null, IsAccessAllowedPredicate, NeedsLargeTube(pullInformation.ItemDefinition) ? IsConveyorLargePredicate : null);
                            AddReachableEndpoints(processedBlock, endpointMap.pullElements, MyInventoryFlags.CanSend);
                        }
                    }

                    else if (pullInformation.Constraint != null)
                    {
                        SetTraversalInventoryItemDefinitionId();
                        using (var invertedConductivity = new MyConveyorLine.InvertedConductivity())
                        {
                            // Once for small tubes
                            PrepareTraversal(processedBlock.ConveyorEndpoint, null, IsAccessAllowedPredicate, IsConveyorSmallPredicate);
                            AddReachableEndpoints(processedBlock, endpointMap.pullElements, MyInventoryFlags.CanSend);

                            // Once for large tubes
                            PrepareTraversal(processedBlock.ConveyorEndpoint, null, IsAccessAllowedPredicate, null);
                            AddReachableEndpoints(processedBlock, endpointMap.pullElements, MyInventoryFlags.CanSend);
                        }
                    }
                }
            }

            // Process push mapping
            PullInformation pushInformation = processedBlock.GetPushInformation();
            if (pushInformation != null)
            {
                endpointMap.pushElements = new List<IMyConveyorEndpointBlock>();

                lock (Pathfinding)
                {
                    SetTraversalPlayerId(pushInformation.OwnerID);

                    HashSet<MyDefinitionId> definitions = new HashSet<MyDefinitionId>();
                    if (pushInformation.ItemDefinition != default(MyDefinitionId))
                    {
                        definitions.Add(pushInformation.ItemDefinition);
                    }

                    if (pushInformation.Constraint != null)
                    {
                        foreach (MyDefinitionId definition in pushInformation.Constraint.ConstrainedIds)
                            definitions.Add(definition);

                        foreach (MyObjectBuilderType constrainedType in pushInformation.Constraint.ConstrainedTypes)
                        {
                            MyDefinitionManager.Static.TryGetDefinitionsByTypeId(constrainedType, definitions);
                        }
                    }

                    // Empty constraint, no need to check, anything that can take items is okay, push requests will re-test anyway
                    if (definitions.Count == 0 && (pushInformation.Constraint == null || pushInformation.Constraint.Description == "Empty constraint"))
                    {
                        SetTraversalInventoryItemDefinitionId();
                        PrepareTraversal(processedBlock.ConveyorEndpoint, null, IsAccessAllowedPredicate);
                        AddReachableEndpoints(processedBlock, endpointMap.pushElements, MyInventoryFlags.CanReceive);
                    }
                    else
                    {
                        // Iterate through all the constrained item definitions
                        foreach (MyDefinitionId definitionId in definitions)
                        {
                            SetTraversalInventoryItemDefinitionId(definitionId);

                            if (NeedsLargeTube(definitionId))
                                PrepareTraversal(processedBlock.ConveyorEndpoint, null, IsAccessAllowedPredicate, IsConveyorLargePredicate);
                            else
                                PrepareTraversal(processedBlock.ConveyorEndpoint, null, IsAccessAllowedPredicate);

                            AddReachableEndpoints(processedBlock, endpointMap.pushElements, MyInventoryFlags.CanReceive, definitionId);
                        }
                    }
                }
            }

            return endpointMap;
        }

        private static void AddReachableEndpoints(IMyConveyorEndpointBlock processedBlock, List<IMyConveyorEndpointBlock> resultList, MyInventoryFlags flagToCheck, MyDefinitionId? definitionId = null)
        {
            foreach (var conveyorEndpoint in MyGridConveyorSystem.Pathfinding)
            {
                // Ignore originating block
                if (conveyorEndpoint.CubeBlock == processedBlock as MyCubeBlock) continue;

                // Ignore endpoints without a block
                if (conveyorEndpoint.CubeBlock == null) continue;

                // Ignore blocks without inventory
                if (!conveyorEndpoint.CubeBlock.HasInventory) continue;

                // Ignore blocks that do not implement IMyConveyorEndpointBlock interface
                IMyConveyorEndpointBlock endpointBlock = conveyorEndpoint.CubeBlock as IMyConveyorEndpointBlock;
                if (endpointBlock == null) continue;

                MyCubeBlock owner = conveyorEndpoint.CubeBlock;

                // Iterate inventories to make sure they can take the items
                bool isInventoryAvailable = false;
                for (int i = 0; i < owner.InventoryCount; ++i)
                {
                    var inventory = owner.GetInventory(i) as MyInventory;
                    System.Diagnostics.Debug.Assert(inventory != null, "Null or other inventory type!");

                    if ((inventory.GetFlags() & flagToCheck) == 0)
                        continue;

                    // Make sure target inventory can take this item
                    if (definitionId.HasValue && !inventory.CheckConstraint(definitionId.Value))
                        continue;

                    isInventoryAvailable = true;
                    break;
                }

                if (isInventoryAvailable && !resultList.Contains(endpointBlock))
                    resultList.Add(endpointBlock);
            }
        }

        /// <summary>
        /// Computes the conveyor endpoint mappings.
        /// The conveyor endpoint blocks come from m_conveyorEndpointBlocks, and are processed iteratively.
        /// It does not process all of them at once, it processes them until the task has been running longer than MAX_RECOMPUTE_DURATION_MILLISECONDS
        /// If it exceeds this time, it will exit and restart itself the next frame.
        /// 
        /// The task can also be aborted, when it is aborted, it will throw away all intermediate data and restart.
        /// 
        /// Accessing the grid conveyor system will be slow until this has finished computing, after which a large performance gain should be had.
        /// </summary>
        void UpdateConveyorEndpointMapping()
        {
            long startTime = Stopwatch.GetTimestamp();

            m_isRecomputationInterrupted = false;

            if (m_endpointIterator == null)
            {
                m_endpointIterator = m_conveyorEndpointBlocks.GetEnumerator();
                m_endpointIterator.MoveNext();
            }

            IMyConveyorEndpointBlock processedBlock = m_endpointIterator.Current;
            while (processedBlock != null)
            {
                // Abort the task if requested
                if (m_isRecomputationIsAborted)
                {
                    m_isRecomputationInterrupted = true;
                    break;
                }

                // Stop iteration if the task has taken enough time for now
                TimeSpan threadDuration = new TimeSpan(Stopwatch.GetTimestamp() - startTime);
                if (threadDuration.TotalMilliseconds > MAX_RECOMPUTE_DURATION_MILLISECONDS)
                {
                    m_isRecomputationInterrupted = true;
                    break;
                }

                // Compute mapping
                ConveyorEndpointMapping endpointMap = ComputeMappingForBlock(processedBlock);
                m_conveyorConnectionsForThread.Add(processedBlock, endpointMap);

                // Next block, but check if iterator is still valid, can be invalidated when blocks are added/removed.
                // Thread will abort if this happens
                if (m_endpointIterator != null)
                {
                    m_endpointIterator.MoveNext();
                    processedBlock = m_endpointIterator.Current;
                }
                else
                {
                    m_isRecomputationIsAborted = true;
                    m_isRecomputationInterrupted = true;
                    break;
                }
            }
        }

        /// <summary>
        /// Called when the computation finishes. If it was aborted, restarts with a clean slate.
        /// Otherwise, adds the results of the computation to the main thread accessible connections map.
        /// Will continue the task if it was interrupted by timeout.
        /// </summary>
        void OnConveyorEndpointMappingUpdateCompleted()
        {
            // If the thread is flagged as aborted, restart it again
            // If the thread is flagged as interrupted, merge intermediate data, then continue where it left off
            // If the thread is finished correctly, merge intermediate data, we are finished

            if (m_isRecomputationIsAborted)
            {
                // Restart recomputation, don't store results
                StartRecomputationThread();
                return;
            }

            // Store results
            foreach (var connectionsMapping in m_conveyorConnectionsForThread)
            {
                if (m_conveyorConnections.ContainsKey(connectionsMapping.Key))
                {
                    m_conveyorConnections[connectionsMapping.Key] = connectionsMapping.Value;
                }
                else
                {
                    m_conveyorConnections.Add(connectionsMapping.Key, connectionsMapping.Value);
                }
            }
            m_conveyorConnectionsForThread.Clear();

            // Continue if we have to, otherwise flag as finished
            if (m_isRecomputationInterrupted)
            {
                ParallelTasks.Parallel.Start(UpdateConveyorEndpointMapping, OnConveyorEndpointMappingUpdateCompleted);
            }
            else
            {
                m_endpointIterator = null;
                m_isRecomputingGraph = false;
            }
        }

        public ConveyorEndpointMapping GetConveyorEndpointMapping(IMyConveyorEndpointBlock block)
        {
            if (m_conveyorConnections.ContainsKey(block))
                return m_conveyorConnections[block];
            return new ConveyorEndpointMapping();
        }

        public static void FindReachable(IMyConveyorEndpoint from, List<IMyConveyorEndpoint> reachableVertices, Predicate<IMyConveyorEndpoint> vertexFilter = null, Predicate<IMyConveyorEndpoint> vertexTraversable = null, Predicate<IMyPathEdge<IMyConveyorEndpoint>> edgeTraversable = null)
        {
            lock (Pathfinding)
            {
                Pathfinding.FindReachable(from, reachableVertices, vertexFilter, vertexTraversable, edgeTraversable);
            }
        }

        public static bool Reachable(IMyConveyorEndpoint from, IMyConveyorEndpoint to)
        {
            bool isReachable = false;
            lock (Pathfinding)
            {
                isReachable = Pathfinding.Reachable(from, to);
            }
            return isReachable;
        }
    }
}
