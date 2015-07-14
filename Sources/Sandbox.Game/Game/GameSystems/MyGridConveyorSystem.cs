using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Algorithms;
using VRage.Collections;
using VRage;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRage.ObjectBuilders;

namespace Sandbox.Game.GameSystems
{
    public class MyGridConveyorSystem : IMyPowerConsumer
    {
        private static readonly float CONVEYOR_SYSTEM_CONSUMPTION = 0.005f;

        readonly HashSet<IMyInventoryOwner> m_blocks = new HashSet<IMyInventoryOwner>();
        readonly HashSet<MyConveyorLine> m_lines = new HashSet<MyConveyorLine>();

        readonly HashSet<MyShipConnector> m_connectors = new HashSet<MyShipConnector>();

        public event Action<IMyInventoryOwner> BlockAdded;
        public event Action<IMyInventoryOwner> BlockRemoved;

        private MyCubeGrid m_grid;

        private static List<ConveyorLinePosition> m_tmpConveyorPositionList = new List<ConveyorLinePosition>(6);
        private static List<MyPhysicalInventoryItem> m_tmpInventoryItems = new List<MyPhysicalInventoryItem>();

        private static List<MyTuple<IMyConveyorEndpointBlock, MyPhysicalInventoryItem>> m_tmpPullRequests = new List<MyTuple<IMyConveyorEndpointBlock, MyPhysicalInventoryItem>>();
        private static PullRequestItemSet m_tmpRequestedItemSet = new PullRequestItemSet();

        private static MyPathFindingSystem<IMyConveyorEndpoint> m_pathfinding = new MyPathFindingSystem<IMyConveyorEndpoint>();
        public static MyPathFindingSystem<IMyConveyorEndpoint> Pathfinding
        {
            get
            {
                return m_pathfinding;
            }
        }

        private Dictionary<ConveyorLinePosition, MyConveyorLine> m_lineEndpoints;
        private Dictionary<Vector3I, MyConveyorLine> m_linePoints;
        private HashSet<MyConveyorLine> m_deserializedLines;

        public bool IsClosing = false;

        public MyPowerReceiver PowerReceiver
        {
            get;
            private set;
        }

        public MyGridConveyorSystem(MyCubeGrid grid)
        {
            m_grid = grid;

            m_lineEndpoints = null;
            m_linePoints = null;
            m_deserializedLines = null;

            PowerReceiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Conveyors,
                false,
                CONVEYOR_SYSTEM_CONSUMPTION,
                () => CalculateConsumption());

            PowerReceiver.Update();
            PowerReceiver.IsPoweredChanged += Receiver_IsPoweredChanged;
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

        public void Add(IMyInventoryOwner block)
        {
            bool added = m_blocks.Add(block);
            System.Diagnostics.Debug.Assert(added, "Double add");

            var handler = BlockAdded;
            if (handler != null) handler(block);
        }

        public void Remove(IMyInventoryOwner block)
        {
            bool removed = m_blocks.Remove(block);
            System.Diagnostics.Debug.Assert(removed, "Double remove or removing something not added");

            var handler = BlockRemoved;
            if (handler != null) handler(block);
        }

        public void AddConveyorBlock(IMyConveyorEndpointBlock endpointBlock)
        {
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
            foreach (var line in m_lines)
            {
                if (!line.IsEmpty)
                    line.Update();
            }
        }

        public void UpdateBeforeSimulation10()
        {
            PowerReceiver.Update();
        }

        void Receiver_IsPoweredChanged()
        {
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
            if (block is MyShipConnector)
                m_connectors.Remove(block as MyShipConnector);

            if (IsClosing) return;

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
                if (connector.GetPlayerRelationToOwner() == MyRelationsBetweenPlayerAndBlock.Enemies) continue;

                if (connected && connector.Connected)
                    connector.TryDisconnect();
                if (!connected)
                    connector.TryConnect();
            }
        }

        private static long m_playerIdForAccessiblePredicate;
        private static void SetTraversalPlayerId(long playerId)
        {
            m_playerIdForAccessiblePredicate = playerId;
        }

        private static MyDefinitionId m_inventoryItemDefinitionId;
        private static void SetTraversalInventoryItemDefinitionId(MyDefinitionId item = new MyDefinitionId())
        {
            m_inventoryItemDefinitionId = item;
        }

        private static IMyConveyorEndpoint m_startingEndpoint;

        private static Predicate<IMyConveyorEndpoint> IsAccessAllowedPredicate = IsAccessAllowed;
        private static bool IsAccessAllowed(IMyConveyorEndpoint endpoint)
        {
            var relation = endpoint.CubeBlock.GetUserRelationToOwner(m_playerIdForAccessiblePredicate);
            var isEnemy = relation == MyRelationsBetweenPlayerAndBlock.Enemies;
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

        private static bool NeedsLargeTube(MyDefinitionId itemDefinitionId)
        {
            MyPhysicalItemDefinition itemDef = MyDefinitionManager.Static.GetPhysicalItemDefinition(itemDefinitionId);
            return itemDef.Size.AbsMax() > 0.25f;
        }

        public static void AppendReachableEndpoints(IMyConveyorEndpoint source, long playerId, List<IMyConveyorEndpoint> reachable, MyPhysicalInventoryItem item, Predicate<IMyConveyorEndpoint> endpointFilter = null)
        {
            IMyConveyorEndpointBlock block = source.CubeBlock as IMyConveyorEndpointBlock;
            Debug.Assert(block != null);
            if (block == null)
                return;

            SetTraversalPlayerId(playerId);
            var itemId = item.Content.GetId();
            SetTraversalInventoryItemDefinitionId(itemId);

            m_pathfinding.FindReachable(block.ConveyorEndpoint, reachable, endpointFilter, IsAccessAllowedPredicate, NeedsLargeTube(itemId) ? IsConveyorLargePredicate : null);
        }

        public HashSetReader<IMyInventoryOwner> Blocks
        {
            get
            {
                return new HashSetReader<IMyInventoryOwner>(m_blocks);
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

        public static void PullAllRequest(IMyConveyorEndpointBlock start, MyInventory destinationInventory, long playerId, MyInventoryConstraint requestedTypeIds)
        {
            SetTraversalPlayerId(playerId);
            m_tmpRequestedItemSet.Set(requestedTypeIds);
            ItemPullAll(start, destinationInventory);
            m_tmpRequestedItemSet.Clear();
        }

        public static void PullAllRequest(IMyConveyorEndpointBlock start, MyInventory destinationInventory, long playerId, MyObjectBuilderType? typeId = null)
        {
            SetTraversalPlayerId(playerId);
            m_tmpRequestedItemSet.Set(typeId);
            ItemPullAll(start, destinationInventory);
            m_tmpRequestedItemSet.Clear();
        }

        public static void PullAllRequest(IMyConveyorEndpointBlock start, MyInventory destinationInventory, long playerId, bool all)
        {
            SetTraversalPlayerId(playerId);
            m_tmpRequestedItemSet.Set(all);
            ItemPullAll(start, destinationInventory);
            m_tmpRequestedItemSet.Clear();
        }

        private static void ItemPullAll(IMyConveyorEndpointBlock start, MyInventory destinationInventory)
        {
            // First, search through small conveyor tubes and request only small items
            PrepareTraversal(start.ConveyorEndpoint, null, IsAccessAllowedPredicate);
            ItemPullAllInternal(destinationInventory, m_tmpRequestedItemSet, true);
            // Then, search again through all tubes and request all items
            PrepareTraversal(start.ConveyorEndpoint, null, IsAccessAllowedPredicate, IsConveyorLargePredicate);
            ItemPullAllInternal(destinationInventory, m_tmpRequestedItemSet, false);
        }

        public static void PrepareTraversal(
            IMyConveyorEndpoint startingVertex,
            Predicate<IMyConveyorEndpoint> vertexFilter = null,
            Predicate<IMyConveyorEndpoint> vertexTraversable = null,
            Predicate<IMyPathEdge<IMyConveyorEndpoint>> edgeTraversable = null)
        {
            m_startingEndpoint = startingVertex;
            m_pathfinding.PrepareTraversal(startingVertex, vertexFilter, vertexTraversable, edgeTraversable);
        }

        private static void ItemPullAllInternal(MyInventory destinationInventory, PullRequestItemSet requestedTypeIds, bool onlySmall)
        {
            SetTraversalInventoryItemDefinitionId();
            Debug.Assert(m_tmpPullRequests.Count == 0, "m_tmpPullRequests is not empty!");
            using (var invertedConductivity = new MyConveyorLine.InvertedConductivity())
            {
                foreach (var conveyorEndpoint in MyGridConveyorSystem.Pathfinding)
                {
                    IMyInventoryOwner owner = conveyorEndpoint.CubeBlock as IMyInventoryOwner;
                    if (owner == null) continue;

                    for (int i = 0; i < owner.InventoryCount; ++i)
                    {
                        var inventory = owner.GetInventory(i);
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
                                return;
                            }

                            var itemId = item.Content.GetId();

                            if (requestedTypeIds != null && !requestedTypeIds.Contains(itemId))
                                continue;

                            if (onlySmall && NeedsLargeTube(itemId))
                                continue;

                            var transferedAmount = item.Amount;

                            var oxygenBottle = item.Content as Sandbox.Common.ObjectBuilders.Definitions.MyObjectBuilder_OxygenContainerObject;
                            if (oxygenBottle != null && oxygenBottle.OxygenLevel == 1f)
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

                            // SK: this is mental
                            m_tmpPullRequests.Add(new MyTuple<IMyConveyorEndpointBlock, MyPhysicalInventoryItem>(m_startingEndpoint.CubeBlock as IMyConveyorEndpointBlock, item));
                            //MyInventory.Transfer(inventory, destinationInventory, item.Content.GetId(), MyItemFlags.None, transferedAmount);
                        }
                    }
                }
            }

            foreach (var tuple in m_tmpPullRequests)
            {
                if (destinationInventory.VolumeFillFactor >= 1.0f)
                {
                    m_tmpPullRequests.Clear();
                    return;
                }

                var start = tuple.Item1;
                var item = tuple.Item2;

                var transferedAmount = item.Amount;
                var fittingAmount = destinationInventory.ComputeAmountThatFits(item.Content.GetId());
                if (item.Content.TypeId != typeof(MyObjectBuilder_Ore) &&
                    item.Content.TypeId != typeof(MyObjectBuilder_Ingot))
                {
                    fittingAmount = MyFixedPoint.Floor(fittingAmount);
                }
                transferedAmount = MyFixedPoint.Min(fittingAmount, transferedAmount);

                if (transferedAmount == 0)
                    continue;

                var itemId = item.Content.GetId();

                SetTraversalInventoryItemDefinitionId(itemId);
                ItemPullRequest(start, destinationInventory, m_playerIdForAccessiblePredicate, itemId, transferedAmount);
            }

            m_tmpPullRequests.Clear();
        }

        public static void ItemPullRequest(IMyConveyorEndpointBlock start, MyInventory destinationInventory, long playerId, MyDefinitionId itemId, MyFixedPoint? amount = null)
        {
            using (var invertedConductivity = new MyConveyorLine.InvertedConductivity())
            {
                if (amount.HasValue)
                    Debug.Assert(itemId.TypeId == typeof(MyObjectBuilder_Ore) ||
                                 itemId.TypeId == typeof(MyObjectBuilder_Ingot) ||
                                 MyFixedPoint.Floor(amount.Value) == amount.Value);

                SetTraversalPlayerId(playerId);
                SetTraversalInventoryItemDefinitionId(itemId);

                PrepareTraversal(start.ConveyorEndpoint, null, IsAccessAllowedPredicate, NeedsLargeTube(itemId) ? IsConveyorLargePredicate : null);
                foreach (var conveyorEndpoint in MyGridConveyorSystem.Pathfinding)
                {
                    IMyInventoryOwner owner = conveyorEndpoint.CubeBlock as IMyInventoryOwner;
                    if (owner == null) continue;

                    for (int i = 0; i < owner.InventoryCount; ++i)
                    {
                        var inventory = owner.GetInventory(i);
                        if ((inventory.GetFlags() & MyInventoryFlags.CanSend) == 0)
                            continue;

                        if (inventory == destinationInventory)
                            continue;

                        if (amount.HasValue)
                        {
                            var availableAmount = inventory.GetItemAmount(itemId);
                            availableAmount = amount.HasValue ? MyFixedPoint.Min(availableAmount, amount.Value) : availableAmount;
                            if (availableAmount == 0)
                                continue;

                            MyInventory.Transfer(inventory, destinationInventory, itemId, MyItemFlags.None, availableAmount);

                            amount -= availableAmount;
                            if (amount.Value == 0)
                                return;
                        }
                        else
                        {
                            MyInventory.Transfer(inventory, destinationInventory, itemId, MyItemFlags.None);
                        }
                    }
                }
            }
        }

        public static void PushAnyRequest(IMyConveyorEndpointBlock start, MyInventory srcInventory, long playerId)
        {
            if (srcInventory.Empty())
                return;

            // try all items and stop on first successfull
            foreach (var item in srcInventory.GetItems())
            {
                if (ItemPushRequest(start, srcInventory, playerId, item))
                    return;
            }
        }

        public static bool ItemPushRequest(IMyConveyorEndpointBlock start, MyInventory srcInventory, long playerId, MyPhysicalInventoryItem toSend, MyFixedPoint? amount = null)
        {
            var itemBuilder = toSend.Content;
            if (amount.HasValue)
                Debug.Assert(toSend.Content.TypeId == typeof(MyObjectBuilder_Ore) ||
                                toSend.Content.TypeId == typeof(MyObjectBuilder_Ingot) ||
                                MyFixedPoint.Floor(amount.Value) == amount.Value);

            MyFixedPoint remainingAmount = toSend.Amount;
            if (amount.HasValue)
            {
                remainingAmount = amount.Value;
            }

            SetTraversalPlayerId(playerId);

            var toSendContentId = toSend.Content.GetId();
            SetTraversalInventoryItemDefinitionId(toSendContentId);

            if (NeedsLargeTube(toSendContentId))
            {
                PrepareTraversal(start.ConveyorEndpoint, null, IsAccessAllowedPredicate, IsConveyorLargePredicate);
            }
            else
            {
                PrepareTraversal(start.ConveyorEndpoint, null, IsAccessAllowedPredicate);
            }

            bool success = false;

            foreach (var conveyorEndpoint in MyGridConveyorSystem.Pathfinding)
            {
                IMyInventoryOwner owner = conveyorEndpoint.CubeBlock as IMyInventoryOwner;
                if (owner == null) continue;

                for (int i = 0; i < owner.InventoryCount; ++i)
                {
                    var inventory = owner.GetInventory(i);
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

                    MyInventory.Transfer(srcInventory, inventory, toSend.ItemId, -1, fittingAmount);
                    success = true;
                }
            }
            return success;
        }
    }
}
